using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

[ActionCategory("Collider2DManager")]
[Tooltip("僅透過 ID 設定 Collider 的啟用/停用狀態（不指定群組，自動搜尋所有群組）。")]
public class SetCollider2DStateById : FsmStateAction
{
    [RequiredField]
    [Tooltip("Collider 的唯一 ID")]
    public FsmString colliderId;

    [RequiredField]
    [Tooltip("true = 啟用, false = 停用")]
    public FsmBool isEnabled;

    public override void Reset()
    {
        colliderId = null;
        isEnabled = true;
    }

    public override void OnEnter()
    {
        if (Collider2DManager.Instance == null)
        {
            Debug.LogError("SetCollider2DStateById: Collider2DManager.Instance not found in scene!");
            Finish();
            return;
        }

        Collider2DManager.Instance.SetColliderStateById(colliderId.Value, isEnabled.Value);
        Finish();
    }
}
