using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

// ==========================================================
// Shared helpers
// ==========================================================
internal static class ProtagonistPlayMakerUtil
{
    public static ProtagonistStatusModel GetProtagonist(string caller)
    {
        var protagonist = GameStatusService.Instance?.Protagonist;
        if (protagonist == null)
            Debug.LogWarning($"[{caller}] 找不到主角 ProtagonistStatusModel");
        return protagonist;
    }

    public static void StoreInt(FsmInt variable, int value)
    {
        if (variable != null && !variable.IsNone) variable.Value = value;
    }

    public static void StoreFloat(FsmFloat variable, float value)
    {
        if (variable != null && !variable.IsNone) variable.Value = value;
    }

    public static void StoreBool(FsmBool variable, bool value)
    {
        if (variable != null && !variable.IsNone) variable.Value = value;
    }
}

// ==========================================================
// Money / SkillPoints
// ==========================================================
[ActionCategory("Protagonist Status")]
[Tooltip("增減主角的金錢 (Money)。正值增加，負值減少，最低為 0。")]
public class AddMoney : FsmStateAction
{
    public FsmInt amount;
    [UIHint(UIHint.Variable)] public FsmInt storeMoney;

    public override void Reset() { amount = 0; storeMoney = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(AddMoney));
        if (p != null)
        {
            p.AddMoney(amount.Value);
            ProtagonistPlayMakerUtil.StoreInt(storeMoney, p.Money);
        }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("嘗試扣除主角指定金額。成功/失敗可觸發對應事件。")]
public class TryReduceMoney : FsmStateAction
{
    public FsmInt cost;
    [UIHint(UIHint.Variable)] public FsmBool storeResult;
    [UIHint(UIHint.Variable)] public FsmInt storeMoney;
    public FsmBool logWarningOnFail;
    public FsmEvent successEvent;
    public FsmEvent failedEvent;

    public override void Reset()
    {
        cost = 0; storeResult = null; storeMoney = null; logWarningOnFail = true; successEvent = null; failedEvent = null;
    }

    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(TryReduceMoney));
        if (p == null) { Finish(); return; }
        bool success = p.TryReduceMoney(cost.Value);
        ProtagonistPlayMakerUtil.StoreBool(storeResult, success);
        ProtagonistPlayMakerUtil.StoreInt(storeMoney, p.Money);
        if (!success && logWarningOnFail.Value)
            Debug.LogWarning($"[TryReduceMoney] 餘額不足。需要 {cost.Value}，目前 {p.Money}");
        Fsm.Event(success ? successEvent : failedEvent);
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("增減主角的技能點 (SkillPoints)。正值增加，負值減少，最低為 0。")]
public class AddSkillPoints : FsmStateAction
{
    public FsmInt amount;
    [UIHint(UIHint.Variable)] public FsmInt storeSkillPoints;

    public override void Reset() { amount = 0; storeSkillPoints = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(AddSkillPoints));
        if (p != null)
        {
            p.AddSkillPoints(amount.Value);
            ProtagonistPlayMakerUtil.StoreInt(storeSkillPoints, p.SkillPoints);
        }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("嘗試扣除主角指定技能點。成功/失敗可觸發對應事件。")]
public class TryReduceSkillPoints : FsmStateAction
{
    public FsmInt cost;
    [UIHint(UIHint.Variable)] public FsmBool storeResult;
    [UIHint(UIHint.Variable)] public FsmInt storeSkillPoints;
    public FsmEvent successEvent;
    public FsmEvent failedEvent;

    public override void Reset() { cost = 0; storeResult = null; storeSkillPoints = null; successEvent = null; failedEvent = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(TryReduceSkillPoints));
        if (p == null) { Finish(); return; }
        bool success = p.TryReduceSkillPoints(cost.Value);
        ProtagonistPlayMakerUtil.StoreBool(storeResult, success);
        ProtagonistPlayMakerUtil.StoreInt(storeSkillPoints, p.SkillPoints);
        Fsm.Event(success ? successEvent : failedEvent);
        Finish();
    }
}

// ==========================================================
// NHK Core Status Actions
// ==========================================================
[ActionCategory("Protagonist Status")]
[Tooltip("增減主角壓力 Stress。正值增加壓力，負值降低壓力。")]
public class AddStress : FsmStateAction
{
    public FsmInt amount;
    [UIHint(UIHint.Variable)] public FsmInt storeStress;
    public override void Reset() { amount = 0; storeStress = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(AddStress));
        if (p != null) { p.AddStress(amount.Value); ProtagonistPlayMakerUtil.StoreInt(storeStress, p.Stress); }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("直接設定主角壓力 Stress。")]
public class SetStress : FsmStateAction
{
    public FsmInt value;
    [UIHint(UIHint.Variable)] public FsmInt storeStress;
    public override void Reset() { value = 0; storeStress = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(SetStress));
        if (p != null) { p.SetStress(value.Value); ProtagonistPlayMakerUtil.StoreInt(storeStress, p.Stress); }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("增減主角生活力 LifePower。")]
public class AddLifePower : FsmStateAction
{
    public FsmInt amount;
    [UIHint(UIHint.Variable)] public FsmInt storeLifePower;
    public override void Reset() { amount = 0; storeLifePower = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(AddLifePower));
        if (p != null) { p.AddLifePower(amount.Value); ProtagonistPlayMakerUtil.StoreInt(storeLifePower, p.LifePower); }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("直接設定主角生活力 LifePower。")]
public class SetLifePower : FsmStateAction
{
    public FsmInt value;
    [UIHint(UIHint.Variable)] public FsmInt storeLifePower;
    public override void Reset() { value = 0; storeLifePower = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(SetLifePower));
        if (p != null) { p.SetLifePower(value.Value); ProtagonistPlayMakerUtil.StoreInt(storeLifePower, p.LifePower); }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("增減主角社會恐懼 SocialFear。")]
public class AddSocialFear : FsmStateAction
{
    public FsmInt amount;
    [UIHint(UIHint.Variable)] public FsmInt storeSocialFear;
    public override void Reset() { amount = 0; storeSocialFear = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(AddSocialFear));
        if (p != null) { p.AddSocialFear(amount.Value); ProtagonistPlayMakerUtil.StoreInt(storeSocialFear, p.SocialFear); }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("直接設定主角社會恐懼 SocialFear。")]
public class SetSocialFear : FsmStateAction
{
    public FsmInt value;
    [UIHint(UIHint.Variable)] public FsmInt storeSocialFear;
    public override void Reset() { value = 0; storeSocialFear = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(SetSocialFear));
        if (p != null) { p.SetSocialFear(value.Value); ProtagonistPlayMakerUtil.StoreInt(storeSocialFear, p.SocialFear); }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("增減主角依賴度 Dependency。")]
public class AddDependency : FsmStateAction
{
    public FsmInt amount;
    [UIHint(UIHint.Variable)] public FsmInt storeDependency;
    public override void Reset() { amount = 0; storeDependency = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(AddDependency));
        if (p != null) { p.AddDependency(amount.Value); ProtagonistPlayMakerUtil.StoreInt(storeDependency, p.Dependency); }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("直接設定主角依賴度 Dependency。")]
public class SetDependency : FsmStateAction
{
    public FsmInt value;
    [UIHint(UIHint.Variable)] public FsmInt storeDependency;
    public override void Reset() { value = 0; storeDependency = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(SetDependency));
        if (p != null) { p.SetDependency(value.Value); ProtagonistPlayMakerUtil.StoreInt(storeDependency, p.Dependency); }
        Finish();
    }
}

// ==========================================================
// Daily Flag Actions
// ==========================================================
[ActionCategory("Protagonist Status")]
[Tooltip("標記今天主角做過某項每日行為。")]
public class MarkProtagonistDailyFlag : FsmStateAction
{
    public enum DailyFlag
    {
        Bathed,
        CleanedRoom,
        HadMeal,
        CheckedMail,
        RepliedFamily,
        IgnoredPhone,
        Escaped,
        GoneOutsideSuccess,
        GoneOutsideFail,
        StressCollapsed
    }

    public DailyFlag flag;

    public override void Reset() { flag = DailyFlag.Bathed; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(MarkProtagonistDailyFlag));
        if (p != null)
        {
            switch (flag)
            {
                case DailyFlag.Bathed: p.MarkBathedToday(); break;
                case DailyFlag.CleanedRoom: p.MarkCleanedRoomToday(); break;
                case DailyFlag.HadMeal: p.MarkHadMealToday(); break;
                case DailyFlag.CheckedMail: p.MarkCheckedMailToday(); break;
                case DailyFlag.RepliedFamily: p.MarkRepliedFamilyToday(); break;
                case DailyFlag.IgnoredPhone: p.MarkIgnoredPhoneToday(); break;
                case DailyFlag.Escaped: p.MarkEscapedToday(); break;
                case DailyFlag.GoneOutsideSuccess: p.MarkGoneOutsideToday(true); break;
                case DailyFlag.GoneOutsideFail: p.MarkGoneOutsideToday(false); break;
                case DailyFlag.StressCollapsed: p.MarkStressCollapsedToday(); break;
            }
        }
        Finish();
    }
}

// ==========================================================
// Obsolete wrappers：保留舊 PlayMaker Action 名稱
// ==========================================================
[ActionCategory("Protagonist Status")]
[Tooltip("舊版 AddStamina。NHK 中轉換為 Stress 反向變化：Stamina + = Stress -。")]
public class AddStamina : FsmStateAction
{
    public FsmFloat amount;
    [UIHint(UIHint.Variable)] public FsmFloat storeStamina;
    public override void Reset() { amount = 0f; storeStamina = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(AddStamina));
        if (p != null)
        {
            p.AddStress(-Mathf.RoundToInt(amount.Value));
            ProtagonistPlayMakerUtil.StoreFloat(storeStamina, 100 - p.Stress);
        }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("舊版 TryReduceStamina。NHK 中視為壓力上升；若會超過 100 則失敗。")]
public class TryReduceStamina : FsmStateAction
{
    public FsmFloat cost;
    [UIHint(UIHint.Variable)] public FsmBool storeResult;
    [UIHint(UIHint.Variable)] public FsmFloat storeStamina;
    public FsmBool logWarningOnFail;
    public FsmEvent successEvent;
    public FsmEvent failedEvent;
    public override void Reset() { cost = 0f; storeResult = null; storeStamina = null; logWarningOnFail = true; successEvent = null; failedEvent = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(TryReduceStamina));
        if (p == null) { Finish(); return; }
        int addStress = Mathf.CeilToInt(cost.Value);
        bool success = p.Stress + addStress <= ProtagonistStatusModel.MAX_STATUS_VALUE;
        if (success) p.AddStress(addStress);
        ProtagonistPlayMakerUtil.StoreBool(storeResult, success);
        ProtagonistPlayMakerUtil.StoreFloat(storeStamina, 100 - p.Stress);
        if (!success && logWarningOnFail.Value) Debug.LogWarning("[TryReduceStamina] NHK 壓力會爆表，判定失敗。");
        Fsm.Event(success ? successEvent : failedEvent);
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("舊版 SetStamina。NHK 中轉換為 Stress = 100 - Stamina。")]
public class SetStamina : FsmStateAction
{
    public FsmFloat value;
    [UIHint(UIHint.Variable)] public FsmFloat storeStamina;
    public override void Reset() { value = 100f; storeStamina = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(SetStamina));
        if (p != null)
        {
            p.SetStress(100 - Mathf.RoundToInt(value.Value));
            ProtagonistPlayMakerUtil.StoreFloat(storeStamina, 100 - p.Stress);
        }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("舊版 SetStaminaMax。NHK Stress 固定 0～100，本 Action 不做事。")]
public class SetStaminaMax : FsmStateAction
{
    public FsmFloat value;
    [UIHint(UIHint.Variable)] public FsmFloat storeStaminaMax;
    [UIHint(UIHint.Variable)] public FsmFloat storeStamina;
    public override void Reset() { value = 100f; storeStaminaMax = null; storeStamina = null; }
    public override void OnEnter()
    {
        ProtagonistPlayMakerUtil.StoreFloat(storeStaminaMax, 100f);
        var p = GameStatusService.Instance?.Protagonist;
        if (p != null) ProtagonistPlayMakerUtil.StoreFloat(storeStamina, 100 - p.Stress);
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("舊版 AddStaminaMax。NHK Stress 固定 0～100，本 Action 不做事。")]
public class AddStaminaMax : FsmStateAction
{
    public FsmFloat delta;
    [UIHint(UIHint.Variable)] public FsmFloat storeStaminaMax;
    [UIHint(UIHint.Variable)] public FsmFloat storeStamina;
    public override void Reset() { delta = 0f; storeStaminaMax = null; storeStamina = null; }
    public override void OnEnter()
    {
        ProtagonistPlayMakerUtil.StoreFloat(storeStaminaMax, 100f);
        var p = GameStatusService.Instance?.Protagonist;
        if (p != null) ProtagonistPlayMakerUtil.StoreFloat(storeStamina, 100 - p.Stress);
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("舊版 AddSuspicion。NHK 中轉換為 Stress 變化。")]
public class AddSuspicion : FsmStateAction
{
    public FsmInt amount;
    [UIHint(UIHint.Variable)] public FsmInt storeSuspicion;
    public override void Reset() { amount = 0; storeSuspicion = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(AddSuspicion));
        if (p != null) { p.AddStress(amount.Value); ProtagonistPlayMakerUtil.StoreInt(storeSuspicion, p.Stress); }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("舊版 SetSuspicion。NHK 中轉換為 SetStress。")]
public class SetSuspicion : FsmStateAction
{
    public FsmInt value;
    [UIHint(UIHint.Variable)] public FsmInt storeSuspicion;
    public override void Reset() { value = 0; storeSuspicion = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(SetSuspicion));
        if (p != null) { p.SetStress(value.Value); ProtagonistPlayMakerUtil.StoreInt(storeSuspicion, p.Stress); }
        Finish();
    }
}
