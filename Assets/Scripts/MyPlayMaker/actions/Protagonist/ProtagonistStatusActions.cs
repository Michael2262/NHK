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
// Daily Flow / Long-term Counters
// ==========================================================
[ActionCategory("Protagonist Status")]
[Tooltip("呼叫主角的 OnDayStart：重置每日狀態旗標。")]
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
[Tooltip("呼叫主角的 OnDayEnd：依當日旗標更新長期統計 (尚未進入下一天)。")]
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
[Tooltip("呼叫主角的 NextDay：結束當日 → 天數 +1 → 開啟新一日。")]
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
            ProtagonistPlayMakerUtil.StoreInt(storeDay, p.Day);
        }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("僅重置主角的每日狀態旗標 (不更新長期統計)。")]
public class ResetProtagonistDailyFlags : FsmStateAction
{
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(ResetProtagonistDailyFlags));
        if (p != null) p.ResetDailyFlags();
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("重置主角所有長期統計計數器。")]
public class ResetProtagonistLongTermCounters : FsmStateAction
{
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(ResetProtagonistLongTermCounters));
        if (p != null) p.ResetLongTermCounters();
        Finish();
    }
}

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

[ActionCategory("Protagonist Status")]
[Tooltip("增加主角的長期統計計數器 (NightExtend1 / NightExtend2 / StayOver)。")]
public class AddProtagonistLongTermCounter : FsmStateAction
{
    public enum CounterType
    {
        NightExtend1Success,
        NightExtend2Success,
        StayOver
    }

    public CounterType counter;
    public FsmInt amount;
    [UIHint(UIHint.Variable)] public FsmInt storeCount;

    public override void Reset() { counter = CounterType.NightExtend1Success; amount = 1; storeCount = null; }

    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(AddProtagonistLongTermCounter));
        if (p != null)
        {
            int n = amount.Value;
            int result = 0;
            switch (counter)
            {
                case CounterType.NightExtend1Success:
                    p.AddNightExtend1SuccessCount(n);
                    result = p.NightExtend1SuccessCount;
                    break;
                case CounterType.NightExtend2Success:
                    p.AddNightExtend2SuccessCount(n);
                    result = p.NightExtend2SuccessCount;
                    break;
                case CounterType.StayOver:
                    p.AddStayOverCount(n);
                    result = p.StayOverCount;
                    break;
            }
            ProtagonistPlayMakerUtil.StoreInt(storeCount, result);
        }
        Finish();
    }
}

// ==========================================================
// 狀態查詢
// ==========================================================
[ActionCategory("Protagonist Status")]
[Tooltip("查詢主角目前的壓力狀態 (Calm / Uneasy / Irritated / Strained / Critical / Collapsed)。")]
public class GetProtagonistStressState : FsmStateAction
{
    [UIHint(UIHint.Variable)] public FsmString storeStateName;
    [UIHint(UIHint.Variable)] public FsmInt storeStateIndex;
    public override void Reset() { storeStateName = null; storeStateIndex = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(GetProtagonistStressState));
        if (p != null)
        {
            var state = p.GetStressState();
            ProtagonistPlayMakerUtil.StoreString(storeStateName, state.ToString());
            ProtagonistPlayMakerUtil.StoreInt(storeStateIndex, (int)state);
        }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("查詢主角目前的生活力狀態 (VeryLow / Unstable / Stable / Healthy)。")]
public class GetProtagonistLifeState : FsmStateAction
{
    [UIHint(UIHint.Variable)] public FsmString storeStateName;
    [UIHint(UIHint.Variable)] public FsmInt storeStateIndex;
    public override void Reset() { storeStateName = null; storeStateIndex = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(GetProtagonistLifeState));
        if (p != null)
        {
            var state = p.GetLifeState();
            ProtagonistPlayMakerUtil.StoreString(storeStateName, state.ToString());
            ProtagonistPlayMakerUtil.StoreInt(storeStateIndex, (int)state);
        }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("查詢主角目前的社會性狀態 (Low / Medium / High)。")]
public class GetProtagonistSocialityState : FsmStateAction
{
    [UIHint(UIHint.Variable)] public FsmString storeStateName;
    [UIHint(UIHint.Variable)] public FsmInt storeStateIndex;
    public override void Reset() { storeStateName = null; storeStateIndex = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(GetProtagonistSocialityState));
        if (p != null)
        {
            var state = p.GetSocialityState();
            ProtagonistPlayMakerUtil.StoreString(storeStateName, state.ToString());
            ProtagonistPlayMakerUtil.StoreInt(storeStateIndex, (int)state);
        }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("查詢主角目前的依賴度狀態 (Low / Medium / High / Extreme)。")]
public class GetProtagonistDependencyState : FsmStateAction
{
    [UIHint(UIHint.Variable)] public FsmString storeStateName;
    [UIHint(UIHint.Variable)] public FsmInt storeStateIndex;
    public override void Reset() { storeStateName = null; storeStateIndex = null; }
    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(GetProtagonistDependencyState));
        if (p != null)
        {
            var state = p.GetDependencyState();
            ProtagonistPlayMakerUtil.StoreString(storeStateName, state.ToString());
            ProtagonistPlayMakerUtil.StoreInt(storeStateIndex, (int)state);
        }
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("檢查主角的某個 bool 狀態 (壓力 / 生活力 / 社會性 / 依賴度)。")]
public class CheckProtagonistStatus : FsmStateAction
{
    public enum CheckType
    {
        IsStressCritical,
        IsStressCollapsed,
        IsLifeVeryLow,
        IsLifeStable,
        IsLifeHealthy,
        IsSocialityHigh,
        IsSocialityLow,
        IsDependencyHigh,
        IsDependencyExtreme
    }

    public CheckType check;
    [UIHint(UIHint.Variable)] public FsmBool storeResult;
    public FsmEvent trueEvent;
    public FsmEvent falseEvent;

    public override void Reset() { check = CheckType.IsStressCritical; storeResult = null; trueEvent = null; falseEvent = null; }

    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(CheckProtagonistStatus));
        if (p == null) { Finish(); return; }

        bool result = false;
        switch (check)
        {
            case CheckType.IsStressCritical: result = p.IsStressCritical(); break;
            case CheckType.IsStressCollapsed: result = p.IsStressCollapsed(); break;
            case CheckType.IsLifeVeryLow: result = p.IsLifeVeryLow(); break;
            case CheckType.IsLifeStable: result = p.IsLifeStable(); break;
            case CheckType.IsLifeHealthy: result = p.IsLifeHealthy(); break;
            case CheckType.IsSocialityHigh: result = p.IsSocialityHigh(); break;
            case CheckType.IsSocialityLow: result = p.IsSocialityLow(); break;
            case CheckType.IsDependencyHigh: result = p.IsDependencyHigh(); break;
            case CheckType.IsDependencyExtreme: result = p.IsDependencyExtreme(); break;
        }

        ProtagonistPlayMakerUtil.StoreBool(storeResult, result);
        Fsm.Event(result ? trueEvent : falseEvent);
        Finish();
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("取得主角面對外界的成功評分 (FaceRealityScore)。可選擇與門檻比對並觸發事件。")]
public class GetProtagonistFaceRealityScore : FsmStateAction
{
    public FsmInt threshold;
    public FsmBool useThreshold;
    [UIHint(UIHint.Variable)] public FsmInt storeScore;
    [UIHint(UIHint.Variable)] public FsmBool storeCanLikelyFace;
    public FsmEvent successEvent;
    public FsmEvent failedEvent;

    public override void Reset()
    {
        threshold = 50; useThreshold = true;
        storeScore = null; storeCanLikelyFace = null;
        successEvent = null; failedEvent = null;
    }

    public override void OnEnter()
    {
        var p = ProtagonistPlayMakerUtil.GetProtagonist(nameof(GetProtagonistFaceRealityScore));
        if (p == null) { Finish(); return; }

        int score = p.GetFaceRealityScore();
        ProtagonistPlayMakerUtil.StoreInt(storeScore, score);

        if (useThreshold.Value)
        {
            bool canFace = p.CanLikelyFaceReality(threshold.Value);
            ProtagonistPlayMakerUtil.StoreBool(storeCanLikelyFace, canFace);
            Fsm.Event(canFace ? successEvent : failedEvent);
        }
        Finish();
    }
}
