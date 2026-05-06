using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

[ActionCategory("Collider2DManager")]
[Tooltip("僅透過 ID 啟用 Collider（不指定群組，自動搜尋所有群組）。")]
public class EnableCollider2DById : FsmStateAction
{
    [RequiredField]
    [Tooltip("Collider 的唯一 ID")]
    public FsmString colliderId;

    public override void Reset()
    {
        colliderId = null;
    }

    public override void OnEnter()
    {
        if (Collider2DManager.Instance == null)
        {
            Debug.LogError("EnableCollider2DById: Collider2DManager.Instance not found in scene!");
            Finish();
            return;
        }

        Collider2DManager.Instance.EnableColliderById(colliderId.Value);
        Finish();
    }
}
