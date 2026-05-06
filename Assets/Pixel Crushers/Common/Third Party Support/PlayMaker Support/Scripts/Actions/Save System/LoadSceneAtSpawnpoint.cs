// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.PlayMakerSupport
{

    [ActionCategory("Pixel Crushers Common")]
    [HutongGames.PlayMaker.Tooltip("Loads a scene and moves the player to a specified spawnpoint.")]
    public class LoadSceneAtSpawnpoint : FsmStateAction
    {

        [HutongGames.PlayMaker.Tooltip("Scene to load.")]
        public FsmString sceneName = new FsmString();

        [HutongGames.PlayMaker.Tooltip("Optional name of GameObject where player should appear.")]
        public FsmString spawnpoint = new FsmString();

        [HutongGames.PlayMaker.Tooltip("Load scene additively instead of replacing current scene. (Does not use spawnpoint.)")]
        public bool additive = false;

        public override void Reset()
        {
            sceneName.Value = string.Empty;
            spawnpoint.Value = string.Empty;
            additive = false;
        }

        public override void OnEnter()
        {
            var s = sceneName.Value;
            if (!string.IsNullOrEmpty(spawnpoint.Value)) s += "@" + spawnpoint.Value;
            if (additive)
            {
                SaveSystem.LoadAdditiveScene(s);
            }
            else
            {
                SaveSystem.LoadScene(s);
            }
            Finish();
        }

    }

}
