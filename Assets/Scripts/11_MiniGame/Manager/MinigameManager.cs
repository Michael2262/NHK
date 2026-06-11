using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-800)]
public class MinigameManager : MonoBehaviour
{
    public static MinigameManager Instance { get; private set; }

    // 暫存字典 (開發度)
    private Dictionary<string, int> _pendingLewdnessExp = new Dictionary<string, int>();

    // ============================================================================
    // 【v4 改動】結束後動作系統 (Post-Game Actions) 已遷移至 SceneActionQueueModel
    // ============================================================================
    // - 佇列改存於 GameStatusService.SceneActionQueue（跨場景常駐）
    // - 執行改由各場景 SceneReadyCoordinator 的 Task_ExecuteSceneActionQueue 負責
    // - MinigameManager 只負責在小遊戲期間 Suspend / 結束時 Resume，
    //   確保佇列命令等到「小遊戲結束、時間推進後」才由返回場景執行（保留舊語意，
    //   串關切換場景時不會提早消費）
    // ============================================================================

    private GameStatusService _gss;

    // --- 跨場景傳遞的狀態 ---
    private List<string> _targetHeroineIDs;
    private string _originSceneName;
    private IMinigameController _currentMinigame;

    private List<HeroineStatusModel> _activeHeroineModels;

    private const string MINIGAME_ENTRY_ID = "MINIGAME_START";

    void Awake()
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

    void Start()
    {
        _gss = GameStatusService.Instance;
        if (_gss == null) Debug.LogError("MGM 找不到 GSS！");
        _activeHeroineModels = new List<HeroineStatusModel>();
    }

    #region 小遊戲啟動流程

    /// <summary>
    /// 外部 (e.g., MinigameStartButton) 呼叫此方法啟動小遊戲
    /// </summary>
    public void StartMinigame(string minigameSceneName, List<string> targetHeroineIDs)
    {
        if (_currentMinigame == null)
        {
            _originSceneName = SceneManager.GetActiveScene().name;
        }

        // 儲存跨場景資料
        _targetHeroineIDs = targetHeroineIDs;
        _originSceneName = SceneManager.GetActiveScene().name;

        // 暫停場景動作佇列的消費：小遊戲期間（含串關）佇列的命令
        // 要等 HandleMinigameFinished 恢復後，才由返回場景執行
        GameStatusService.Instance?.SceneActionQueue?.Suspend();

        // 整合：使用現有的 GameDataManager
        GameDataManager.Instance.SetNextSceneEntry(MINIGAME_ENTRY_ID);

        // 整合：使用現有的 SceneController
        SceneController.ChangeScene(minigameSceneName);
    }

    /// <summary>
    /// 小遊戲場景中的 Controller 必須呼叫此方法進行註冊
    /// </summary>
    public void RegisterMinigameController(IMinigameController controller)
    {
        Debug.Log($"[MGM] 收到 Controller '{controller.GetType().Name}' 的報到。");

        _currentMinigame = controller;
        _currentMinigame.OnGameFinished += HandleMinigameFinished;

        var protagonist = _gss.Protagonist;

        // 核心：建立 Model 列表
        _activeHeroineModels.Clear();
        bool allFound = true;
        foreach (string id in _targetHeroineIDs)
        {
            if (_gss.Heroines.TryGetValue(id, out var heroine))
            {
                _activeHeroineModels.Add(heroine);
            }
            else
            {
                Debug.LogError($"[MGM] 致命錯誤：在報到時找不到 ID 為 {id} 的女主角！");
                allFound = false;
            }
        }

        if (!allFound || _activeHeroineModels.Count == 0)
        {
            HandleMinigameFinished();
            return;
        }

        // 製作背包 (Context)
        var context = new MinigameContext(
            protagonist: _gss.Protagonist,
            activeHeroines: _activeHeroineModels,
            skills: _gss.Skills,
            time: _gss.Time,
            riskAgents: _gss.RiskAgents,
            statusEffect: _gss.StatusEffectModel,
            progressFlags: _gss.ProgressFlags,
            scenario: _gss.Scenario,
            timeManager: _gss.TimeManager
        );

        Debug.Log($"[MGM] 資料打包完成，正在注入給小遊戲...");

        // 把背包交給小遊戲
        _currentMinigame.Initialize(context);
        _currentMinigame.StartGame();
    }

    #endregion

    #region 遊戲中累積操作

    /// <summary>
    /// 暫存點數(開發度)的方法
    /// </summary>
    public void AccumulateLewdness(string heroineID, int amount)
    {
        if (!_pendingLewdnessExp.ContainsKey(heroineID))
            _pendingLewdnessExp[heroineID] = 0;
        _pendingLewdnessExp[heroineID] += amount;
    }

    /// <summary>
    /// 跳轉下一關的方法
    /// </summary>
    public void ContinueToNextMinigame(string nextSceneName)
    {
        Debug.Log($"[MGM] 準備跳轉至下一關：{nextSceneName}");

        if (_currentMinigame != null)
        {
            _currentMinigame.OnGameFinished -= HandleMinigameFinished;
            _currentMinigame = null;
        }
        SceneController.ChangeScene(nextSceneName);
    }

    #endregion

    #region 小遊戲結束處理

    /// <summary>
    /// 小遊戲回報結果 - 核心處理流程
    /// 
    /// 【v3 改動】
    /// 步驟 1 現在會區分「推進後是否跨日」：
    /// - 不跨日 → 正常推進，然後用 ChangeScene 返回
    /// - 跨日   → 推進日期，然後用 PerformDayTransitionThenChangeScene 返回（帶隔天演出）
    /// - 不推進 → 直接 ChangeScene 返回
    /// </summary>
    private void HandleMinigameFinished()
    {
        if (_currentMinigame == null) return;

        Debug.Log($"[MGM] 收到小遊戲結束回報。");

        // ============================================
        // 步驟 1: 判斷時間推進模式
        // ============================================
        bool willCrossDay = false;

        if (_currentMinigame.AdvanceTimeOnFinish)
        {
            var timeModel = _gss.Time;

            // 檢查：推進 1 格後是否會跨日？
            if (timeModel.CanAdvanceWithinDay(1))
            {
                // 不跨日：正常推進
                Debug.Log("[MGM] 時間推進 1 格（不跨日）。");
                _gss.TimeManager.ConsumeOneSlot();
            }
            else
            {
                // 跨日：推進日期，稍後用隔天演出返回
                Debug.Log("[MGM] 時間推進會跨日，執行日期推進。");
                int remaining = timeModel.GetRemainingSlotsInDay();
                timeModel.AdvanceTime(remaining + 1);
                willCrossDay = true;
            }
        }

        // ============================================
        // 步驟 2: 恢復場景動作佇列的消費 (在時間推進之後)
        // 佇列命令將由返回場景的 Task_ExecuteSceneActionQueue 在淡入前執行；
        // 時間已於步驟 1 推進完畢，強制移動不會被 RecalculateWorldState 覆蓋
        // ============================================
        GameStatusService.Instance?.SceneActionQueue?.Resume();

        // ============================================
        // 步驟 3: 取消訂閱
        // ============================================
        _currentMinigame.OnGameFinished -= HandleMinigameFinished;
        _currentMinigame = null;

        // ============================================
        // 步驟 4: 結算開發度
        // ============================================
        foreach (var entry in _pendingLewdnessExp)
        {
            if (_gss.Heroines.TryGetValue(entry.Key, out var heroine))
                heroine.AddLewdnessExp(entry.Value);
        }
        _pendingLewdnessExp.Clear();

        // ============================================
        // 步驟 5: 返回原場景（根據是否跨日選擇方式）
        // ============================================
        if (!string.IsNullOrEmpty(_originSceneName))
        {
            if (willCrossDay)
            {
                // 跨日：帶隔天演出 + 切場景
                Debug.Log($"[MGM] 跨日返回：使用隔天演出轉場回 {_originSceneName}");
                SceneController.PerformDayTransitionThenChangeScene(_originSceneName);
            }
            else
            {
                // 不跨日 or 不推進：普通轉場
                SceneController.ChangeScene(_originSceneName);
            }
        }

        // ============================================
        // 步驟 6: 清理狀態
        // ============================================
        _targetHeroineIDs?.Clear();
        _activeHeroineModels?.Clear();
    }

    #endregion

    #region 輔助方法

    /// <summary>
    /// 檢查是否有待處理的小遊戲資料
    /// </summary>
    public bool HasPendingMinigameData()
    {
        if (_gss == null) return false;
        if (_targetHeroineIDs == null || _targetHeroineIDs.Count == 0) return false;
        return true;
    }

    /// <summary>
    /// 覆寫小遊戲結束後的回歸場景。
    /// 用於在小遊戲過程中，根據結果動態改變回歸地點。
    /// </summary>
    public void SetReturnScene(string sceneName)
    {
        _originSceneName = sceneName;
        Debug.Log($"[MGM] 回歸場景已覆寫為: '{sceneName}'");
    }

    /// <summary>
    /// 測試用：手動設定小遊戲結束後要回歸的場景名稱（保留向下相容）
    /// </summary>
    public void DebugSetOriginScene(string sceneName)
    {
        SetReturnScene(sceneName);
    }

    /// <summary>
    /// 供 Mock 模式使用：只註冊 Controller，不執行資料注入
    /// </summary>
    public void RegisterControllerForMock(IMinigameController controller)
    {
        Debug.Log($"[MGM] Mock 模式：註冊 Controller '{controller.GetType().Name}'");

        if (_currentMinigame != null)
        {
            _currentMinigame.OnGameFinished -= HandleMinigameFinished;
        }

        _currentMinigame = controller;
        _currentMinigame.OnGameFinished += HandleMinigameFinished;
    }

    #endregion
}