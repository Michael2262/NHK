// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.PlayMakerSupport
{

    /// <summary>
    /// Listens for a message from the Message System and sends an event to PlayMaker.
    /// Add this to a GameObject with an FSM.
    /// </summary>
    [AddComponentMenu("Pixel Crushers/Third Party/PlayMaker Support/Message System to PlayMaker")]
    public class MessageSystemToPlayMaker : MonoBehaviour, IMessageHandler
    {

        [UnityEngine.Tooltip("Optional required message sender.")]
        public string sender;

        [UnityEngine.Tooltip("Optional required message target.")]
        public string target;

        [UnityEngine.Tooltip("Message to listen for.")]
        public string message;

        [UnityEngine.Tooltip("Optional required parameter.")]
        public string parameter;

        [UnityEngine.Tooltip("Event to send to FSM. Event string data will contain message:parameter.")]
        public string FSMEventName;

        /// <summary>
        /// The FSMs that will receive events. If left empty, all FSMs on this GameObject.
        /// </summary>
        [UnityEngine.Tooltip("FSMs that will receive events. If empty, all FSMs on this GameObject.")]
        public PlayMakerFSM[] FSMs;

        private void Start()
        {
            if (FSMs == null || FSMs.Length == 0)
            {
                FSMs = GetComponents<PlayMakerFSM>();
            }
        }

        private void OnEnable()
        {
            MessageSystem.AddListener(this, message, parameter);
        }

        private void OnDisable()
        {
            MessageSystem.RemoveListener(this);
        }

        public void OnMessage(MessageArgs messageArgs)
        {
            if ((string.IsNullOrEmpty(sender) || string.Equals(sender, messageArgs.sender)) &&
                (string.IsNullOrEmpty(target) || string.Equals(sender, messageArgs.target)))
            {
                Fsm.EventData.StringData = string.IsNullOrEmpty(parameter) ? message : (message + ":" + parameter);
                SendEvent(FSMEventName);
            }
        }
        /// <summary>
        /// Sends an event to the FSMs.
        /// </summary>
        /// <param name="fsmEventName">FSM event name.</param>
        public void SendEvent(string fsmEventName)
        {
            if (!enabled || FSMs == null) return;
            for (int f = 0; f < FSMs.Length; f++)
            {
                var behavior = FSMs[f];
                behavior.Fsm.Event(fsmEventName);
            }
        }

    }

}