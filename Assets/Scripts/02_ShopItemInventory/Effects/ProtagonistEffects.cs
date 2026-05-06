using UnityEngine;

// ==========================================================
// NHK 主角效果：Stress / LifePower / SocialFear / Dependency
// 原檔名保留。舊 Stamina / Spirit / Action / ShootTimes 等效果已改為 NHK 狀態效果。
// ==========================================================

[System.Serializable]
public class AddStressEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    public int Amount = 10;

    public override void Apply(EffectContext ctx) => ctx.Protagonist.AddStress(Amount);

    public override ValueChangeRecord? ReportChange(EffectContext ctx) => new ValueChangeRecord
    {
        isHeroineResource = false,
        resourceTypeKey = "Stress",
        finalAmount = Amount,
        effectResult = ValueChangeResult.EffectResult.Normal,
        heroineNameKey = ""
    };
}

[System.Serializable]
public class ReduceStressEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    public int Amount = 10;

    public override void Apply(EffectContext ctx) => ctx.Protagonist.ReduceStress(Amount);

    public override ValueChangeRecord? ReportChange(EffectContext ctx) => new ValueChangeRecord
    {
        isHeroineResource = false,
        resourceTypeKey = "Stress",
        finalAmount = -Amount,
        effectResult = ValueChangeResult.EffectResult.Normal,
        heroineNameKey = ""
    };
}

[System.Serializable]
public class AddLifePowerEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    public int Amount = 10;

    public override void Apply(EffectContext ctx) => ctx.Protagonist.AddLifePower(Amount);

    public override ValueChangeRecord? ReportChange(EffectContext ctx) => new ValueChangeRecord
    {
        isHeroineResource = false,
        resourceTypeKey = "LifePower",
        finalAmount = Amount,
        effectResult = ValueChangeResult.EffectResult.Normal,
        heroineNameKey = ""
    };
}

[System.Serializable]
public class ReduceLifePowerEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    public int Amount = 10;

    public override void Apply(EffectContext ctx) => ctx.Protagonist.ReduceLifePower(Amount);

    public override ValueChangeRecord? ReportChange(EffectContext ctx) => new ValueChangeRecord
    {
        isHeroineResource = false,
        resourceTypeKey = "LifePower",
        finalAmount = -Amount,
        effectResult = ValueChangeResult.EffectResult.Normal,
        heroineNameKey = ""
    };
}

[System.Serializable]
public class AddSocialFearEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    public int Amount = 10;

    public override void Apply(EffectContext ctx) => ctx.Protagonist.AddSocialFear(Amount);

    public override ValueChangeRecord? ReportChange(EffectContext ctx) => new ValueChangeRecord
    {
        isHeroineResource = false,
        resourceTypeKey = "SocialFear",
        finalAmount = Amount,
        effectResult = ValueChangeResult.EffectResult.Normal,
        heroineNameKey = ""
    };
}

[System.Serializable]
public class ReduceSocialFearEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    public int Amount = 10;

    public override void Apply(EffectContext ctx) => ctx.Protagonist.ReduceSocialFear(Amount);

    public override ValueChangeRecord? ReportChange(EffectContext ctx) => new ValueChangeRecord
    {
        isHeroineResource = false,
        resourceTypeKey = "SocialFear",
        finalAmount = -Amount,
        effectResult = ValueChangeResult.EffectResult.Normal,
        heroineNameKey = ""
    };
}

[System.Serializable]
public class AddDependencyEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    public int Amount = 10;

    public override void Apply(EffectContext ctx) => ctx.Protagonist.AddDependency(Amount);

    public override ValueChangeRecord? ReportChange(EffectContext ctx) => new ValueChangeRecord
    {
        isHeroineResource = false,
        resourceTypeKey = "Dependency",
        finalAmount = Amount,
        effectResult = ValueChangeResult.EffectResult.Normal,
        heroineNameKey = ""
    };
}

[System.Serializable]
public class ReduceDependencyEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    public int Amount = 10;

    public override void Apply(EffectContext ctx) => ctx.Protagonist.ReduceDependency(Amount);

    public override ValueChangeRecord? ReportChange(EffectContext ctx) => new ValueChangeRecord
    {
        isHeroineResource = false,
        resourceTypeKey = "Dependency",
        finalAmount = -Amount,
        effectResult = ValueChangeResult.EffectResult.Normal,
        heroineNameKey = ""
    };
}

// ==========================================================
// 保留資源：Money / SkillPoints
// ==========================================================
[System.Serializable]
public class AddMoneyEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    public int Amount = 100;

    public override void Apply(EffectContext ctx) => ctx.Protagonist.AddMoney(Amount);

    public override ValueChangeRecord? ReportChange(EffectContext ctx) => new ValueChangeRecord
    {
        isHeroineResource = false,
        resourceTypeKey = "Money",
        finalAmount = Amount,
        effectResult = ValueChangeResult.EffectResult.Normal,
        heroineNameKey = ""
    };
}

[System.Serializable]
public class AddSkillPointsEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    public int Amount = 1;

    public override void Apply(EffectContext ctx) => ctx.Protagonist.AddSkillPoints(Amount);

    public override ValueChangeRecord? ReportChange(EffectContext ctx) => new ValueChangeRecord
    {
        isHeroineResource = false,
        resourceTypeKey = "SkillPoints",
        finalAmount = Amount,
        effectResult = ValueChangeResult.EffectResult.Normal,
        heroineNameKey = ""
    };
}

// ==========================================================
// Daily Flag Effects
// ==========================================================
[System.Serializable]
public class MarkBathedTodayEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    public bool Value = true;
    public override void Apply(EffectContext ctx) => ctx.Protagonist.MarkBathedToday(Value);
}

[System.Serializable]
public class MarkCleanedRoomTodayEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    public bool Value = true;
    public override void Apply(EffectContext ctx) => ctx.Protagonist.MarkCleanedRoomToday(Value);
}

[System.Serializable]
public class MarkHadMealTodayEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    public bool Value = true;
    public override void Apply(EffectContext ctx) => ctx.Protagonist.MarkHadMealToday(Value);
}

[System.Serializable]
public class MarkCheckedMailTodayEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    public bool Value = true;
    public override void Apply(EffectContext ctx) => ctx.Protagonist.MarkCheckedMailToday(Value);
}

[System.Serializable]
public class MarkRepliedFamilyTodayEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    public bool Value = true;
    public override void Apply(EffectContext ctx) => ctx.Protagonist.MarkRepliedFamilyToday(Value);
}

[System.Serializable]
public class MarkIgnoredPhoneTodayEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    public bool Value = true;
    public override void Apply(EffectContext ctx) => ctx.Protagonist.MarkIgnoredPhoneToday(Value);
}

[System.Serializable]
public class MarkEscapedTodayEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    public bool Value = true;
    public override void Apply(EffectContext ctx) => ctx.Protagonist.MarkEscapedToday(Value);
}

[System.Serializable]
public class MarkGoneOutsideTodayEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    [Tooltip("true = 出門成功；false = 出門失敗")]
    public bool Succeeded = true;
    public override void Apply(EffectContext ctx) => ctx.Protagonist.MarkGoneOutsideToday(Succeeded);
}

// ==========================================================
// Flag Effect：保留原本功能
// ==========================================================
[System.Serializable]
public class ActivateFlagOnUseEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;

    [Tooltip("使用道具時要啟動的 Flag 定義檔")]
    public ProgressFlagDefinition FlagToActivate;

    [Tooltip("Flag 持續時間")]
    public FlagDurationType Duration = FlagDurationType.Persistent;

    public override void Apply(EffectContext ctx)
    {
        if (FlagToActivate == null)
        {
            Debug.LogWarning("[ActivateFlagOnUseEffect] FlagToActivate 未指定，跳過");
            return;
        }

        var progressFlags = GameStatusService.Instance.ProgressFlags;

        switch (Duration)
        {
            case FlagDurationType.Persistent:
                progressFlags.AddPersistentFlag(FlagToActivate.FlagID);
                break;
            case FlagDurationType.Day:
                progressFlags.AddDailyFlag(FlagToActivate.FlagID);
                break;
            case FlagDurationType.Phase:
                progressFlags.AddPhaseFlag(FlagToActivate.FlagID);
                break;
        }

        Debug.Log($"[ActivateFlagOnUseEffect] 使用道具啟動 Flag '{FlagToActivate.FlagID}' ({Duration})");
    }
}

// ==========================================================
// Legacy class aliases - keep for asset compatibility where possible.
// ==========================================================
[System.Serializable]
public class AddStaminaEffect : AddLifePowerEffect { }

[System.Serializable]
public class AddSpiritEffect : ReduceStressEffect { }

[System.Serializable]
public class AddActionEffect : AddLifePowerEffect { }

[System.Serializable]
public class AddShootTimesEffect : AddDependencyEffect { }

[System.Serializable]
public class AddExcuseChargeEffect : ReduceStressEffect { }

[System.Serializable]
public class AddProtagonistAttackEffect : AddLifePowerEffect { }

[System.Serializable]
public class AddProtagonistDefenseEffect : ReduceSocialFearEffect { }

[System.Serializable]
public class AddRestRecoveryPerSlotEffect : ReduceStressEffect { }

[System.Serializable]
public class AddNightlyRestRecoveryEffect : ReduceStressEffect { }

[System.Serializable]
public class SetShootItemDepletionEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Protagonist;
    public int DepletionValue = 3;
    public override void Apply(EffectContext ctx)
    {
        Debug.LogWarning("[SetShootItemDepletionEffect] NHK no longer uses ShootItemDepletion. Effect ignored.");
    }
}
