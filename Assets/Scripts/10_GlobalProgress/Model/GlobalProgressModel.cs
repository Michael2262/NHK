using System;
using System.Collections.Generic;

/// <summary>
/// 全域進度 Model — 跨存檔的永久資料（畫廊解鎖、跳過開場、周回次數等）。
/// 獨立於 ProgressFlagModel，生命週期為「整個遊戲安裝期間」。
/// </summary>
public class GlobalProgressModel
{
    // ───── 儲存容器 ─────
    private readonly HashSet<string> _flags = new HashSet<string>();
    private readonly Dictionary<string, int> _variables = new Dictionary<string, int>();

    // ───── 事件 ─────
    public event Action<string, bool> OnFlagChanged;
    public event Action<string, int> OnVariableChanged;

    // ───── Flag API ─────

    /// <summary> 解鎖一個全域旗標（畫廊 CG、場景曾開啟等） </summary>
    public void UnlockFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag)) return;
        if (_flags.Add(flag))
            OnFlagChanged?.Invoke(flag, true);
    }

    /// <summary> 檢查是否已解鎖 </summary>
    public bool HasFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag)) return false;
        return _flags.Contains(flag);
    }

    /// <summary> 移除旗標（通常不需要，但保留彈性） </summary>
    public void RemoveFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag)) return;
        if (_flags.Remove(flag))
            OnFlagChanged?.Invoke(flag, false);
    }

    // ───── Value API ─────

    public void SetValue(string key, int value)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (!_variables.ContainsKey(key) || _variables[key] != value)
        {
            _variables[key] = value;
            OnVariableChanged?.Invoke(key, value);
        }
    }

    public int GetValue(string key) =>
        _variables.TryGetValue(key, out int val) ? val : 0;

    public void AddValue(string key, int amount) =>
        SetValue(key, GetValue(key) + amount);

    // ───── 清除（開發/測試用） ─────

    /// <summary> 完全清除所有全域進度（記憶體層） </summary>
    public void ResetAll()
    {
        _flags.Clear();
        _variables.Clear();
    }

    // ───── 序列化 ─────

    public GlobalProgressSaveData ToSaveData()
    {
        var data = new GlobalProgressSaveData
        {
            UnlockedFlags = new HashSet<string>(_flags)
        };

        foreach (var kv in _variables)
            data.Variables.Add(new GlobalProgressSaveData.VariableEntry
                { Key = kv.Key, Value = kv.Value });

        return data;
    }

    public void LoadFromSaveData(GlobalProgressSaveData data)
    {
        _flags.Clear();
        _variables.Clear();
        if (data == null) return;

        if (data.UnlockedFlags != null)
            _flags.UnionWith(data.UnlockedFlags);

        if (data.Variables != null)
            foreach (var entry in data.Variables)
                _variables[entry.Key] = entry.Value;
    }
}
