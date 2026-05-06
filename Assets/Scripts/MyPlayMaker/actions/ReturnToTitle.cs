// ═══════════════════════════════════════════════════════
//  PlayMaker Custom Action: ReturnToTitle
//
//  在 PlayMaker FSM 中返回標題場景。
//  使用 SceneController 進行場景切換（帶轉場效果）。
// ═══════════════════════════════════════════════════════

using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

[ActionCategory("SaveSystem")]
[Tooltip("返回標題場景 (TitleScene)。使用 SceneController 進行轉場。")]
public class ReturnToTitle : FsmStateAction
{
    [Tooltip("標題場景名稱")]
    public FsmString titleSceneName;

    [Tooltip("完成後發送的 Event（可選）")]
    public FsmEvent finishEvent;

    public override void Reset()
    {
        titleSceneName = "TitleScene";
        finishEvent = null;
    }

    public override void OnEnter()
    {
        string sceneName = titleSceneName.Value;

        if (SceneController.Instance != null)
        {
            SceneController.ChangeScene(sceneName);
            Debug.Log($"[ReturnToTitle Action] 正在返回標題場景: {sceneName}");
        }
        else
        {
            Debug.LogWarning("[ReturnToTitle Action] 找不到 SceneController！");
        }

        if (finishEvent != null)
            Fsm.Event(finishEvent);

        Finish();
    }
}
