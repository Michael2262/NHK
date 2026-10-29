using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>互斥行程核心。Flag 是唯一持久狀態；行程 ID 只用於呼叫與設定。</summary>
public sealed class HeroineFlagScheduleModel : IDisposable
{
    private sealed class Entry
    {
        public string ID, Name, FlagID, Location;
        public string[] Activities;
    }

    private readonly ProgressFlagModel _flags;
    private readonly CurrentScenarioModel _scenario;
    private readonly Action<string> _warning;
    private readonly Func<int, int> _pickIndex;
    private readonly Dictionary<string, List<Entry>> _rules = new Dictionary<string, List<Entry>>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _configErrors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _reported = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _applied = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _dependents = new Dictionary<string, HashSet<string>>();
    private readonly HashSet<string> _dirty = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _reapply = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private int _batchDepth;
    private bool _restoring, _flushing, _switching;

    public bool IsSuspended => _restoring || _batchDepth > 0;
    public bool IsRestoring => _restoring;
    public int BatchDepth => _batchDepth;
    public string LastError { get; private set; }

    public HeroineFlagScheduleModel(ProgressFlagModel flags, CurrentScenarioModel scenario,
        IEnumerable<string> heroineIDs, IEnumerable<HeroineFlagScheduleConfig> configs, Action<string> warning = null,
        Func<int, int> pickIndex = null)
    {
        _flags = flags ?? throw new ArgumentNullException(nameof(flags));
        _scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
        _warning = warning;
        _pickIndex = pickIndex ?? new Func<int, int>(new Random().Next);
        foreach (string id in heroineIDs ?? Enumerable.Empty<string>())
            if (!string.IsNullOrWhiteSpace(id)) _rules[id] = new List<Entry>();
        var configured = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // 跨角色也不能共用狀態 Flag，否則對其中一人操作就會更動另一人的行程。
        var owners = new Dictionary<string, string>();
        foreach (var config in configs ?? Enumerable.Empty<HeroineFlagScheduleConfig>())
        {
            if (config == null) continue;
            string heroine = config.heroineID;
            if (string.IsNullOrWhiteSpace(heroine) || !_rules.ContainsKey(heroine))
            {
                _warning?.Invoke($"[Flag 行程] 規則表「{config.name}」的女主角不在正式名單內，已略過。");
                continue;
            }
            if (!configured.Add(heroine))
                _configErrors[heroine] = "同一女主角指定了多份行程表。";
            _defaults[heroine] = config.defaultRuleID;
            var ids = new HashSet<string>();
            var activities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var rule in config.rules ?? new List<HeroineFlagScheduleRule>())
            {
                if (rule == null)
                {
                    _configErrors[heroine] = "行程資料不可為空。";
                    continue;
                }
                if (string.IsNullOrWhiteSpace(rule.ruleID) || !ids.Add(rule.ruleID))
                    _configErrors[heroine] = "行程 ID 缺少或重複。";
                if (rule.stateFlag == null)
                {
                    _configErrors[heroine] = $"行程「{rule.ruleName}」未指定必填的狀態 Flag。";
                    continue;
                }
                string flag = rule.stateFlag.FlagID;
                if (string.IsNullOrWhiteSpace(flag))
                {
                    _configErrors[heroine] = "狀態 Flag 的 ID 不可為空。";
                    continue;
                }
                if (owners.TryGetValue(flag, out var other))
                {
                    _configErrors[heroine] = _configErrors[other] = $"狀態 Flag「{flag}」被多個行程共用。";
                }
                else owners.Add(flag, heroine);
                if (string.IsNullOrWhiteSpace(rule.locationID))
                    _configErrors[heroine] = $"行程「{rule.ruleName}」缺少地點。";
                var activityIDs = rule.GetActivityIDs();
                if (activityIDs.Length == 0)
                    _configErrors[heroine] = $"行程「{rule.ruleName}」至少需要一個活動 ID。";
                foreach (string activity in activityIDs)
                {
                    if (string.IsNullOrWhiteSpace(activity) || !activities.Add(activity))
                        _configErrors[heroine] = $"行程「{rule.ruleName}」的活動 ID 為空或在同角色內重複：{activity}";
                }
                _rules[heroine].Add(new Entry { ID = rule.ruleID, Name = rule.ruleName, FlagID = flag,
                    Location = rule.locationID, Activities = activityIDs });
                if (!_dependents.TryGetValue(flag, out var dependents))
                    _dependents[flag] = dependents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                dependents.Add(heroine);
            }
        }
        foreach (string id in _rules.Keys)
        {
            if (!configured.Contains(id) || _rules[id].Count == 0) _configErrors[id] = "未設定可用的行程表。";
            _dirty.Add(id);
        }
        _flags.OnFlagChanged += HandleFlagChanged;
    }

    public void Dispose() => _flags.OnFlagChanged -= HandleFlagChanged;

    private void HandleFlagChanged(string flag, bool value)
    {
        if (flag != null && _dependents.TryGetValue(flag, out var ids)) _dirty.UnionWith(ids);
    }

    private bool Fail(string message)
    {
        LastError = message;
        _warning?.Invoke("[Flag 行程] " + message);
        return false;
    }

    /// <summary>編輯器／程式用：指定行程 ID 或 Flag ID，從該行程抽選活動。</summary>
    public bool TryActivate(string heroineID, string ruleIDOrFlagID)
    {
        if (!CanActivate(heroineID, out var rules)) return false;
        var matches = rules.Where(r => r.ID == ruleIDOrFlagID || r.FlagID == ruleIDOrFlagID).ToList();
        if (matches.Count != 1) return Fail("找不到唯一的目標行程，請使用行程 ID 或狀態 Flag ID。");
        return Activate(heroineID, rules, matches[0], null);
    }

    /// <summary>指定活動並同步切換其所屬行程的互斥 Flag；本次不抽選活動。</summary>
    public bool TryActivateActivity(string heroineID, string activityID)
    {
        if (!CanActivate(heroineID, out var rules)) return false;
        if (string.IsNullOrWhiteSpace(activityID)) return Fail("請指定活動 ID。");
        var matches = rules.Where(r => r.Activities.Contains(activityID)).ToList();
        if (matches.Count != 1) return Fail($"女主角「{heroineID}」找不到唯一對應的活動「{activityID}」，未切換行程。");
        return Activate(heroineID, rules, matches[0], activityID);
    }

    /// <summary>只接受此角色的專用 Flag ID；每次呼叫從所屬行程抽選一次活動。</summary>
    public bool TryActivateFlag(string heroineID, string flagID)
    {
        if (!CanActivate(heroineID, out var rules)) return false;
        if (string.IsNullOrWhiteSpace(flagID)) return Fail("請指定狀態 Flag ID。");
        var target = rules.FirstOrDefault(r => r.FlagID == flagID);
        if (target == null) return Fail($"女主角「{heroineID}」沒有對應的行程 Flag「{flagID}」，未切換行程。");
        return Activate(heroineID, rules, target, null);
    }

    /// <summary>離場：關閉同角色所有行程 Flag 並移除位置與活動；讀檔仍維持離場。</summary>
    public bool TryDeactivate(string heroineID)
    {
        if (!CanActivate(heroineID, out var rules)) return false;
        return Activate(heroineID, rules, null, null);
    }

    private bool CanActivate(string heroineID, out List<Entry> rules)
    {
        rules = null;
        if (IsSuspended || _flushing || _switching) return Fail("行程正在載入、批次更新或通知中，無法重入切換。");
        if (string.IsNullOrWhiteSpace(heroineID) || !_rules.TryGetValue(heroineID, out rules))
            return Fail("找不到指定的女主角。");
        if (_configErrors.TryGetValue(heroineID, out var error)) return Fail(heroineID + "：" + error);
        return true;
    }

    private bool Activate(string heroineID, List<Entry> rules, Entry target, string activityID)
    {
        LastError = null;
        bool changed;
        _switching = true;
        try
        {
            // 不先通知：Flag 訂閱者執行時，位置與整組 Flag 都已是完整的新狀態。
            changed = Apply(heroineID, target, activityID);
            _reapply.Remove(heroineID);
            if (target == null) _flags.ClearExclusiveFlags(rules.Select(r => r.FlagID));
            else _flags.SetExclusivePersistentFlag(rules.Select(r => r.FlagID), target.FlagID);
            _dirty.Add(heroineID);
        }
        finally { _switching = false; }
        changed |= FlushPending(notify: false);
        _switching = true;
        try { if (changed) _scenario.NotifyScenarioRecalculated(); }
        finally { _switching = false; }
        var actual = ResolveActive(heroineID, out string error);
        if (error != null || actual?.ID != target?.ID) return Fail(error ?? "其他 Flag 訂閱者改變了切換結果。");
        LastError = null;
        return true;
    }

    /// <summary>只供新遊戲／直接 Play 初始化；讀檔絕不使用預設行程補回缺少的 Flag。</summary>
    public void InitializeNewGame()
    {
        _switching = true;
        try
        {
            foreach (var pair in _rules)
            {
                if (_configErrors.ContainsKey(pair.Key)) continue;
                _defaults.TryGetValue(pair.Key, out string defaultID);
                var target = pair.Value.FirstOrDefault(r => r.ID == defaultID);
                if (target == null)
                {
                    Fail(pair.Key + " 未指定有效的新遊戲預設行程。");
                    continue;
                }
                _flags.SetExclusivePersistentFlag(pair.Value.Select(r => r.FlagID), target.FlagID);
            }
            _applied.Clear();
            _dirty.UnionWith(_rules.Keys);
        }
        finally { _switching = false; }
        FlushPending();
    }

    public void BeginBatch() => _batchDepth++;
    public void EndBatch()
    {
        if (_batchDepth <= 0) return;
        _batchDepth--;
        FlushPending();
    }

    public void BeginRestore()
    {
        _restoring = true;
        _batchDepth = 0;
        _dirty.Clear();
        _reapply.Clear();
        _applied.Clear();
        _reported.Clear();
    }

    public void CompleteRestore(bool notify = true)
    {
        _restoring = false;
        // 永遠覆寫存檔內的地點快照；唯一來源是已還原的 Flag。
        Refresh(notify);
    }

    /// <summary>明確依 Flag 重新套用行程並抽選活動；不讀取舊的行程 ID 存檔。</summary>
    public void Refresh(bool notify = true)
    {
        _dirty.UnionWith(_rules.Keys);
        _reapply.UnionWith(_rules.Keys);
        FlushPending(notify);
    }

    public void PrepareForSave()
    {
        if (IsSuspended || _flushing || _switching) throw new InvalidOperationException("行程仍在批次、切換或載入中，請完成後再存檔。");
        for (int i = 0; i < 16 && _dirty.Count > 0; i++) FlushPending();
        if (_dirty.Count > 0) throw new InvalidOperationException("行程 Flag 持續變動，請檢查互相觸發的條件。");
        foreach (string id in _rules.Keys)
        {
            ResolveActive(id, out string error);
            if (error != null) throw new InvalidOperationException(id + "：" + error);
        }
    }

    public bool FlushPending(bool notify = true)
    {
        if (IsSuspended || _flushing || _switching || _dirty.Count == 0) return false;
        _flushing = true;
        try
        {
            bool changed = false;
            foreach (string id in _dirty.OrderBy(x => x, StringComparer.Ordinal).ToArray())
            {
                _dirty.Remove(id);
                bool reapply = _reapply.Remove(id);
                var active = ResolveActive(id, out string error);
                if (error != null)
                {
                    if (!_reported.TryGetValue(id, out string previous) || previous != error)
                    {
                        _reported[id] = error;
                        _warning?.Invoke($"[Flag 行程] {id}：{error} 角色不套用任何行程，請修正設定或呼叫切換行程。");
                    }
                }
                else _reported.Remove(id);
                if (!reapply && _applied.TryGetValue(id, out string last) && last == active?.ID) continue;
                changed |= Apply(id, active);
            }
            if (changed && notify) _scenario.NotifyScenarioRecalculated();
            return changed;
        }
        finally { _flushing = false; }
    }

    private bool Apply(string heroine, Entry active, string activityID = null)
    {
        string activity = active == null ? null : activityID ?? active.Activities[
            active.Activities.Length == 1 ? 0 : _pickIndex(active.Activities.Length)];
        _applied[heroine] = active?.ID;
        return _scenario.ReplaceHeroineState(heroine, active?.Location, activity);
    }

    private Entry ResolveActive(string heroine, out string error)
    {
        if (_configErrors.TryGetValue(heroine, out error)) return null;
        var active = _rules[heroine].Where(r => _flags.Contains(r.FlagID)).ToList();
        // 零個 ON 是合法的離場狀態，仍完全由 Flag 表達，不另存離場布林。
        if (active.Count == 0) { error = null; return null; }
        if (active.Count > 1)
        {
            error = "同時有多個行程 Flag 開啟：" + string.Join("、", active.Select(r => r.FlagID));
            return null;
        }
        if (!_flags.IsPersistentFlag(active[0].FlagID))
        {
            error = "行程狀態必須是永久 Flag，請使用切換行程入口設定。";
            return null;
        }
        error = null;
        return active[0];
    }

    public string GetActiveRuleID(string heroineID)
        => !string.IsNullOrEmpty(heroineID) && _rules.ContainsKey(heroineID) ? ResolveActive(heroineID, out _)?.ID : null;

    public string Describe(string heroineID)
    {
        if (string.IsNullOrEmpty(heroineID) || !_rules.ContainsKey(heroineID)) return "不在正式女主角名單內";
        var active = ResolveActive(heroineID, out string error);
        if (error != null) return error;
        return active == null ? "目前離場：所有行程 Flag 皆為 OFF。"
            : $"目前行程：{active.Name}；狀態 Flag：{active.FlagID}（ON）";
    }
}
