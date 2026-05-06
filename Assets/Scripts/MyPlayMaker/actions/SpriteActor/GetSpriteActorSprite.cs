using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

[ActionCategory("SpriteActor")]
[Tooltip("取得指定 SpriteActor 目前顯示的 Sprite，存入變數。")]
public class GetSpriteActorSprite : FsmStateAction
{
    [RequiredField]
    [Tooltip("SpriteActor 的 ID")]
    public FsmString actorID;

    [RequiredField]
    [ObjectType(typeof(Sprite))]
    [UIHint(UIHint.Variable)]
    [Tooltip("將取得的 Sprite 存入此變數")]
    public FsmObject storeSprite;

    [Tooltip("同時將目前圖片的名稱存入此字串變數（選填）")]
    [UIHint(UIHint.Variable)]
    public FsmString storeSpriteName;

    [Tooltip("如果找不到 Actor 或 Sprite 為空，是否發送事件")]
    public FsmEvent notFoundEvent;

    public override void OnEnter()
    {
        bool success = false;

        if (!actorID.IsNone && !string.IsNullOrEmpty(actorID.Value))
        {
            var actor = SpriteActor.Find(actorID.Value);
            if (actor != null && actor.targetRenderer != null && actor.targetRenderer.sprite != null)
            {
                storeSprite.Value = actor.targetRenderer.sprite;

                if (!storeSpriteName.IsNone)
                {
                    storeSpriteName.Value = actor.GetCurrentSpriteName() ?? "";
                }

                success = true;
            }
        }

        if (!success)
        {
            Debug.LogWarning($"[GetSpriteActorSprite] 無法取得 Sprite（ID: {actorID.Value}）");

            if (notFoundEvent != null)
            {
                Fsm.Event(notFoundEvent);
                return;
            }
        }

        Finish();
    }

    public override void Reset()
    {
        actorID = null;
        storeSprite = null;
        storeSpriteName = new FsmString { UseVariable = true };
        notFoundEvent = null;
    }
}
