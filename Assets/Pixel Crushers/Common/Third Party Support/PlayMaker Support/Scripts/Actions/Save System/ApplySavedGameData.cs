// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.PlayMakerSupport
{

    [ActionCategory("Pixel Crushers Common")]
    [HutongGames.PlayMaker.Tooltip("Apply previously-recorded saved game data.")]
    public class ApplySavedGameData : FsmStateAction
    {

        [HutongGames.PlayMaker.Tooltip("String containing previously-recorded saved game data.")]
        [RequiredField]
        public FsmString data = new FsmString();

        public override void Reset()
        {
            data = new FsmString();
        }

        public override void OnEnter()
        {
            var savedGameData = SaveSystem.Deserialize<SavedGameData>(data.Value);
            if (savedGameData != null)
            {
                SaveSystem.ApplySavedGameData(savedGameData);
            }
            Finish();
        }

    }

}