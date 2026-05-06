using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace WoodenMan.PlayMakerActions
{
    // 檢查指定 ID 的鬼怪是否正在啟動中
    [ActionCategory("Wooden Man")]
    [Tooltip("檢查指定 ActionID 的鬼怪是否正在啟動中。")]
    public class WoodenManCheckGhostActiveByID : FsmStateAction
    {
        [RequiredField]
        [Tooltip("要檢查的 RiskAction inspectionTypeID")]
        public FsmString actionID;

        [UIHint(UIHint.Variable)]
        [Tooltip("儲存結果的 Bool 變數 (True 代表該鬼啟動中)")]
        public FsmBool storeResult;

        [Tooltip("如果該鬼啟動中，觸發此事件")]
        public FsmEvent activeEvent;

        [Tooltip("如果該鬼未啟動，觸發此事件")]
        public FsmEvent inactiveEvent;

        [Tooltip("每一幀都執行檢查")]
        public bool everyFrame;

        public override void Reset()
        {
            actionID = null;
            storeResult = null;
            activeEvent = null;
            inactiveEvent = null;
            everyFrame = false;
        }

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

            bool isActive = WoodenManGameManager.Instance.IsGhostActiveByID(actionID.Value);

            if (storeResult != null) storeResult.Value = isActive;

            if (isActive)
            {
                if (activeEvent != null) Fsm.Event(activeEvent);
            }
            else
            {
                if (inactiveEvent != null) Fsm.Event(inactiveEvent);
            }
        }
    }
}
