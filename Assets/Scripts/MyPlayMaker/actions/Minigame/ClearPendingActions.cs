using UnityEngine;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("Minigame")]
    [Tooltip("清除 SceneActionQueue 中所有已佇列的動作（含時間推進）。用於取消或重置時使用，不影響小遊戲的暫停狀態。")]
    public class ClearPendingActions : FsmStateAction
    {
        public override void OnEnter()
        {
            if (GameStatusService.Instance == null)
            {
                Debug.LogWarning("[ClearPendingActions] GameStatusService.Instance 不存在！");
            }
            else
            {
                GameStatusService.Instance.SceneActionQueue.Clear();
            }

            Finish();
        }
    }
}
