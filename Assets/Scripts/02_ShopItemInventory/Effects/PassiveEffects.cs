using UnityEngine;

// ==========================================================
// 被動效果：永久數值提升（購買時自動套用）
// NHK 版：對 Stress / LifePower / SocialFear / Dependency / Money / SkillPoints 生效。
// 注意：Stress / SocialFear 正值是變糟，若要降低請填負數。
// ==========================================================
[System.Serializable]
public class PermanentStatBoostEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;

    public enum BoostStatType
    {
        Stress,
        LifePower,
        SocialFear,
        Dependency,
        Money,
        SkillPoints
    }

    public BoostStatType StatToBoost = BoostStatType.LifePower;
    public int Amount = 10;

    public override void Apply(EffectContext ctx)
    {
        switch (StatToBoost)
        {
            case BoostStatType.Stress:
                ctx.Protagonist.AddStress(Amount);
                break;
            case BoostStatType.LifePower:
                ctx.Protagonist.AddLifePower(Amount);
                break;
            case BoostStatType.SocialFear:
                ctx.Protagonist.AddSocialFear(Amount);
                break;
            case BoostStatType.Dependency:
                ctx.Protagonist.AddDependency(Amount);
                break;
            case BoostStatType.Money:
                ctx.Protagonist.AddMoney(Amount);
                break;
            case BoostStatType.SkillPoints:
                ctx.Protagonist.AddSkillPoints(Amount);
                break;
        }
    }

    public override ValueChangeRecord? ReportChange(EffectContext ctx)
    {
        return new ValueChangeRecord
        {
            isHeroineResource = false,
            resourceTypeKey = StatToBoost.ToString(),
            finalAmount = Amount,
            effectResult = ValueChangeResult.EffectResult.Normal,
            heroineNameKey = ""
        };
    }
}

// ==========================================================
// 被動效果：啟動 Flag
// ==========================================================
[System.Serializable]
public class ActivateFlagEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    [Tooltip("要啟動的 Flag 定義檔 (ScriptableObject)")]
    public ProgressFlagDefinition FlagToActivate;

    [Tooltip("Flag 類型：Persistent = 永久, Day = 每日重設, Phase = 每階段重設")]
    public FlagDurationType Duration = FlagDurationType.Persistent;

    public override void Apply(EffectContext ctx)
    {
        if (FlagToActivate == null)
        {
            Debug.LogWarning("[ActivateFlagEffect] FlagToActivate 未指定，跳過");
            return;
        }

        var progressFlags = GameStatusService.Instance.ProgressFlags;

        switch (Duration)
        {
            case FlagDurationType.Persistent:
                progressFlags.AddPersistentFlag(FlagToActivate.FlagID);
                Debug.Log($"[ActivateFlagEffect] 啟動永久 Flag '{FlagToActivate.FlagID}'");
                break;
            case FlagDurationType.Day:
                progressFlags.AddDailyFlag(FlagToActivate.FlagID);
                Debug.Log($"[ActivateFlagEffect] 啟動每日 Flag '{FlagToActivate.FlagID}'");
                break;
            case FlagDurationType.Phase:
                progressFlags.AddPhaseFlag(FlagToActivate.FlagID);
                Debug.Log($"[ActivateFlagEffect] 啟動階段 Flag '{FlagToActivate.FlagID}'");
                break;
        }
    }
}

public enum FlagDurationType
{
    Persistent,
    Day,
    Phase
}
