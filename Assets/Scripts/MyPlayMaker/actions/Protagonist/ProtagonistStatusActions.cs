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

    public static void StoreString(FsmString variable, string value)
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
[Tooltip("扣除主角金錢 (數量為正數)。最低為 0。")]
public class ReduceMoney : FsmStateAction
{
    public FsmInt amount;
    [UIHint(UIHint.Variable)] public FsmInt storeMoney;

    public override void Reset() { amount = 0; storeMoney = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(ReduceMoney));
        if (p != null)
        {
            p.AddMoney(-Mathf.Max(0, amount.Value));
            ProtagonistPlayMakerUtil.StoreInt(storeMoney, p.Money);
        }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("直接設定主角金錢數量。")]
public class SetMoney : FsmStateAction
{
    public FsmInt value;
    [UIHint(UIHint.Variable)] public FsmInt storeMoney;

    public override void Reset() { value = 0; storeMoney = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(SetMoney));
        if (p != null)
        {
            p.SetMoney(value.Value);
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
[Tooltip("扣除主角技能點 (數量為正數)。最低為 0。")]
public class ReduceSkillPoints : FsmStateAction
{
    public FsmInt amount;
    [UIHint(UIHint.Variable)] public FsmInt storeSkillPoints;

    public override void Reset() { amount = 0; storeSkillPoints = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(ReduceSkillPoints));
        if (p != null)
        {
            p.ReduceSkillPoints(Mathf.Max(0, amount.Value));
            ProtagonistPlayMakerUtil.StoreInt(storeSkillPoints, p.SkillPoints);
        }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("直接設定主角技能點。")]
public class SetSkillPoints : FsmStateAction
{
    public FsmInt value;
    [UIHint(UIHint.Variable)] public FsmInt storeSkillPoints;

    public override void Reset() { value = 0; storeSkillPoints = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(SetSkillPoints));
        if (p != null)
        {
            p.SetSkillPoints(value.Value);
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
[Tooltip("降低主角壓力 Stress (數量為正數)。")]
public class ReduceStress : FsmStateAction
{
    public FsmInt amount;
    [UIHint(UIHint.Variable)] public FsmInt storeStress;
    public override void Reset() { amount = 0; storeStress = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(ReduceStress));
        if (p != null) { p.ReduceStress(Mathf.Max(0, amount.Value)); ProtagonistPlayMakerUtil.StoreInt(storeStress, p.Stress); }
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
[Tooltip("降低主角生活力 LifePower (數量為正數)。")]
public class ReduceLifePower : FsmStateAction
{
    public FsmInt amount;
    [UIHint(UIHint.Variable)] public FsmInt storeLifePower;
    public override void Reset() { amount = 0; storeLifePower = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(ReduceLifePower));
        if (p != null) { p.ReduceLifePower(Mathf.Max(0, amount.Value)); ProtagonistPlayMakerUtil.StoreInt(storeLifePower, p.LifePower); }
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
[Tooltip("增減主角社會性 Sociality。")]
public class AddSociality : FsmStateAction
{
    public FsmInt amount;
    [UIHint(UIHint.Variable)] public FsmInt storeSociality;
    public override void Reset() { amount = 0; storeSociality = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(AddSociality));
        if (p != null) { p.AddSociality(amount.Value); ProtagonistPlayMakerUtil.StoreInt(storeSociality, p.Sociality); }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("降低主角社會性 Sociality (數量為正數)。")]
public class ReduceSociality : FsmStateAction
{
    public FsmInt amount;
    [UIHint(UIHint.Variable)] public FsmInt storeSociality;
    public override void Reset() { amount = 0; storeSociality = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(ReduceSociality));
        if (p != null) { p.ReduceSociality(Mathf.Max(0, amount.Value)); ProtagonistPlayMakerUtil.StoreInt(storeSociality, p.Sociality); }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("直接設定主角社會性 Sociality。")]
public class SetSociality : FsmStateAction
{
    public FsmInt value;
    [UIHint(UIHint.Variable)] public FsmInt storeSociality;
    public override void Reset() { value = 0; storeSociality = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(SetSociality));
        if (p != null) { p.SetSociality(value.Value); ProtagonistPlayMakerUtil.StoreInt(storeSociality, p.Sociality); }
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
[Tooltip("降低主角依賴度 Dependency (數量為正數)。")]
public class ReduceDependency : FsmStateAction
{
    public FsmInt amount;
    [UIHint(UIHint.Variable)] public FsmInt storeDependency;
    public override void Reset() { amount = 0; storeDependency = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(ReduceDependency));
        if (p != null) { p.ReduceDependency(Mathf.Max(0, amount.Value)); ProtagonistPlayMakerUtil.StoreInt(storeDependency, p.Dependency); }
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

[ActionCategory("Protagonist Status")]
[Tooltip("一次套用主角四項核心數值的變化 (Stress / LifePower / Sociality / Dependency)。")]
public class ApplyProtagonistStatusChange : FsmStateAction
{
    public FsmInt stressDelta;
    public FsmInt lifePowerDelta;
    public FsmInt socialityDelta;
    public FsmInt dependencyDelta;

    [UIHint(UIHint.Variable)] public FsmInt storeStress;
    [UIHint(UIHint.Variable)] public FsmInt storeLifePower;
    [UIHint(UIHint.Variable)] public FsmInt storeSociality;
    [UIHint(UIHint.Variable)] public FsmInt storeDependency;

    public override void Reset()
    {
        stressDelta = 0; lifePowerDelta = 0; socialityDelta = 0; dependencyDelta = 0;
        storeStress = null; storeLifePower = null; storeSociality = null; storeDependency = null;
    }

    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(ApplyProtagonistStatusChange));
        if (p != null)
        {
            var change = new ProtagonistStatusChange(
                stressDelta.Value, lifePowerDelta.Value, socialityDelta.Value, dependencyDelta.Value);
            p.ApplyStatusChange(change);

            ProtagonistPlayMakerUtil.StoreInt(storeStress, p.Stress);
            ProtagonistPlayMakerUtil.StoreInt(storeLifePower, p.LifePower);
            ProtagonistPlayMakerUtil.StoreInt(storeSociality, p.Sociality);
            ProtagonistPlayMakerUtil.StoreInt(storeDependency, p.Dependency);
        }
        Finish();
    }
}

// ==========================================================
// Daily Flow (預留空殼，未來可加邏輯)
// ==========================================================
[ActionCategory("Protagonist Status")]
[Tooltip("呼叫主角的 OnDayStart（預留擴充點）。")]
public class ProtagonistDayStart : FsmStateAction
{
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(ProtagonistDayStart));
        if (p != null) p.OnDayStart();
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("呼叫主角的 OnDayEnd（預留擴充點）。")]
public class ProtagonistDayEnd : FsmStateAction
{
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(ProtagonistDayEnd));
        if (p != null) p.OnDayEnd();
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("呼叫主角的 NextDay（OnDayEnd → OnDayStart）。Day 由 TimeSystemModel 管理。")]
public class ProtagonistNextDay : FsmStateAction
{
    [UIHint(UIHint.Variable)] public FsmInt storeDay;
    public override void Reset() { storeDay = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(ProtagonistNextDay));
        if (p != null)
        {
            p.NextDay();
            // Day 由 TimeSystemModel.DayIndex 統一管理
            var time = GameStatusService.Instance?.Time;
            if (time != null)
                ProtagonistPlayMakerUtil.StoreInt(storeDay, time.DayIndex);
        }
        Finish();
    }
}

// ==========================================================
// 分級查詢（統一 StatusGrade enum）
// ==========================================================
[ActionCategory("Protagonist Status")]
[Tooltip("查詢主角指定數值的分級 (Low / Medium / High / Extreme)。")]
public class GetProtagonistGrade : FsmStateAction
{
    public enum GradeTarget
    {
        Stress,
        LifePower,
        Sociality,
        Dependency
    }

    public GradeTarget target;
    [UIHint(UIHint.Variable)] public FsmString storeGradeName;
    [UIHint(UIHint.Variable)] public FsmInt storeGradeIndex;

    public override void Reset() { target = GradeTarget.Stress; storeGradeName = null; storeGradeIndex = null; }

    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(GetProtagonistGrade));
        if (p != null)
        {
            StatusGrade grade = target switch
            {
                GradeTarget.Stress => p.GetStressGrade(),
                GradeTarget.LifePower => p.GetLifeGrade(),
                GradeTarget.Sociality => p.GetSocialityGrade(),
                GradeTarget.Dependency => p.GetDependencyGrade(),
                _ => StatusGrade.Low
            };

            ProtagonistPlayMakerUtil.StoreString(storeGradeName, grade.ToString());
            ProtagonistPlayMakerUtil.StoreInt(storeGradeIndex, (int)grade);
        }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("檢查主角的某個 bool 狀態。")]
public class CheckProtagonistStatus : FsmStateAction
{
    public enum CheckType
    {
        IsStressHigh,
        IsStressExtreme,
        IsLifeLow,
        IsLifeHigh,
        IsSocialityLow,
        IsSocialityHigh,
        IsDependencyHigh,
        IsDependencyExtreme,
        IsOverShoot
    }

    public CheckType check;
    [UIHint(UIHint.Variable)] public FsmBool storeResult;
    public FsmEvent trueEvent;
    public FsmEvent falseEvent;

    public override void Reset() { check = CheckType.IsStressHigh; storeResult = null; trueEvent = null; falseEvent = null; }

    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(CheckProtagonistStatus));
        if (p == null) { Finish(); return; }

        bool result = check switch
        {
            CheckType.IsStressHigh => p.IsStressHigh(),
            CheckType.IsStressExtreme => p.IsStressExtreme(),
            CheckType.IsLifeLow => p.IsLifeLow(),
            CheckType.IsLifeHigh => p.IsLifeHigh(),
            CheckType.IsSocialityLow => p.IsSocialityLow(),
            CheckType.IsSocialityHigh => p.IsSocialityHigh(),
            CheckType.IsDependencyHigh => p.IsDependencyHigh(),
            CheckType.IsDependencyExtreme => p.IsDependencyExtreme(),
            CheckType.IsOverShoot => p.IsOverShoot,
            _ => false
        };

        ProtagonistPlayMakerUtil.StoreBool(storeResult, result);
        Fsm.Event(result ? trueEvent : falseEvent);
        Finish();
    }
}

// ==========================================================
// ShootTimes（射精次數）
// ==========================================================
[ActionCategory("Protagonist Status")]
[Tooltip("查詢主角目前的射精次數 ShootTimes，以及是否處於 IsOverShoot 狀態。")]
public class CheckShootTimesAction : FsmStateAction
{
    [UIHint(UIHint.Variable)] public FsmInt storeShootTimes;
    [UIHint(UIHint.Variable)] public FsmBool storeIsOverShoot;

    public override void Reset() { storeShootTimes = null; storeIsOverShoot = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(CheckShootTimesAction));
        if (p != null)
        {
            ProtagonistPlayMakerUtil.StoreInt(storeShootTimes, p.CheckShootTimes());
            ProtagonistPlayMakerUtil.StoreBool(storeIsOverShoot, p.IsOverShoot);
        }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("增加主角射精次數。無上限，可以一直加。")]
public class AddShootTimesAction : FsmStateAction
{
    public FsmInt amount;
    [UIHint(UIHint.Variable)] public FsmInt storeShootTimes;

    public override void Reset() { amount = 1; storeShootTimes = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(AddShootTimesAction));
        if (p != null)
        {
            p.AddShootTimes(amount.Value);
            ProtagonistPlayMakerUtil.StoreInt(storeShootTimes, p.CheckShootTimes());
        }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("嘗試消耗主角射精次數。到達下限時回傳失敗，可走 failedEvent 分支。")]
public class TryReduceShootTimesAction : FsmStateAction
{
    public FsmInt amount;
    [UIHint(UIHint.Variable)] public FsmBool storeResult;
    [UIHint(UIHint.Variable)] public FsmInt storeShootTimes;
    public FsmEvent successEvent;
    public FsmEvent failedEvent;

    public override void Reset() { amount = 1; storeResult = null; storeShootTimes = null; successEvent = null; failedEvent = null; }

    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(TryReduceShootTimesAction));
        if (p == null) { Finish(); return; }

        bool success = p.TryReduceShootTimes(amount.Value);
        ProtagonistPlayMakerUtil.StoreBool(storeResult, success);
        ProtagonistPlayMakerUtil.StoreInt(storeShootTimes, p.CheckShootTimes());
        Fsm.Event(success ? successEvent : failedEvent);
        Finish();
    }
}