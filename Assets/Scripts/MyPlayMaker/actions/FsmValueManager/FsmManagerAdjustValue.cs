using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

[ActionCategory("FsmValueManager")]
[Tooltip("透過 FsmValueManager 增量調整數值 (僅限 Int 和 Float)")]
public class FsmManagerAdjustValue : FsmStateAction
{
    [RequiredField]
    public FsmString targetIdentifier;
    public bool useGroupMode;

    [RequiredField]
    public FsmString variableName;

    [RequiredField]
    [Tooltip("要增加或減少的數值")]
    public FsmVar deltaValue;

    public override void OnEnter()
    {
        if (FsmValueManager.Instance != null)
        {
            // FsmVar 綁定變數時，讀取前必須先 UpdateValue() 同步即時值
            deltaValue.UpdateValue();

            object delta = null;
            if (deltaValue.Type == VariableType.Int) delta = deltaValue.intValue;
            else if (deltaValue.Type == VariableType.Float) delta = deltaValue.floatValue;

            if (delta != null)
            {
                if (useGroupMode)
                    FsmValueManager.Instance.AdjustGroupValue(targetIdentifier.Value, variableName.Value, delta);
                else
                    FsmValueManager.Instance.AdjustValue(targetIdentifier.Value, variableName.Value, delta);
            }
        }
        Finish();
    }
}