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
[Tooltip("已退役：情緒卡池已移除，請刪除此舊 Action。")]
[System.Obsolete("情緒卡池已退役，請移除此 Action。") ]
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
    { Debug.LogWarning("[SetEmotion] 情緒卡池已退役，請移除此舊 Action。");  Finish(); }
}

// ============================================================
// AddEmotionCard
// ============================================================
[ActionCategory("Heroine Status")]
[Tooltip("已退役：情緒卡池已移除，請刪除此舊 Action。")]
[System.Obsolete("情緒卡池已退役，請移除此 Action。") ]
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
    { Debug.LogWarning("[AddEmotionCard] 情緒卡池已退役，請移除此舊 Action。");  Finish(); }
}

// ============================================================
// RemoveEmotionCard
// ============================================================
[ActionCategory("Heroine Status")]
[Tooltip("已退役：情緒卡池已移除，請刪除此舊 Action。")]
[System.Obsolete("情緒卡池已退役，請移除此 Action。") ]
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
    { Debug.LogWarning("[RemoveEmotionCard] 情緒卡池已退役，請移除此舊 Action。"); if (storeSuccess != null && !storeSuccess.IsNone) storeSuccess.Value = false; Fsm.Event(failedEvent); Finish(); }
}

// ============================================================
// HeroineEmotionCardChange（合併版：選 AddOrReplace / RemoveOne）
// ============================================================
[ActionCategory("Heroine Status")]
[Tooltip("已退役：情緒卡池已移除，請刪除此舊 Action。")]
[System.Obsolete("情緒卡池已退役，請移除此 Action。") ]
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
    { Debug.LogWarning("[HeroineEmotionCardChange] 情緒卡池已退役，請移除此舊 Action。"); SetResult(false); Fsm.Event(failedEvent); Finish(); }

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
[Tooltip("已退役：情緒卡池已移除，請刪除此舊 Action。")]
[System.Obsolete("情緒卡池已退役，請移除此 Action。") ]
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
    { Debug.LogWarning("[CheckEmotionCardCount] 情緒卡池已退役，請移除此舊 Action。"); if (storeCount != null && !storeCount.IsNone) storeCount.Value = 0; Fsm.Event(failEvent); Finish(); }
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
[System.Obsolete("情緒卡池已退役，請移除此 Action。") ]
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
    { Debug.LogWarning("[CheckDominantEmotion] 情緒卡池已退役，請移除此舊 Action。");  Finish(); }
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
//  性興味（Interest）
// ############################################################

[ActionCategory("Heroine Status")]
[Tooltip("取得女主角的性興味值。")]
public class GetInterest : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField][UIHint(UIHint.Variable)] public FsmInt storeValue;

    public override void Reset() { heroineID = null; storeValue = null; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "GetInterest"))
            storeValue.Value = h.Interest;
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("設定女主角的性興味值（0~999）。")]
public class SetInterestAction : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField] public FsmInt value;

    public override void Reset() { heroineID = null; value = 0; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "SetInterestAction"))
            h.SetInterest(value.Value);
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("增減女主角的性興味值。正值增加，負值減少。")]
public class AddInterestAction : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField] public FsmInt amount;

    public override void Reset() { heroineID = null; amount = 0; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "AddInterestAction"))
            h.AddInterest(amount.Value);
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("檢查女主角的性興味值是否符合條件（>, >=, ==, <, <=）。")]
public class CheckInterest : FsmStateAction
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
        if (!HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "CheckInterest"))
        {
            Fsm.Event(failEvent);
            Finish();
            return;
        }

        int val = h.Interest;
        if (storeValue != null && !storeValue.IsNone) storeValue.Value = val;

        var op = compareOperator != null && compareOperator.Value != null
            ? (HeroineCompareOp)compareOperator.Value
            : HeroineCompareOp.GreaterOrEqual;

        Fsm.Event(HeroineActionHelper.Compare(val, threshold.Value, op) ? passEvent : failEvent);
        Finish();
    }
}

// ############################################################
//  性欲（Libido）
// ############################################################

[ActionCategory("Heroine Status")]
[Tooltip("取得女主角的性欲值。")]
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
[Tooltip("設定女主角的性欲值（0~250）。")]
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
[Tooltip("增減女主角的性欲值。正值增加，負值減少。")]
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
[Tooltip("檢查女主角的性欲值是否符合條件（>, >=, ==, <, <=）。")]
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
    [Tooltip("比較門檻值（要拿來對照的目標數字）。例：compareOperator 選 >=、threshold 填 50，代表「信賴 >= 50 時算通過」。信賴範圍 0~999，預設 50。")]
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
//  好感度（Affinity）
// ############################################################

[ActionCategory("Heroine Status")]
[Tooltip("取得女主角的好感度。")]
public class GetAffinity : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField][UIHint(UIHint.Variable)] public FsmInt storeValue;

    public override void Reset() { heroineID = null; storeValue = null; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "GetAffinity"))
            storeValue.Value = h.Affinity;
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("設定女主角的好感度（0~150）。")]
public class SetAffinityAction : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField] public FsmInt value;

    public override void Reset() { heroineID = null; value = 0; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "SetAffinityAction"))
            h.SetAffinity(value.Value);
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("增減女主角的好感度。正值增加，負值減少。")]
public class AddAffinityAction : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField] public FsmInt amount;

    public override void Reset() { heroineID = null; amount = 0; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "AddAffinityAction"))
            h.AddAffinity(amount.Value);
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("檢查女主角的好感度是否符合條件（>, >=, ==, <, <=）。")]
public class CheckAffinity : FsmStateAction
{
    [RequiredField]
    [Tooltip("要檢查的女主角 ID。")]
    public FsmString heroineID;

    [ObjectType(typeof(HeroineCompareOp))]
    [Tooltip("比較運算子：拿「目前好感度」跟下方 threshold 做比較。可選 >, >=, ==, <=, <。")]
    public FsmEnum compareOperator;

    [RequiredField]
    [Tooltip("比較門檻值。例：compareOperator 選 >=、threshold 填 50，代表「好感度 >= 50 時算通過」。好感度範圍 0~150，預設 50。")]
    public FsmInt threshold;

    [UIHint(UIHint.Variable)]
    [Tooltip("（選填）把「目前讀到的好感度」存進這個 FSM 變數。不需要就留空，不影響 pass/fail 判斷。")]
    public FsmInt storeValue;

    [Tooltip("比較「成立」時送出的 Event（好感度達標分支）。")]
    public FsmEvent passEvent;

    [Tooltip("比較「不成立」時送出的 Event（好感度不足分支）；找不到該女主角時也會走這裡。")]
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
        if (!HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "CheckAffinity"))
        {
            Fsm.Event(failEvent);
            Finish();
            return;
        }

        int val = h.Affinity;
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

// ############################################################
//  發情（InHeat）
// ############################################################

[ActionCategory("Heroine Status")]
[Tooltip("設定女主角的發情開關（獨立狀態，會存檔）。")]
public class SetInHeatAction : FsmStateAction
{
    [RequiredField] public FsmString heroineID;

    [RequiredField]
    [Tooltip("true = 進入發情；false = 解除發情。")]
    public FsmBool value;

    public override void Reset() { heroineID = null; value = false; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "SetInHeatAction"))
            h.SetInHeat(value.Value);
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("檢查女主角是否正在發情，依結果送出 inHeat / notInHeat 事件。")]
public class CheckInHeat : FsmStateAction
{
    [RequiredField] public FsmString heroineID;

    [UIHint(UIHint.Variable)]
    [Tooltip("（選填）把目前發情狀態存進這個 FSM Bool 變數。")]
    public FsmBool storeValue;

    [Tooltip("發情中送出的 Event。")]
    public FsmEvent inHeatEvent;

    [Tooltip("非發情送出的 Event；找不到該女主角時也走這裡。")]
    public FsmEvent notInHeatEvent;

    public override void Reset()
    {
        heroineID = null;
        storeValue = null;
        inHeatEvent = null;
        notInHeatEvent = null;
    }

    public override void OnEnter()
    {
        if (!HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "CheckInHeat"))
        {
            Fsm.Event(notInHeatEvent);
            Finish();
            return;
        }

        bool inHeat = h.IsInHeat;
        if (storeValue != null && !storeValue.IsNone) storeValue.Value = inHeat;

        Fsm.Event(inHeat ? inHeatEvent : notInHeatEvent);
        Finish();
    }
}

// 獨立興奮度入口；舊版興奮等級／經驗值不使用這組 Action。
[ActionCategory("Heroine Status")]
[Tooltip("取得女主角的獨立興奮度（0~100）。")]
public class GetExcitement : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField][UIHint(UIHint.Variable)] public FsmInt storeValue;

    public override void Reset() { heroineID = null; storeValue = null; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "GetExcitement"))
            storeValue.Value = h.GetExcitement();
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("直接設定獨立興奮度（0~100），不套用性欲倍率。")]
public class SetExcitementAction : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField] public FsmInt value;

    public override void Reset() { heroineID = null; value = 0; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "SetExcitementAction"))
            h.SetExcitement(value.Value);
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("一般增減獨立興奮度，不套用性欲倍率。")]
public class AddExcitementAction : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField] public FsmInt amount;

    public override void Reset() { heroineID = null; amount = 0; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "AddExcitementAction"))
            h.AddExcitement(amount.Value);
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("舊名稱保留：一般增減獨立興奮度，已取消性欲加成。")]
public class WeightedAddExcitementAction : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField] public FsmInt amount;

    public override void Reset() { heroineID = null; amount = 0; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "WeightedAddExcitementAction"))
            h.AddExcitement(amount.Value);
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("取得女主角的獨立高潮度（0~100）。")]
public class GetOrgasm : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField][UIHint(UIHint.Variable)] public FsmInt storeValue;

    public override void Reset() { heroineID = null; storeValue = null; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "GetOrgasm"))
            storeValue.Value = h.GetOrgasm();
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("設定女主角的獨立高潮度，限制在 0~100。")]
public class SetOrgasmAction : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField] public FsmInt value;

    public override void Reset() { heroineID = null; value = 0; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "SetOrgasmAction"))
            h.SetOrgasm(value.Value);
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("增減女主角的獨立高潮度，正數增加、負數減少。")]
public class AddOrgasmAction : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField] public FsmInt amount;

    public override void Reset() { heroineID = null; amount = 0; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "AddOrgasmAction"))
            h.AddOrgasm(amount.Value);
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("檢查女主角的獨立興奮度是否符合條件（>, >=, ==, <, <=）。")]
public class CheckExcitement : FsmStateAction
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
        if (!HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "CheckExcitement"))
        {
            Fsm.Event(failEvent);
            Finish();
            return;
        }

        int val = h.Excitement;
        if (storeValue != null && !storeValue.IsNone) storeValue.Value = val;

        var op = compareOperator != null && compareOperator.Value != null
            ? (HeroineCompareOp)compareOperator.Value
            : HeroineCompareOp.GreaterOrEqual;

        Fsm.Event(HeroineActionHelper.Compare(val, threshold.Value, op) ? passEvent : failEvent);
        Finish();
    }
}

[ActionCategory("Heroine Status")]
[Tooltip("檢查女主角的獨立高潮度是否符合條件（>, >=, ==, <, <=）。")]
public class CheckOrgasm : FsmStateAction
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
        if (!HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "CheckOrgasm"))
        {
            Fsm.Event(failEvent);
            Finish();
            return;
        }

        int val = h.Orgasm;
        if (storeValue != null && !storeValue.IsNone) storeValue.Value = val;

        var op = compareOperator != null && compareOperator.Value != null
            ? (HeroineCompareOp)compareOperator.Value
            : HeroineCompareOp.GreaterOrEqual;

        Fsm.Event(HeroineActionHelper.Compare(val, threshold.Value, op) ? passEvent : failEvent);
        Finish();
    }
}

[ActionCategory("Heroine Status")]
public class GetMood : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField][UIHint(UIHint.Variable)] public FsmInt storeValue;

    public override void Reset() { heroineID = null; storeValue = null; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "GetMood"))
            storeValue.Value = h.Mood;
        Finish();
    }
}

[ActionCategory("Heroine Status")]
public class SetMoodAction : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField] public FsmInt value;

    public override void Reset() { heroineID = null; value = 0; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "SetMoodAction"))
            h.SetMood(value.Value);
        Finish();
    }
}

[ActionCategory("Heroine Status")]
public class AddMoodAction : FsmStateAction
{
    [RequiredField] public FsmString heroineID;
    [RequiredField] public FsmInt amount;

    public override void Reset() { heroineID = null; amount = 0; }

    public override void OnEnter()
    {
        if (HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "AddMoodAction"))
            h.AddMood(amount.Value);
        Finish();
    }
}

[ActionCategory("Heroine Status")]
public class CheckMood : FsmStateAction
{
    [RequiredField]
    [Tooltip("要檢查的女主角 ID。")]
    public FsmString heroineID;

    [ObjectType(typeof(HeroineCompareOp))]
    [Tooltip("比較運算子：拿「目前心情值」跟下方 threshold 做比較。可選 >, >=, ==, <=, <。")]
    public FsmEnum compareOperator;

    [RequiredField]
    [Tooltip("比較門檻值（要拿來對照的目標數字）。例：compareOperator 選 >=、threshold 填 5，代表「心情 >= 5 時算通過」。心情範圍 0~10，預設 5。")]
    public FsmInt threshold;

    [UIHint(UIHint.Variable)]
    [Tooltip("（選填）把「目前讀到的心情值」存進這個 FSM 變數，方便後續狀態直接使用或顯示。不需要就留空，不影響 pass/fail 判斷。")]
    public FsmInt storeValue;

    [Tooltip("比較「成立」時送出的 Event（心情達標分支）。")]
    public FsmEvent passEvent;

    [Tooltip("比較「不成立」時送出的 Event（心情不足分支）；找不到該女主角時也會走這裡。")]
    public FsmEvent failEvent;

    public override void Reset()
    {
        heroineID = null;
        compareOperator = HeroineCompareOp.GreaterOrEqual;
        threshold = 5;
        storeValue = null;
        passEvent = null;
        failEvent = null;
    }

    public override void OnEnter()
    {
        if (!HeroineActionHelper.TryGetHeroine(heroineID.Value, out var h, "CheckMood"))
        {
            Fsm.Event(failEvent);
            Finish();
            return;
        }

        int val = h.Mood;
        if (storeValue != null && !storeValue.IsNone) storeValue.Value = val;

        var op = compareOperator != null && compareOperator.Value != null
            ? (HeroineCompareOp)compareOperator.Value
            : HeroineCompareOp.GreaterOrEqual;

        Fsm.Event(HeroineActionHelper.Compare(val, threshold.Value, op) ? passEvent : failEvent);
        Finish();
    }
}
