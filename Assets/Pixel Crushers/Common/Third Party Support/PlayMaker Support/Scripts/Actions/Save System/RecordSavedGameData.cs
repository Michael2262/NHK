// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.PlayMakerSupport
{

    [ActionCategory("Pixel Crushers Common")]
    [HutongGames.PlayMaker.Tooltip("Record saved game data to a string.")]
    public class RecordSavedGameData : FsmStateAction
    {

        [HutongGames.PlayMaker.Tooltip("String in which to record game data.")]
        [RequiredField]
        public FsmString data = new FsmString();

        public override void Reset()
        {
            data = new FsmString();
        }

        public override void OnEnter()
        {
            data.Value = SaveSystem.Serialize(SaveSystem.RecordSavedGameData());
            Finish();
        }

    }

}