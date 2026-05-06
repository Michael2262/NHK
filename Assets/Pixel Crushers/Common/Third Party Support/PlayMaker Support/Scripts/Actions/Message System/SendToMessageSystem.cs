// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.PlayMakerSupport
{

    [ActionCategory("Pixel Crushers Common")]
    [HutongGames.PlayMaker.Tooltip("Sends a message through the Message System.")]
    public class SendToMessageSystem : FsmStateAction
    {

        [HutongGames.PlayMaker.TooltipAttribute("Optional name of message sender.")]
        public FsmString sender = new FsmString();

        [HutongGames.PlayMaker.TooltipAttribute("Optional target to whom message is directed.")]
        public FsmString target = new FsmString();

        [HutongGames.PlayMaker.TooltipAttribute("Message to send.")]
        public FsmString message = new FsmString();

        [HutongGames.PlayMaker.TooltipAttribute("Optional parameter to send with message.")]
        public FsmString parameter = new FsmString();

        public override void Reset()
        {
            sender.Value = string.Empty;
            target.Value = string.Empty;
            message.Value = string.Empty;
            parameter.Value = string.Empty;
        }

        public override void OnEnter()
        {
            MessageSystem.SendMessageWithTarget(sender.Value, target.Value, message.Value, parameter.Value);
            Finish();
        }

    }

}