using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

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

// ==========================================================
// SetEmotion — 舊名保留：新增情緒卡，CurrentEmotion 由卡池自動決定
// ==========================================================
[ActionCategory("Heroine Status")]
[Tooltip("舊名保留：新增指定情緒卡。CurrentEmotion 不可直接設定，會由情緒卡池自動決定。")]
public class SetEmotion : FsmStateAction
{
    [RequiredField]
    [Tooltip("女主角的 HeroineID")]
    public FsmString heroineID;

    [RequiredField]
    [ObjectType(typeof(HeroineEmotionCardType))]
    [Tooltip("要新增的情緒卡")]
    public FsmEnum newEmotion;

    public override void Reset()
    {
        heroineID = null;
        newEmotion = null;
    }

    public override void OnEnter()
    {
        if (!TryGetHeroine(heroineID.Value, out var heroine, "SetEmotion"))
        {
            Finish();
            return;
        }

        heroine.ReplaceEmotionCard((HeroineEmotionCardType)newEmotion.Value);
        Finish();
    }

    internal static bool TryGetHeroine(string id, out HeroineStatusModel heroine, string logPrefix)
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
}

// ==========================================================
// HeroineEmotionDraw — PlayMaker 情緒卡抽選
// 可選：有表演 / 無表演、小抽選 / 中抽選 / 大抽選 / 造假大抽選。
// 抽選完成後可依照結果送出不同 FsmEvent。
// ==========================================================
[ActionCategory("Heroine Status")]
[Tooltip("女主角情緒卡抽選。可有表演/無表演、小抽選/中抽選/大抽選/造假大抽選，並依結果送出路線事件。")]
public class HeroineEmotionDraw : FsmStateAction
{
    [RequiredField]
    [Tooltip("女主角的 HeroineID")]
    public FsmString heroineID;

    [ObjectType(typeof(HeroineEmotionDrawKind))]
    [Tooltip("抽選種類：Small 小抽選 / Medium 中抽選 / Big 大抽選 / FakeBig 造假大抽選")]
    public FsmEnum drawKind;

    [Tooltip("是否播放抽選動畫。")]
    public FsmBool playShow;

    [ObjectType(typeof(HeroineEmotionCardType))]
    [Tooltip("造假大抽選要指定的結果。只有 drawKind=FakeBig 時使用。")]
    public FsmEnum fakeResult;

    [UIHint(UIHint.Variable)]
    [ObjectType(typeof(HeroineEmotionCardType))]
    [Tooltip("抽選結果儲存到這個 FsmEnum。")]
    public FsmEnum storeResult;

    [UIHint(UIHint.Variable)]
    [Tooltip("抽選結果名稱儲存到這個 FsmString。")]
    public FsmString storeResultString;

    [UIHint(UIHint.Variable)]
    [Tooltip("造假是否成功。只有 drawKind=FakeBig 時有意義。")]
    public FsmBool storeFakeSucceeded;

    [Header("Route Events")]
    public FsmEvent angryEvent;
    public FsmEvent shyEvent;
    public FsmEvent worriedEvent;
    public FsmEvent maternalEvent;
    public FsmEvent relaxedEvent;
    public FsmEvent disappointedEvent;

    [Tooltip("沒有設定對應情緒事件時，送出這個事件。")]
    public FsmEvent defaultEvent;

    [Tooltip("抽選失敗時送出這個事件。")]
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
        defaultEvent = null;
        failedEvent = null;
    }

    public override void OnEnter()
    {
        cancelled = false;

        if (EmotionCardDrawMachine.Instance == null)
        {
            Debug.LogWarning("[HeroineEmotionDraw] 場景中找不到 EmotionCardDrawMachine。");
            SendEventAndFinish(failedEvent);
            return;
        }

        HeroineEmotionDrawKind kind = drawKind != null && drawKind.Value != null
            ? (HeroineEmotionDrawKind)drawKind.Value
            : HeroineEmotionDrawKind.Big;

        bool show = playShow == null || playShow.Value;
        string id = heroineID != null ? heroineID.Value : string.Empty;

        if (!show)
        {
            EmotionDrawResult result;
            switch (kind)
            {
                case HeroineEmotionDrawKind.Small:
                    result = EmotionCardDrawMachine.Instance.DrawSmallWithoutShow(id);
                    break;
                case HeroineEmotionDrawKind.Medium:
                    result = EmotionCardDrawMachine.Instance.DrawMediumWithoutShow(id);
                    break;
                case HeroineEmotionDrawKind.FakeBig:
                    result = EmotionCardDrawMachine.Instance.DrawFakeBigWithoutShow(id, GetFakeResult());
                    break;
                default:
                    result = EmotionCardDrawMachine.Instance.DrawBigWithoutShow(id);
                    break;
            }

            HandleResult(result);
            return;
        }

        switch (kind)
        {
            case HeroineEmotionDrawKind.Small:
                EmotionCardDrawMachine.Instance.StartSmallDraw(id, HandleResult);
                break;

            case HeroineEmotionDrawKind.Medium:
                EmotionCardDrawMachine.Instance.StartMediumDraw(id, HandleResult);
                break;

            case HeroineEmotionDrawKind.FakeBig:
                EmotionCardDrawMachine.Instance.StartFakeBigDraw(id, GetFakeResult(), true, HandleResult);
                break;

            default:
                EmotionCardDrawMachine.Instance.StartBigDraw(id, HandleResult);
                break;
        }
    }

    public override void OnExit()
    {
        cancelled = true;
    }

    private HeroineEmotionCardType GetFakeResult()
    {
        if (fakeResult != null && fakeResult.Value != null)
            return (HeroineEmotionCardType)fakeResult.Value;
        return HeroineEmotionCardType.Angry;
    }

    private void HandleResult(EmotionDrawResult result)
    {
        if (cancelled) return;

        if (result == null)
        {
            SendEventAndFinish(failedEvent);
            return;
        }

        if (storeResult != null && !storeResult.IsNone)
            storeResult.Value = result.ResultEmotion;

        if (storeResultString != null && !storeResultString.IsNone)
            storeResultString.Value = result.ResultEmotion.ToString();

        if (storeFakeSucceeded != null && !storeFakeSucceeded.IsNone)
            storeFakeSucceeded.Value = result.FakeSucceeded;

        SendEventAndFinish(GetRouteEvent(result.ResultEmotion));
    }

    private FsmEvent GetRouteEvent(HeroineEmotionCardType type)
    {
        switch (type)
        {
            case HeroineEmotionCardType.Angry: return angryEvent ?? defaultEvent;
            case HeroineEmotionCardType.Shy: return shyEvent ?? defaultEvent;
            case HeroineEmotionCardType.Worried: return worriedEvent ?? defaultEvent;
            case HeroineEmotionCardType.Maternal: return maternalEvent ?? defaultEvent;
            case HeroineEmotionCardType.Relaxed: return relaxedEvent ?? defaultEvent;
            case HeroineEmotionCardType.Disappointed: return disappointedEvent ?? defaultEvent;
            default: return defaultEvent;
        }
    }

    private void SendEventAndFinish(FsmEvent evt)
    {
        if (evt != null) Fsm.Event(evt);
        Finish();
    }
}

// ==========================================================
// HeroineEmotionCardChange — PlayMaker 情緒卡增減
// AddOrReplace：新增指定情緒卡，若超過上限會依 HeroineStatusModel 規則移除一張。
// RemoveOne：移除指定情緒卡 1 張，若卡池沒有該情緒則失敗。
// ==========================================================
[ActionCategory("Heroine Status")]
[Tooltip("女主角情緒卡增減。AddOrReplace 會新增指定情緒卡並依規則替換；RemoveOne 會移除指定情緒卡一張。")]
public class HeroineEmotionCardChange : FsmStateAction
{
    [RequiredField]
    [Tooltip("女主角的 HeroineID")]
    public FsmString heroineID;

    [ObjectType(typeof(HeroineEmotionCardChangeOperation))]
    [Tooltip("AddOrReplace 新增/替換；RemoveOne 移除一張。")]
    public FsmEnum operation;

    [RequiredField]
    [ObjectType(typeof(HeroineEmotionCardType))]
    [Tooltip("要增減的情緒卡")]
    public FsmEnum emotion;

    [UIHint(UIHint.Variable)]
    [Tooltip("操作是否成功。AddOrReplace 通常會成功；RemoveOne 沒有該情緒卡時會失敗。")]
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
        if (!SetEmotion.TryGetHeroine(heroineID.Value, out var heroine, "HeroineEmotionCardChange"))
        {
            SetSuccess(false);
            SendEventAndFinish(failedEvent);
            return;
        }

        HeroineEmotionCardType type = (HeroineEmotionCardType)emotion.Value;
        HeroineEmotionCardChangeOperation op = operation != null && operation.Value != null
            ? (HeroineEmotionCardChangeOperation)operation.Value
            : HeroineEmotionCardChangeOperation.AddOrReplace;

        bool success = true;
        switch (op)
        {
            case HeroineEmotionCardChangeOperation.RemoveOne:
                success = heroine.RemoveOneCardOfType(type);
                break;

            case HeroineEmotionCardChangeOperation.AddOrReplace:
            default:
                heroine.ReplaceEmotionCard(type);
                success = true;
                break;
        }

        SetSuccess(success);
        SendEventAndFinish(success ? successEvent : failedEvent);
    }

    private void SetSuccess(bool success)
    {
        if (storeSuccess != null && !storeSuccess.IsNone)
            storeSuccess.Value = success;
    }

    private void SendEventAndFinish(FsmEvent evt)
    {
        if (evt != null) Fsm.Event(evt);
        Finish();
    }
}
