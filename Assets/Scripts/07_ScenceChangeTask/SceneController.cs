/**
 * =========================================================================================
 * | 文件名稱: SceneController.cs
 * | 功能描述: 
 * |   全局唯一的場景切換管理器,以單例模式存在。
 * |   負責處理所有遊戲場景的載入、卸載與轉場流程,並管理一個常駐的全局UI場景。
 * |
 * | 【v3.0 更新】
 * |   - ExecuteSceneReadyHandlers 不再硬編碼執行順序
 * |   - 所有準備工作的順序由各場景的 SceneReadyCoordinator 在 Inspector 中決定
 * |   - 保留原有的所有轉場 API(ChangeScene / ChangeSceneImmediate / DayTransition / SlotTransition)
 * |

 * =========================================================================================
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    // 轉場「開始前」觸發 (適合存檔、暫停音效)
    public static event Action OnBeforeSceneChange;

    // 場景載入後、ReadyHandlers 執行前觸發 (原有事件,保持向下相容)
    public static event Action OnSceneChanged;

    // 【v3 新增】場景載入 + ReadyHandlers 全部完成後觸發
    // 這才是「場景真正準備好」的時機,適合用於 Sequencer Command 等需要等待完整初始化的場合
    public static event Action OnSceneFullyReady;

    [SerializeField] private string globalUISceneName = "GlobalStatusUI";
    private string currentMainScene = string.Empty;
    private bool isTransitioning = false;

    // ============================================================
    // 單例與初始化
    // ============================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Debug.Log("[SceneController] 初始化完成");
        StartCoroutine(StartupRoutine());

        // 訂閱時間系統的隔天事件
        SubscribeToTimeSystem();
    }

    private void OnDestroy()
    {
        UnsubscribeFromTimeSystem();
    }

    // ============================================================
    // 訂閱時間系統事件
    // ============================================================

    private void SubscribeToTimeSystem()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        while (GameStatusService.Instance == null)
        {
            yield return null;
        }

        GameStatusService.Instance.Time.OnDaySkipRequested += HandleDaySkipRequested;
        Debug.Log("[SceneController] 已訂閱 OnDaySkipRequested 事件");
    }

    private void UnsubscribeFromTimeSystem()
    {
        if (GameStatusService.Instance?.Time != null)
        {
            GameStatusService.Instance.Time.OnDaySkipRequested -= HandleDaySkipRequested;
        }
    }

    private void HandleDaySkipRequested(Action onDaySkipComplete)
    {
        Debug.Log("[SceneController] 收到隔天演出請求");

        if (DayTransitionUI.IsAvailable)
        {
            DayTransitionUI.PerformTransition(
                onMidTransition: onDaySkipComplete,
                onComplete: null
            );
        }
        else
        {
            Debug.LogWarning("[SceneController] DayTransitionUI 不存在,使用簡易轉場");
            StartCoroutine(SimpleDayTransitionRoutine(onDaySkipComplete));
        }
    }

    private IEnumerator SimpleDayTransitionRoutine(Action onDaySkipComplete)
    {
        if (TransitionFader.Instance != null)
        {
            yield return TransitionFader.Instance.FadeToColor(Color.black, 0.3f);
            onDaySkipComplete?.Invoke();
            yield return new WaitForSecondsRealtime(1f);
            yield return TransitionFader.Instance.FadeToClear(0.3f);
        }
        else
        {
            onDaySkipComplete?.Invoke();
        }
    }

    // ============================================================
    // 確保 GlobalUI 場景載入
    // ============================================================

    private IEnumerator EnsureGlobalUISceneLoaded()
    {
        if (!string.IsNullOrEmpty(globalUISceneName) && !SceneManager.GetSceneByName(globalUISceneName).isLoaded)
        {
            yield return SceneManager.LoadSceneAsync(globalUISceneName, LoadSceneMode.Additive);
        }
    }

    // ============================================================
    // 場景切換 API(public static 入口)
    // ============================================================

    public static void ChangeScene(string newSceneName)
    {
        if (Instance != null) Instance.StartCoroutine(Instance.ChangeSceneRoutine(newSceneName, -1));
        else Debug.LogError("[SceneController] Instance missing!");
    }

    public static void ChangeScene(string newSceneName, int fadePhaseIndex)
    {
        if (Instance != null) Instance.StartCoroutine(Instance.ChangeSceneRoutine(newSceneName, fadePhaseIndex));
        else Debug.LogError("[SceneController] Instance missing!");
    }

    /// <summary>
    /// 【v3】帶回呼的場景切換(含淡入淡出)。
    /// onFullyReady 在 ReadyHandlers 全部跑完後觸發。
    /// 適合 Sequencer Command 等需要等待完整初始化的場合。
    /// </summary>
    public static void ChangeScene(string newSceneName, int fadePhaseIndex, Action onFullyReady)
    {
        if (Instance != null) Instance.StartCoroutine(Instance.ChangeSceneRoutine(newSceneName, fadePhaseIndex, onFullyReady));
        else Debug.LogError("[SceneController] Instance missing!");
    }

    /// <summary>
    /// 【v4】帶雙階段回呼的場景切換。
    /// onBeforeHandlers:場景載完但 ReadyHandlers 還沒跑前觸發(讀檔流程專用,用於注入存檔資料)
    /// onFullyReady:ReadyHandlers 全部跑完、場景完全就緒後觸發
    /// </summary>
    public static void ChangeScene(string newSceneName, int fadePhaseIndex, Action onBeforeHandlers, Action onFullyReady)
    {
        if (Instance != null) Instance.StartCoroutine(Instance.ChangeSceneRoutine(newSceneName, fadePhaseIndex, onBeforeHandlers, onFullyReady));
        else Debug.LogError("[SceneController] Instance missing!");
    }

    public static void ChangeSceneImmediate(string newSceneName)
    {
        if (Instance != null) Instance.StartCoroutine(Instance.ChangeSceneImmediateRoutine(newSceneName));
        else Debug.LogError("[SceneController] Instance missing!");
    }

    /// <summary>
    /// 【v3】帶回呼的直接場景切換(無淡入淡出)。
    /// onFullyReady 在 ReadyHandlers 全部跑完後觸發。
    /// </summary>
    public static void ChangeSceneImmediate(string newSceneName, Action onFullyReady)
    {
        if (Instance != null) Instance.StartCoroutine(Instance.ChangeSceneImmediateRoutine(newSceneName, onFullyReady));
        else Debug.LogError("[SceneController] Instance missing!");
    }

    // ============================================================
    // 場景切換 Routine(private IEnumerator 實作)
    // ============================================================

    private IEnumerator ChangeSceneRoutine(string newSceneName, int fadePhaseIndex = -1)
    {
        yield return ChangeSceneRoutine(newSceneName, fadePhaseIndex, null, null);
    }

    private IEnumerator ChangeSceneRoutine(string newSceneName, int fadePhaseIndex, Action onFullyReady)
    {
        // 舊簽名轉發到新簽名,onBeforeHandlers 為 null
        yield return ChangeSceneRoutine(newSceneName, fadePhaseIndex, null, onFullyReady);
    }

    /// <summary>
    /// 【v4 完整版】帶雙階段回呼的場景切換 Routine。
    /// </summary>
    private IEnumerator ChangeSceneRoutine(string newSceneName, int fadePhaseIndex, Action onBeforeHandlers, Action onFullyReady)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        currentMainScene = SceneManager.GetActiveScene().name;
        if (currentMainScene == newSceneName)
        {
            isTransitioning = false;
            yield break;
        }

        Debug.Log($"[SceneController] 開始轉場: 從 {currentMainScene} 到 {newSceneName}");
        OnBeforeSceneChange?.Invoke();

        yield return EnsureGlobalUISceneLoaded();

        // 1. 淡出
        if (TransitionFader.Instance != null) yield return TransitionFader.Instance.FadeToColor(fadePhaseIndex);

        // 2. 載入與卸載場景
        yield return SceneManager.LoadSceneAsync(newSceneName, LoadSceneMode.Additive);
        Scene newScene = SceneManager.GetSceneByName(newSceneName);
        SceneManager.SetActiveScene(newScene);

        if (!string.IsNullOrEmpty(currentMainScene) && SceneManager.GetSceneByName(currentMainScene).isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(currentMainScene);
        }

        currentMainScene = newSceneName;
        OnSceneChanged?.Invoke();

        // 2.5 ★ v4 新增:ReadyHandlers 執行前的 callback(供讀檔流程注入資料用)
        if (onBeforeHandlers != null)
        {
            Debug.Log("[SceneController] 執行 onBeforeHandlers 回呼...");
            onBeforeHandlers.Invoke();
        }

        // 3. 統一由 Coordinator 管理的初始化序列
        yield return ExecuteSceneReadyHandlers(newScene);

        // 3.5 通知:場景完全就緒(ReadyHandlers 全部跑完)
        OnSceneFullyReady?.Invoke();
        onFullyReady?.Invoke();

        // 4. 淡入
        if (TransitionFader.Instance != null) yield return TransitionFader.Instance.FadeToClear();

        isTransitioning = false;
    }
    private IEnumerator ChangeSceneImmediateRoutine(string newSceneName)
    {
        yield return ChangeSceneImmediateRoutine(newSceneName, null);
    }

    private IEnumerator ChangeSceneImmediateRoutine(string newSceneName, Action onFullyReady)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        currentMainScene = SceneManager.GetActiveScene().name;
        if (currentMainScene == newSceneName)
        {
            isTransitioning = false;
            yield break;
        }

        Debug.Log($"[SceneController] 直接轉場(無淡入淡出): 從 {currentMainScene} 到 {newSceneName}");
        OnBeforeSceneChange?.Invoke();

        yield return EnsureGlobalUISceneLoaded();

        yield return SceneManager.LoadSceneAsync(newSceneName, LoadSceneMode.Additive);
        Scene newScene = SceneManager.GetSceneByName(newSceneName);
        SceneManager.SetActiveScene(newScene);

        if (!string.IsNullOrEmpty(currentMainScene) && SceneManager.GetSceneByName(currentMainScene).isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(currentMainScene);
        }

        currentMainScene = newSceneName;
        OnSceneChanged?.Invoke();

        yield return ExecuteSceneReadyHandlers(newScene);

        // 通知:場景完全就緒
        OnSceneFullyReady?.Invoke();
        onFullyReady?.Invoke();

        isTransitioning = false;
    }

    // ============================================================
    // 輕量時段轉場 API
    // ============================================================

    public static void PerformSlotTransition(Action onMidTransition, Action onComplete = null)
    {
        if (Instance != null)
        {
            Instance.StartCoroutine(Instance.SlotTransitionRoutine(onMidTransition, onComplete));
        }
        else
        {
            Debug.LogWarning("[SceneController] Instance missing,直接執行回呼。");
            onMidTransition?.Invoke();
            onComplete?.Invoke();
        }
    }

    private IEnumerator SlotTransitionRoutine(Action onMidTransition, Action onComplete)
    {
        if (TransitionFader.Instance != null)
        {
            yield return TransitionFader.Instance.FadeToColor(-1, 0.2f);
            onMidTransition?.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);
            yield return TransitionFader.Instance.FadeToClear(0.2f);
        }
        else
        {
            onMidTransition?.Invoke();
        }

        onComplete?.Invoke();
    }

    // ============================================================
    // 隔天轉場 API
    // ============================================================

    public static void PerformDayTransition(Action onComplete = null)
    {
        if (DayTransitionUI.IsAvailable)
        {
            DayTransitionUI.PerformTransition(
                onMidTransition: null,
                onComplete: onComplete
            );
        }
        else if (Instance != null)
        {
            Instance.StartCoroutine(Instance.SimpleDayTransitionRoutine(onComplete));
        }
        else
        {
            Debug.LogError("[SceneController] 無法執行隔天轉場!");
            onComplete?.Invoke();
        }
    }

    public static void PerformDayTransitionThenChangeScene(string newSceneName)
    {
        if (Instance != null)
        {
            Instance.StartCoroutine(Instance.DayTransitionThenChangeSceneRoutine(newSceneName));
        }
        else
        {
            Debug.LogError("[SceneController] Instance missing!");
        }
    }

    private IEnumerator DayTransitionThenChangeSceneRoutine(string newSceneName)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        Debug.Log($"[SceneController] 開始隔天轉場 + 場景切換: {newSceneName}");
        OnBeforeSceneChange?.Invoke();

        yield return EnsureGlobalUISceneLoaded();

        bool transitionComplete = false;

        Action onMidTransition = () =>
        {
            StartCoroutine(LoadSceneDuringTransition(newSceneName));
        };

        if (DayTransitionUI.IsAvailable)
        {
            DayTransitionUI.PerformTransition(
                onMidTransition: onMidTransition,
                onComplete: () => { transitionComplete = true; }
            );

            while (!transitionComplete)
            {
                yield return null;
            }
        }
        else
        {
            if (TransitionFader.Instance != null)
            {
                yield return TransitionFader.Instance.FadeToColor(Color.black, 0.3f);
            }

            yield return LoadSceneDuringTransition(newSceneName);
            yield return new WaitForSecondsRealtime(1f);

            if (TransitionFader.Instance != null)
            {
                yield return TransitionFader.Instance.FadeToClear(0.3f);
            }
        }

        isTransitioning = false;
    }

    private IEnumerator LoadSceneDuringTransition(string newSceneName)
    {
        currentMainScene = SceneManager.GetActiveScene().name;

        yield return SceneManager.LoadSceneAsync(newSceneName, LoadSceneMode.Additive);
        Scene newScene = SceneManager.GetSceneByName(newSceneName);
        SceneManager.SetActiveScene(newScene);

        if (!string.IsNullOrEmpty(currentMainScene) && SceneManager.GetSceneByName(currentMainScene).isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(currentMainScene);
        }
        currentMainScene = newSceneName;

        OnSceneChanged?.Invoke();

        yield return ExecuteSceneReadyHandlers(newScene);

        // 通知:場景完全就緒
        OnSceneFullyReady?.Invoke();
    }

    

    // ============================================================
    // 【v3 改動】場景初始化 — 簡化為純粹的 Handler 搜集與執行
    // ============================================================

    /// <summary>
    /// 搜尋場景中所有 ISceneReadyHandler 並依序執行。
    /// 
    /// 不再硬編碼 HubController / ProgressStateController 的優先順序。
    /// 執行順序完全由各場景的 SceneReadyCoordinator 在 Inspector 中的 Task 列表決定。
    /// 
    /// 如果場景中有多個 ISceneReadyHandler(例如一個 Coordinator + 其他獨立 Handler),
    /// 它們會按照在 Hierarchy 中的順序被執行。
    /// </summary>
    private IEnumerator ExecuteSceneReadyHandlers(Scene scene)
    {
        GameObject[] rootObjects = scene.GetRootGameObjects();
        List<ISceneReadyHandler> allHandlers = new List<ISceneReadyHandler>();

        foreach (var root in rootObjects)
        {
            allHandlers.AddRange(root.GetComponentsInChildren<ISceneReadyHandler>(true));
        }

        if (allHandlers.Count == 0)
        {
            Debug.Log("[SceneController] 此場景無 ISceneReadyHandler,跳過初始化。");
            yield break;
        }

        Debug.Log($"[SceneController] 發現 {allHandlers.Count} 個 ISceneReadyHandler,開始執行...");

        foreach (var handler in allHandlers)
        {
            yield return handler.OnSceneReady();
        }
    }

    // ============================================================
    // 啟動流程
    // ============================================================

    private IEnumerator StartupRoutine()
    {
        yield return EnsureGlobalUISceneLoaded();

        string sceneToLoad = string.Empty;

#if UNITY_EDITOR
        sceneToLoad = UnityEditor.EditorPrefs.GetString("SceneController.PreviousScenePath", null);
        UnityEditor.EditorPrefs.DeleteKey("SceneController.PreviousScenePath");
#endif
        yield return null;
    }

    // ============================================================
    // 存檔系統整合 API
    // ============================================================

    public void ValidatePersistentUI()
    {
        Debug.Log("[SceneController] 正在驗證常駐 UI 是否存在...");
        StartCoroutine(EnsureGlobalUISceneLoaded());
    }

    // ============================================================
    // Global UI 卸載功能
    // ============================================================

    public static void UnloadGlobalUI()
    {
        if (Instance != null)
            Instance.StartCoroutine(Instance.UnloadGlobalUIRoutine());
    }

    private IEnumerator UnloadGlobalUIRoutine()
    {
        if (SceneManager.GetSceneByName(globalUISceneName).isLoaded)
            yield return SceneManager.UnloadSceneAsync(globalUISceneName);
    }
}