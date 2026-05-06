using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

[ActionCategory("FsmValueManager")]
[Tooltip("透過 FsmValueManager 設置特定 ID 或 Group 的變數數值")]
public class FsmManagerSetValue : FsmStateAction
{
    [CompoundArray("Settings", "Variable Name", "Value")]
    public FsmString[] varNames;
    [RequiredField]
    public FsmVar[] values;

    [Tooltip("如果是 ID 模式，則填寫 ID；如果是 Group 模式，則填寫 Group Name")]
    public FsmString targetIdentifier;

    public bool useGroupMode;

    [Tooltip("是否每幀執行")]
    public bool everyFrame;

    public override void OnEnter()
    {
        DoSetValues();
        if (!everyFrame) Finish();
    }

    public override void OnUpdate()
    {
        DoSetValues();
    }

    void DoSetValues()
    {
        if (FsmValueManager.Instance == null || targetIdentifier == null) return;

        for (int i = 0; i < varNames.Length; i++)
        {
            object realValue = null;

            // 根據 FsmVar 的類型提取數值
            switch (values[i].Type)
            {
                case VariableType.Int: realValue = values[i].intValue; break;
                case VariableType.Float: realValue = values[i].floatValue; break;
                case VariableType.Bool: realValue = values[i].boolValue; break;
                case VariableType.String: realValue = values[i].stringValue; break;
            }

            if (useGroupMode)
                FsmValueManager.Instance.SetGroupValue(targetIdentifier.Value, varNames[i].Value, realValue);
            else
                FsmValueManager.Instance.SetValue(targetIdentifier.Value, varNames[i].Value, realValue);
        }
    }
}