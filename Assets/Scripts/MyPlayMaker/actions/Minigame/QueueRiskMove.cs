using UnityEngine;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("Minigame")]
    [Tooltip("佇列風險/家人強制移動。此動作會在小遊戲結束後、時間推進後、場景淡入前執行。")]
    public class QueueRiskMove : FsmStateAction
    {
        [RequiredField]
        [Tooltip("風險代理人 ID (例如: mother, father)")]
        public FsmString agentID;

        [RequiredField]
        [Tooltip("目標地點 ID")]
        public FsmString targetLocationID;

        [RequiredField]
        [Tooltip("行為類型 ID (對應 Risk 物件的顯示邏輯)")]
        public FsmString inspectionTypeID;

        [Tooltip("是否每幀執行 (通常保持 false)")]
        public bool everyFrame;

        public override void Reset()
        {
            agentID = null;
            targetLocationID = null;
            inspectionTypeID = null;
            everyFrame = false;
        }

        public override void OnEnter()
        {
            DoQueueRiskMove();

            if (!everyFrame)
            {
                Finish();
            }
        }

        public override void OnUpdate()
        {
            if (everyFrame)
            {
                DoQueueRiskMove();
            }
        }

        private void DoQueueRiskMove()
        {
            if (MinigameManager.Instance == null)
            {
                Debug.LogWarning("[QueueRiskMove] MinigameManager.Instance 不存在！");
                return;
            }

            if (string.IsNullOrEmpty(agentID.Value))
            {
                Debug.LogWarning("[QueueRiskMove] agentID 為空！");
                return;
            }

            MinigameManager.Instance.QueueRiskMove(
                agentID.Value,
                targetLocationID.Value,
                inspectionTypeID.Value
            );
        }
    }
}
