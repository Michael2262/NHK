// Copyright (c) NHK Project. All rights reserved.
// 選項圖片對照表：定義「圖片 ID → Sprite」的對應，
// 供 NhkUIResponseButton 依對話節點欄位上的 ID 查圖。
//
// 建立方式：Project 視窗右鍵 → Create → NHK → Option Image Database

using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem
{
    [CreateAssetMenu(fileName = "OptionImageDatabase", menuName = "NHK/Option Image Database")]
    public class OptionImageDatabase : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            [Tooltip("圖片 ID，對話節點的 Option Image 欄位填這個字串")]
            public string id;

            [Tooltip("對應顯示的圖片")]
            public Sprite sprite;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<string, Sprite> lookup;

        /// <summary>
        /// 依 ID 取得 Sprite，找不到回傳 null。
        /// </summary>
        public Sprite GetSprite(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            if (lookup == null)
            {
                lookup = new Dictionary<string, Sprite>(entries.Count);
                foreach (var entry in entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.id)) continue;
                    if (lookup.ContainsKey(entry.id))
                    {
                        Debug.LogWarning($"OptionImageDatabase: 圖片 ID '{entry.id}' 重複定義，只會使用第一筆。", this);
                        continue;
                    }
                    lookup.Add(entry.id, entry.sprite);
                }
            }

            Sprite sprite;
            return lookup.TryGetValue(id, out sprite) ? sprite : null;
        }

        private void OnValidate()
        {
            // Inspector 修改後重建快取
            lookup = null;
        }
    }
}
