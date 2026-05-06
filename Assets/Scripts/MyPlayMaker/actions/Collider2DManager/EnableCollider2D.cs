using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

[ActionCategory("Collider2DManager")]
[Tooltip("啟用單一 Collider（便捷方法）。")]
public class EnableCollider2D : FsmStateAction
{
    [RequiredField]
    [ObjectType(typeof(ColliderGroupName))]
    [Tooltip("群組名稱")]
    public FsmEnum groupName;

    [RequiredField]
    [Tooltip("Collider 的唯一 ID")]
    public FsmString colliderId;

    public override void Reset()
    {
        groupName = ColliderGroupName.noGroup;
        colliderId = null;
    }

    public override void OnEnter()
    {
        if (Collider2DManager.Instance == null)
        {
            Debug.LogError("EnableCollider2D: Collider2DManager.Instance not found in scene!");
            Finish();
            return;
        }

        Collider2DManager.Instance.EnableCollider(
            (ColliderGroupName)groupName.Value, colliderId.Value);
        Finish();
    }
}
