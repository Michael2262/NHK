using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

[ActionCategory("SpriteActor")]
[Tooltip("透過 SpriteActor 的 ID 替換其顯示的 Sprite。")]
public class SetSpriteActor : FsmStateAction
{
    [RequiredField]
    [Tooltip("SpriteActor 的 ID")]
    public FsmString actorID;

    [RequiredField]
    [Tooltip("要替換成的圖片名稱（填 None 可清除圖片）")]
    public FsmString spriteName;

    public override void OnEnter()
    {
        if (!actorID.IsNone && !string.IsNullOrEmpty(actorID.Value))
        {
            SpriteActor.Set(actorID.Value, spriteName.Value);
        }
        else
        {
            Debug.LogWarning("[SetSpriteActor] actorID 未設定。");
        }

        Finish();
    }

    public override void Reset()
    {
        actorID = null;
        spriteName = null;
    }
}
