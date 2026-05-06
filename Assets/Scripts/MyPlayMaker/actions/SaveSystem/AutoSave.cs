// ═══════════════════════════════════════════════════════
//  PlayMaker Custom Action: AutoSave
//
//  在 PlayMaker FSM 中觸發自動存檔（槽位 0）。
//  存檔完成後自動 Finish，可選擇性發送完成事件。
// ═══════════════════════════════════════════════════════

using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

[ActionCategory("SaveSystem")]
[Tooltip("執行自動存檔到槽位 0，並顯示存檔通知。")]
public class AutoSave : FsmStateAction
{
    [Tooltip("存檔完成後發送的 Event（可選）")]
    public FsmEvent finishEvent;

    public override void Reset()
    {
        finishEvent = null;
    }

    public override void OnEnter()
    {
        if (GameStatusService.Instance != null)
        {
            GameStatusService.Instance.AutoSave();
        }
        else
        {
            Debug.LogWarning("[AutoSave Action] 找不到 GameStatusService！");
        }

        if (finishEvent != null)
            Fsm.Event(finishEvent);

        Finish();
    }
}
