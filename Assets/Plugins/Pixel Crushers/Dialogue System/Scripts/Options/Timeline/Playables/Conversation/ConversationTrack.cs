// Recompile at 2025/6/8 下午 08:25:04

#if USE_TIMELINE
#if UNITY_2017_1_OR_NEWER
// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PixelCrushers.DialogueSystem
{

    [TrackColor(0.855f, 0.8623f, 0.87f)]
    [TrackClipType(typeof(StartConversationClip))]
    [TrackBindingType(typeof(GameObject))]
    public class ConversationTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<ConversationMixerBehaviour>.Create(graph, inputCount);
        }
    }
}
#endif
#endif
