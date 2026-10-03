using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>共用 Spine 事件名稱目錄；場景各自保留 UnityEvent 反應。</summary>
[CreateAssetMenu(fileName = "SpineEventDatabase", menuName = "Game/Spine/事件 ID 資料庫")]
public class SpineEventDatabase : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("必須與 Spine 動畫的事件名稱完全一致，區分大小寫。")]
        public string id;
        public string displayName;
        [TextArea] public string description;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();
    public IReadOnlyList<Entry> Entries => entries;

    public Entry Find(string id)
    {
        if (string.IsNullOrEmpty(id) || entries == null) return null;
        foreach (var entry in entries)
            if (entry != null && string.Equals(entry.id, id, StringComparison.Ordinal)) return entry;
        return null;
    }

    public IEnumerable<string> GetValidationErrors()
    {
        if (entries == null) yield break;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                yield return $"第 {i + 1} 筆事件 ID 未填寫。";
            else
            {
                if (!ids.Add(entry.id)) yield return $"事件 ID 重複：{entry.id}";
                if (entry.id != entry.id.Trim()) yield return $"事件 ID「{entry.id}」含頭尾空白，請確認是否符合 Spine 事件名稱。";
            }
        }
    }
}
