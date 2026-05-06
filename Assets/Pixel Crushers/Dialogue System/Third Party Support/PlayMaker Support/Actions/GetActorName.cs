using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.DialogueSystem.PlayMaker
{

    [ActionCategory("Dialogue System")]
    [HutongGames.PlayMaker.TooltipAttribute("Gets the actor name of a GameObject.")]
    public class GetActorName : FsmStateAction
    {

        [RequiredField]
        [HutongGames.PlayMaker.TooltipAttribute("The GameObject for which to get the name")]
        public FsmOwnerDefault gameObject = new FsmOwnerDefault();

        [RequiredField]
        [HutongGames.PlayMaker.TooltipAttribute("Get the internal name used for the save system")]
        public FsmBool getInternalName = new FsmBool();

        [UIHint(UIHint.Variable)]
        [HutongGames.PlayMaker.TooltipAttribute("The actor name as a string")]
        public FsmString storeStringResult = new FsmString();

        public override void Reset()
        {
            gameObject = null;
            getInternalName = false;
            storeStringResult = null;
        }

        public override void OnEnter()
        {
            var target = Fsm.GetOwnerDefaultTarget(gameObject);
            if (storeStringResult != null)
            {
                if (target == null || target.gameObject == null)
                {
                    storeStringResult.Value = null;
                }
                else
                {
                    var go = target.gameObject;
                    if (getInternalName == null || getInternalName.Value == false)
                    {
                        storeStringResult.Value = DialogueActor.GetActorName(go.transform);
                    }
                    else
                    {
                        storeStringResult.Value = DialogueActor.GetPersistentDataName(go.transform);
                    }
                }
            }
            Finish();
        }

    }

}
