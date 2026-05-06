using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 通用的數值觸發管理器
/// 負責：監聽任意數值來源，根據 SO 定義的規則自動操作 ProgressFlagModel
/// 
/// 使用方式：
/// 1. 建立 ValueTriggerContainer SO，設定 SourceID 和 Rules
/// 2. 初始化時傳入所有 Container
/// 3. 當數值變化時呼叫 OnValueChanged(sourceID, newValue)
/// </summary>
public class ValueTriggerManager
{
    private readonly ProgressFlagModel _flags;

    // SourceID → Container 的查詢表
    private readonly Dictionary<string, ValueTriggerContainer> _containers
        = new Dictionary<string, ValueTriggerContainer>();

    // SourceID → 當前數值的快取（用於 Refresh）
    private readonly Dictionary<string, int> _currentValues
        = new Dictionary<string, int>();

    public ValueTriggerManager(ProgressFlagModel flags, List<ValueTriggerContainer> containers)
    {
        _flags = flags;

        foreach (var container in containers)
        {
            if (container == null || string.IsNullOrEmpty(container.SourceID)) continue;
            _containers[container.SourceID] = container;
            _currentValues[container.SourceID] = 0;
        }
    }

    /// <summary>
    /// 當來源數值變化時呼叫此方法
    /// </summary>
    /// <param name="sourceID">來源識別（如 "Phase", "DayOfWeek"）</param>
    /// <param name="newValue">新的數值</param>
    /// <param name="isRefresh">是否為讀檔/初始化時的刷新（影響 FireAndForget 行為）</param>
    public void OnValueChanged(string sourceID, int newValue, bool isRefresh = false)
    {
        if (!_containers.TryGetValue(sourceID, out var container)) return;

        _currentValues[sourceID] = newValue;

        // 1. 同步數值（如果有設定 SyncValueSO）
        if (container.SyncValueSO != null)
        {
            _flags.SetValue(container.SyncValueSO.FlagID, newValue);
        }

        // 2. 執行規則判定
        ExecuteRules(container.Rules, newValue, isRefresh);
    }

    /// <summary>
    /// 執行規則判定（支援智慧 Value 競爭與 Flag 互斥）
    /// </summary>
    private void ExecuteRules(List<StatTriggerRule> rules, int currentValue, bool isRefresh)
    {
        if (rules == null || rules.Count == 0) return;

        // 依據目標 SO 分組，確保同一個 Flag/Value 的規則會一起判斷
        var groups = rules.GroupBy(r => r.ProgressSO);

        foreach (var group in groups)
        {
            var targetSO = group.Key;
            if (targetSO == null) continue;

            string key = targetSO.FlagID;

            // 找出滿足條件的所有規則
            var metRules = group.Where(r => r.Evaluate(currentValue)).ToList();

            if (targetSO is ProgressValueDefinition valSO)
            {
                // ═══ Value 處理：取滿足條件中 TargetValue 最高的 ═══
                var bestRule = metRules.OrderByDescending(r => r.TargetValue).FirstOrDefault();

                if (bestRule != null)
                {
                    // 讀檔重整時不處理 FireAndForget，避免強制覆蓋玩家在劇情中手動關閉的標記
                    if (isRefresh && bestRule.Behavior == TriggerBehavior.FireAndForget) continue;
                    _flags.SetValue(key, bestRule.TargetValue);
                }
                else
                {
                    // 沒有規則滿足：檢查是否有 Toggle 需要回落到預設值
                    bool hasToggle = group.Any(r => r.Behavior == TriggerBehavior.Toggle);
                    if (hasToggle && !isRefresh)
                    {
                        _flags.SetValue(key, valSO.DefaultValue);
                    }
                }
            }
            else
            {
                // ═══ Flag 處理 ═══
                foreach (var rule in group)
                {
                    // 讀檔重整時不處理 FireAndForget
                    if (isRefresh && rule.Behavior == TriggerBehavior.FireAndForget) continue;

                    bool isMet = rule.Evaluate(currentValue);

                    if (isMet)
                    {
                        _flags.AddPersistentFlag(key);
                    }
                    else if (rule.Behavior == TriggerBehavior.Toggle && !isRefresh)
                    {
                        _flags.RemoveFlag(key);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 讀檔或啟動時呼叫,刷新所有來源的規則
    /// </summary>
    public void RefreshAll()
    {
        //  修正:先快照 key,避免 OnValueChanged 回寫 _currentValues 造成迭代衝突
        //   (OnValueChanged → SetValue → 觸發事件 → 其他訂閱者又呼叫 OnValueChanged
        //    → 寫入 _currentValues → 迭代器 version 變動 → 崩潰)
        var snapshot = _currentValues.ToList();
        foreach (var kvp in snapshot)
        {
            OnValueChanged(kvp.Key, kvp.Value, isRefresh: true);
        }
    }

    /// <summary>
    /// 刷新特定來源
    /// </summary>
    public void Refresh(string sourceID)
    {
        if (_currentValues.TryGetValue(sourceID, out int value))
        {
            OnValueChanged(sourceID, value, isRefresh: true);
        }
    }
}
