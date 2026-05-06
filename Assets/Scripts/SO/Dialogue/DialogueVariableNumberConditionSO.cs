// DialogueVariableNumberConditionSO.cs 
// 使用 Pixel Crushers 的 Dialogue System 來檢查數值型變數

using UnityEngine;
using PixelCrushers;
using PixelCrushers.DialogueSystem;

namespace UIVisibility
{
    [CreateAssetMenu(fileName = "New Dialogue Number Condition", menuName = "UI Conditions /Dialogue/Dialogue Variable (Number)")]
    public class DialogueVariableNumberConditionSO : UIConditionSO, IMessageHandler
    {
        public enum ComparisonOperator
        {
            IsEqualTo, IsNotEqualTo, IsGreaterThan, IsGreaterThanOrEqualTo, IsLessThan, IsLessThanOrEqualTo
        }

        [Header("Dialogue System 變數")]
        [Tooltip("要檢查的 Dialogue System 變數名稱（大小寫敏感）")]
        public string variableName;
        [Tooltip("要使用的比較運算子")]
        public ComparisonOperator comparison = ComparisonOperator.IsEqualTo;
        [Tooltip("要與變數進行比較的值")]
        public float valueToCompare;

        private void OnEnable()
        {
            MessageSystem.AddListener(this, "Variable Changed", (string)null);
        }

        private void OnDisable()
        {
            MessageSystem.RemoveListener(this);
        }

        public void OnMessage(MessageArgs messageArgs)
        {
            if (messageArgs.message == "Variable Changed")
            {
                
                string changedVarName = messageArgs.parameter;
                if (variableName == changedVarName)
                {
                    Raise();
                }
            }
        }

        public override bool IsMet()
        {
            if (string.IsNullOrEmpty(variableName))
            {
                return false;
            }

            float currentValue = DialogueLua.GetVariable(variableName).AsFloat;

            switch (comparison)
            {
                case ComparisonOperator.IsEqualTo:
                    return Mathf.Approximately(currentValue, valueToCompare);
                case ComparisonOperator.IsNotEqualTo:
                    return !Mathf.Approximately(currentValue, valueToCompare);
                case ComparisonOperator.IsGreaterThan:
                    return currentValue > valueToCompare;
                case ComparisonOperator.IsGreaterThanOrEqualTo:
                    return currentValue >= valueToCompare;
                case ComparisonOperator.IsLessThan:
                    return currentValue < valueToCompare;
                case ComparisonOperator.IsLessThanOrEqualTo:
                    return currentValue <= valueToCompare;
                default:
                    return false;
            }
        }
    }
}