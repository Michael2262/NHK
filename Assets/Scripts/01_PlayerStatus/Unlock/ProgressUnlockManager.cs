using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 監聽女主角 (Libido / Trust / HCount) 與主角 (Stress / LifePower / Sociality / Dependency)
/// 的數值變化，依照所有 Rule Asset 對 ProgressFlagModel 做對應的開關 / 附值操作。
///
/// 使用方式：在 GameStatusService 中傳入一個 List&lt;ProgressUnlockConfig&gt;，
///          Manager 會把所有 Config 內的 Rule 攤平並依條件來源建立索引。
///
/// 設計原則：
/// - Config 只是分組容器，不提供權威資訊
/// - Rule 上的 heroineID 與 conditions 才是真正的權威
/// - 一個 Rule 可以同時出現在多個 Config (雖然少見)，Manager 會自動去重避免重複評估
/// - 一個 Rule 可以同時含女主角與主角條件，任一來源變化都會觸發重評
/// </summary>
public class ProgressUnlockManager
{
    private readonly ProgressFlagModel _progress;
    private readonly Dictionary<string, HeroineStatusModel> _heroines;
    private readonly ProtagonistStatusModel _protagonist;

    // 以 heroineID 為 key，value 是含該女主角條件的所有 Rule (去重後)
    private readonly Dictionary<string, List<ProgressUnlockRuleAsset>> _rulesByHeroine
        = new Dictionary<string, List<ProgressUnlockRuleAsset>>();

    // 含主角條件的所有 Rule (去重後)
    private readonly List<ProgressUnlockRuleAsset> _protagonistRules
        = new List<ProgressUnlockRuleAsset>();

    // 紀錄每條 Rule「目前是否已套用」(用 Rule 本身為 key；Rule 是 ScriptableObject，引用即唯一)
    private readonly Dictionary<ProgressUnlockRuleAsset, bool> _ruleApplied
        = new Dictionary<ProgressUnlockRuleAsset, bool>();

    // 每位女主角共用一個 handler，掛在 Libido / Trust / HCount 三個事件上 (方便解除訂閱)
    private readonly Dictionary<string, Action<int>> _heroineHandlers
        = new Dictionary<string, Action<int>>();

    // 主角的 handler，掛在 Stress / LifePower / Sociality / Dependency 四個事件上
    private Action<int> _protagonistHandler;

    public ProgressUnlockManager(
        ProgressFlagModel progress,
        Dictionary<string, HeroineStatusModel> heroines,
        ProtagonistStatusModel protagonist,
        List<ProgressUnlockConfig> configs)
    {
        _progress = progress;
        _heroines = heroines;
        _protagonist = protagonist;

        if (configs == null || configs.Count == 0)
        {
            Debug.LogWarning("[ProgressUnlockManager] 未傳入任何 ProgressUnlockConfig，解鎖規則不會啟動。");
            return;
        }

        // 攤平所有 Rule，依條件來源建立索引
        // 使用 HashSet 避免同一個 Rule 被放進多個 Config 時重複評估
        var seen = new HashSet<ProgressUnlockRuleAsset>();

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

                if (rule.conditions == null || rule.conditions.Count == 0)
                {
                    Debug.LogWarning(
                        $"[ProgressUnlockManager] Rule '{rule.name}' 沒有設定任何條件，已跳過。"
                    );
                    continue;
                }

                bool indexed = false;

                if (rule.HasHeroineCondition)
                {
                    if (string.IsNullOrEmpty(rule.heroineID))
                    {
                        Debug.LogWarning(
                            $"[ProgressUnlockManager] Rule '{rule.name}' 含女主角條件但未設定 heroineID，已跳過。"
                        );
                        continue;
                    }

                    if (!_rulesByHeroine.TryGetValue(rule.heroineID, out var list))
                    {
                        list = new List<ProgressUnlockRuleAsset>();
                        _rulesByHeroine[rule.heroineID] = list;
                    }
                    list.Add(rule);
                    indexed = true;
                }

                if (rule.HasProtagonistCondition)
                {
                    _protagonistRules.Add(rule);
                    indexed = true;
                }

                if (indexed)
                    _ruleApplied[rule] = false;
            }
        }

        SubscribeAll();
    }

    // ==========================================================
    // 訂閱數值變化事件
    // ==========================================================
    private void SubscribeAll()
    {
        // ── 女主角：Libido / Trust / HCount ──
        foreach (var kv in _heroines)
        {
            string id = kv.Key;
            HeroineStatusModel model = kv.Value;

            // 沒有任何 Rule 對應此女主角的話，不用訂閱
            if (!_rulesByHeroine.ContainsKey(id)) continue;

            Action<int> handler = (_) => EvaluateRulesForHeroine(id);

            model.OnLibidoChanged += handler;
            model.OnTrustChanged += handler;
            model.OnHCountChanged += handler;

            _heroineHandlers[id] = handler;
        }

        // ── 主角：Stress / LifePower / Sociality / Dependency ──
        if (_protagonistRules.Count > 0 && _protagonist != null)
        {
            _protagonistHandler = (_) => EvaluateProtagonistRules();

            _protagonist.OnStressChanged += _protagonistHandler;
            _protagonist.OnLifePowerChanged += _protagonistHandler;
            _protagonist.OnSocialityChanged += _protagonistHandler;
            _protagonist.OnDependencyChanged += _protagonistHandler;
        }
    }

    /// <summary>
    /// 解除所有訂閱 (在重建 Heroines 或遊戲結束時呼叫)
    /// </summary>
    public void UnsubscribeAll()
    {
        foreach (var kv in _heroines)
        {
            if (!_heroineHandlers.TryGetValue(kv.Key, out var handler)) continue;

            kv.Value.OnLibidoChanged -= handler;
            kv.Value.OnTrustChanged -= handler;
            kv.Value.OnHCountChanged -= handler;
        }
        _heroineHandlers.Clear();

        if (_protagonistHandler != null && _protagonist != null)
        {
            _protagonist.OnStressChanged -= _protagonistHandler;
            _protagonist.OnLifePowerChanged -= _protagonistHandler;
            _protagonist.OnSocialityChanged -= _protagonistHandler;
            _protagonist.OnDependencyChanged -= _protagonistHandler;
        }
        _protagonistHandler = null;
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
        var keys = new List<ProgressUnlockRuleAsset>(_ruleApplied.Keys);
        foreach (var k in keys) _ruleApplied[k] = false;

        foreach (var rule in keys)
            EvaluateRule(rule);
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

    private void EvaluateProtagonistRules()
    {
        foreach (var rule in _protagonistRules)
            EvaluateRule(rule);
    }

    private void EvaluateRule(ProgressUnlockRuleAsset rule)
    {
        if (rule == null) return;

        // OnlyCondition 類型只用於 UI 阻擋，不執行任何 Progress 動作
        if (rule.action == ProgressActionType.OnlyCondition) return;

        if (rule.target == null)
        {
            Debug.LogWarning(
                $"[ProgressUnlockManager] 規則 '{rule.name}' 的 target 未設定且動作不是 OnlyCondition，已跳過。"
            );
            return;
        }

        // 含女主角條件時必須找得到該女主角；純主角條件的規則 heroine 為 null 也能評估
        HeroineStatusModel heroine = null;
        if (rule.HasHeroineCondition
            && !_heroines.TryGetValue(rule.heroineID, out heroine))
        {
            return;
        }

        bool meetsCondition = rule.IsConditionMet(heroine, _protagonist);
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
    private void ApplyAction(ProgressUnlockRuleAsset rule, bool meet)
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
