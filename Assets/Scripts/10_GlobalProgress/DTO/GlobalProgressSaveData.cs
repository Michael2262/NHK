using System;
using System.Collections.Generic;

/// <summary>
/// 全域進度的可序列化存檔資料。
/// 獨立於存檔槽，存放在單獨的 JSON 檔案中。
/// </summary>
[Serializable]
public class GlobalProgressSaveData
{
    /// <summary> 布林開關（跳過開場、畫廊解鎖、場景曾開啟…） </summary>
    public HashSet<string> UnlockedFlags = new HashSet<string>();

    /// <summary> 數值變數（周回次數、二周目繼承數值…） </summary>
    public List<VariableEntry> Variables = new List<VariableEntry>();

    [Serializable]
    public class VariableEntry
    {
        public string Key;
        public int Value;
    }
}
