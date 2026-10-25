using System.Collections.Generic;
using UnityEngine;

// ==========================================================
// 女主角效果：加好感經驗（固定值）
// ==========================================================
[System.Serializable]
[System.Obsolete("舊女主角數值效果已停用，請重新配置道具。") ]
public class AddAffinityExpEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Heroine;
    public override bool IsRetired => true;

    public int Amount = 20;

    public override void Apply(EffectContext ctx)
    { Debug.LogWarning("[AddAffinityExpEffect] 舊女主角數值效果已退役，請在道具設定中重新配置。"); }

    public override ValueChangeRecord? ReportChange(EffectContext ctx)
    { return null; }
}

// ==========================================================
// 女主角效果：加好感經驗（依開發度等級線性加成）
// ==========================================================
[System.Serializable]
[System.Obsolete("舊女主角數值效果已停用，請重新配置道具。") ]
public class ScaledAffinityExpEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Heroine;
    public override bool IsRetired => true;

    public int BaseAmount = 20;

    [Tooltip("每級開發度額外加多少好感經驗")]
    public int BonusPerLewdnessLevel = 5;

    public override void Apply(EffectContext ctx)
    { Debug.LogWarning("[ScaledAffinityExpEffect] 舊女主角數值效果已退役，請在道具設定中重新配置。"); }

    public override ValueChangeRecord? ReportChange(EffectContext ctx)
    { return null; }
}

// ==========================================================
// 女主角效果：加好感經驗（依興奮度查表）
// ==========================================================
[System.Serializable]
[System.Obsolete("舊女主角數值效果已停用，請重新配置道具。") ]
public class TableAffinityExpEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Heroine;
    public override bool IsRetired => true;

    [System.Serializable]
    public class TierEntry
    {
        [Tooltip("女主角的興奮度等級 >= 此值時套用此效果量")]
        public int MinExcitementLevel;
        public int AffinityAmount;
    }

    [Tooltip("由高到低排列，第一個符合的就套用")]
    public List<TierEntry> Tiers = new List<TierEntry>();

    public int FallbackAmount = 10;

    public override void Apply(EffectContext ctx)
    { Debug.LogWarning("[TableAffinityExpEffect] 舊女主角數值效果已退役，請在道具設定中重新配置。"); }

    public override ValueChangeRecord? ReportChange(EffectContext ctx)
    { return null; }
}

// ==========================================================
// 女主角效果：加興奮經驗
// ==========================================================
[System.Serializable]
[System.Obsolete("舊女主角數值效果已停用，請重新配置道具。") ]
public class AddExcitementExpEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Heroine;
    public override bool IsRetired => true;

    public int Amount = 10;

    public override void Apply(EffectContext ctx)
    { Debug.LogWarning("[AddExcitementExpEffect] 舊女主角數值效果已退役，請在道具設定中重新配置。"); }

    public override ValueChangeRecord? ReportChange(EffectContext ctx)
    { return null; }
}

// ==========================================================
// 女主角效果：加開發度經驗
// ==========================================================
[System.Serializable]
[System.Obsolete("舊女主角數值效果已停用，請重新配置道具。") ]
public class AddLewdnessExpEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Heroine;
    public override bool IsRetired => true;

    public int Amount = 10;

    public override void Apply(EffectContext ctx)
    { Debug.LogWarning("[AddLewdnessExpEffect] 舊女主角數值效果已退役，請在道具設定中重新配置。"); }

    public override ValueChangeRecord? ReportChange(EffectContext ctx)
    { return null; }
}

// ==========================================================
// 女主角效果：加不快值
// ==========================================================
[System.Serializable]
[System.Obsolete("舊女主角數值效果已停用，請重新配置道具。") ]
public class AddDiscomfortEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Heroine;
    public override bool IsRetired => true;

    public int Amount = 10;

    public override void Apply(EffectContext ctx)
    { Debug.LogWarning("[AddDiscomfortEffect] 舊女主角數值效果已退役，請在道具設定中重新配置。"); }

    public override ValueChangeRecord? ReportChange(EffectContext ctx)
    { return null; }
}

// ==========================================================
// 女主角效果：加體力
// ==========================================================
[System.Serializable]
[System.Obsolete("舊女主角數值效果已停用，請重新配置道具。") ]
public class AddHeroineStaminaEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Heroine;
    public override bool IsRetired => true;

    public int Amount = 10;

    public override void Apply(EffectContext ctx)
    { Debug.LogWarning("[AddHeroineStaminaEffect] 舊女主角數值效果已退役，請在道具設定中重新配置。"); }

    public override ValueChangeRecord? ReportChange(EffectContext ctx)
    { return null; }
}

// ==========================================================
// 女主角效果：加精神力
// ==========================================================
[System.Serializable]
[System.Obsolete("舊女主角數值效果已停用，請重新配置道具。") ]
public class AddHeroineSpiritEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Heroine;
    public override bool IsRetired => true;

    public int Amount = 10;

    public override void Apply(EffectContext ctx)
    { Debug.LogWarning("[AddHeroineSpiritEffect] 舊女主角數值效果已退役，請在道具設定中重新配置。"); }

    public override ValueChangeRecord? ReportChange(EffectContext ctx)
    { return null; }
}

// ==========================================================
// 女主角效果：增減個人可疑度
// ==========================================================
[System.Serializable]
[System.Obsolete("舊女主角數值效果已停用，請重新配置道具。") ]
public class AddPersonalSuspicionEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Heroine;
    public override bool IsRetired => true;

    [Tooltip("正數 = 增加可疑度，負數 = 減少可疑度")]
    public int Amount = -100;

    public override void Apply(EffectContext ctx)
    { Debug.LogWarning("[AddPersonalSuspicionEffect] 舊女主角數值效果已退役，請在道具設定中重新配置。"); }

    public override ValueChangeRecord? ReportChange(EffectContext ctx)
    { return null; }
}

// ==========================================================
// 女主角效果：增加個人可疑度上限
// ==========================================================
[System.Serializable]
[System.Obsolete("舊女主角數值效果已停用，請重新配置道具。") ]
public class AddPersonalSuspicionMaxEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Heroine;
    public override bool IsRetired => true;

    [Tooltip("增加的可疑度上限量")]
    public int Amount = 200;

    public override void Apply(EffectContext ctx)
    { Debug.LogWarning("[AddPersonalSuspicionMaxEffect] 舊女主角數值效果已退役，請在道具設定中重新配置。"); }

    public override ValueChangeRecord? ReportChange(EffectContext ctx)
    { return null; }
}

// ==========================================================
// 女主角效果：開啟發情狀態
// ==========================================================
[System.Serializable]
public class SetInHeatEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Heroine;

    [Tooltip("true = 開啟發情，false = 關閉發情")]
    public bool IsInHeat = true;

    public override void Apply(EffectContext ctx)
    {
        if (ctx.Heroine == null) return;
        ctx.Heroine.SetInHeat(IsInHeat);
    }

    // 狀態開關類效果不需要數值報告，維持回傳 null
}

// ==========================================================
// 女主角效果：送禮成功時開啟 Flag
// ==========================================================
[System.Serializable]
public class ActivateFlagOnGiftEffect : ItemEffect
{
    public override EffectTarget Target => EffectTarget.Heroine;

    [Tooltip("送禮成功時要啟動的 Flag 定義檔")]
    public ProgressFlagDefinition FlagToActivate;

    [Tooltip("Flag 持續時間")]
    public FlagDurationType Duration = FlagDurationType.Persistent;

    public override void Apply(EffectContext ctx)
    {
        if (ctx.Heroine == null) return;

        if (FlagToActivate == null)
        {
            Debug.LogWarning("[ActivateFlagOnGiftEffect] FlagToActivate 未指定，跳過");
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

        Debug.Log($"[ActivateFlagOnGiftEffect] 送禮給 {ctx.Heroine.HeroineID} 啟動 Flag '{FlagToActivate.FlagID}' ({Duration})");
    }

    // Flag 開關類效果不需要數值報告，維持回傳 null
}