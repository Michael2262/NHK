using UnityEngine;

// ==========================================================
// 送禮條件：抽象基底
// ==========================================================

/// <summary>
/// 送禮條件的抽象基底。
/// 使用 [SerializeReference] 內嵌在 HeroineGiftOverride 中。
/// </summary>
[System.Serializable]
public abstract class GiftCondition
{
    [Tooltip("對這個條件的描述，方便理解用途")]
    [TextArea] public string description;

    [Tooltip("拒收時要播放的 Dialogue System 對話 Title\n" +
             "例如: Chat/Sister/Reject_AffinityLow\n" +
             "（此欄位同時作為拒收時的 Localization Key 使用）")]
    public string RejectConversationTitle = "";

    /// <summary>
    /// 回傳 true = 條件通過（可以收禮），false = 拒收
    /// </summary>
    public abstract bool IsMet(HeroineStatusModel heroine);
}


// ==========================================================
// 具體條件：親密度等級門檻
// ==========================================================

[System.Serializable]
public class MinAffinityLevelCondition : GiftCondition
{
    [Tooltip("女主角的親密度等級必須 >= 此值才能收禮")]
    public int RequiredLevel = 1;

    public override bool IsMet(HeroineStatusModel heroine)
    {
        return heroine.BaseAffinityLevel >= RequiredLevel;
    }
}


// ==========================================================
// 具體條件：開發度等級門檻
// ==========================================================

[System.Serializable]
public class MinLewdnessLevelCondition : GiftCondition
{
    [Tooltip("女主角的開發度等級必須 >= 此值才能收禮")]
    public int RequiredLevel = 1;

    public override bool IsMet(HeroineStatusModel heroine)
    {
        return heroine.LewdnessLevel >= RequiredLevel;
    }
}


// ==========================================================
// 具體條件：興奮度等級門檻
// ==========================================================

[System.Serializable]
public class MinExcitementLevelCondition : GiftCondition
{
    [Tooltip("女主角的總興奮度等級必須 >= 此值才能收禮")]
    public int RequiredLevel = 1;

    public override bool IsMet(HeroineStatusModel heroine)
    {
        return heroine.TotalExcitementLevel >= RequiredLevel;
    }
}


// ==========================================================
// 具體條件：複合條件（多條件全部通過才算通過）
// ==========================================================

[System.Serializable]
public class CompositeCondition : GiftCondition
{
    [Tooltip("所有子條件都必須符合")]
    [SerializeReference] public GiftCondition[] SubConditions;

    public override bool IsMet(HeroineStatusModel heroine)
    {
        if (SubConditions == null || SubConditions.Length == 0) return true;

        foreach (var condition in SubConditions)
        {
            if (condition != null && !condition.IsMet(heroine))
                return false;
        }
        return true;
    }
}
