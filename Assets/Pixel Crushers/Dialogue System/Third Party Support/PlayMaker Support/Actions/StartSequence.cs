using System;
using System.Collections.Generic;
using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.DialogueSystem.PlayMaker
{

    [ActionCategory("Dialogue System")]
    [HutongGames.PlayMaker.TooltipAttribute("Starts a cutscene sequence.")]
    public class StartSequence : FsmStateAction
    {

        [RequiredField]
        [HutongGames.PlayMaker.TooltipAttribute("The sequence to play")]
        public FsmString sequence;

        [HutongGames.PlayMaker.TooltipAttribute("The speaker, if the sequence references 'speaker' (optional)")]
        public FsmGameObject speaker;

        [HutongGames.PlayMaker.TooltipAttribute("The listener (optional)")]
        public FsmGameObject listener;

        [HutongGames.PlayMaker.TooltipAttribute("Tick to send 'OnSequenceStart' and 'OnSequenceEnd' messages to the participants")]
        public FsmBool informParticipants;

        [UIHint(UIHint.Variable)]
        [HutongGames.PlayMaker.TooltipAttribute("Store the resulting sequence handler in an Object variable")]
        public FsmObject storeResult;

        [HutongGames.PlayMaker.TooltipAttribute("If set, wait for the sequencer to receive this message and don't end the action until it's received")]
        public FsmString waitForMessage;

        [HutongGames.PlayMaker.TooltipAttribute("If Wait For Message is set, raise this event when the sequencer receives the message")]
        public FsmEvent messageEvent;

        private Sequencer sequencer;

        public override void Reset()
        {
            if (sequence != null) sequence.Value = string.Empty;
            if (speaker != null) speaker.Value = null;
            if (listener != null) listener.Value = null;
            if (informParticipants != null) informParticipants.Value = false;
            storeResult = null;
        }

        public override void OnEnter()
        {
            Transform speakerTransform = (speaker.Value != null) ? speaker.Value.transform : null;
            Transform listenerTransform = (listener.Value != null) ? listener.Value.transform : null;
            sequencer = DialogueManager.PlaySequence(sequence.Value, speakerTransform, listenerTransform, informParticipants.Value);
            storeResult = sequencer;
            if (waitForMessage == null || string.IsNullOrEmpty(waitForMessage.Value))
            {
                Finish();
            }
            else
            {
                sequencer.receivedMessage += OnReceivedMessage;
            }
        }

        private void OnReceivedMessage(string message)
        {
            sequencer.receivedMessage -= OnReceivedMessage;
            Fsm.Event(messageEvent);
        }

    }

}
