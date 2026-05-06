using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.DialogueSystem.PlayMaker
{

    [ActionCategory("Dialogue System")]
    [HutongGames.PlayMaker.TooltipAttribute("Gets an actor's portrait image.")]
    public class GetPortraitImage : FsmStateAction
    {

        [RequiredField]
        [HutongGames.PlayMaker.TooltipAttribute("The actor's GameObject")]
        public FsmOwnerDefault gameObject = new FsmOwnerDefault();

        [RequiredField]
        [HutongGames.PlayMaker.TooltipAttribute("Portrait number to get, or zero for the current portrait")]
        public FsmInt portraitNumber = new FsmInt();

        [UIHint(UIHint.Variable)]
        [HutongGames.PlayMaker.TooltipAttribute("The portrait image")]
        public FsmTexture storeTextureResult = new FsmTexture();
        
        public override void Reset()
        {
            gameObject = null;
            portraitNumber = 0;
            storeTextureResult = null;
        }

        public override void OnEnter()
        {
            var target = Fsm.GetOwnerDefaultTarget(gameObject);
            if (storeTextureResult != null)
            {
                storeTextureResult.Value = null;
                if (target != null)
                {
                    var actor = DialogueManager.MasterDatabase.GetActor(DialogueActor.GetActorName(target.transform));
                    if (actor != null)
                    {
                        Sprite sprite = null;
                        if (portraitNumber.Value == 0)
                        {
                            sprite = actor.GetPortraitSprite(1);
                        }
                        else
                        {
                            sprite = actor.GetPortraitSprite(portraitNumber.Value);
                        }
                        storeTextureResult.Value = (sprite != null) ? sprite.texture : null;
                    }
                }
            }
            Finish();
        }

    }

}
