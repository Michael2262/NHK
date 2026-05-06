using UnityEngine;
using HutongGames.PlayMaker;
using PixelCrushers.DialogueSystem;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    // ==========================================
    // 檢查 Dialogue System 的數值變數
    // ==========================================
    [ActionCategory("Story")]
    [Tooltip("檢查 Dialogue System 的數值變數，可與指定值進行比較，發送對應事件。")]
    public class CheckDialogueValue : FsmStateAction
    {
        public enum CompareType
        {
            Equal,
            NotEqual,
            GreaterThan,
            GreaterOrEqual,
            LessThan,
            LessOrEqual
        }

        [RequiredField]
        [Tooltip("Dialogue System 中的變數名稱")]
        public FsmString variableName;

        [Tooltip("比較方式")]
        public CompareType compareType = CompareType.GreaterOrEqual;

        [Tooltip("要比較的數值")]
        public FsmInt compareValue;

        [Tooltip("條件成立時發送")]
        public FsmEvent trueEvent;

        [Tooltip("條件不成立時發送")]
        public FsmEvent falseEvent;

        [UIHint(UIHint.Variable)]
        [Tooltip("將當前數值存入此變數（可選）")]
        public FsmInt storeCurrentValue;

        [UIHint(UIHint.Variable)]
        [Tooltip("將比較結果存入此變數（可選）")]
        public FsmBool storeResult;

        [Tooltip("是否每幀檢查")]
        public bool everyFrame;

        public override void Reset()
        {
            variableName = null;
            compareType = CompareType.GreaterOrEqual;
            compareValue = 0;
            trueEvent = null;
            falseEvent = null;
            storeCurrentValue = null;
            storeResult = null;
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

        private void DoCheck()
        {
            int currentValue = DialogueLua.GetVariable(variableName.Value).asInt;

            if (!storeCurrentValue.IsNone)
                storeCurrentValue.Value = currentValue;

            bool result = compareType switch
            {
                CompareType.Equal => currentValue == compareValue.Value,
                CompareType.NotEqual => currentValue != compareValue.Value,
                CompareType.GreaterThan => currentValue > compareValue.Value,
                CompareType.GreaterOrEqual => currentValue >= compareValue.Value,
                CompareType.LessThan => currentValue < compareValue.Value,
                CompareType.LessOrEqual => currentValue <= compareValue.Value,
                _ => false
            };

            if (!storeResult.IsNone)
                storeResult.Value = result;

            Fsm.Event(result ? trueEvent : falseEvent);
        }
    }

    // ==========================================
    // 檢查 Dialogue System 的布林變數
    // ==========================================
    [ActionCategory("Dialogue System Custom")]
    [Tooltip("檢查 Dialogue System 的布林變數，發送對應事件。")]
    public class CheckDialogueBool : FsmStateAction
    {
        [RequiredField]
        [Tooltip("Dialogue System 中的變數名稱")]
        public FsmString variableName;

        [Tooltip("期望的布林值（預設檢查是否為 true）")]
        public FsmBool expectedValue;

        [Tooltip("條件成立時發送")]
        public FsmEvent trueEvent;

        [Tooltip("條件不成立時發送")]
        public FsmEvent falseEvent;

        [UIHint(UIHint.Variable)]
        [Tooltip("將當前布林值存入此變數（可選）")]
        public FsmBool storeCurrentValue;

        [UIHint(UIHint.Variable)]
        [Tooltip("將比較結果存入此變數（可選）")]
        public FsmBool storeResult;

        [Tooltip("是否每幀檢查")]
        public bool everyFrame;

        public override void Reset()
        {
            variableName = null;
            expectedValue = true;
            trueEvent = null;
            falseEvent = null;
            storeCurrentValue = null;
            storeResult = null;
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

        private void DoCheck()
        {
            bool currentValue = DialogueLua.GetVariable(variableName.Value).asBool;

            if (!storeCurrentValue.IsNone)
                storeCurrentValue.Value = currentValue;

            bool result = (currentValue == expectedValue.Value);

            if (!storeResult.IsNone)
                storeResult.Value = result;

            Fsm.Event(result ? trueEvent : falseEvent);
        }
    }
}