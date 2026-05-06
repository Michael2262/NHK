using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace WoodenMan.PlayMakerActions
{
    // 7. 檢查是否有任何啟動中的鬼怪 (更新版)
    [ActionCategory("Wooden Man")]
    [Tooltip("檢查目前場上是否有任何已啟動且正在被 GameManager 監測的鬼怪。")]
    public class WoodenManCheckAnyGhostActive : FsmStateAction
    {
        [UIHint(UIHint.Variable)]
        [Tooltip("儲存結果的 Bool 變數 (True 代表場上有鬼)")]
        public FsmBool storeResult;

        [Tooltip("如果場上有鬼怪，則觸發此事件")]
        public FsmEvent activeEvent;

        [Tooltip("如果場上沒有鬼怪，則觸發此事件")]
        public FsmEvent inactiveEvent;

        [Tooltip("每一幀都執行檢查")]
        public bool everyFrame;

        public override void OnEnter()
        {
            DoCheck();
            if (!everyFrame) Finish();
        }

        public override void OnUpdate()
        {
            DoCheck();
        }

        void DoCheck()
        {
            if (WoodenManGameManager.Instance == null) return;

            // 使用你剛新增的屬性
            bool hasGhost = WoodenManGameManager.Instance.HasActiveGhosts;

            storeResult.Value = hasGhost;

            // 根據結果發送事件
            if (hasGhost)
            {
                if (activeEvent != null) Fsm.Event(activeEvent);
            }
            else
            {
                if (inactiveEvent != null) Fsm.Event(inactiveEvent);
            }
        }
    }

    // 8. 檢查是否任何鬼正在注視
    [ActionCategory("Wooden Man")]
    [Tooltip("檢查目前是否有任何鬼怪處於『注視 (Looking)』狀態。")]
    public class WoodenManCheckAnyGhostLooking : FsmStateAction
    {
        [UIHint(UIHint.Variable)]
        [Tooltip("儲存結果的 Bool 變數 (True 代表有鬼在回頭)")]
        public FsmBool isLooking;

        [Tooltip("如果有任何鬼正在看，觸發此事件")]
        public FsmEvent trueEvent;

        [Tooltip("如果所有鬼都沒在看，觸發此事件")]
        public FsmEvent falseEvent;

        [Tooltip("每一幀都執行檢查")]
        public bool everyFrame;

        public override void OnEnter()
        {
            DoCheck();
            if (!everyFrame) Finish();
        }

        public override void OnUpdate()
        {
            DoCheck();
        }

        void DoCheck()
        {
            if (WoodenManGameManager.Instance == null) return;

            // 呼叫原本就有的 IsAnyGhostLooking()
            bool looking = WoodenManGameManager.Instance.IsAnyGhostLooking();
            isLooking.Value = looking;

            if (looking)
            {
                if (trueEvent != null) Fsm.Event(trueEvent);
            }
            else
            {
                if (falseEvent != null) Fsm.Event(falseEvent);
            }
        }
    }
}