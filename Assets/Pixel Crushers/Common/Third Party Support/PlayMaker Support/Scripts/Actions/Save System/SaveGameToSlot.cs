// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.PlayMakerSupport
{

    [ActionCategory("Pixel Crushers Common")]
    [HutongGames.PlayMaker.Tooltip("Saves the game into a slot.")]
    public class SaveGameToSlot : FsmStateAction
    {

        [HutongGames.PlayMaker.Tooltip("Slot to save game into.")]
        public FsmInt slot = new FsmInt();

        public override void Reset()
        {
            slot.Value = 0;
        }

        public override void OnEnter()
        {
            SaveSystem.SaveToSlot(slot.Value);
            Finish();
        }

    }

}