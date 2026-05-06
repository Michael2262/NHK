// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.PlayMakerSupport
{

    [ActionCategory("Pixel Crushers Common")]
    [HutongGames.PlayMaker.Tooltip("Checks if there is a saved game in a slot.")]
    public class IsGameSavedInSlot : FsmStateAction
    {

        [HutongGames.PlayMaker.Tooltip("Slot to check.")]
        public FsmInt slot = new FsmInt();

        [RequiredField]
        [UIHint(UIHint.Variable)]
        [HutongGames.PlayMaker.Tooltip("Store the result in a Bool variable.")]
        public FsmBool storeResult = new FsmBool();

        public override void Reset()
        {
            slot.Value = 0;
        }

        public override void OnEnter()
        {
            storeResult.Value = SaveSystem.HasSavedGameInSlot(slot.Value);
            Finish();
        }

    }

}