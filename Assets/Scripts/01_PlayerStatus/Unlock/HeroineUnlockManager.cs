using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 監聽女主角的 Lewdness / Affinity 變化，依照所有 Rule Asset
/// 對 ProgressFlagModel 做對應的開關 / 附值操作。
///
/// 使用方式：在 GameStatusService 中傳入一個 List&lt;HeroineUnlockConfig&gt;，
///          Manager 會把所有 Config 內的 Rule 攤平並按 Rule.heroineID 分組索引。
///
/// 設計原則：
/// - Config 只是分組容器，不提供權威資訊
/// - Rule 上的 heroineID 才是真正的權威
/// - 一個 Rule 可以同時出現在多個 Config (雖然少見)，Manager 會自動去重避免重複評估
/// </summary>
public class HeroineUnlockManager
{
    private readonly ProgressFlagModel _progress;
    private readonly Dictionary<string, HeroineStatusModel> _heroines;

    // 以 heroineID 為 key，value 是該女主角相關的所有 Rule (去重後)
    private readonly Dictionary<string, List<HeroineUnlockRuleAsset>> _rulesByHeroine
        = new Dictionary<string, List<HeroineUnlockRuleAsset>>();

    // 紀錄每條 Rule「目前是否已套用」(用 Rule 本身為 key；Rule 是 ScriptableObject，引用即唯一)
    private readonly Dictionary<HeroineUnlockRuleAsset, bool> _ruleApplied
        = new Dictionary<HeroineUnlockRuleAsset, bool>();

    // 每個女主角的事件 handler (方便解除訂閱)
    private readonly Dictionary<string, Action<int>> _lewdHandlers
        = new Dictionary<string, Action<int>>();
    private readonly Dictionary<string, Action<int>> _affinityHandlers
        = new Dictionary<string, Action<int>>();

    public HeroineUnlockManager(
        ProgressFlagModel progress,
        Dictionary<string, HeroineStatusModel> heroines,
        List<HeroineUnlockConfig> configs)
    {
        _progress = progress;
        _heroines = heroines;

        if (configs == null || configs.Count == 0)
        {
            Debug.LogWarning("[HeroineUnlockManager] 未傳入任何 HeroineUnlockConfig，解鎖規則不會啟動。");
            return;
        }

        // 攤平所有 Rule，按 Rule.heroineID 分組
        // 使用 HashSet 避免同一個 Rule 被放進多個 Config 時重複評估
        var seen = new HashSet<HeroineUnlockRuleAsset>();

        foreach (var cfg in configs)
        {
            if (cfg == null || cfg.rules == null) continue;

            foreach (var rule in cfg.rules)
            {
                if (rule == null) continue;
                if (!seen.Add(rule))
                {
                    // 已經看過這個 Rule，跳過 (另一個 Config 也引用了它)
                    continue;
                }

                if (string.IsNullOrEmpty(rule.heroineID))
                {
                    Debug.LogWarning(
                        $"[HeroineUnlockManager] Rule '{rule.name}' 未設定 heroineID，已跳過。"
                    );
                    continue;
                }

                if (!_rulesByHeroine.TryGetValue(rule.heroineID, out var list))
                {
                    list = new List<HeroineUnlockRuleAsset>();
                    _rulesByHeroine[rule.heroineID] = list;
                }
                list.Add(rule);
                _ruleApplied[rule] = false;
            }
        }

        SubscribeToHeroines();
    }

    // ==========================================================
    // 訂閱女主角事件
    // ==========================================================
    private void SubscribeToHeroines()
    {
        foreach (var kv in _heroines)
        {
            string id = kv.Key;
            HeroineStatusModel model = kv.Value;

            // 沒有任何 Rule 對應此女主角的話，不用訂閱
            if (!_rulesByHeroine.ContainsKey(id)) continue;

            Action<int> lewdHandler = (_) => EvaluateRulesForHeroine(id);
            Action<int> affinityHandler = (_) => EvaluateRulesForHeroine(id);

            model.OnLewdnessChanged += lewdHandler;
            model.OnAffinityChanged += affinityHandler;

            _lewdHandlers[id] = lewdHandler;
            _affinityHandlers[id] = affinityHandler;
        }
    }

    /// <summary>
    /// 解除所有訂閱 (在重建 Heroines 或遊戲結束時呼叫)
    /// </summary>
    public void UnsubscribeFromHeroines()
    {
        foreach (var kv in _heroines)
        {
            string id = kv.Key;
            if (_lewdHandlers.TryGetValue(id, out var lh))
                kv.Value.OnLewdnessChanged -= lh;
            if (_affinityHandlers.TryGetValue(id, out var ah))
                kv.Value.OnAffinityChanged -= ah;
        }
        _lewdHandlers.Clear();
        _affinityHandlers.Clear();
    }

    // ==========================================================
    // 讀檔 / 新遊戲時呼叫：刷新所有規則狀態
    // ==========================================================

    /// <summary>
    /// 檢查所有規則的當前達成狀態，並套用對應動作。
    /// 通常在遊戲載入完成、女主角字典重建後呼叫。
    /// </summary>
    public void RefreshAllRules()
    {
        var keys = new List<HeroineUnlockRuleAsset>(_ruleApplied.Keys);
        foreach (var k in keys) _ruleApplied[k] = false;

        foreach (var kv in _rulesByHeroine)
        {
            foreach (var rule in kv.Value)
                EvaluateRule(rule);
        }
    }

    // ==========================================================
    // 核心：評估規則
    // ==========================================================

    private void EvaluateRulesForHeroine(string heroineID)
    {
        if (!_rulesByHeroine.TryGetValue(heroineID, out var list)) return;

        foreach (var rule in list)
            EvaluateRule(rule);
    }

    private void EvaluateRule(HeroineUnlockRuleAsset rule)
    {
        if (rule == null) return;

        // OnlyCondition 類型只用於 UI 阻擋，不執行任何 Progress 動作
        if (rule.action == ProgressActionType.OnlyCondition) return;

        if (rule.target == null)
        {
            Debug.LogWarning(
                $"[HeroineUnlockManager] 規則 '{rule.name}' 的 target 未設定且動作不是 OnlyCondition，已跳過。"
            );
            return;
        }

        if (!_heroines.TryGetValue(rule.heroineID, out var heroine)) return;

        bool meetsCondition = rule.IsConditionMet(heroine);
        _ruleApplied.TryGetValue(rule, out bool isApplied);

        if (meetsCondition && !isApplied)
        {
            ApplyAction(rule, true);
            _ruleApplied[rule] = true;
        }
        else if (!meetsCondition && isApplied && rule.revertWhenConditionFails)
        {
            ApplyAction(rule, false);
            _ruleApplied[rule] = false;
        }
    }

    /// <summary>
    /// 套用 (meet=true) 或撤銷 (meet=false) 規則
    /// </summary>
    private void ApplyAction(HeroineUnlockRuleAsset rule, bool meet)
    {
        string flagID = rule.target.FlagID;

        switch (rule.action)
        {
            case ProgressActionType.SetFlagOn:
                if (meet) _progress.AddPersistentFlag(flagID);
                else _progress.RemoveFlag(flagID);
                break;

            case ProgressActionType.SetFlagOff:
                if (meet) _progress.RemoveFlag(flagID);
                else _progress.AddPersistentFlag(flagID);
                break;

            case ProgressActionType.SetValue:
                if (meet) _progress.SetValue(flagID, rule.valueToSet);
                else _progress.SetValue(flagID, 0);
                break;

            case ProgressActionType.OnlyCondition:
                // 不該走到這裡 (已在 EvaluateRule 前置過濾)
                break;
        }
    }
}
