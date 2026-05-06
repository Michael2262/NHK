using System;
using System.Collections.Generic;

public enum FlagLifetime

{

    Persistent,    // 永久：存入存檔，除非手動移除否則永遠存在
    Scene,         // 場景：切換場景即消失 (目前的 Temporary 邏輯)
    UntilNextSlot, // 時段：只要時間推進 (任何 Slot 消耗) 就消失
    UntilNextPhase,// 階段：進入下一個 Phase (如白天進入黃昏) 就消失
    UntilNextDay   // 每日：跨日 (DayPassed) 就消失

}

public class ProgressFlagModel
{
    // ───── 儲存容器 ─────
    private readonly HashSet<string> _persistentFlags = new HashSet<string>();
    private readonly HashSet<string> _sceneFlags = new HashSet<string>();
    private readonly HashSet<string> _slotFlags = new HashSet<string>();
    private readonly HashSet<string> _phaseFlags = new HashSet<string>();
    private readonly HashSet<string> _dayFlags = new HashSet<string>();
    private readonly Dictionary<string, int> _variables = new Dictionary<string, int>();

    // ───── 事件 ─────
    public event Action<string, bool> OnFlagChanged;
    public event Action<string, int> OnVariableChanged;

    // ───── 數值 API ─────

    public void SetValue(string key, int value)
    {
        if (string.IsNullOrEmpty(key)) return;

        if (!_variables.ContainsKey(key) || _variables[key] != value)
        {
            _variables[key] = value;
            OnVariableChanged?.Invoke(key, value);
            // 相容性：數字 > 0 視為 true
            OnFlagChanged?.Invoke(key, value > 0);
        }
    }

    public int GetValue(string key) => _variables.TryGetValue(key, out int val) ? val : 0;

    public void AddValue(string key, int amount) => SetValue(key, GetValue(key) + amount);

    // ───── 核心布林 API ─────

    public void AddFlag(string flag, FlagLifetime lifetime)
    {
        if (string.IsNullOrEmpty(flag)) return;

        bool changed = lifetime switch
        {
            FlagLifetime.Persistent => _persistentFlags.Add(flag),
            FlagLifetime.Scene => _sceneFlags.Add(flag),
            FlagLifetime.UntilNextSlot => _slotFlags.Add(flag),
            FlagLifetime.UntilNextPhase => _phaseFlags.Add(flag),
            FlagLifetime.UntilNextDay => _dayFlags.Add(flag),
            _ => false
        };

        if (changed) OnFlagChanged?.Invoke(flag, true);
    }

    /// <summary> 移除標記 (包含布林與數值) </summary>
    public void RemoveFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag)) return;

        // 移除所有桶子中的布林標記
        bool removedFromSets = _persistentFlags.Remove(flag) || _sceneFlags.Remove(flag) ||
                               _slotFlags.Remove(flag) || _phaseFlags.Remove(flag) || _dayFlags.Remove(flag);

        // ★ 補齊：也移除數值字典中的記錄
        bool removedFromVars = _variables.Remove(flag);

        if (removedFromSets || removedFromVars)
        {
            OnFlagChanged?.Invoke(flag, false);
            if (removedFromVars) OnVariableChanged?.Invoke(flag, 0);
        }
    }

    /// <summary> 檢查開關 (包含布林桶子與數值是否 > 0) </summary>
    public bool Contains(string flag)
    {
        if (string.IsNullOrEmpty(flag)) return false;

        // 數值 > 0 也視為包含此 Flag (為了舊腳本相容)
        if (_variables.TryGetValue(flag, out int val) && val > 0) return true;

        return _persistentFlags.Contains(flag) || _sceneFlags.Contains(flag) ||
               _slotFlags.Contains(flag) || _phaseFlags.Contains(flag) || _dayFlags.Contains(flag);
    }

    // ───── 語法糖 ─────
    public void AddPersistentFlag(string flag) => AddFlag(flag, FlagLifetime.Persistent);
    public void AddSceneFlag(string flag) => AddFlag(flag, FlagLifetime.Scene);
    public void AddSlotFlag(string flag) => AddFlag(flag, FlagLifetime.UntilNextSlot);
    public void AddPhaseFlag(string flag) => AddFlag(flag, FlagLifetime.UntilNextPhase);
    public void AddDailyFlag(string flag) => AddFlag(flag, FlagLifetime.UntilNextDay);

    // ───── 清理與存檔 ─────

    /// <summary>
    /// 清除指定桶子中的所有 flag,並為「真正消失」的 flag 發出 OnFlagChanged(false)。
    /// 「真正消失」的定義:清除後,該 flag 不再存在於任何其他桶子或 variables 中。
    /// </summary>
    private void ClearBucketWithEvents(HashSet<string> bucket)
    {
        if (bucket == null || bucket.Count == 0) return;

        // 1. 快照要被清掉的 flag 名單
        var snapshot = new List<string>(bucket);

        // 2. 實際清空桶子
        bucket.Clear();

        // 3. 對每個被清掉的 flag,檢查是否還存在於其他地方
        //    若不存在了 → 發 false 事件
        //    (Contains 會掃 variables + 其他 4 個桶子)
        foreach (var flag in snapshot)
        {
            if (!Contains(flag))
                OnFlagChanged?.Invoke(flag, false);
        }
    }

    public void ClearSceneFlags() => ClearBucketWithEvents(_sceneFlags);
    public void ClearSlotFlags() => ClearBucketWithEvents(_slotFlags);
    public void ClearPhaseFlags() => ClearBucketWithEvents(_phaseFlags);
    public void ClearDayFlags() => ClearBucketWithEvents(_dayFlags);

    public void OnSceneChanged() => ClearSceneFlags();

    /// <summary>
    /// 新遊戲:清空所有資料,並為每個原本存在的 flag/variable 發出消失事件。
    /// 這樣任何訂閱者 (如 Indicator) 可以正確刷新到「一切歸零」的狀態。
    /// </summary>
    public void NewGame()
    {
        // 1. 先蒐集所有目前存在的 flag 和 variable key(用 HashSet 去重)
        var allFlags = new HashSet<string>();
        allFlags.UnionWith(_persistentFlags);
        allFlags.UnionWith(_sceneFlags);
        allFlags.UnionWith(_slotFlags);
        allFlags.UnionWith(_phaseFlags);
        allFlags.UnionWith(_dayFlags);

        var allVarKeys = new List<string>(_variables.Keys);

        // 2. 實際清空
        _persistentFlags.Clear(); _sceneFlags.Clear(); _slotFlags.Clear();
        _phaseFlags.Clear(); _dayFlags.Clear(); _variables.Clear();

        // 3. 廣播消失事件
        foreach (var flag in allFlags)
        {
            // 此時 Contains 會回 false(因為全清了),直接發事件
            OnFlagChanged?.Invoke(flag, false);
        }
        foreach (var key in allVarKeys)
        {
            OnVariableChanged?.Invoke(key, 0);
            // 若 variable key 不在 allFlags 裡(例如只有當過 variable 沒當過 flag),
            // 也要發一次 OnFlagChanged(false),保持相容性
            if (!allFlags.Contains(key))
                OnFlagChanged?.Invoke(key, false);
        }
    }

    public ProgressFlagSaveData ToSaveData()
    {
        // 建立存檔物件並填充資料
        var data = new ProgressFlagSaveData
        {
            ActiveFlags = new HashSet<string>(_persistentFlags),
            SlotFlags = new HashSet<string>(_slotFlags),    // 保存時段標記
            PhaseFlags = new HashSet<string>(_phaseFlags),  // 保存階段標記
            DayFlags = new HashSet<string>(_dayFlags)       // 保存每日標記
        };

        foreach (var kv in _variables)
            data.ActiveVariables.Add(new ProgressFlagSaveData.VariableEntry { Key = kv.Key, Value = kv.Value });

        return data;
    }

    public void LoadFromSaveData(ProgressFlagSaveData data)
    {
        NewGame();
        if (data == null) return;

        // 注意:載檔時刻意「不」逐項廣播 OnFlagChanged / OnVariableChanged。
        // 原因:GameStatusService 會在所有 Model 載完後統一發 OnGameStatusLoaded,
        //      讓各系統整批 refresh。中途逐項廣播會導致訂閱者看到「半殘的世界狀態」。

        // 還原永久標記
        if (data.ActiveFlags != null) _persistentFlags.UnionWith(data.ActiveFlags);

        // 還原時效性標記
        if (data.SlotFlags != null) _slotFlags.UnionWith(data.SlotFlags);
        if (data.PhaseFlags != null) _phaseFlags.UnionWith(data.PhaseFlags);
        if (data.DayFlags != null) _dayFlags.UnionWith(data.DayFlags);

        // 還原數值變數
        if (data.ActiveVariables != null)
            foreach (var entry in data.ActiveVariables) _variables[entry.Key] = entry.Value;
    }
}