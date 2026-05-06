using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

[ActionCategory("Collider2DManager")]
[Tooltip("設定單一 Collider 的啟用/停用狀態。")]
public class SetCollider2DState : FsmStateAction
{
    [RequiredField]
    [ObjectType(typeof(ColliderGroupName))]
    [Tooltip("群組名稱")]
    public FsmEnum groupName;

    [RequiredField]
    [Tooltip("Collider 的唯一 ID")]
    public FsmString colliderId;

    [RequiredField]
    [Tooltip("true = 啟用, false = 停用")]
    public FsmBool isEnabled;

    public override void Reset()
    {
        groupName = ColliderGroupName.noGroup;
        colliderId = null;
        isEnabled = true;
    }

    public override void OnEnter()
    {
        if (Collider2DManager.Instance == null)
        {
            Debug.LogError("SetCollider2DState: Collider2DManager.Instance not found in scene!");
            Finish();
            return;
        }

        Collider2DManager.Instance.SetColliderState(
            (ColliderGroupName)groupName.Value, colliderId.Value, isEnabled.Value);
        Finish();
    }
}
