using UnityEngine;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("Minigame")]
    [Tooltip("清除所有已佇列的結束後動作。用於取消或重置時使用。")]
    public class ClearPendingActions : FsmStateAction
    {
        public override void OnEnter()
        {
            if (MinigameManager.Instance == null)
            {
                Debug.LogWarning("[ClearPendingActions] MinigameManager.Instance 不存在！");
            }
            else
            {
                MinigameManager.Instance.ClearPendingActions();
            }

            Finish();
        }
    }
}
