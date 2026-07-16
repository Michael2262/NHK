using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 場景準備工作的總指揮官（v2）。
/// 
/// 【改動重點】
/// - 現在是場景中「唯一的 ISceneReadyHandler 入口」，SceneController 只需找到它並執行。
/// - 所有準備工作（包括原本硬編碼在 SceneController 裡的 HubController、ProgressStateController）
///   都統一以 SceneReadyTaskBase 的形式，在 Inspector 的列表中排序執行。
/// - 支援 Prefab 化：做成一個公用 Prefab，每個場景拖入後在 Inspector 調整 Task 列表即可。
/// 
/// 【使用方式】
/// 1. 建立一個 Prefab，掛上此腳本
/// 2. 在 Prefab 上（或子物件上）掛載需要的 SceneReadyTaskBase 衍生類別
/// 3. 在 Inspector 的 sceneTasks 列表中，按照想要的執行順序排列
/// 4. 每個場景放一個此 Prefab 的實例，視需求增減 / 覆寫 Task
/// </summary>
public class SceneReadyCoordinator : MonoBehaviour, ISceneReadyHandler
{
    [Header("場景準備任務列表（依序執行）")]
    [Tooltip("Inspector 中的排列順序 = 執行順序。所有準備工作都在這裡管理。")]
    [SerializeField] private List<SceneReadyTaskBase> sceneTasks = new List<SceneReadyTaskBase>();

    [Header("除錯：直接 Play 此場景時")]
    [Tooltip("勾選後，若不是經由 SceneController 正規轉場進場（例如你直接在此場景按 Play 測試），" +
             "會在 Start 時自動補跑一次 OnSceneReady。正式轉場進場則不會重複執行。")]
    [SerializeField] private bool autoRunWhenPlayedDirectly = false;

    // 已經執行過（不論是 SceneController 呼叫，還是自動補跑），用來防止重複執行。
    private bool _hasRun;

    private IEnumerator Start()
    {
        if (!autoRunWhenPlayedDirectly)
            yield break;

        // 等一幀，讓 SceneController（若有走正規轉場）有機會先呼叫 OnSceneReady。
        yield return null;

        if (_hasRun)
        {
            Debug.Log("--- Coordinator: 已由正規轉場執行過，略過自動補跑 ---");
            yield break;
        }

        Debug.Log("--- Coordinator: 偵測到直接 Play 進場，自動補跑 OnSceneReady ---");
        yield return OnSceneReady();
    }

    public IEnumerator OnSceneReady()
    {
        _hasRun = true;
        string entryID = GameDataManager.Instance?.SceneEntryID ?? "Unknown";
        Debug.Log($"--- Coordinator: 開始準備場景，入口ID: [{entryID}] ---");

        if (sceneTasks.Count == 0)
        {
            Debug.Log("--- Coordinator: 無任務，直接完成 ---");
            yield break;
        }

        for (int i = 0; i < sceneTasks.Count; i++)
        {
            var task = sceneTasks[i];
            if (task == null)
            {
                Debug.LogWarning($"--- Coordinator: 任務 [{i}] 為 null，已跳過 ---");
                continue;
            }

            if (!task.enabled)
            {
                Debug.Log($"--- Coordinator: 任務 [{i}] {task.GetType().Name} 已停用，跳過 ---");
                continue;
            }

            Debug.Log($"--- Coordinator: 執行任務 [{i}] {task.GetType().Name} ---");
            yield return task.ExecuteTask(entryID);
        }

        Debug.Log("--- Coordinator: 所有任務執行完畢 ---");
    }

    // 是否正在執行中，避免按鈕連點造成多條 Coroutine 同時跑任務列表。
    private bool _isRunning;

    /// <summary>
    /// 給 UI Button.onClick / UnityEvent 呼叫：重新觸發整份 OnSceneReady 任務列表。
    /// 執行中再次呼叫會被忽略。
    /// </summary>
    public void ReRunFromUI()
    {
        if (_isRunning)
        {
            Debug.LogWarning("--- Coordinator: OnSceneReady 尚在執行中，忽略此次觸發 ---");
            return;
        }
        Debug.Log("--- Coordinator: UI 觸發重新執行 OnSceneReady ---");
        StartCoroutine(RunFromUIRoutine());
    }

    private IEnumerator RunFromUIRoutine()
    {
        _isRunning = true;
        yield return OnSceneReady();
        _isRunning = false;
    }

    // ============================================================
    // 編輯器輔助
    // ============================================================

    [ContextMenu("▶ 立即重新觸發 OnSceneReady（限 Play 中）")]
    private void ReRunNow()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("--- Coordinator: 此功能僅能在 Play Mode 中使用 ---");
            return;
        }
        Debug.Log("--- Coordinator: 手動重新觸發 OnSceneReady ---");
        StartCoroutine(OnSceneReady());
    }

    [ContextMenu("Auto-Find Tasks on this GameObject")]
    private void AutoFindTasks()
    {
        sceneTasks.Clear();
        GetComponents(sceneTasks);
        Debug.Log($"自動找到了 {sceneTasks.Count} 個任務。");
    }

    [ContextMenu("Auto-Find Tasks in Children")]
    private void AutoFindTasksInChildren()
    {
        sceneTasks.Clear();
        GetComponentsInChildren(true, sceneTasks);
        // 排除自己身上沒有的（如果 Coordinator 本身也繼承了 TaskBase 的話）
        Debug.Log($"自動找到了 {sceneTasks.Count} 個任務（含子物件）。");
    }
}