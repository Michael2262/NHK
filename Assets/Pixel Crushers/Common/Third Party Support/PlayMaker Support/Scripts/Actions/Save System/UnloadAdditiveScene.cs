// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.PlayMakerSupport
{

    [ActionCategory("Pixel Crushers Common")]
    [HutongGames.PlayMaker.Tooltip("Unloads a scene that was previously loaded additively.")]
    public class UnloadAdditiveScene : FsmStateAction
    {

        [HutongGames.PlayMaker.Tooltip("Additive scene to unload.")]
        public FsmString sceneName = new FsmString();

        public override void Reset()
        {
            sceneName.Value = string.Empty;
        }

        public override void OnEnter()
        {
            var s = sceneName.Value;
            SaveSystem.UnloadAdditiveScene(sceneName.Value);
            Finish();
        }

    }

}
