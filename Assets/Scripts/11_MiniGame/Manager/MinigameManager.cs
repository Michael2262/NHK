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
    // 【內嵌資料結構】結束後動作系統 (Post-Game Actions)
    // ============================================================================
    // 使用說明：
    // - 此區塊讓 MinigameManager 可以獨立運作，不依賴外部檔案
    // - 若你想使用獨立的 MinigameResultActions.cs 檔案，請刪除此 #region 區塊
    // - 兩種方式功能完全相同，選擇你偏好的組織方式即可
    // ============================================================================
    #region 內嵌資料結構 (若使用獨立檔案請刪除此區塊)

    [System.Serializable]
    public class MinigameResultActions
    {
        public List<ForceMoveHeroineAction> heroineMoves = new List<ForceMoveHeroineAction>();
        public List<ForceMoveRiskAction> riskMoves = new List<ForceMoveRiskAction>();
        public List<FlagAction> flagChanges = new List<FlagAction>();

        public bool HasAnyAction()
        {
            return (heroineMoves != null && heroineMoves.Count > 0) ||
                   (riskMoves != null && riskMoves.Count > 0) ||
                   (flagChanges != null && flagChanges.Count > 0);
        }

        public void Clear()
        {
            heroineMoves?.Clear();
            riskMoves?.Clear();
            flagChanges?.Clear();
        }
    }

    [System.Serializable]
    public struct ForceMoveHeroineAction
    {
        public string heroineID;
        public string targetLocationID;
        public string newActivity;

        public ForceMoveHeroineAction(string heroineID, string targetLocationID, string newActivity)
        {
            this.heroineID = heroineID;
            this.targetLocationID = targetLocationID;
            this.newActivity = newActivity;
        }
    }

    [System.Serializable]
    public struct ForceMoveRiskAction
    {
        public string agentID;
        public string targetLocationID;
        public string inspectionTypeID;

        public ForceMoveRiskAction(string agentID, string targetLocationID, string inspectionTypeID)
        {
            this.agentID = agentID;
            this.targetLocationID = targetLocationID;
            this.inspectionTypeID = inspectionTypeID;
        }
    }

    [System.Serializable]
    public struct FlagAction
    {
        public string flagID;
        public bool shouldAdd;
        public FlagLifetime lifetime; // 新增：指定 Flag 的生命週期

        public FlagAction(string flagID, bool shouldAdd, FlagLifetime lifetime = FlagLifetime.Persistent)
        {
            this.flagID = flagID;
            this.shouldAdd = shouldAdd;
            this.lifetime = lifetime;
        }
    }

    #endregion

    // 待執行的結束動作 (可為 null，不影響核心流程)
    private MinigameResultActions _pendingActions;

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

    #region 結束後動作佇列 API

    /// <summary>
    /// 【新增】註冊完整的結束後動作集合
    /// </summary>
    public void RegisterPostGameActions(MinigameResultActions actions)
    {
        if (actions == null) return;

        if (_pendingActions == null)
        {
            _pendingActions = actions;
        }
        else
        {
            // 合併到現有的動作中
            if (actions.heroineMoves != null)
                _pendingActions.heroineMoves.AddRange(actions.heroineMoves);
            if (actions.riskMoves != null)
                _pendingActions.riskMoves.AddRange(actions.riskMoves);
            if (actions.flagChanges != null)
                _pendingActions.flagChanges.AddRange(actions.flagChanges);
        }

        Debug.Log($"[MGM] 已註冊結束後動作 (女主角移動:{actions.heroineMoves?.Count ?? 0}, 風險移動:{actions.riskMoves?.Count ?? 0}, Flag變更:{actions.flagChanges?.Count ?? 0})");
    }

    /// <summary>
    /// 【新增】佇列單一女主角強制移動
    /// </summary>
    public void QueueHeroineMove(string heroineID, string locationID, string activity)
    {
        if (string.IsNullOrEmpty(heroineID)) return;

        if (_pendingActions == null)
            _pendingActions = new MinigameResultActions();

        _pendingActions.heroineMoves.Add(new ForceMoveHeroineAction(heroineID, locationID, activity));
        Debug.Log($"[MGM] 佇列女主角移動: {heroineID} → {locationID} ({activity})");
    }

    /// <summary>
    /// 【新增】佇列單一風險/家人強制移動
    /// </summary>
    public void QueueRiskMove(string agentID, string locationID, string inspectionTypeID)
    {
        if (string.IsNullOrEmpty(agentID)) return;

        if (_pendingActions == null)
            _pendingActions = new MinigameResultActions();

        _pendingActions.riskMoves.Add(new ForceMoveRiskAction(agentID, locationID, inspectionTypeID));
        Debug.Log($"[MGM] 佇列風險移動: {agentID} → {locationID} ({inspectionTypeID})");
    }

    /// <summary>
    /// 【新增】佇列旗標變更
    /// </summary>
    /// <param name="flagID">旗標 ID</param>
    /// <param name="shouldAdd">true = 新增, false = 移除</param>
    /// <param name="lifetime">Flag 生命週期 (預設為 Persistent 永久)</param>
    public void QueueFlagChange(string flagID, bool shouldAdd, FlagLifetime lifetime = FlagLifetime.Persistent)
    {
        if (string.IsNullOrEmpty(flagID)) return;

        if (_pendingActions == null)
            _pendingActions = new MinigameResultActions();

        _pendingActions.flagChanges.Add(new FlagAction(flagID, shouldAdd, lifetime));
        Debug.Log($"[MGM] 佇列 Flag 變更: {flagID} ({(shouldAdd ? "新增" : "移除")}, 生命週期: {lifetime})");
    }

    /// <summary>
    /// 【新增】清除所有待執行動作
    /// </summary>
    public void ClearPendingActions()
    {
        _pendingActions?.Clear();
        _pendingActions = null;
        Debug.Log("[MGM] 已清除所有待執行動作");
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
        // 步驟 2: 執行自定義動作 (在時間推進之後)
        // 這樣不會被 RecalculateWorldState 覆蓋
        // ============================================
        ExecutePendingActions();

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

    /// <summary>
    /// 【新增】執行所有累積的結束動作
    /// </summary>
    private void ExecutePendingActions()
    {
        if (_pendingActions == null || !_pendingActions.HasAnyAction())
        {
            Debug.Log("[MGM] 無待執行的結束動作。");
            return;
        }

        Debug.Log($"[MGM] 開始執行結束後動作...");

        // 取得 ScenarioManager
        var scenarioMgr = ScenarioManager.Instance;
        if (scenarioMgr == null)
        {
            Debug.LogWarning("[MGM] 找不到 ScenarioManager，無法執行位置變更動作！");
        }

        // --- 執行女主角強制移動 ---
        if (_pendingActions.heroineMoves != null && scenarioMgr != null)
        {
            foreach (var move in _pendingActions.heroineMoves)
            {
                Debug.Log($"[MGM] 執行女主角強制移動: {move.heroineID} → {move.targetLocationID} ({move.newActivity})");
                scenarioMgr.ForceMoveHeroine(move.heroineID, move.targetLocationID, move.newActivity);
            }
        }

        // --- 執行風險/家人強制移動 ---
        if (_pendingActions.riskMoves != null && scenarioMgr != null)
        {
            foreach (var move in _pendingActions.riskMoves)
            {
                Debug.Log($"[MGM] 執行風險強制移動: {move.agentID} → {move.targetLocationID} ({move.inspectionTypeID})");
                scenarioMgr.ForceMoveRisk(move.agentID, move.targetLocationID, move.inspectionTypeID);
            }
        }

        // --- 執行 Flag 變更 ---
        if (_pendingActions.flagChanges != null && _gss?.ProgressFlags != null)
        {
            foreach (var flag in _pendingActions.flagChanges)
            {
                if (flag.shouldAdd)
                {
                    Debug.Log($"[MGM] 新增 Flag: {flag.flagID} (生命週期: {flag.lifetime})");
                    _gss.ProgressFlags.AddFlag(flag.flagID, flag.lifetime);
                }
                else
                {
                    Debug.Log($"[MGM] 移除 Flag: {flag.flagID}");
                    _gss.ProgressFlags.RemoveFlag(flag.flagID);
                }
            }
        }

        // --- 清空待執行動作 ---
        _pendingActions.Clear();
        _pendingActions = null;

        Debug.Log("[MGM] 結束後動作執行完畢。");
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