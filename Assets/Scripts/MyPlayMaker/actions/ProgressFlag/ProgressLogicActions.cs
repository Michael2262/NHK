using HutongGames.PlayMaker;

namespace MyGame.Actions
{
    [ActionCategory("Progress - Logic")]
    [Tooltip("檢查標記是否存在（或數值 > 0），發送 True/False 事件。")]
    public class CheckProgressFlag : FsmStateAction
    {
        [RequiredField] public FsmString flag;
        public FsmEvent trueEvent;
        public FsmEvent falseEvent;

        [UIHint(UIHint.Variable)]
        public FsmBool storeResult;

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

        private void DoCheck()
        {
            bool exists = GameStatusService.Instance.ProgressFlags.Contains(flag.Value);
            storeResult.Value = exists;
            Fsm.Event(exists ? trueEvent : falseEvent);
        }
    }

    [ActionCategory("Progress - Logic")]
    [Tooltip("在當前狀態等待，直到指定標記出現才發送事件跳轉。")]
    public class WaitForProgressFlag : FsmStateAction
    {
        [RequiredField] public FsmString flag;
        [RequiredField] public FsmEvent sendEvent;

        public override void OnUpdate()
        {
            if (GameStatusService.Instance.ProgressFlags.Contains(flag.Value))
            {
                Fsm.Event(sendEvent);
                Finish();
            }
        }
    }
}