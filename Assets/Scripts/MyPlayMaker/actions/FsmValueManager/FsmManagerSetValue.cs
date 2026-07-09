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
        if (FsmValueManager.Instance == null)
        {
            Debug.LogWarning($"[FsmManagerSetValue] FsmValueManager.Instance==null，SetValue 未執行（target={targetIdentifier?.Value}）");
            return;
        }
        if (targetIdentifier == null) return;

        for (int i = 0; i < varNames.Length; i++)
        {
            // FsmVar 綁定變數時，intValue 等欄位只是快照，不會自動同步，
            // 讀取前必須先 UpdateValue() 從變數取得即時值
            values[i].UpdateValue();

            object realValue = null;

            // 根據 FsmVar 的類型提取數值
            switch (values[i].Type)
            {
                case VariableType.Int: realValue = values[i].intValue; break;
                case VariableType.Float: realValue = values[i].floatValue; break;
                case VariableType.Bool: realValue = values[i].boolValue; break;
                case VariableType.String: realValue = values[i].stringValue; break;
            }

            Debug.Log($"[FsmManagerSetValue] {targetIdentifier.Value}.{varNames[i].Value} = {realValue}");

            if (useGroupMode)
                FsmValueManager.Instance.SetGroupValue(targetIdentifier.Value, varNames[i].Value, realValue);
            else
                FsmValueManager.Instance.SetValue(targetIdentifier.Value, varNames[i].Value, realValue);
        }
    }
}