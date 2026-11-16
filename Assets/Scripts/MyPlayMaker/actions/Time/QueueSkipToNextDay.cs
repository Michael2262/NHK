using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    [ActionCategory("Time")]
    [Tooltip("在下一個場景播放睡到隔天的切幕，跳到隔天第一格。登記後請接場景切換。")]
    public class QueueSkipToNextDay : FsmStateAction
    {
        public override void OnEnter()
        {
            var queue = GameStatusService.Instance?.SceneActionQueue;
            if (queue != null)
                queue.EnqueueSkipToNextDay();
            else
                Debug.LogError("[QueueSkipToNextDay] 找不到場景動作佇列！");

            Finish();
        }
    }
}
