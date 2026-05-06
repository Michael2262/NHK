using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

[ActionCategory("FsmValueManager")]
[Tooltip("還原特定變數至初始狀態，或重置全部")]
public class FsmManagerRevert : FsmStateAction
{
    public enum RevertType { SingleID, Group, ResetEverything }
    public RevertType mode;

    [Tooltip("ID 或 Group 的名稱 (ResetEverything 模式下無效)")]
    public FsmString targetName;

    [Tooltip("變數名稱 (ResetEverything 模式下無效)")]
    public FsmString varName;

    public override void OnEnter()
    {
        if (FsmValueManager.Instance == null) { Finish(); return; }

        switch (mode)
        {
            case RevertType.SingleID:
                FsmValueManager.Instance.RevertValue(targetName.Value, varName.Value);
                break;
            case RevertType.Group:
                FsmValueManager.Instance.RevertGroupValue(targetName.Value, varName.Value);
                break;
            case RevertType.ResetEverything:
                FsmValueManager.Instance.ResetAll();
                break;
        }
        Finish();
    }
}