using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 定義某位女主角收到特定禮物時的「專屬條件 + 專屬效果 + 專屬反應」。
/// 如果在 ItemConfigData.HeroineOverrides 中有配置,則取代 DefaultGiftEffects / DefaultGiftReaction。
/// </summary>
[System.Serializable]
public class HeroineGiftOverride
{
    [Tooltip("要覆寫的女主角 ID")]
    public string HeroineID;

    [Tooltip("送禮前的額外條件(全部通過才能送)。\n" +
             "留空 = 無額外條件限制。")]
    [SerializeReference] public List<GiftCondition> Conditions = new List<GiftCondition>();

    [Tooltip("通過條件後執行的專屬效果(完全取代 DefaultGiftEffects)")]
    [SerializeReference] public List<ItemEffect> Effects = new List<ItemEffect>();

    [Tooltip("此女主角收到這個禮物的專屬反應(對話 + UnityEvent)。\n" +
             "如果此處的對話與 Event 都留空,會 fallback 到 ItemConfigData.DefaultGiftReaction。")]
    public GiftReaction Reaction = new GiftReaction();
}