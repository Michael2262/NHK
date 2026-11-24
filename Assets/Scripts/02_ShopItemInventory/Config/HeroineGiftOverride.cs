using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 以下只保留舊資料的序列化型別；ItemConfigData 已移除所有送禮設定與執行入口。
[System.Serializable]
public class GiftReaction
{
    public string DialogueConversationTitle;
    public UnityEvent ReactionEvent;
}

[System.Serializable]
public class HeroineGiftOverride
{
    public string HeroineID;
    [SerializeReference] public List<GiftCondition> Conditions = new List<GiftCondition>();
    [SerializeReference] public List<ItemEffect> Effects = new List<ItemEffect>();
    public GiftReaction Reaction = new GiftReaction();
}
