using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

[ActionCategory("Collider2DManager")]
[Tooltip("將一個 Collider2D 註冊到 Collider2DManager 中。")]
public class RegisterCollider2D : FsmStateAction
{
    [RequiredField]
    [ObjectType(typeof(ColliderGroupName))]
    [Tooltip("群組名稱")]
    public FsmEnum groupName;

    [RequiredField]
    [Tooltip("Collider 的唯一 ID (e.g., \"A-1\", \"Head\")")]
    public FsmString colliderId;

    [ObjectType(typeof(Collider2D))]
    [Tooltip("要註冊的 Collider2D 元件。若未指定且 useOwnerCollider 為 true，會從 Owner 上取得。")]
    public FsmObject colliderObject;

    [Tooltip("如果 colliderObject 未指定，是否自動從 Owner 取得 Collider2D")]
    public FsmBool useOwnerCollider;

    public override void Reset()
    {
        groupName = ColliderGroupName.noGroup;
        colliderId = null;
        colliderObject = null;
        useOwnerCollider = true;
    }

    public override void OnEnter()
    {
        if (Collider2DManager.Instance == null)
        {
            Debug.LogError("RegisterCollider2D: Collider2DManager.Instance not found in scene!");
            Finish();
            return;
        }

        Collider2D col = colliderObject.Value as Collider2D;

        if (col == null && useOwnerCollider.Value)
        {
            col = Owner.GetComponent<Collider2D>();
        }

        if (col == null)
        {
            Debug.LogError("RegisterCollider2D: No Collider2D found!");
            Finish();
            return;
        }

        Collider2DManager.Instance.RegisterCollider(
            (ColliderGroupName)groupName.Value, colliderId.Value, col);
        Finish();
    }
}
