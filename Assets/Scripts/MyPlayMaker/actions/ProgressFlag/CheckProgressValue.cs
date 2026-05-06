using HutongGames.PlayMaker;

namespace MyGame.Actions
{
    [ActionCategory("Progress - Logic")]
    [Tooltip("檢查進度數值，可與指定值進行比較，發送對應事件。")]
    public class CheckProgressValue : FsmStateAction
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
        [Tooltip("要檢查的數值 Key")]
        public FsmString key;

        [Tooltip("比較方式")]
        public CompareType compareType = CompareType.GreaterThan;

        [Tooltip("要比較的數值")]
        public FsmInt compareValue;

        [Tooltip("條件成立時發送")]
        public FsmEvent trueEvent;

        [Tooltip("條件不成立時發送")]
        public FsmEvent falseEvent;

        [UIHint(UIHint.Variable)]
        [Tooltip("將當前數值存入此變數")]
        public FsmInt storeCurrentValue;

        [UIHint(UIHint.Variable)]
        [Tooltip("將比較結果存入此變數")]
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
            int currentValue = GameStatusService.Instance.ProgressFlags.GetValue(key.Value);

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
}