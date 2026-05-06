// ═══════════════════════════════════════════════════════
//  PlayMaker Custom Action: OpenLoadMenu
//
//  在 PlayMaker FSM 中開啟讀檔選單 UI（Load 模式）。
// ═══════════════════════════════════════════════════════

using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

[ActionCategory("SaveSystem")]
[Tooltip("開啟讀檔選單 UI（Load 模式）。")]
public class OpenLoadMenu : FsmStateAction
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
            SaveLoadMenu.Instance.OpenLoadScreen();
        }
        else
        {
            Debug.LogWarning("[OpenLoadMenu Action] 找不到 SaveLoadMenu！");
        }

        if (finishEvent != null)
            Fsm.Event(finishEvent);

        Finish();
    }
}
