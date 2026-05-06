using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 情緒卡 prefab 對照表。
///
/// 用途:
/// - 統一管理「每種 HeroineEmotionCardType 對應的 EmotionCard prefab」。
/// - 所有需要顯示情緒卡的 View (抽選表演、卡池變化提示、未來的圖鑑或選單等) 都從本資產取 prefab。
/// - 美術改卡面、新增情緒種類時,只要改這一份資產即可。
///
/// 使用方式:
/// 1. Project 視窗右鍵 Create → Heroine Status → Emotion Card Catalog,生成一份資產。
/// 2. 把每種情緒對應的 EmotionCard prefab 拉進去。
/// 3. 各 View 在 Inspector 拉這份資產,而不是各自維護一份對照表。
/// </summary>
[CreateAssetMenu(menuName = "Heroine Status/Emotion Card Catalog", fileName = "EmotionCardCatalog")]
public class EmotionCardCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public HeroineEmotionCardType Type;
        public EmotionCard Prefab;
    }

    [Tooltip("每種情緒對應一個 EmotionCard prefab。Prefab 內的圖與字請直接自行設定。")]
    [SerializeField] private List<Entry> entries = new List<Entry>();

    private Dictionary<HeroineEmotionCardType, EmotionCard> map;

    public IReadOnlyList<Entry> Entries => entries;

    private void OnEnable()
    {
        RebuildMap();
    }

    private void OnValidate()
    {
        RebuildMap();
    }

    /// <summary>
    /// 取得指定情緒對應的 prefab。找不到時回傳 null。
    /// </summary>
    public EmotionCard GetPrefab(HeroineEmotionCardType type)
    {
        EnsureMap();
        return map.TryGetValue(type, out var prefab) ? prefab : null;
    }

    /// <summary>
    /// 是否有指定情緒的 prefab。
    /// </summary>
    public bool Contains(HeroineEmotionCardType type)
    {
        EnsureMap();
        return map.ContainsKey(type) && map[type] != null;
    }

    private void EnsureMap()
    {
        if (map == null) RebuildMap();
    }

    private void RebuildMap()
    {
        if (map == null) map = new Dictionary<HeroineEmotionCardType, EmotionCard>();
        else map.Clear();

        if (entries == null) return;

        foreach (var entry in entries)
        {
            if (entry == null || entry.Prefab == null) continue;
            map[entry.Type] = entry.Prefab;
        }
    }
}
