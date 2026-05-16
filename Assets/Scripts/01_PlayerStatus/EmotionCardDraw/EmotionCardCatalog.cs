using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tachie 表情預設組。
/// 對應 TachieController 的各部位切換：Eye, Mouth, Eyebrow, Blush, Other, Above, Body。
/// 留空的欄位表示「不變更該部位」。
/// </summary>
[Serializable]
public class TachieFacePreset
{
    [Tooltip("眼睛。留空 = 不變更。")]
    public string Eye = "";

    [Tooltip("嘴巴。留空 = 不變更。")]
    public string Mouth = "";

    [Tooltip("眉毛。留空 = 不變更。")]
    public string Eyebrow = "";

    [Tooltip("腮紅。留空 = 不變更。")]
    public string Blush = "";

    [Tooltip("其他 (Other)。留空 = 不變更。")]
    public string Other = "";

    [Tooltip("最上層 (Above)。留空 = 不變更。")]
    public string Above = "";

    [Tooltip("身體。留空 = 不變更。")]
    public string Body = "";

    /// <summary>
    /// 透過 TachieController 套用此預設組到指定 groupID。
    /// 只會變更有填值的欄位。
    /// </summary>
    public void ApplyTo(string groupID)
    {
        if (string.IsNullOrEmpty(groupID)) return;
        var tc = TachieController.Instance;
        if (tc == null)
        {
            Debug.LogWarning("[TachieFacePreset] TachieController.Instance is null.");
            return;
        }

        if (!string.IsNullOrEmpty(Eye)) tc.ChangeEye(groupID, Eye);
        if (!string.IsNullOrEmpty(Mouth)) tc.ChangeMouth(groupID, Mouth);
        if (!string.IsNullOrEmpty(Eyebrow)) tc.ChangeEyebrow(groupID, Eyebrow);
        if (!string.IsNullOrEmpty(Blush)) tc.ChangeBlush(groupID, Blush);
        if (!string.IsNullOrEmpty(Other)) tc.ChangeOther(groupID, Other);
        if (!string.IsNullOrEmpty(Above)) tc.ChangeAbove(groupID, Above);
        if (!string.IsNullOrEmpty(Body)) tc.ChangeBody(groupID, Body);
    }
}

/// <summary>
/// 情緒卡對照表（重構版）。
///
/// 每種 HeroineEmotionCardType 對應：
/// 1. EmotionCard prefab（保留向下相容）
/// 2. EmotionNameTextKey（情緒名稱的 TextTable Key，用於 Emotion.Change 的 {1}）
/// 3. SmallDrawFace — 小/中抽選用的考慮表情預設組（完整 Tachie 各部位）
/// 4. BigDrawFace — 大抽選第二段用的猶豫表情預設組（完整 Tachie 各部位）
///
/// ★ Normal 不需要在 entries 中配置。Catalog 的查詢方法會安全處理 Normal，
///   回傳 null / 預設值而不報錯。
/// </summary>
[CreateAssetMenu(menuName = "Heroine Status/Emotion Card Catalog", fileName = "EmotionCardCatalog")]
public class EmotionCardCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public HeroineEmotionCardType Type;

        [Tooltip("（保留）EmotionCard prefab。新表演不再使用。")]
        public EmotionCard Prefab;

        [Tooltip("該情緒的 TextTable Key，例如 Emotion.Angry。用於 Emotion.Change 的 {1}。")]
        public string EmotionNameTextKey;

        [Tooltip("小/中抽選表演時套用的 Tachie 表情。")]
        public TachieFacePreset SmallDrawFace;

        [Tooltip("大抽選第二段表演時套用的 Tachie 表情。")]
        public TachieFacePreset BigDrawFace;
    }

    [Header("Tachie Group ID")]
    [Tooltip("Tachie 切換時使用的預設 groupID（例如 Sister）。可在 View 層覆寫。")]
    [SerializeField] private string defaultTachieGroupID = "Sister";

    [Header("Normal 情緒設定")]
    [Tooltip("Normal 情緒的 TextTable Key。Normal 不進卡池也不需要 Tachie 表情，但可能需要顯示名稱。")]
    [SerializeField] private string normalEmotionNameTextKey = "Emotion.Normal";

    [Tooltip("每種情緒對應的資料。")]
    [SerializeField] private List<Entry> entries = new List<Entry>();

    private Dictionary<HeroineEmotionCardType, Entry> map;

    public IReadOnlyList<Entry> Entries => entries;
    public string DefaultTachieGroupID => defaultTachieGroupID;

    private void OnEnable() => RebuildMap();
    private void OnValidate() => RebuildMap();

    /// <summary>
    /// 取得指定情緒的完整 Entry。
    /// Normal 沒有 Entry，回傳 null。
    /// </summary>
    public Entry GetEntry(HeroineEmotionCardType type)
    {
        if (type == HeroineEmotionCardType.Normal) return null;
        EnsureMap();
        return map.TryGetValue(type, out var entry) ? entry : null;
    }

    /// <summary>
    /// 取得指定情緒對應的 prefab（保留向下相容）。
    /// </summary>
    public EmotionCard GetPrefab(HeroineEmotionCardType type)
    {
        var entry = GetEntry(type);
        return entry?.Prefab;
    }

    /// <summary>
    /// 取得該情緒的 TextTable Key（情緒名稱）。
    /// Normal 會回傳專用的 normalEmotionNameTextKey。
    /// </summary>
    public string GetEmotionNameTextKey(HeroineEmotionCardType type)
    {
        if (type == HeroineEmotionCardType.Normal) return normalEmotionNameTextKey;
        var entry = GetEntry(type);
        return entry != null ? entry.EmotionNameTextKey : type.ToString();
    }

    /// <summary>
    /// 取得小/中抽選用的 Tachie 表情預設組。
    /// Normal 回傳 null（不需要抽選表情）。
    /// </summary>
    public TachieFacePreset GetSmallDrawFace(HeroineEmotionCardType type)
    {
        var entry = GetEntry(type);
        return entry?.SmallDrawFace;
    }

    /// <summary>
    /// 取得大抽選第二段用的 Tachie 表情預設組。
    /// Normal 回傳 null（不需要抽選表情）。
    /// </summary>
    public TachieFacePreset GetBigDrawFace(HeroineEmotionCardType type)
    {
        var entry = GetEntry(type);
        return entry?.BigDrawFace;
    }

    /// <summary>
    /// 是否有指定情緒的資料。Normal 永遠回傳 false。
    /// </summary>
    public bool Contains(HeroineEmotionCardType type)
    {
        if (type == HeroineEmotionCardType.Normal) return false;
        EnsureMap();
        return map.ContainsKey(type);
    }

    private void EnsureMap()
    {
        if (map == null) RebuildMap();
    }

    private void RebuildMap()
    {
        if (map == null) map = new Dictionary<HeroineEmotionCardType, Entry>();
        else map.Clear();

        if (entries == null) return;

        foreach (var entry in entries)
        {
            if (entry == null) continue;

            // ★ 防禦：如果有人在 Inspector 不小心加了 Normal Entry，跳過它
            if (entry.Type == HeroineEmotionCardType.Normal)
            {
                Debug.LogWarning("[EmotionCardCatalog] Normal should not have an Entry in the catalog. Skipping.");
                continue;
            }

            map[entry.Type] = entry;
        }
    }
}
