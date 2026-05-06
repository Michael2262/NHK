using UnityEngine;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("Minigame")]
    [Tooltip("佇列女主角強制移動。此動作會在小遊戲結束後、時間推進後、場景淡入前執行。")]
    public class QueueHeroineMove : FsmStateAction
    {
        [RequiredField]
        [Tooltip("女主角的唯一識別 ID")]
        public FsmString heroineID;

        [RequiredField]
        [Tooltip("目標地點 ID")]
        public FsmString targetLocationID;

        [RequiredField]
        [Tooltip("新的活動狀態 (對應 HubController 的 activityState)")]
        public FsmString newActivity;

        [Tooltip("是否每幀執行 (通常保持 false)")]
        public bool everyFrame;

        public override void Reset()
        {
            heroineID = null;
            targetLocationID = null;
            newActivity = null;
            everyFrame = false;
        }

        public override void OnEnter()
        {
            DoQueueHeroineMove();

            if (!everyFrame)
            {
                Finish();
            }
        }

        public override void OnUpdate()
        {
            if (everyFrame)
            {
                DoQueueHeroineMove();
            }
        }

        private void DoQueueHeroineMove()
        {
            if (MinigameManager.Instance == null)
            {
                Debug.LogWarning("[QueueHeroineMove] MinigameManager.Instance 不存在！");
                return;
            }

            if (string.IsNullOrEmpty(heroineID.Value))
            {
                Debug.LogWarning("[QueueHeroineMove] heroineID 為空！");
                return;
            }

            MinigameManager.Instance.QueueHeroineMove(
                heroineID.Value,
                targetLocationID.Value,
                newActivity.Value
            );
        }
    }
}
