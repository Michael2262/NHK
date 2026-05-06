// ═══════════════════════════════════════════════════════
//  PlayMaker Custom Action: OpenSaveMenu
//
//  在 PlayMaker FSM 中開啟存檔選單 UI（Save 模式）。
// ═══════════════════════════════════════════════════════

using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

[ActionCategory("SaveSystem")]
[Tooltip("開啟存檔選單 UI（Save 模式）。")]
public class OpenSaveMenu : FsmStateAction
{
    [Tooltip("開啟後發送的 Event（可選）")]
    public FsmEvent finishEvent;

    public override void Reset()
    {
        finishEvent = null;
    }

    public override void OnEnter()
    {
        if (SaveLoadMenu.Instance != null)
        {
            SaveLoadMenu.Instance.OpenSaveScreen();
        }
        else
        {
            Debug.LogWarning("[OpenSaveMenu Action] 找不到 SaveLoadMenu！");
        }

        if (finishEvent != null)
            Fsm.Event(finishEvent);

        Finish();
    }
}
