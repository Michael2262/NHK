using UnityEngine;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("Minigame")]
    [Tooltip("佇列風險/家人強制移動（存入 SceneActionQueue）。於下一個場景淡入前執行；" +
             "小遊戲中則等小遊戲結束、時間推進後，由返回場景執行。")]
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
            if (GameStatusService.Instance == null)
            {
                Debug.LogWarning("[QueueRiskMove] GameStatusService.Instance 不存在！");
                return;
            }

            if (string.IsNullOrEmpty(agentID.Value))
            {
                Debug.LogWarning("[QueueRiskMove] agentID 為空！");
                return;
            }

            GameStatusService.Instance.SceneActionQueue.EnqueueRiskMove(
                agentID.Value,
                targetLocationID.Value,
                inspectionTypeID.Value
            );
        }
    }
}
