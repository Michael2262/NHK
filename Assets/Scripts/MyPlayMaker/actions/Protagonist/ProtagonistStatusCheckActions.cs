using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

public enum ProtagonistCompareMode
{
    GreaterThan,
    GreaterOrEqual,
    Equal,
    LessOrEqual,
    LessThan
}

internal static class ProtagonistCheckUtil
{
    public static bool CompareInt(int current, ProtagonistCompareMode mode, int threshold)
    {
        switch (mode)
        {
            case ProtagonistCompareMode.GreaterThan: return current > threshold;
            case ProtagonistCompareMode.GreaterOrEqual: return current >= threshold;
            case ProtagonistCompareMode.Equal: return current == threshold;
            case ProtagonistCompareMode.LessOrEqual: return current <= threshold;
            case ProtagonistCompareMode.LessThan: return current < threshold;
            default: return false;
        }
    }

    public static void Fire(Fsm fsm, bool result, FsmEvent trueEvent, FsmEvent falseEvent)
    {
        fsm.Event(result ? trueEvent : falseEvent);
    }
}

public abstract class ProtagonistIntCheckActionBase : FsmStateAction
{
    [RequiredField]
    public ProtagonistCompareMode compareMode = ProtagonistCompareMode.GreaterOrEqual;

    [RequiredField]
    public FsmInt threshold;

    [UIHint(UIHint.Variable)]
    public FsmInt storeValue;

    public FsmEvent trueEvent;
    public FsmEvent falseEvent;
    public bool everyFrame;
    public FsmBool onlyFireOnChange;

    private bool _lastResult;
    private bool _initialized;

    protected abstract string DebugName { get; }
    protected abstract int GetCurrentValue(ProtagonistStatusModel protagonist);

    public override void Reset()
    {
        compareMode = ProtagonistCompareMode.GreaterOrEqual;
        threshold = 0;
        storeValue = null;
        trueEvent = null;
        falseEvent = null;
        everyFrame = false;
        onlyFireOnChange = true;
    }

    public override void OnEnter()
    {
        _initialized = false;
        DoCheck();
        if (!everyFrame) Finish();
    }

    public override void OnUpdate()
    {
        DoCheck();
    }

    private void DoCheck()
    {
        var protagonist = GameStatusService.Instance?.Protagonist;
        if (protagonist == null)
        {
            if (!everyFrame) Debug.LogWarning($"[{DebugName}] 找不到主角 ProtagonistStatusModel");
            return;
        }

        int current = GetCurrentValue(protagonist);
        if (storeValue != null && !storeValue.IsNone) storeValue.Value = current;

        bool result = ProtagonistCheckUtil.CompareInt(current, compareMode, threshold.Value);
        if (everyFrame)
        {
            bool shouldFire = !onlyFireOnChange.Value || !_initialized || result != _lastResult;
            if (shouldFire)
            {
                _lastResult = result;
                _initialized = true;
                ProtagonistCheckUtil.Fire(Fsm, result, trueEvent, falseEvent);
            }
        }
        else
        {
            ProtagonistCheckUtil.Fire(Fsm, result, trueEvent, falseEvent);
        }
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("比較主角壓力 Stress 與指定閾值。")]
public class CheckStress : ProtagonistIntCheckActionBase
{
    protected override string DebugName => nameof(CheckStress);
    protected override int GetCurrentValue(ProtagonistStatusModel protagonist) => protagonist.Stress;
}

[ActionCategory("Protagonist Status")]
[Tooltip("比較主角生活力 LifePower 與指定閾值。")]
public class CheckLifePower : ProtagonistIntCheckActionBase
{
    protected override string DebugName => nameof(CheckLifePower);
    protected override int GetCurrentValue(ProtagonistStatusModel protagonist) => protagonist.LifePower;
}

[ActionCategory("Protagonist Status")]
[Tooltip("比較主角社會恐懼 SocialFear 與指定閾值。")]
public class CheckSocialFear : ProtagonistIntCheckActionBase
{
    protected override string DebugName => nameof(CheckSocialFear);
    protected override int GetCurrentValue(ProtagonistStatusModel protagonist) => protagonist.SocialFear;
}

[ActionCategory("Protagonist Status")]
[Tooltip("比較主角依賴度 Dependency 與指定閾值。")]
public class CheckDependency : ProtagonistIntCheckActionBase
{
    protected override string DebugName => nameof(CheckDependency);
    protected override int GetCurrentValue(ProtagonistStatusModel protagonist) => protagonist.Dependency;
}

[ActionCategory("Protagonist Status")]
[Tooltip("比較主角金錢 Money 與指定閾值。")]
public class CheckMoney : ProtagonistIntCheckActionBase
{
    protected override string DebugName => nameof(CheckMoney);
    protected override int GetCurrentValue(ProtagonistStatusModel protagonist) => protagonist.Money;
}

[ActionCategory("Protagonist Status")]
[Tooltip("比較主角技能點 SkillPoints 與指定閾值。")]
public class CheckSkillPoints : ProtagonistIntCheckActionBase
{
    protected override string DebugName => nameof(CheckSkillPoints);
    protected override int GetCurrentValue(ProtagonistStatusModel protagonist) => protagonist.SkillPoints;
}

// ==========================================================
// Obsolete compatibility checks
// ==========================================================
[ActionCategory("Protagonist Status")]
[Tooltip("舊版 CheckStamina。NHK 中以 MentalCapacity = 100 - Stress 進行比較。")]
public class CheckStamina : FsmStateAction
{
    public enum CompareMode { GreaterThan, GreaterOrEqual, Equal, LessOrEqual, LessThan }
    public CompareMode compareMode = CompareMode.GreaterOrEqual;
    public FsmFloat threshold;
    [UIHint(UIHint.Variable)] public FsmFloat storeStamina;
    public FsmEvent trueEvent;
    public FsmEvent falseEvent;
    public bool everyFrame;
    public FsmBool onlyFireOnChange;

    private bool _lastResult;
    private bool _initialized;

    public override void Reset()
    {
        compareMode = CompareMode.GreaterOrEqual;
        threshold = 0f;
        storeStamina = null;
        trueEvent = null;
        falseEvent = null;
        everyFrame = false;
        onlyFireOnChange = true;
    }

    public override void OnEnter()
    {
        _initialized = false;
        DoCheck();
        if (!everyFrame) Finish();
    }

    public override void OnUpdate() => DoCheck();

    private void DoCheck()
    {
        var p = GameStatusService.Instance?.Protagonist;
        if (p == null) return;
        float current = 100 - p.Stress;
        if (storeStamina != null && !storeStamina.IsNone) storeStamina.Value = current;

        bool result = false;
        switch (compareMode)
        {
            case CompareMode.GreaterThan: result = current > threshold.Value; break;
            case CompareMode.GreaterOrEqual: result = current >= threshold.Value; break;
            case CompareMode.Equal: result = Mathf.Approximately(current, threshold.Value); break;
            case CompareMode.LessOrEqual: result = current <= threshold.Value; break;
            case CompareMode.LessThan: result = current < threshold.Value; break;
        }

        if (everyFrame)
        {
            bool shouldFire = !onlyFireOnChange.Value || !_initialized || result != _lastResult;
            if (shouldFire)
            {
                _lastResult = result;
                _initialized = true;
                Fsm.Event(result ? trueEvent : falseEvent);
            }
        }
        else Fsm.Event(result ? trueEvent : falseEvent);
    }
}

[ActionCategory("Protagonist Status")]
[Tooltip("舊版 CheckShootTimes。NHK 已移除 ShootTimes；此 Action 固定回傳 false，保留僅為避免 class 遺失。")]
public class CheckShootTimes : FsmStateAction
{
    public enum CompareMode { GreaterThan, GreaterOrEqual, Equal, LessOrEqual, LessThan }
    public CompareMode compareMode;
    public FsmInt threshold;
    [UIHint(UIHint.Variable)] public FsmInt storeShootTimes;
    public FsmEvent trueEvent;
    public FsmEvent falseEvent;
    public bool everyFrame;
    public FsmBool onlyFireOnChange;
    public override void Reset() { threshold = 0; storeShootTimes = null; trueEvent = null; falseEvent = null; everyFrame = false; onlyFireOnChange = true; }
    public override void OnEnter() { if (storeShootTimes != null && !storeShootTimes.IsNone) storeShootTimes.Value = 0; Fsm.Event(falseEvent); if (!everyFrame) Finish(); }
    public override void OnUpdate() { if (everyFrame) Fsm.Event(falseEvent); }
}

[ActionCategory("Protagonist Status")]
[Tooltip("舊版 CheckShootTimesOverdrawing。NHK 已移除 ShootTimes；固定回傳 notOverdrawingEvent。")]
public class CheckShootTimesOverdrawing : FsmStateAction
{
    public FsmInt shootTimesThreshold;
    [UIHint(UIHint.Variable)] public FsmInt storeShootTimes;
    public FsmEvent overdrawingEvent;
    public FsmEvent notOverdrawingEvent;
    public bool everyFrame;
    public FsmBool onlyFireOnChange;
    public override void Reset() { shootTimesThreshold = 0; storeShootTimes = null; overdrawingEvent = null; notOverdrawingEvent = null; everyFrame = false; onlyFireOnChange = true; }
    public override void OnEnter() { if (storeShootTimes != null && !storeShootTimes.IsNone) storeShootTimes.Value = 0; Fsm.Event(notOverdrawingEvent); if (!everyFrame) Finish(); }
    public override void OnUpdate() { if (everyFrame) Fsm.Event(notOverdrawingEvent); }
}

[ActionCategory("Protagonist Status")]
[Tooltip("舊版 CheckShootTimesExhausted。NHK 已移除 ShootTimes；固定回傳 notExhaustedEvent。")]
public class CheckShootTimesExhausted : FsmStateAction
{
    [UIHint(UIHint.Variable)] public FsmInt storeShootTimes;
    public FsmEvent exhaustedEvent;
    public FsmEvent notExhaustedEvent;
    public bool everyFrame;
    public FsmBool onlyFireOnChange;
    public override void Reset() { storeShootTimes = null; exhaustedEvent = null; notExhaustedEvent = null; everyFrame = false; onlyFireOnChange = true; }
    public override void OnEnter() { if (storeShootTimes != null && !storeShootTimes.IsNone) storeShootTimes.Value = 0; Fsm.Event(notExhaustedEvent); if (!everyFrame) Finish(); }
    public override void OnUpdate() { if (everyFrame) Fsm.Event(notExhaustedEvent); }
}

[ActionCategory("Protagonist Status")]
[Tooltip("舊版 CheckEnergyDrinkCharges。NHK 已移除 EnergyDrink；固定回傳 falseEvent。")]
public class CheckEnergyDrinkCharges : FsmStateAction
{
    public enum CompareMode { GreaterThan, GreaterOrEqual, Equal, LessOrEqual, LessThan }
    public CompareMode compareMode;
    public FsmInt threshold;
    [UIHint(UIHint.Variable)] public FsmInt storeEnergyDrinkCharges;
    public FsmEvent trueEvent;
    public FsmEvent falseEvent;
    public bool everyFrame;
    public FsmBool onlyFireOnChange;
    public override void Reset() { threshold = 0; storeEnergyDrinkCharges = null; trueEvent = null; falseEvent = null; everyFrame = false; onlyFireOnChange = true; }
    public override void OnEnter() { if (storeEnergyDrinkCharges != null && !storeEnergyDrinkCharges.IsNone) storeEnergyDrinkCharges.Value = 0; Fsm.Event(falseEvent); if (!everyFrame) Finish(); }
    public override void OnUpdate() { if (everyFrame) Fsm.Event(falseEvent); }
}
