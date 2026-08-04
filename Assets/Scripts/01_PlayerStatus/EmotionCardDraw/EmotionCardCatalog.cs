using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 情緒卡對照表（重構版）。
///
/// 每種 HeroineEmotionCardType 對應：
/// 1. EmotionNameTextKey（情緒名稱的 TextTable Key，用於 Emotion.Change 的 {1}）
/// 2. SmallDrawFace — 小/中抽選用的考慮臉（表情 ID + 身體）
/// 3. BigDrawFace — 大抽選第二段用的猶豫臉（表情 ID + 身體）
///
/// ★ 表情一律以 TachieExpressionConfig 的 preset 名稱（ID）填寫，
///   實際各部位內容定義在各 TachieActor 掛載的 TachieExpressionConfig 上，
///   套用時透過 TachieController.ChangeExpression(groupID, id) 完成（支援連動群組）。
///   身體則透過 TachieController.ChangeBody(groupID, body) 套用。
///
/// ★ Normal 可以（但不強制）在 entries 中配置。
///   配置後，current 抽選結果為 Normal 時會用該 Entry 的臉演出；
///   未配置時查詢回傳 null / 預設值，不報錯（View 只印 Warning、表情不變）。
/// </summary>
[CreateAssetMenu(menuName = "Heroine Status/Emotion Card Catalog", fileName = "EmotionCardCatalog")]
public class EmotionCardCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public HeroineEmotionCardType Type;

        [Tooltip("該情緒的 TextTable Key，例如 Emotion.Angry。用於 Emotion.Change 的 {1}。")]
        public string EmotionNameTextKey;

        [Tooltip("小/中抽選表演時套用的臉：表情 ID + 身體。")]
        public TachieFace SmallDrawFace = new TachieFace();

        [Tooltip("大抽選第二段表演時套用的臉：表情 ID + 身體。")]
        public TachieFace BigDrawFace = new TachieFace();
    }

    [Header("Tachie Group ID")]
    [Tooltip("Tachie 切換時使用的預設 groupID（例如 Sister）。可在 View 層覆寫。")]
    [SerializeField] private string defaultTachieGroupID = "Sister";

    [Header("Think 表演秒數")]
    [Tooltip("Think / ThinkPhase1 第一段（掂量，SmallDrawFace）停留秒數。")]
    [SerializeField, Min(0f)] private float phase1Duration = 2f;

    [Tooltip("Think / ThinkPhase2 第二段（猶豫，BigDrawFace）停留秒數。")]
    [SerializeField, Min(0f)] private float phase2Duration = 3f;

    [Header("Normal 情緒設定")]
    [Tooltip("Normal 情緒的 TextTable Key（entries 未配置 Normal 或其 Key 留空時的 fallback）。")]
    [SerializeField] private string normalEmotionNameTextKey = "Emotion.Normal";

    [Tooltip("每種情緒對應的資料。")]
    [SerializeField] private List<Entry> entries = new List<Entry>();

    private Dictionary<HeroineEmotionCardType, Entry> map;

    public IReadOnlyList<Entry> Entries => entries;
    public string DefaultTachieGroupID => defaultTachieGroupID;
    public float Phase1Duration => phase1Duration;
    public float Phase2Duration => phase2Duration;

    private void OnEnable() => RebuildMap();
    private void OnValidate() => RebuildMap();

    /// <summary>
    /// 取得指定情緒的完整 Entry。
    /// Normal 若未在 entries 中配置則回傳 null。
    /// </summary>
    public Entry GetEntry(HeroineEmotionCardType type)
    {
        EnsureMap();
        return map.TryGetValue(type, out var entry) ? entry : null;
    }

    /// <summary>
    /// 取得該情緒的 TextTable Key（情緒名稱）。
    /// Normal 優先用 Entry 的 Key，未配置（或留空）時退回 normalEmotionNameTextKey。
    /// </summary>
    public string GetEmotionNameTextKey(HeroineEmotionCardType type)
    {
        var entry = GetEntry(type);

        if (type == HeroineEmotionCardType.Normal)
        {
            return entry != null && !string.IsNullOrEmpty(entry.EmotionNameTextKey)
                ? entry.EmotionNameTextKey
                : normalEmotionNameTextKey;
        }

        return entry != null ? entry.EmotionNameTextKey : type.ToString();
    }

    /// <summary>
    /// 取得小/中抽選用的臉（表情 ID + 身體）。
    /// Normal 未配置 Entry 時回傳 null。
    /// </summary>
    public TachieFace GetSmallDrawFace(HeroineEmotionCardType type)
    {
        var entry = GetEntry(type);
        return entry?.SmallDrawFace;
    }

    /// <summary>
    /// 取得大抽選第二段用的臉（表情 ID + 身體）。
    /// Normal 未配置 Entry 時回傳 null。
    /// </summary>
    public TachieFace GetBigDrawFace(HeroineEmotionCardType type)
    {
        var entry = GetEntry(type);
        return entry?.BigDrawFace;
    }

    /// <summary>
    /// 是否有指定情緒的資料。Normal 只有在 entries 中配置了才回傳 true。
    /// </summary>
    public bool Contains(HeroineEmotionCardType type)
    {
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
            map[entry.Type] = entry;
        }
    }
}
