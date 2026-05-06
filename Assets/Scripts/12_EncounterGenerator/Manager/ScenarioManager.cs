using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 【例外前提劇本】定義
/// 當指定的 Flag 條件達成時，強制重設該角色的位置與行為，優先級最高。
/// </summary>
[System.Serializable]
public class HeroinePriorityScript
{
    [Tooltip("只是方便在 Inspector 辨識")]
    public string ruleName;
    [Tooltip("填寫該角色的唯一識別字串")]
    public string heroineID;

    [Tooltip("必須同時滿足這些 Flag，此劇本才會生效 (AND 邏輯)")]
    public List<ProgressFlagDefinition> requiredFlags;

    [Header("強制覆蓋後的狀態")]
    [Tooltip("填寫角色被「強制傳送」過去的地點代碼")]
    public string priorityLocationID;
    [Tooltip("填寫角色在該地點正在進行的動作描述字串，字串必須對應到該場景 HubController 裡 heroineRules 所設定的 activityState")]
    public string priorityActivity;
}
[System.Serializable]
public class RiskPriorityScript
{
    [Tooltip("方便在 Inspector 辨識")]
    public string ruleName;
    [Tooltip("填寫家人的 agentID (例如: mother)")]
    public string agentID;
    [Tooltip("強制執行的行為 ID (對應 Risk 物件的顯示邏輯)")]
    public string priorityInspectionTypeID;

    [Tooltip("必須同時滿足這些 Flag，此劇本才會生效")]
    public List<ProgressFlagDefinition> requiredFlags;

    [Header("強制覆蓋後的狀態")]
    [Tooltip("強制傳送的地點 ID")]
    public string priorityLocationID;
}

/// <summary>
/// 職責：負責「全地圖」角色的位置分配與情境生成。
/// 這是遊戲邏輯的源頭，確保資料層 (Model) 在時間切換時保持正確。
/// </summary>
public class ScenarioManager : MonoBehaviour
{
    public static ScenarioManager Instance { get; private set; }

    private GameStatusService _service;
    private TimeSystemModel _time;
    private ProgressFlagModel _flags;

    // Configs
    private LocationDatabase _locationDB;
    private RiskDatabase _riskDB;
    private HeroineStatusConfig _heroineConfig;

    [Header("--- 優先劇本 (例外前提) ---")]
    [Tooltip("定義特定 Flag 達成時，必須發生的固定劇本。這會攔截並取代常規排程。")]
    public List<HeroinePriorityScript> priorityScripts = new List<HeroinePriorityScript>();
    [Tooltip("定義特定 Flag 達成時，家人必須出現的固定位置")]
    public List<RiskPriorityScript> riskPriorityScripts = new List<RiskPriorityScript>();

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        _service = GameStatusService.Instance;
        if (_service == null) return;

        _time = _service.Time;
        _flags = _service.ProgressFlags;
        _locationDB = _service.LocationDB;
        _riskDB = _service.RiskDB;
        _heroineConfig = _service.HeroineConfig;

        _service.TimeManager.OnTimeSlotAdvanced += HandleSlotAdvanced;
        // 【NEW】訂閱每日事件，用來重抽當日鍊
        _service.TimeManager.OnDayPassed += HandleDayPassed;

        _service.OnGameStatusLoaded += () => {
            Debug.Log("[ScenarioManager] 遊戲存檔載入，保持當前地圖狀態。");
        };

        // 【NEW】遊戲開始時，若存檔裡沒有今日鍊(新遊戲第一天)，先抽一次
        TryRollDailyChainsIfEmpty();
    }

    void OnDestroy()
    {
        if (_service?.TimeManager != null)
        {
            _service.TimeManager.OnTimeSlotAdvanced -= HandleSlotAdvanced;
            _service.TimeManager.OnDayPassed -= HandleDayPassed;
        }
    }

    // ==========================================================
    // 【NEW】每日鍊重抽
    // ==========================================================

    /// <summary>
    /// 每天開始時，為每位女主角抽一條當日鍊。
    /// </summary>
    private void HandleDayPassed()
    {
        RollDailyChainsForAllHeroines();
        // 不需要在這裡呼叫 RecalculateWorldState，因為 OnDayPassed 之後
        // 通常會接著觸發 OnPhaseChanged / OnTimeSlotAdvanced，屆時會重算。
        // 若你的時間系統不會自動帶動 slot advance，可在這裡手動補呼叫一次。
    }

    /// <summary>
    /// 若 Model 裡的今日鍊是空的(新遊戲初始狀態),補抽一次。
    /// 存檔載入時不呼叫(存檔裡已有鍊)。
    /// </summary>
    private void TryRollDailyChainsIfEmpty()
    {
        if (_service?.Scenario == null) return;
        if (_service.Scenario.TodaysChainByHeroine.Count == 0)
        {
            RollDailyChainsForAllHeroines();
        }
    }

    /// <summary>
    /// 為所有女主角重抽當日鍊。
    /// </summary>
    public void RollDailyChainsForAllHeroines()
    {
        if (_heroineConfig?.AllSchedules == null) return;

        _service.Scenario.ClearTodaysChains();

        bool isWeekend = _time.IsWeekend;

        foreach (var scheduleConfig in _heroineConfig.AllSchedules)
        {
            string hID = scheduleConfig.heroineID;
            string chosenChainName = RollChainFor(scheduleConfig, isWeekend);
            _service.Scenario.AssignTodaysChain(hID, chosenChainName);

            if (!string.IsNullOrEmpty(chosenChainName))
                Debug.Log($"<color=#a0e060>[DailyChain] {hID} 今日鍊：「{chosenChainName}」</color>");
            else
                Debug.Log($"<color=#808080>[DailyChain] {hID} 今日無鍊，將使用 fallback schedule。</color>");
        }
    }

    /// <summary>
    /// 為單一女主角從她的 config 抽一條鍊。回傳 chainName 或 null。
    /// </summary>
    private string RollChainFor(HeroineScheduleConfig config, bool isWeekend)
    {
        if (config.dailyChains == null || config.dailyChains.Count == 0)
            return null;

        // 1. 過濾出所有「符合條件」的鍊
        var candidates = new List<DailyChain>();
        foreach (var chain in config.dailyChains)
        {
            if (!IsChainEligible(chain, isWeekend)) continue;
            if (chain.probabilityWeight <= 0) continue; // 權重 0 視為不抽 (保留給劇情觸發用)
            candidates.Add(chain);
        }

        if (candidates.Count == 0) return null;

        // 2. 加權隨機
        int totalWeight = candidates.Sum(c => c.probabilityWeight);
        if (totalWeight <= 0) return null;

        int roll = Random.Range(0, totalWeight);
        int acc = 0;
        foreach (var chain in candidates)
        {
            acc += chain.probabilityWeight;
            if (roll < acc) return chain.chainName;
        }
        return candidates.Last().chainName;
    }

    /// <summary>
    /// 判斷某條鍊是否符合今日條件。
    /// </summary>
    private bool IsChainEligible(DailyChain chain, bool isWeekend)
    {
        if (chain == null) return false;

        // DayType 過濾
        switch (chain.dayType)
        {
            case ScheduleDayType.WeekdayOnly: if (isWeekend) return false; break;
            case ScheduleDayType.WeekendOnly: if (!isWeekend) return false; break;
            case ScheduleDayType.EveryDay: break;
        }

        // requiredFlag 過濾
        if (chain.requiredFlag != null && !_flags.Contains(chain.requiredFlag.FlagID))
            return false;

        // forbiddenFlag 過濾
        if (chain.forbiddenFlag != null && _flags.Contains(chain.forbiddenFlag.FlagID))
            return false;

        return true;
    }

    /// <summary>
    /// 【NEW】Debug 用：強制重抽今日所有女主角的鍊 (立即生效,會觸發場景重算)
    /// </summary>
    [ContextMenu("Debug/Reroll Today's Chains")]
    public void DebugRerollTodaysChains()
    {
        RollDailyChainsForAllHeroines();
        RecalculateWorldState();
        _service.Scenario.NotifyScenarioRecalculated();
    }

    // ==========================================================
    // 全域重算
    // ==========================================================

    /// <summary>
    /// 【全域重算】核心邏輯
    /// 根據當前時間與 Flag，決定「所有人」的位置。
    /// </summary>
    public void RecalculateWorldState()
    {
        // 1. 清空地圖狀態，準備重新填入
        _service.Scenario.ClearAllStates();

        int phase = _time.CurrentPhaseIndex;
        int slot = _time.CurrentSlotInPhase;
        int day = _time.DayIndex;
        bool isWeekend = _time.IsWeekend;

        Debug.Log($"<color=yellow>[ScenarioManager] 重算開始 (Day {day} - Phase {phase})</color>");

        // 用於記錄哪些女主角已經被處理過，避免重複分配
        HashSet<string> handledHeroineIDs = new HashSet<string>();
        HashSet<string> handledRiskIDs = new HashSet<string>();

        // ----------------------------------------------------------
        // 階段 A：處理【優先劇本 (Priority Rules)】
        // ----------------------------------------------------------
        // --- 女主角的優先劇本 ---
        if (priorityScripts != null)
        {
            foreach (var script in priorityScripts)
            {
                if (handledHeroineIDs.Contains(script.heroineID)) continue;

                // 檢查是否滿足所有定義的進度旗標
                bool allFlagsMet = script.requiredFlags.All(f => _flags.Contains(f.FlagID));

                if (allFlagsMet)
                {
                    _service.Scenario.AddHeroineToLocation(script.priorityLocationID, script.heroineID, script.priorityActivity);
                    handledHeroineIDs.Add(script.heroineID);
                    Debug.Log($"<color=cyan>[Priority] {script.heroineID} 符合條件，強制重設至: {script.priorityLocationID}</color>");
                }
            }
        }
        // --- 風險的優先劇本 ---
        if (riskPriorityScripts != null)
        {
            foreach (var script in riskPriorityScripts)
            {
                if (handledRiskIDs.Contains(script.agentID)) continue;

                bool allFlagsMet = script.requiredFlags.All(f => _flags.Contains(f.FlagID));

                if (allFlagsMet)
                {
                    // 建立一個臨時的 RiskAction 用於傳遞數據
                    RiskAction priorityAction = new RiskAction
                    {
                        inspectionTypeID = script.priorityInspectionTypeID,
                        actionType = RiskActionType.Fixed // 強制視為固定出現在該地
                    };

                    _service.Scenario.AddRiskToLocation(script.priorityLocationID, priorityAction);
                    handledRiskIDs.Add(script.agentID);
                    Debug.Log($"<color=red>[RiskPriority] {script.agentID} 強制觸發劇本: {script.ruleName}</color>");
                }
            }
        }

        // ----------------------------------------------------------
        // 階段 B：處理【常規日程表 (Regular Schedule)】
        // 【v4 改動】優先查「今日鍊」,找不到再 fallback 到舊的 schedule
        // ----------------------------------------------------------
        foreach (var scheduleConfig in _heroineConfig.AllSchedules)
        {
            string hID = scheduleConfig.heroineID;

            // 如果該角色已經被階段 A 攔截，則跳過隨機排程
            if (handledHeroineIDs.Contains(hID)) continue;

            // 檢查是否不在家 (例如上學中)
            if (_service.Heroines[hID].IsCurrentlyAbsent(day, phase)) continue;

            // ── B1. 先嘗試使用「今日鍊」
            ScheduleScenario result = TryResolveFromDailyChain(scheduleConfig, hID, phase, slot);

            // ── B2. 鍊沒有覆蓋到此時段 → fallback 到舊 schedule
            if (result == null)
            {
                result = ResolveFromLegacySchedule(scheduleConfig, phase, slot, isWeekend);
            }

            if (result != null)
            {
                _service.Scenario.AddHeroineToLocation(result.locationID, hID, result.activityState);
            }
        }

        // ----------------------------------------------------------
        // 階段 C：處理【風險/家人 (Risk Agents)】
        // ----------------------------------------------------------
        foreach (var agentData in _riskDB.allAgents)
        {
            string aID = agentData.agentID;

            if (handledRiskIDs.Contains(aID)) continue;
            if (_service.RiskAgents.TryGetValue(aID, out var agentModel) &&
                agentModel.IsCurrentlyAbsent(day, phase)) continue;

            List<RiskAction> actions = _riskDB.GetActiveActions(aID, phase, slot, isWeekend);
            if (actions == null || actions.Count == 0) continue;

            RiskAction chosenAction = null;

            // 優先嘗試觸發固定點 (Fixed)
            RiskAction fixedAction = actions.Find(a => a.actionType == RiskActionType.Fixed);
            if (fixedAction != null && IsActionTriggered(fixedAction))
            {
                chosenAction = fixedAction;
            }

            // 若無固定點，則嘗試觸發巡邏 (Patrol)
            if (chosenAction == null)
            {
                var patrolActions = actions.Where(a => a.actionType == RiskActionType.Patrol).ToList();
                foreach (var p in patrolActions)
                {
                    if (IsActionTriggered(p)) { chosenAction = p; break; }
                }
            }

            if (chosenAction != null)
            {
                string targetLocationID = ResolveRiskLocation(chosenAction);
                if (!string.IsNullOrEmpty(targetLocationID))
                {
                    _service.Scenario.AddRiskToLocation(targetLocationID, chosenAction);
                }
            }
        }
    }

    /// <summary>
    /// 【NEW】從「今日被選中的鍊」解析此 (phase, slot) 的情境。
    /// 若今日沒鍊 / 鍊內沒有對應格 → 回傳 null。
    /// </summary>
    private ScheduleScenario TryResolveFromDailyChain(HeroineScheduleConfig config, string heroineID, int phase, int slot)
    {
        string chainName = _service.Scenario.GetTodaysChain(heroineID);
        if (string.IsNullOrEmpty(chainName)) return null;
        if (config.dailyChains == null) return null;

        // 用名稱找回今日鍊
        var chain = config.dailyChains.Find(c => c.chainName == chainName);
        if (chain == null)
        {
            Debug.LogWarning($"[ScenarioManager] 找不到 {heroineID} 今日被抽中的鍊「{chainName}」(可能 Config 改過)。將 fallback。");
            return null;
        }

        // 找這條鍊在此 (phase, slot) 的格子
        var block = chain.blocks?.Find(b => b.phaseIndex == phase && b.slotIndex == slot);
        if (block == null) return null; // 此時段鍊沒覆蓋

        return ChooseScenario(block.possibleScenarios);
    }

    /// <summary>
    /// 【原邏輯抽出】從舊的 per-slot schedule 解析。作為 fallback。
    /// </summary>
    private ScheduleScenario ResolveFromLegacySchedule(HeroineScheduleConfig config, int phase, int slot, bool isWeekend)
    {
        if (config.schedule == null) return null;

        var potentialBlocks = config.schedule.Where(b => b.phaseIndex == phase && b.slotIndex == slot);
        ScheduleBlock chosenBlock = null;

        if (isWeekend) chosenBlock = potentialBlocks.FirstOrDefault(b => b.dayType == ScheduleDayType.WeekendOnly);
        else chosenBlock = potentialBlocks.FirstOrDefault(b => b.dayType == ScheduleDayType.WeekdayOnly);

        if (chosenBlock == null) chosenBlock = potentialBlocks.FirstOrDefault(b => b.dayType == ScheduleDayType.EveryDay);

        if (chosenBlock == null) return null;
        return ChooseScenario(chosenBlock.possibleScenarios);
    }

    private void HandleSlotAdvanced(int slots)
    {
        RecalculateWorldState();
        _service.Scenario.NotifyScenarioRecalculated();//資料確認更新後，廣播通知
    }

    // ==========================================================
    // 輔助方法
    // ==========================================================

    private string ResolveRiskLocation(RiskAction action)
    {
        if (action.actionType == RiskActionType.Fixed)
        {
            if (action.fixedLocationIDList != null && action.fixedLocationIDList.Count > 0)
                return action.fixedLocationIDList[Random.Range(0, action.fixedLocationIDList.Count)];
        }
        else if (action.actionType == RiskActionType.Patrol)
        {
            var allLocIDs = _locationDB.allLocations.Select(l => l.LocationID).ToList();
            var validIDs = allLocIDs.Except(action.excludedLocationIDs).ToList();
            if (validIDs.Count > 0) return validIDs[Random.Range(0, validIDs.Count)];
        }
        return null;
    }

    private ScheduleScenario ChooseScenario(List<ScheduleScenario> scenarios)
    {
        if (scenarios == null || scenarios.Count == 0) return null;

        // 檢查 Flag 限制
        List<ScheduleScenario> validScenarios = scenarios
            .Where(s => s.requiredFlag == null || _flags.Contains(s.requiredFlag.FlagID))
            .ToList();

        if (validScenarios.Count == 0) return null;

        int totalWeight = validScenarios.Sum(s => s.probabilityWeight);
        if (totalWeight == 0) return validScenarios[0];

        int randomRoll = Random.Range(0, totalWeight);
        int currentWeight = 0;
        foreach (var s in validScenarios)
        {
            currentWeight += s.probabilityWeight;
            if (randomRoll < currentWeight) return s;
        }
        return validScenarios.Last();
    }

    private bool IsActionTriggered(RiskAction action)
    {
        if (!string.IsNullOrEmpty(action.requiredFlag) && !_flags.Contains(action.requiredFlag))
            return false;
        return (Random.Range(0, 100) < action.triggerChance);
    }

    public void OnPlayerSelectLocation(string locationID) //這個完全是給地圖類的場景移動按鈕呼叫，普通場景移動不是用這個
    {
        LocationData locData = _locationDB.FindLocationByID(locationID);
        if (locData == null) return;

        // 檢查解鎖條件
        if (!locData.UnlockedByDefault && !_flags.Contains(locData.RequiredFlag)) return;
        if (!locData.AllowedPhaseIndices.Contains(_time.CurrentPhaseIndex)) return;

        _service.Scenario.SetPlayerLocation(locationID);
        SceneController.ChangeScene(locData.hubSceneName);
    }

    public void ChangeLocation(string locationID) //更新locationID與數據狀態，不執行驗證與場景跳轉
    {
        if (_service == null || _service.Scenario == null)
        {
            Debug.LogError("[ScenarioManager] ChangeLocation 失敗：GameStatusService 或 ScenarioModel 為空。");
            return;
        }

        // 直接呼叫 Model 層的 SetPlayerLocation 執行這兩項任務
        _service.Scenario.SetPlayerLocation(locationID);

        Debug.Log($"<color=lime>[ScenarioManager] 地點已手動更新為: {locationID} (IsInScenario: true)</color>");
    }

    public void ForceMoveHeroine(string heroineID, string targetLocationID, string newActivity)
    {
        if (string.IsNullOrEmpty(heroineID)) return;
        _service.Scenario.ForceSetHeroineLocation(heroineID, targetLocationID, newActivity);
    }

    // 在 ScenarioManager.cs 中新增
    public void ForceMoveRisk(string agentID, string targetLocationID, string inspectionTypeID)
    {
        if (string.IsNullOrEmpty(agentID)) return;

        // 從資料庫中找出對應的 RiskAction
        RiskAgentData agentData = _riskDB.FindAgentByID(agentID);
        RiskAction targetAction = null;

        if (agentData != null)
        {
            // 尋找符合該行為 ID 的 Action
            foreach (var block in agentData.schedule)
            {
                targetAction = block.possibleActions.Find(a => a.inspectionTypeID == inspectionTypeID);
                if (targetAction != null) break;
            }
        }

        // 如果找不到現成的，則手動建立一個臨時的
        if (targetAction == null)
        {
            targetAction = new RiskAction { inspectionTypeID = inspectionTypeID };
        }

        _service.Scenario.ForceSetRiskLocation(agentID, targetLocationID, targetAction);
    }

    [ContextMenu("Debug/Log World State")]
    public void DebugLogWorldState()
    {
        var allStates = _service.Scenario.AllLocationStates;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"<color=yellow>=== 全地圖分佈 (Day {_time.DayIndex}) ===</color>");

        // 【NEW】一併顯示今日鍊
        sb.AppendLine($"<color=#a0e060>今日鍊指派：</color>");
        foreach (var kvp in _service.Scenario.TodaysChainByHeroine)
        {
            sb.AppendLine($"   - {kvp.Key}: 「{(string.IsNullOrEmpty(kvp.Value) ? "(無,走 fallback)" : kvp.Value)}」");
        }

        foreach (var kvp in allStates)
        {
            if (kvp.Value.Heroines.Count > 0 || kvp.Value.Risks.Count > 0)
            {
                sb.AppendLine($"📍 <b>[{kvp.Key}]</b>:");
                foreach (var h in kvp.Value.Heroines) sb.AppendLine($"   - 👩 {h.HeroineID} ({h.Activity})");
                foreach (var r in kvp.Value.Risks) sb.AppendLine($"   - ⚡ 風險: {r.inspectionTypeID}");
            }
        }
        Debug.Log(sb.ToString());
    }
}