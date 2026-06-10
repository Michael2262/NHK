using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

[ActionCategory("FsmValueManager")]
[Tooltip("透過 FsmValueManager 對註冊的 FSM 發送 PlayMaker Event")]
public class FsmManagerSendEvent : FsmStateAction
{
    [RequiredField]
    [Tooltip("目標 ID 或 Group 名稱")]
    public FsmString targetIdentifier;

    [Tooltip("勾選後以 Group 模式發送，否則以單一 ID 發送")]
    public bool useGroupMode;

    [RequiredField]
    [Tooltip("要發送的 Event 名稱")]
    public FsmString eventName;

    [Tooltip("是否每幀發送（通常不需要）")]
    public bool everyFrame;

    public override void OnEnter()
    {
        DoSendEvent();
        if (!everyFrame) Finish();
    }

    public override void OnUpdate()
    {
        DoSendEvent();
    }

    void DoSendEvent()
    {
        if (FsmValueManager.Instance == null)
        {
            Debug.LogWarning($"[診斷] FsmValueManager.Instance==null，event「{eventName.Value}」未送出（target={targetIdentifier.Value}）");
            return;
        }
        if (string.IsNullOrEmpty(targetIdentifier.Value) || string.IsNullOrEmpty(eventName.Value))
        {
            Debug.LogWarning($"[診斷] target 或 eventName 為空（target={targetIdentifier.Value}, event={eventName.Value}），未送出");
            return;
        }

        if (useGroupMode)
            FsmValueManager.Instance.SendEventByGroup(targetIdentifier.Value, eventName.Value);
        else
            FsmValueManager.Instance.SendEventById(targetIdentifier.Value, eventName.Value);
    }
}
