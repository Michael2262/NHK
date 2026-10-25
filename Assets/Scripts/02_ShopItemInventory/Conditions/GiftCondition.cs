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
[System.Obsolete("舊等級條件已退役，請改用數值條件。") ]
public class MinAffinityLevelCondition : GiftCondition
{
    [Tooltip("女主角的親密度等級必須 >= 此值才能收禮")]
    public int RequiredLevel = 1;

    public override bool IsMet(HeroineStatusModel heroine)
    { Debug.LogWarning("[MinAffinityLevelCondition] 舊等級條件已退役，條件不通過；請改用新數值條件並重新設定門檻。"); return false; }
}

// ==========================================================
// 具體條件：開發度等級門檻
// ==========================================================

[System.Serializable]
[System.Obsolete("舊等級條件已退役，請改用數值條件。") ]
public class MinLewdnessLevelCondition : GiftCondition
{
    [Tooltip("女主角的開發度等級必須 >= 此值才能收禮")]
    public int RequiredLevel = 1;

    public override bool IsMet(HeroineStatusModel heroine)
    { Debug.LogWarning("[MinLewdnessLevelCondition] 舊等級條件已退役，條件不通過；請改用新數值條件並重新設定門檻。"); return false; }
}

// ==========================================================
// 具體條件：興奮度等級門檻
// ==========================================================

[System.Serializable]
[System.Obsolete("舊等級條件已退役，請改用數值條件。") ]
public class MinExcitementLevelCondition : GiftCondition
{
    [Tooltip("女主角的總興奮度等級必須 >= 此值才能收禮")]
    public int RequiredLevel = 1;

    public override bool IsMet(HeroineStatusModel heroine)
    { Debug.LogWarning("[MinExcitementLevelCondition] 舊等級條件已退役，條件不通過；請改用新數值條件並重新設定門檻。"); return false; }
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

/// <summary>以正式 Trust 數值檢查送禮門檻。</summary>
[System.Serializable]
public class MinTrustValueCondition : GiftCondition
{
    [Min(0)] public int RequiredValue = 0;
    public override bool IsMet(HeroineStatusModel heroine) => heroine != null && heroine.Trust >= RequiredValue;
}

/// <summary>以正式 Affinity 數值檢查送禮門檻。</summary>
[System.Serializable]
public class MinAffinityValueCondition : GiftCondition
{
    [Min(0)] public int RequiredValue = 0;
    public override bool IsMet(HeroineStatusModel heroine) => heroine != null && heroine.Affinity >= RequiredValue;
}

/// <summary>以正式 Mood 數值檢查送禮門檻。</summary>
[System.Serializable]
public class MinMoodValueCondition : GiftCondition
{
    [Min(0)] public int RequiredValue = 0;
    public override bool IsMet(HeroineStatusModel heroine) => heroine != null && heroine.Mood >= RequiredValue;
}

/// <summary>以正式 Libido 數值檢查送禮門檻。</summary>
[System.Serializable]
public class MinLibidoValueCondition : GiftCondition
{
    [Min(0)] public int RequiredValue = 0;
    public override bool IsMet(HeroineStatusModel heroine) => heroine != null && heroine.Libido >= RequiredValue;
}

/// <summary>以正式 Excitement 數值檢查送禮門檻。</summary>
[System.Serializable]
public class MinExcitementValueCondition : GiftCondition
{
    [Min(0)] public int RequiredValue = 0;
    public override bool IsMet(HeroineStatusModel heroine) => heroine != null && heroine.Excitement >= RequiredValue;
}
