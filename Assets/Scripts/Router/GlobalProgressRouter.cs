using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System;

[AddComponentMenu("Game/UI/Global Progress Router")]
public class GlobalProgressRouter : MonoBehaviour
{
    public enum ConditionType
    {
        Flag,   // 檢查布林開關（HasFlag）
        Value   // 檢查數值（GetValue）
    }

    public enum CompareOperator
    {
        GreaterThan,      // >
        GreaterOrEqual,   // >=
        Equal,            // ==
        LessOrEqual,      // <=
        LessThan          // <
    }

    [Serializable]
    public class GlobalConditionBranch
    {
        public string label; // 僅供 Inspector 辨識用，例如 "曾經通關"

        [Header("條件設定")]
        public ConditionType conditionType = ConditionType.Flag;

        [Tooltip("對應 GlobalFlagKeys 或 GlobalValueKeys 中的常數")]
        public string key;

        [Header("Flag 模式")]
        [Tooltip("勾選 = Flag 存在時滿足；取消 = Flag 不存在時滿足")]
        public bool triggerIfUnlocked = true;

        [Header("Value 模式（僅 conditionType = Value 時生效）")]
        public CompareOperator compareOperator = CompareOperator.GreaterOrEqual;
        public int compareValue = 1;

        [Header("觸發事件")]
        public UnityEvent onTriggered;
    }

    [Header("分支清單（依序檢查）")]
    public List<GlobalConditionBranch> branches = new List<GlobalConditionBranch>();

    [Header("預設事件（皆不符合時觸發）")]
    public UnityEvent onDefault;

    /// <summary>
    /// 綁定在 Unity Button 的 OnClick() 事件
    /// </summary>
    public void Trigger()
    {
        foreach (var branch in branches)
        {
            if (IsBranchSatisfied(branch))
            {
                branch.onTriggered.Invoke();
                return;
            }
        }

        onDefault.Invoke();
    }

    private bool IsBranchSatisfied(GlobalConditionBranch branch)
    {
        if (string.IsNullOrEmpty(branch.key)) return false;

        var gp = GameStatusService.Instance.GlobalProgress;

        switch (branch.conditionType)
        {
            case ConditionType.Flag:
                bool hasFlag = gp.HasFlag(branch.key);
                return branch.triggerIfUnlocked == hasFlag;

            case ConditionType.Value:
                int val = gp.GetValue(branch.key);
                return branch.compareOperator switch
                {
                    CompareOperator.GreaterThan    => val > branch.compareValue,
                    CompareOperator.GreaterOrEqual => val >= branch.compareValue,
                    CompareOperator.Equal          => val == branch.compareValue,
                    CompareOperator.LessOrEqual    => val <= branch.compareValue,
                    CompareOperator.LessThan       => val < branch.compareValue,
                    _ => false
                };

            default:
                return false;
        }
    }
}
