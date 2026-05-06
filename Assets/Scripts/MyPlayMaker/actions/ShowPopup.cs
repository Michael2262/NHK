// ═══════════════════════════════════════════════════════
//  PlayMaker Custom Action: ShowPopup
//
//  在 PlayMaker FSM 中呼叫 PopupController 顯示彈窗。
//  Confirm / Cancel 各自對應一條可選的 FsmEvent，
//  讓 FSM 走不同的狀態路線。
// ═══════════════════════════════════════════════════════

using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

[ActionCategory("UI")]
[Tooltip("顯示 Popup 彈窗。Confirm 和 Cancel 可各自走不同 Event 路線。")]
public class ShowPopup : FsmStateAction
{
    // ══════════════════════════════════════════
    //  參數
    // ══════════════════════════════════════════

    [UIHint(UIHint.FsmString)]
    [Tooltip("告示文字的多語系 Key（優先使用）")]
    public FsmString messageLocalizationKey;

    [UIHint(UIHint.FsmString)]
    [Tooltip("若不使用多語系，直接填入文字")]
    public FsmString rawMessage;

    [ObjectType(typeof(PopupController.ButtonMode))]
    [Tooltip("按鈕模式：ConfirmOnly 或 ConfirmAndCancel")]
    public FsmEnum buttonMode;

    [UIHint(UIHint.FsmString)]
    [Tooltip("Confirm 按鈕多語系 Key（留空用預設）")]
    public FsmString confirmLocalizationKey;

    [UIHint(UIHint.FsmString)]
    [Tooltip("Cancel 按鈕多語系 Key（留空用預設）")]
    public FsmString cancelLocalizationKey;

    // ── 事件路線 ──

    [Tooltip("按下 Confirm 後發送的 Event（可選）")]
    public FsmEvent confirmEvent;

    [Tooltip("按下 Cancel 後發送的 Event（可選）")]
    public FsmEvent cancelEvent;

    // ══════════════════════════════════════════
    //  Action 生命週期
    // ══════════════════════════════════════════

    public override void Reset()
    {
        messageLocalizationKey = null;
        rawMessage = null;
        buttonMode = new FsmEnum { Value = PopupController.ButtonMode.ConfirmOnly };
        confirmLocalizationKey = null;
        cancelLocalizationKey = null;
        confirmEvent = null;
        cancelEvent = null;
    }

    public override void OnEnter()
    {
        if (PopupController.Instance == null)
        {
            Debug.LogWarning("[ShowPopup] PopupController 單例不存在。");
            Finish();
            return;
        }

        var mode = (PopupController.ButtonMode)buttonMode.Value;

        var data = new PopupController.PopupData
        {
            messageLocalizationKey = messageLocalizationKey.Value,
            rawMessage = rawMessage.Value,
            buttonMode = mode,
            confirmLocalizationKey = confirmLocalizationKey.Value,
            cancelLocalizationKey = cancelLocalizationKey.Value,

            onConfirm = () =>
            {
                if (confirmEvent != null)
                    Fsm.Event(confirmEvent);
                Finish();
            },
            onCancel = () =>
            {
                if (cancelEvent != null)
                    Fsm.Event(cancelEvent);
                Finish();
            }
        };

        PopupController.Instance.Show(data);

        // 如果是單按鈕模式且沒有配 confirmEvent，
        // 不提前 Finish()，等按鈕回調再 Finish。
    }
}
