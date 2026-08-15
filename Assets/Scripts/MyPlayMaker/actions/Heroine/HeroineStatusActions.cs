using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

// ============================================================
// 共用 Enum
// ============================================================

public enum HeroineEmotionDrawKind
{
    Small,
    Medium,
    Big,
    FakeBig
}

public enum HeroineEmotionCardChangeOperation
{
    AddOrReplace,
    RemoveOne
}

/// <summary>
/// 數值比較運算子，供多個 Check 系列 Action 共用。
/// </summary>
public enum HeroineCompareOp
{
    GreaterThan,
    GreaterOrEqual,
    Equal,
    LessThan,
    LessOrEqual
}

// ============================================================
// 共用 Helper
// ============================================================

/// <summary>
/// 靜態工具：從 GameStatusService.Heroines 查找指定女主角。
/// 所有 Heroine Status Action 都透過此方法取得 Model。
/// </summary>
public static class HeroineActionHelper
{
    public static bool TryGetHeroine(string id, out HeroineStatusModel heroine, string logPrefix)
    {
        heroine = null;

        if (GameStatusService.Instance == null || GameStatusService.Instance.Heroines == null)
        {
            Debug.LogWarning($"[{logPrefix}] GameStatusService 或 Heroines 尚未準備好。");
            return false;
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning($"[{logPrefix}] heroineID 是空的。");
            return false;
        }

        if (!GameStatusService.Instance.Heroines.TryGetValue(id, out heroine) || heroine == null)
        {
            Debug.LogWarning($"[{logPrefix}] 找不到女主角: {id}");
            return false;
        }

        return true;
    }

    public static bool Compare(int value, int threshold, HeroineCompareOp op)
    {
        switch (op)
        {
            case HeroineCompareOp.GreaterThan: return value > threshold;
            case HeroineCompareOp.GreaterOrEqual: return value >= threshold;
            case HeroineCompareOp.Equal: return value == threshold;
            case HeroineCompareOp.LessThan: return value < threshold;
            case HeroineCompareOp.LessOrEqual: return value <= threshold;
            default: return false;
        }
    }

    /// <summary>
    /// 共用路線分發：依情緒類型回傳對應 event，未設定則 fallback 到 defaultEvent。
    /// </summary>
    public static FsmEvent GetEmotionRouteEvent(
        HeroineEmotionCardType type,
        FsmEvent angryEvent,
        FsmEvent shyEvent,
        FsmEvent worriedEvent,
        FsmEvent maternalEvent,
        FsmEvent relaxedEvent,
        FsmEvent disappointedEvent,
        FsmEvent normalEvent,
        FsmEvent defaultEvent)
    {
        switch (type)
        {
            case HeroineEmotionCardType.Normal: return normalEvent ?? defaultEvent;
            case HeroineEmotionCardType.Angry: return angryEvent ?? defaultEvent;
            case HeroineEmotionCardType.Shy: return shyEvent ?? defaultEvent;
            case HeroineEmotionCardType.Worried: return worriedEvent ?? defaultEvent;
            case HeroineEmotionCardType.Maternal: return maternalEvent ?? defaultEvent;
            case HeroineEmotionCardType.Relaxed: return relaxedEvent ?? defaultEvent;
            case HeroineEmotionCardType.Disappointed: return disappointedEvent ?? defaultEvent;
            default: return defaultEvent;
        }
    }
}

// ############################################################
//  情緒卡 — 增減
// ############################################################

// ============================================================
// SetEmotion（舊名保留，功能同 AddEmotionCard）
// ============================================================
[ActionCategory("Heroine Status")]
[Tooltip("舊名保留：新增指定情緒卡（等同 AddEmotionCard）。")]
public class SetEmotion : FsmStateAction
{
    [RequiredField] public FsmString heroineID;

    [RequiredField]
    [ObjectType(typeof(HeroineEmotionCardType))]
    public FsmEnum newEmotion;

    public override void Reset()
    {
        heroineID = null;
        newEmotion = null;
    }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var heroine, "SetEmotion"))
            heroine.ReplaceEmotionCard((HeroineEmotionCardType)newEmotion.Value);
        Finish();
    }
}

// ============================================================
// AddEmotionCard
// ============================================================
[ActionCategory("Heroine Status")]
[Tooltip("新增一張情緒卡（依規則自動替換最舊的對立卡）。")]
public class AddEmotionCard : FsmStateAction
{
    [RequiredField] public FsmString heroineID;

    [RequiredField]
    [ObjectType(typeof(HeroineEmotionCardType))]
    public FsmEnum emotion;

    public override void Reset()
    {
        heroineID = null;
        emotion = HeroineEmotionCardType.Angry;
    }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var heroine, "AddEmotionCard"))
            heroine.ReplaceEmotionCard((HeroineEmotionCardType)emotion.Value);
        Finish();
    }
}

// ============================================================
// RemoveEmotionCard
// ============================================================
[ActionCategory("Heroine Status")]
[Tooltip("移除一張指定情緒卡。卡池沒有該情緒則失敗。")]
public class RemoveEmotionCard : FsmStateAction
{
    [RequiredField] public FsmString heroineID;

    [RequiredField]
    [ObjectType(typeof(HeroineEmotionCardType))]
    public FsmEnum emotion;

    [UIHint(UIHint.Variable)]
    public FsmBool storeSuccess;

    public FsmEvent successEvent;
    public FsmEvent failedEvent;

    public override void Reset()
    {
        heroineID = null;
        emotion = HeroineEmotionCardType.Angry;
        storeSuccess = null;
        successEvent = null;
        failedEvent = null;
    }

    public override void OnEnter()
    {
        bool success = false;

        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var heroine, "RemoveEmotionCard"))
            success = heroine.RemoveOneCardOfType((HeroineEmotionCardType)emotion.Value);

        if (storeSuccess != null && !storeSuccess.IsNone)
            storeSuccess.Value = success;

        Fsm.Event(success ? successEvent : failedEvent);
        Finish();
    }
}

// ============================================================
// HeroineEmotionCardChange（合併版：選 AddOrReplace / RemoveOne）
// ============================================================
[ActionCategory("Heroine Status")]
[Tooltip("女主角情緒卡增減。AddOrReplace 新增/替換；RemoveOne 移除一張。")]
public class HeroineEmotionCardChange : FsmStateAction
{
    [RequiredField] public FsmString heroineID;

    [ObjectType(typeof(HeroineEmotionCardChangeOperation))]
    public FsmEnum operation;

    [RequiredField]
    [ObjectType(typeof(HeroineEmotionCardType))]
    public FsmEnum emotion;

    [UIHint(UIHint.Variable)]
    public FsmBool storeSuccess;

    public FsmEvent successEvent;
    public FsmEvent failedEvent;

    public override void Reset()
    {
        heroineID = null;
        operation = HeroineEmotionCardChangeOperation.AddOrReplace;
        emotion = HeroineEmotionCardType.Angry;
        storeSuccess = null;
        successEvent = null;
        failedEvent = null;
    }

    public override void OnEnter()
    {
        if (!HeroineActionHelper.TryGetHeroine(heroineID.Value, out var heroine, "HeroineEmotionCardChange"))
        {
            SetResult(false);
            Fsm.Event(failedEvent);
            Finish();
            return;
        }

        var type = (HeroineEmotionCardType)emotion.Value;
        var op = operation != null && operation.Value != null
            ? (HeroineEmotionCardChangeOperation)operation.Value
            : HeroineEmotionCardChangeOperation.AddOrReplace;

        bool success = true;
        switch (op)
        {
            case HeroineEmotionCardChangeOperation.RemoveOne:
                success = heroine.RemoveOneCardOfType(type);
                break;
            default:
                heroine.ReplaceEmotionCard(type);
                break;
        }

        SetResult(success);
        Fsm.Event(success ? successEvent : failedEvent);
        Finish();
    }

    private void SetResult(bool success)
    {
        if (storeSuccess != null && !storeSuccess.IsNone)
            storeSuccess.Value = success;
    }
}

// ############################################################
//  情緒卡 — 檢查數量
// ############################################################

// ============================================================
// CheckEmotionCardCount
// ============================================================
[ActionCategory("Heroine Status")]
[Tooltip("檢查指定情緒卡數量是否符合條件（>, >=, ==, <, <=）。")]
public class CheckEmotionCardCount : FsmStateAction
{
    [RequiredField] public FsmString heroineID;

    [RequiredField]
    [ObjectType(typeof(HeroineEmotionCardType))]
    public FsmEnum emotion;

    [ObjectType(typeof(HeroineCompareOp))]
    public FsmEnum compareOperator;

    [RequiredField] public FsmInt threshold;

    [UIHint(UIHint.Variable)]
    public FsmInt storeCount;

    public FsmEvent passEvent;
    public FsmEvent failEvent;

    public override void Reset()
    {
        heroineID = null;
        emotion = HeroineEmotionCardType.Angry;
        compareOperator = HeroineCompareOp.GreaterOrEqual;
        threshold = 3;
        storeCount = null;
        passEvent = null;
        failEvent = null;
    }

    public override void OnEnter()
    {
        if (!HeroineActionHelper.TryGetHeroine(heroineID.Value, out var heroine, "CheckEmotionCardCount"))
        {
            Fsm.Event(failEvent);
            Finish();
            return;
        }

        int count = heroine.GetCardCount((HeroineEmotionCardType)emotion.Value);

        if (storeCount != null && !storeCount.IsNone)
            storeCount.Value = count;

        var op = compareOperator != null && compareOperator.Value != null
            ? (HeroineCompareOp)compareOperator.Value
            : HeroineCompareOp.GreaterOrEqual;

        Fsm.Event(HeroineActionHelper.Compare(count, threshold.Value, op) ? passEvent : failEvent);
        Finish();
    }
}

// ############################################################
//  主導情緒（CurrentEmotion）— 多情緒分支
// ############################################################

// ============================================================
// CheckCurrentEmotion
// ============================================================
[ActionCategory("Heroine Status")]
[Tooltip("檢查主導情緒（CurrentEmotion），依情緒類型送出對應路線事件。未設定的情緒走 defaultEvent。")]
public class CheckCurrentEmotion : FsmStateAction
{
    [RequiredField] public FsmString heroineID;

    [UIHint(UIHint.Variable)]
    [ObjectType(typeof(HeroineEmotionCardType))]
    public FsmEnum storeCurrent;

    [UIHint(UIHint.Variable)]
    public FsmString storeCurrentString;

    [Header("Route Events")]
    public FsmEvent angryEvent;
    public FsmEvent shyEvent;
    public FsmEvent worriedEvent;
    public FsmEvent maternalEvent;
    public FsmEvent relaxedEvent;
    public FsmEvent disappointedEvent;
    [Tooltip("Normal（普通）情緒的路線事件。未設定時走 defaultEvent。")]
    public FsmEvent normalEvent;
    public FsmEvent defaultEvent;
    public FsmEvent failedEvent;

    public override void Reset()
    {
        heroineID = null;
        storeCurrent = null;
        storeCurrentString = null;
        angryEvent = null;
        shyEvent = null;
        worriedEvent = null;
        maternalEvent = null;
        relaxedEvent = null;
        disappointedEvent = null;
        normalEvent = null;
        defaultEvent = null;
        failedEvent = null;
    }

    public override void OnEnter()
    {
        if (!HeroineActionHelper.TryGetHeroine(heroineID.Value, out var heroine, "CheckCurrentEmotion"))
        {
            Fsm.Event(failedEvent);
            Finish();
            return;
        }

        var current = heroine.CurrentEmotion;

        if (storeCurrent != null && !storeCurrent.IsNone)
            storeCurrent.Value = current;
        if (storeCurrentString != null && !storeCurrentString.IsNone)
            storeCurrentString.Value = current.ToString();

        Fsm.Event(HeroineActionHelper.GetEmotionRouteEvent(
            current,
            angryEvent, shyEvent, worriedEvent,
            maternalEvent, relaxedEvent, disappointedEvent,
            normalEvent, defaultEvent));
        Finish();
    }
}

// ============================================================
// SetCurrentEmotionAction
// ============================================================
[ActionCategory("Heroine Status")]
[Tooltip("設定女主角的主導情緒（CurrentEmotion）。")]
public class SetCurrentEmotionAction : FsmStateAction
{
    [RequiredField] public FsmString heroineID;

    [RequiredField]
    [ObjectType(typeof(HeroineEmotionCardType))]
    public FsmEnum newEmotion;

    public override void Reset()
    {
        heroineID = null;
        newEmotion = HeroineEmotionCardType.Angry;
    }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var heroine, "SetCurrentEmotionAction"))
            heroine.SetCurrentEmotion((HeroineEmotionCardType)newEmotion.Value);
        Finish();
    }
}

// ############################################################
//  大宗情緒（DominantEmotion）— 多情緒分支
// ############################################################

// ============================================================
// CheckDominantEmotion
// ============================================================
[ActionCategory("Heroine Status")]
[Tooltip("檢查大宗情緒（DominantEmotion，卡池最多者），依情緒類型送出對應路線事件。未設定的情緒走 defaultEvent。")]
public class CheckDominantEmotion : FsmStateAction
{
    [RequiredField] public FsmString heroineID;

    [UIHint(UIHint.Variable)]
    [ObjectType(typeof(HeroineEmotionCardType))]
    public FsmEnum storeDominant;

    [UIHint(UIHint.Variable)]
    public FsmString storeDominantString;

    [Header("Route Events")]
    public FsmEvent angryEvent;
    public FsmEvent shyEvent;
    public FsmEvent worriedEvent;
    public FsmEvent maternalEvent;
    public FsmEvent relaxedEvent;
    public FsmEvent disappointedEvent;
    [Tooltip("Normal（普通）情緒的路線事件。未設定時走 defaultEvent。")]
    public FsmEvent normalEvent;
    public FsmEvent defaultEvent;
    public FsmEvent failedEvent;

    public override void Reset()
    {
        heroineID = null;
        storeDominant = null;
        storeDominantString = null;
        angryEvent = null;
        shyEvent = null;
        worriedEvent = null;
        maternalEvent = null;
        relaxedEvent = null;
        disappointedEvent = null;
        normalEvent = null;
        defaultEvent = null;
        failedEvent = null;
    }

    public override void OnEnter()
    {
        if (!HeroineActionHelper.TryGetHeroine(heroineID.Value, out var heroine, "CheckDominantEmotion"))
        {
            Fsm.Event(failedEvent);
            Finish();
            return;
        }

        var dominant = heroine.DominantEmotion;

        if (storeDominant != null && !storeDominant.IsNone)
            storeDominant.Value = dominant;
        if (storeDominantString != null && !storeDominantString.IsNone)
            storeDominantString.Value = dominant.ToString();

        Fsm.Event(HeroineActionHelper.GetEmotionRouteEvent(
            dominant,
            angryEvent, shyEvent, worriedEvent,
            maternalEvent, relaxedEvent, disappointedEvent,
            normalEvent, defaultEvent));
        Finish();
    }
}

// ############################################################
//  情緒卡抽選（HeroineEmotionDraw）
// ############################################################

[ActionCategory("Heroine Status")]
[Tooltip("女主角情緒卡抽選。可有表演/無表演、小/中/大/造假大抽選，並依結果送出路線事件。")]
public class HeroineEmotionDraw : FsmStateAction
{
    [RequiredField] public FsmString heroineID;

    [ObjectType(typeof(HeroineEmotionDrawKind))]
    public FsmEnum drawKind;

    public FsmBool playShow;

    [ObjectType(typeof(HeroineEmotionCardType))]
    [Tooltip("造假大抽選要指定的結果。只有 drawKind=FakeBig 時使用。")]
    public FsmEnum fakeResult;

    [UIHint(UIHint.Variable)]
    [ObjectType(typeof(HeroineEmotionCardType))]
    public FsmEnum storeResult;

    [UIHint(UIHint.Variable)]
    public FsmString storeResultString;

    [UIHint(UIHint.Variable)]
    public FsmBool storeFakeSucceeded;

    [Header("Route Events")]
    public FsmEvent angryEvent;
    public FsmEvent shyEvent;
    public FsmEvent worriedEvent;
    public FsmEvent maternalEvent;
    public FsmEvent relaxedEvent;
    public FsmEvent disappointedEvent;
    [Tooltip("Normal（普通）情緒的路線事件。未設定時走 defaultEvent。")]
    public FsmEvent normalEvent;
    public FsmEvent defaultEvent;
    public FsmEvent failedEvent;

    private bool cancelled;

    public override void Reset()
    {
        heroineID = null;
        drawKind = HeroineEmotionDrawKind.Big;
        playShow = true;
        fakeResult = HeroineEmotionCardType.Angry;
        storeResult = null;
        storeResultString = null;
        storeFakeSucceeded = null;
        angryEvent = null;
        shyEvent = null;
        worriedEvent = null;
        maternalEvent = null;
        relaxedEvent = null;
        disappointedEvent = null;
        normalEvent = null;
        defaultEvent = null;
        failedEvent = null;
    }

    public override void OnEnter()
    {
        cancelled = false;

        if (EmotionCardDrawMachine.Instance == null)
        {
            Debug.LogWarning("[HeroineEmotionDraw] 場景中找不到 EmotionCardDrawMachine。");
            SendAndFinish(failedEvent);
            return;
        }

        var kind = drawKind != null && drawKind.Value != null
            ? (HeroineEmotionDrawKind)drawKind.Value
            : HeroineEmotionDrawKind.Big;

        bool show = playShow == null || playShow.Value;
        string id = heroineID != null ? heroineID.Value : string.Empty;

        if (!show)
        {
            EmotionDrawResult result;
            switch (kind)
            {
                case HeroineEmotionDrawKind.Small: result = EmotionCardDrawMachine.Instance.DrawSmallWithoutShow(id); break;
                case HeroineEmotionDrawKind.Medium: result = EmotionCardDrawMachine.Instance.DrawMediumWithoutShow(id); break;
                case HeroineEmotionDrawKind.FakeBig: result = EmotionCardDrawMachine.Instance.DrawFakeBigWithoutShow(id, GetFake()); break;
                default: result = EmotionCardDrawMachine.Instance.DrawBigWithoutShow(id); break;
            }
            HandleResult(result);
            return;
        }

        switch (kind)
        {
            case HeroineEmotionDrawKind.Small: EmotionCardDrawMachine.Instance.StartSmallDraw(id, HandleResult); break;
            case HeroineEmotionDrawKind.Medium: EmotionCardDrawMachine.Instance.StartMediumDraw(id, HandleResult); break;
            case HeroineEmotionDrawKind.FakeBig: EmotionCardDrawMachine.Instance.StartFakeBigDraw(id, GetFake(), true, HandleResult); break;
            default: EmotionCardDrawMachine.Instance.StartBigDraw(id, HandleResult); break;
        }
    }

    public override void OnExit() => cancelled = true;

    private HeroineEmotionCardType GetFake()
    {
        return fakeResult != null && fakeResult.Value != null
            ? (HeroineEmotionCardType)fakeResult.Value
            : HeroineEmotionCardType.Angry;
    }

    private void HandleResult(EmotionDrawResult result)
    {
        if (cancelled) return;
        if (result == null) { SendAndFinish(failedEvent); return; }

        if (storeResult != null && !storeResult.IsNone)
            storeResult.Value = result.ResultEmotion;
        if (storeResultString != null && !storeResultString.IsNone)
            storeResultString.Value = result.ResultEmotion.ToString();
        if (storeFakeSucceeded != null && !storeFakeSucceeded.IsNone)
            storeFakeSucceeded.Value = result.FakeSucceeded;

        SendAndFinish(HeroineActionHelper.GetEmotionRouteEvent(
            result.ResultEmotion,
            angryEvent, shyEvent, worriedEvent,
            maternalEvent, relaxedEvent, disappointedEvent,
            normalEvent, defaultEvent));
    }

    private void SendAndFinish(FsmEvent evt)
    {
        if (evt != null) Fsm.Event(evt);
        Finish();
    }
}

// ############################################################
//  性慾（Libido）
// ############################################################

[ActionCategory("Heroine Status")]
[Tooltip("取得女主角的性慾值。")]
public class GetLibido : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField][UIHint(UIHint.Variable)] public FsmInt storeValue;

    public override void Reset() { heroineID = null; storeValue = null; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "GetLibido"))
            storeValue.Value = h.Libido;
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("設定女主角的性慾值（0~150）。")]
public class SetLibidoAction : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField] public FsmInt value;

    public override void Reset() { heroineID = null; value = 0; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "SetLibidoAction"))
            h.SetLibido(value.Value);
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("增減女主角的性慾值。正值增加，負值減少。")]
public class AddLibidoAction : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField] public FsmInt amount;

    public override void Reset() { heroineID = null; amount = 0; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "AddLibidoAction"))
            h.AddLibido(amount.Value);
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("檢查女主角的性慾值是否符合條件（>, >=, ==, <, <=）。")]
public class CheckLibido : FsmStateAction
{
    [RequiredField] public FsmString heroineID;

    [ObjectType(typeof(HeroineCompareOp))]
    public FsmEnum compareOperator;

    [RequiredField] public FsmInt threshold;

    [UIHint(UIHint.Variable)] public FsmInt storeValue;

    public FsmEvent passEvent;
    public FsmEvent failEvent;

    public override void Reset()
    {
        heroineID = null;
        compareOperator = HeroineCompareOp.GreaterOrEqual;
        threshold = 50;
        storeValue = null;
        passEvent = null;
        failEvent = null;
    }

    public override void OnEnter()
    {
        if (!HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "CheckLibido"))
        {
            Fsm.Event(failEvent);
            Finish();
            return;
        }

        int val = h.Libido;
        if (storeValue != null && !storeValue.IsNone) storeValue.Value = val;

        var op = compareOperator != null && compareOperator.Value != null
            ? (HeroineCompareOp)compareOperator.Value
            : HeroineCompareOp.GreaterOrEqual;

        Fsm.Event(HeroineActionHelper.Compare(val, threshold.Value, op) ? passEvent : failEvent);
        Finish();
    }
}

// ############################################################
//  信賴（Trust）
// ############################################################

[ActionCategory("Heroine Status")]
[Tooltip("取得女主角的信賴值。")]
public class GetTrust : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField][UIHint(UIHint.Variable)] public FsmInt storeValue;

    public override void Reset() { heroineID = null; storeValue = null; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "GetTrust"))
            storeValue.Value = h.Trust;
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("設定女主角的信賴值（0~150）。")]
public class SetTrustAction : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField] public FsmInt value;

    public override void Reset() { heroineID = null; value = 0; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "SetTrustAction"))
            h.SetTrust(value.Value);
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("增減女主角的信賴值。正值增加，負值減少。")]
public class AddTrustAction : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField] public FsmInt amount;

    public override void Reset() { heroineID = null; amount = 0; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "AddTrustAction"))
            h.AddTrust(amount.Value);
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("檢查女主角的信賴值是否符合條件（>, >=, ==, <, <=）。")]
public class CheckTrust : FsmStateAction
{
    [RequiredField]
    [Tooltip("要檢查的女主角 ID。")]
    public FsmString heroineID;

    [ObjectType(typeof(HeroineCompareOp))]
    [Tooltip("比較運算子：拿「目前信賴值」跟下方 threshold 做比較。可選 >, >=, ==, <=, <。")]
    public FsmEnum compareOperator;

    [RequiredField]
    [Tooltip("比較門檻值（要拿來對照的目標數字）。例：compareOperator 選 >=、threshold 填 50，代表「信賴 >= 50 時算通過」。信賴範圍 0~150，預設 50。")]
    public FsmInt threshold;

    [UIHint(UIHint.Variable)]
    [Tooltip("（選填）把「目前讀到的信賴值」存進這個 FSM 變數，方便後續狀態直接使用或顯示。不需要就留空，不影響 pass/fail 判斷。")]
    public FsmInt storeValue;

    [Tooltip("比較「成立」時送出的 Event（信賴達標分支）。")]
    public FsmEvent passEvent;

    [Tooltip("比較「不成立」時送出的 Event（信賴不足分支）；找不到該女主角時也會走這裡。")]
    public FsmEvent failEvent;

    public override void Reset()
    {
        heroineID = null;
        compareOperator = HeroineCompareOp.GreaterOrEqual;
        threshold = 50;
        storeValue = null;
        passEvent = null;
        failEvent = null;
    }

    public override void OnEnter()
    {
        if (!HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "CheckTrust"))
        {
            Fsm.Event(failEvent);
            Finish();
            return;
        }

        int val = h.Trust;
        if (storeValue != null && !storeValue.IsNone) storeValue.Value = val;

        var op = compareOperator != null && compareOperator.Value != null
            ? (HeroineCompareOp)compareOperator.Value
            : HeroineCompareOp.GreaterOrEqual;

        Fsm.Event(HeroineActionHelper.Compare(val, threshold.Value, op) ? passEvent : failEvent);
        Finish();
    }
}

// ############################################################
//  H 次數（HCount）
// ############################################################

[ActionCategory("Heroine Status")]
[Tooltip("取得女主角的 H 次數。")]
public class GetHCount : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField][UIHint(UIHint.Variable)] public FsmInt storeValue;

    public override void Reset() { heroineID = null; storeValue = null; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "GetHCount"))
            storeValue.Value = h.HCount;
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("設定女主角的 H 次數。")]
public class SetHCountAction : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField] public FsmInt value;

    public override void Reset() { heroineID = null; value = 0; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "SetHCountAction"))
            h.SetHCount(value.Value);
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("增加女主角的 H 次數。")]
public class AddHCountAction : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [Tooltip("增加量（預設 1）")]
    public FsmInt amount;

    public override void Reset() { heroineID = null; amount = 1; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "AddHCountAction"))
            h.AddHCount(amount.Value);
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("檢查女主角的 H 次數是否符合條件（>, >=, ==, <, <=）。")]
public class CheckHCount : FsmStateAction
{
    [RequiredField] public FsmString heroineID;

    [ObjectType(typeof(HeroineCompareOp))]
    public FsmEnum compareOperator;

    [RequiredField] public FsmInt threshold;

    [UIHint(UIHint.Variable)] public FsmInt storeValue;

    public FsmEvent passEvent;
    public FsmEvent failEvent;

    public override void Reset()
    {
        heroineID = null;
        compareOperator = HeroineCompareOp.GreaterOrEqual;
        threshold = 1;
        storeValue = null;
        passEvent = null;
        failEvent = null;
    }

    public override void OnEnter()
    {
        if (!HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "CheckHCount"))
        {
            Fsm.Event(failEvent);
            Finish();
            return;
        }

        int val = h.HCount;
        if (storeValue != null && !storeValue.IsNone) storeValue.Value = val;

        var op = compareOperator != null && compareOperator.Value != null
            ? (HeroineCompareOp)compareOperator.Value
            : HeroineCompareOp.GreaterOrEqual;

        Fsm.Event(HeroineActionHelper.Compare(val, threshold.Value, op) ? passEvent : failEvent);
        Finish();
    }
}