// ═══════════════════════════════════════════════════════
//  PlayMaker Custom Action: LoadLatestSave
//
//  在 PlayMaker FSM 中讀取最近一次的存檔（不分手動或自動）。
//  適用於 Game Over 畫面的「從上一次存檔繼續」按鈕。
// ═══════════════════════════════════════════════════════

using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

[ActionCategory("SaveSystem")]
[Tooltip("讀取最近一次的存檔（不分手動或自動）。找不到存檔時發送失敗事件。")]
public class LoadLatestSave : FsmStateAction
{
    [Tooltip("成功找到存檔並開始讀取後發送的 Event（可選）")]
    public FsmEvent successEvent;

    [Tooltip("找不到任何存檔時發送的 Event（可選）")]
    public FsmEvent noSaveFoundEvent;

    public override void Reset()
    {
        successEvent = null;
        noSaveFoundEvent = null;
    }

    public override void OnEnter()
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("[LoadLatestSave Action] 找不到 GameStatusService！");
            Finish();
            return;
        }

        bool success = GameStatusService.Instance.LoadLatestSave();

        if (success)
        {
            if (successEvent != null)
                Fsm.Event(successEvent);
        }
        else
        {
            Debug.LogWarning("[LoadLatestSave Action] 找不到任何存檔。");
            if (noSaveFoundEvent != null)
                Fsm.Event(noSaveFoundEvent);
        }

        Finish();
    }
}
