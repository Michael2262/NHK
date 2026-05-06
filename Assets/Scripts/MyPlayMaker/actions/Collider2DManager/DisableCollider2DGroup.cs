using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

[ActionCategory("Collider2DManager")]
[Tooltip("停用指定群組中的所有 Collider。")]
public class DisableCollider2DGroup : FsmStateAction
{
    [RequiredField]
    [ObjectType(typeof(ColliderGroupName))]
    [Tooltip("要停用的群組名稱")]
    public FsmEnum groupName;

    public override void Reset()
    {
        groupName = ColliderGroupName.noGroup;
    }

    public override void OnEnter()
    {
        if (Collider2DManager.Instance == null)
        {
            Debug.LogError("DisableCollider2DGroup: Collider2DManager.Instance not found in scene!");
            Finish();
            return;
        }

        Collider2DManager.Instance.DisableGroup((ColliderGroupName)groupName.Value);
        Finish();
    }
}
