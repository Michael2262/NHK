using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

[ActionCategory("FsmValueManager")]
[Tooltip("透過 FsmValueManager 讀取指定 ID 的 FSM 變數數值，存進自身 FSM 的變數。依「儲存變數」的型別決定讀取方式（Int / Float / Bool / String）。不支援 Group 模式。")]
public class FsmManagerGetValue : FsmStateAction
{
    [RequiredField]
    [Tooltip("FsmValueManager 中註冊的 ID（Group 對應多個 FSM 無法取單一值，故不支援）")]
    public FsmString targetIdentifier;

    [RequiredField]
    [Tooltip("要讀取的來源 FSM 變數名稱")]
    public FsmString variableName;

    [RequiredField]
    [UIHint(UIHint.Variable)]
    [Tooltip("存放結果的自身 FSM 變數，型別需與來源變數一致（型別不符或找不到時會存入預設值）")]
    public FsmVar storeResult;

    public override void Reset()
    {
        targetIdentifier = null;
        variableName = null;
        storeResult = null;
    }

    public override void OnEnter()
    {
        StoreValue();
        Finish();
    }

    private void StoreValue()
    {
        if (FsmValueManager.Instance == null)
        {
            Debug.LogWarning("[FsmManagerGetValue] 找不到 FsmValueManager Instance");
            return;
        }

        if (storeResult == null || string.IsNullOrEmpty(storeResult.variableName))
        {
            Debug.LogWarning("[FsmManagerGetValue] 未指定要儲存結果的變數");
            return;
        }

        string id = targetIdentifier.Value;
        string varName = variableName.Value;

        switch (storeResult.Type)
        {
            case VariableType.Int:
                {
                    var v = Fsm.Variables.GetFsmInt(storeResult.variableName);
                    if (v != null) v.Value = FsmValueManager.Instance.GetIntById(id, varName);
                    break;
                }
            case VariableType.Float:
                {
                    var v = Fsm.Variables.GetFsmFloat(storeResult.variableName);
                    if (v != null) v.Value = FsmValueManager.Instance.GetFloatById(id, varName);
                    break;
                }
            case VariableType.Bool:
                {
                    var v = Fsm.Variables.GetFsmBool(storeResult.variableName);
                    if (v != null) v.Value = FsmValueManager.Instance.GetBoolById(id, varName);
                    break;
                }
            case VariableType.String:
                {
                    var v = Fsm.Variables.GetFsmString(storeResult.variableName);
                    if (v != null) v.Value = FsmValueManager.Instance.GetStringById(id, varName);
                    break;
                }
            default:
                Debug.LogWarning($"[FsmManagerGetValue] 不支援的變數型別: {storeResult.Type}");
                break;
        }
    }
}
