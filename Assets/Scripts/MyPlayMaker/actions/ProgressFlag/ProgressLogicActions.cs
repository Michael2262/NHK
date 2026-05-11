using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    [ActionCategory("Progress - Logic")]
    [Tooltip("檢查標記是否存在（或數值 > 0），發送 True/False 事件。")]
    public class CheckProgressFlag : FsmStateAction
    {
        [RequiredField]
        [Tooltip("拖入旗標定義")]
        public ProgressFlagDefinition flagDef;

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
            bool exists = flagDef != null && GameStatusService.Instance.ProgressFlags.Contains(flagDef.FlagID);
            storeResult.Value = exists;
            Fsm.Event(exists ? trueEvent : falseEvent);
        }
    }

    [ActionCategory("Progress - Logic")]
    [Tooltip("在當前狀態等待，直到指定標記出現才發送事件跳轉。")]
    public class WaitForProgressFlag : FsmStateAction
    {
        [RequiredField]
        [Tooltip("拖入旗標定義")]
        public ProgressFlagDefinition flagDef;

        [RequiredField]
        public FsmEvent sendEvent;

        public override void OnUpdate()
        {
            if (flagDef != null && GameStatusService.Instance.ProgressFlags.Contains(flagDef.FlagID))
            {
                Fsm.Event(sendEvent);
                Finish();
            }
        }
    }
}
