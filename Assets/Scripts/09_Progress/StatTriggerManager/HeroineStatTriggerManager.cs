using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 職責：監控女角數值變化，並根據 SO 定義的規則自動操作 ProgressFlagModel。
/// 支援：智慧 Value 覆蓋、數值同步映射、FireAndForget 模式。
/// </summary>
public class HeroineStatTriggerManager
{
    private readonly ProgressFlagModel _flags;

    // 規則尋找字典
    private readonly Dictionary<string, List<StatTriggerRule>> _lewdnessLookup = new Dictionary<string, List<StatTriggerRule>>();
    private readonly Dictionary<string, List<StatTriggerRule>> _excitementLookup = new Dictionary<string, List<StatTriggerRule>>();

    // 數值同步映射
    private readonly Dictionary<string, ProgressValueDefinition> _lewdnessSyncMap = new Dictionary<string, ProgressValueDefinition>();
    private readonly Dictionary<string, ProgressValueDefinition> _excitementSyncMap = new Dictionary<string, ProgressValueDefinition>();

    public HeroineStatTriggerManager(ProgressFlagModel flags, Dictionary<string, HeroineStatusModel> heroines, List<HeroineTriggerContainer> containers)
    {
        _flags = flags;

        // 1. 初始化資料結構，將 SO 內容整理進字典
        foreach (var container in containers)
        {
            if (container == null || string.IsNullOrEmpty(container.HeroineID)) continue;

            _lewdnessLookup[container.HeroineID] = container.LewdnessRules;
            _excitementLookup[container.HeroineID] = container.ExcitementRules;

            if (container.LewdnessSyncSO != null) _lewdnessSyncMap[container.HeroineID] = container.LewdnessSyncSO;
            if (container.ExcitementSyncSO != null) _excitementSyncMap[container.HeroineID] = container.ExcitementSyncSO;
        }

        // 2. 訂閱所有女角的數值變更事件
        foreach (var heroine in heroines.Values)
        {
            // 開發度變更時：同步數值 + 檢查規則
            heroine.OnLewdnessChanged += (delta) => {
                SyncLewdness(heroine);
                CheckLewdnessRules(heroine.HeroineID, heroine.LewdnessLevel, false);
            };

            // 興奮度變更時：同步數值 + 檢查規則
            heroine.OnTotalExcitementLevelChanged += (level) => {
                SyncExcitement(heroine);
                CheckExcitementRules(heroine.HeroineID, level, false);
            };
        }
    }

    #region 核心檢查邏輯

    private void CheckLewdnessRules(string heroineID, int currentLevel, bool isRefresh)
    {
        if (_lewdnessLookup.TryGetValue(heroineID, out var rules))
            ExecuteRules(rules, currentLevel, isRefresh);
    }

    private void CheckExcitementRules(string heroineID, int currentLevel, bool isRefresh)
    {
        if (_excitementLookup.TryGetValue(heroineID, out var rules))
            ExecuteRules(rules, currentLevel, isRefresh);
    }

    /// <summary>
    /// 執行規則判定。包含智慧 Value 覆蓋邏輯。
    /// </summary>
    private void ExecuteRules(List<StatTriggerRule> rules, int currentValue, bool isRefresh)
    {
        if (rules == null || rules.Count == 0) return;

        // 1. 根據目標 SO 進行分組，確保同一個 Flag/Value 的規則會一起判斷
        var groups = rules.GroupBy(r => r.ProgressSO);

        foreach (var group in groups)
        {
            var targetSO = group.Key;
            if (targetSO == null) continue;

            string key = targetSO.FlagID; //

            // 2. 找出目前滿足門檻的所有規則
            var metRules = group.Where(r => currentValue >= r.Threshold).ToList();

            if (targetSO is ProgressValueDefinition valSO)
            {
                // --- 智慧 Value 處理：取門檻最高的那一條規則執行 ---
                var highestMetRule = metRules.OrderByDescending(r => r.Threshold).FirstOrDefault();

                if (highestMetRule != null)
                {
                    // 如果是讀檔重整且行為是 FireAndForget，則跳過設定
                    if (isRefresh && highestMetRule.Behavior == TriggerBehavior.FireAndForget) continue;

                    _flags.SetValue(key, highestMetRule.TargetValue); //
                }
                else
                {
                    // 若沒有規則滿足：檢查是否有 Toggle 規則需要回落到預設值
                    bool hasToggle = group.Any(r => r.Behavior == TriggerBehavior.Toggle);
                    if (hasToggle && !isRefresh)
                    {
                        _flags.SetValue(key, valSO.DefaultValue); //
                    }
                }
            }
            else // --- Flag (布林) 處理 ---
            {
                foreach (var rule in group)
                {
                    // 讀檔重整時不處理 FireAndForget，避免強制蓋掉玩家在劇情中手動關閉的標記
                    if (isRefresh && rule.Behavior == TriggerBehavior.FireAndForget) continue;

                    bool isMet = currentValue >= rule.Threshold;

                    if (isMet)
                    {
                        _flags.AddPersistentFlag(key); //
                    }
                    else if (rule.Behavior == TriggerBehavior.Toggle && !isRefresh)
                    {
                        _flags.RemoveFlag(key); //
                    }
                }
            }
        }
    }

    #endregion

    #region 數值同步映射 (Value Mapping)

    private void SyncLewdness(HeroineStatusModel heroine)
    {
        if (_lewdnessSyncMap.TryGetValue(heroine.HeroineID, out var so))
            _flags.SetValue(so.FlagID, heroine.LewdnessLevel); //
    }

    private void SyncExcitement(HeroineStatusModel heroine)
    {
        if (_excitementSyncMap.TryGetValue(heroine.HeroineID, out var so))
            _flags.SetValue(so.FlagID, heroine.TotalExcitementLevel); //
    }

    #endregion

    /// <summary>
    /// 【讀檔或啟動時呼叫】強制刷新所有規則與數值同步。
    /// </summary>
    public void RefreshAllRules(Dictionary<string, HeroineStatusModel> heroines)
    {
        // ★ 快照,避免迭代中間接事件修改原字典
        var snapshot = heroines.Values.ToList();
        foreach (var heroine in snapshot)
        {
            SyncLewdness(heroine);
            SyncExcitement(heroine);
            CheckLewdnessRules(heroine.HeroineID, heroine.LewdnessLevel, true);
            CheckExcitementRules(heroine.HeroineID, heroine.TotalExcitementLevel, true);
        }
    }
}