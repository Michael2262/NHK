using System;
using System.Text;
using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.DialogueSystem.PlayMaker
{

    /// <summary>
    /// This component sends Dialogue System events to PlayMaker FSMs. Generally you will add this
    /// component to the Dialogue Manager or an actor such as the player object. See the user manual
    /// section Script Overview > Notification Messages for information about Dialogue System events.
    /// </summary>
    [AddComponentMenu("Dialogue System/Third Party/PlayMaker/Dialogue System Events To PlayMaker")]
    public class DialogueSystemEventsToPlayMaker : MonoBehaviour
    {

        [UnityEngine.Tooltip("Notify all FSMs. If ticked, the Fsms list below is ignored.")]
        public bool notifyAllFsms = false;

        /// <summary>
        /// The FSMs that will receive Dialogue System events.
        /// </summary>
        [UnityEngine.Tooltip("If Notify All Fsms In Scene is unticked, notify these FSMs.")]
        [UnityEngine.Serialization.FormerlySerializedAs("FSMs")]
        public PlayMakerFSM[] Fsms;

        [Serializable]
        public class EventData
        {
            [UnityEngine.Tooltip("Separate responses with this string. If blank, the OnConversationResponseMenu event doesn't set event string data.")]
            public string responseSeparator;
        }

        public EventData eventData = new EventData();

        public bool debug = false;

        /// <summary>
        /// Sends an event to the FSMs.
        /// </summary>
        /// <param name="fsmEventName">FSM event name.</param>
        public virtual void SendEvent(string fsmEventName)
        {
            if (Fsms == null)
            {
                if (debug || DialogueDebug.LogWarnings) Debug.LogWarning("Dialogue System: Want to send event " + fsmEventName + " but no FSMs are assigned to Dialogue System Events To Play Maker.", this);
            }
            else
            {
                var receiverFsms = notifyAllFsms ? FindObjectsOfType<PlayMakerFSM>() : Fsms;
                foreach (var behavior in receiverFsms)
                {
                    if (behavior == null || !behavior.enabled) continue;
                    if (debug) Debug.Log("Dialogue System: Sending " + fsmEventName + " to " + behavior.Fsm.Name);
                    behavior.Fsm.Event(fsmEventName);
                }
            }
        }

        /// <summary>
        /// Sent at the start of a conversation. The actor is the other participant in the conversation. 
        /// This message is also broadcast to the Dialogue Manager object and its children.
        /// Fsm.EventData.GameObjectData is set to the actor.
        /// </summary>
        /// <param name="actor">The other participant in the conversation.</param>
        protected virtual void OnConversationStart(Transform actor)
        {
            Fsm.EventData.GameObjectData = (actor != null) ? actor.gameObject : null;
            SendEvent("OnConversationStart");
        }

        /// <summary>
        /// Sent at the end of a conversation. The actor is the other participant in the conversation. 
        /// This message is also broadcast to the Dialogue Manager object and its children after the 
        /// dialogue UI has closed.
        /// Fsm.EventData.GameObjectData is set to the actor.
        /// </summary>
        /// <param name="actor">Actor.</param>
        protected virtual void OnConversationEnd(Transform actor)
        {
            Fsm.EventData.GameObjectData = (actor != null) ? actor.gameObject : null;
            SendEvent("OnConversationEnd");
        }

        /// <summary>
        /// Broadcast to the Dialogue Manager object (not the participants) if a conversation ended because 
        /// the player presses the cancel key or button during the player response menu.
        /// Fsm.EventData.GameObjectData is set to the actor.
        /// </summary>
        /// <param name="actor">Actor.</param>
        protected virtual void OnConversationCancelled(Transform actor)
        {
            Fsm.EventData.GameObjectData = (actor != null) ? actor.gameObject : null;
            SendEvent("OnConversationCancelled");
        }

        /// <summary>
        /// Sent whenever a line is spoken. See the PixelCrushers.DialogueSystem.Subtitle reference.
        /// </summary>
        /// <param name="subtitle">Subtitle.</param>
        protected virtual void OnConversationLine(Subtitle subtitle)
        {
            Fsm.EventData.StringData = subtitle.formattedText.text;
            SendEvent("OnConversationLine");
        }

        /// <summary>
        /// Broadcast to the Dialogue Manager object (not the participants) if the player presses the 
        /// cancel key or button while a line is being delivered. Cancelling causes the Dialogue System to 
        /// jump to the end of the line and continue to the next line or response menu.
        /// </summary>
        /// <param name="subtitle">Subtitle.</param>
        protected virtual void OnConversationLineCancelled(Subtitle subtitle)
        {
            SendEvent("OnConversationLineCancelled");
        }

        /// <summary>
        /// Broadcast to the Dialogue Manager object (not the participants) just prior to setting
        /// up the player response menu with responses.
        /// </summary>
        /// <param name="responses">Responses.</param>
        protected virtual void OnConversationResponseMenu(Response[] responses)
        {
            if (!string.IsNullOrEmpty(eventData.responseSeparator) && responses != null)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < responses.Length; i++)
                {
                    if (i > 0) sb.Append(eventData.responseSeparator);
                    sb.Append(responses[i].destinationEntry.MenuText);
                }
                if (debug) Debug.Log("Dialogue System: FSM Event OnConversationResponseMenu '" + sb.ToString() + "'.");
                Fsm.EventData.StringData = sb.ToString();
            }
            SendEvent("OnConversationResponseMenu");
        }

        /// <summary>
        /// Sent to the Dialogue Manager object (not the participants) if the response menu times out. 
        /// The DialogueSystemController script handles timeouts according to its display settings. You 
        /// can add your own scripts to the Dialogue Manager object that also listens for this message.
        /// </summary>
        protected virtual void OnConversationTimeout()
        {
            SendEvent("OnConversationTimeout");
        }

        /// <summary>
        /// Broadcast to the Dialogue Manager and participants when following a cross-conversation link.
        /// Fsm.EventData.GameObjectData is set to the actor.
        /// </summary>
        /// <param name="actor">Actor.</param>
        protected virtual void OnLinkedConversationStart(Transform actor)
        {
            Fsm.EventData.GameObjectData = (actor != null) ? actor.gameObject : null;
            SendEvent("OnLinkedConversationStart");
        }

        /// <summary>
        /// Sent at the start of a bark. The actor is the other participant.
        /// Fsm.EventData.GameObjectData is set to the actor.
        /// </summary>
        /// <param name="actor">Actor.</param>
        protected virtual void OnBarkStart(Transform actor)
        {
            Fsm.EventData.GameObjectData = (actor != null) ? actor.gameObject : null;
            SendEvent("OnBarkStart");
        }

        /// <summary>
        /// Sent at the end of a bark. The actor is the other participant.
        /// Fsm.EventData.GameObjectData is set to the actor.
        /// </summary>
        /// <param name="actor">Actor.</param>
        protected virtual void OnBarkEnd(Transform actor)
        {
            Fsm.EventData.GameObjectData = (actor != null) ? actor.gameObject : null;
            SendEvent("OnBarkEnd");
        }

        /// <summary>
        /// Sent when barking a line.
        /// Fsm.EventData.GameObjectData is set to the barker.
        /// </summary>
        /// <param name="subtitle">Subtitle being barked.</param>
        protected virtual void OnBarkLine(Subtitle subtitle)
        {
            Fsm.EventData.GameObjectData = (subtitle.speakerInfo.transform != null) ? subtitle.speakerInfo.transform.gameObject : null;
            SendEvent("OnBarkLine");
        }

        /// <summary>
        /// Sent at the beginning of a cutscene sequence. The actor is the other participant. 
        /// (Sequences can have an optional speaker and listener.)
        /// Fsm.EventData.GameObjectData is set to the actor.
        /// </summary>
        /// <param name="actor">Actor.</param>
        protected virtual void OnSequenceStart(Transform actor)
        {
            Fsm.EventData.GameObjectData = (actor != null) ? actor.gameObject : null;
            SendEvent("OnSequenceStart");
        }

        /// <summary>
        /// Sent at the end of a sequence. The actor is the other participant.
        /// Fsm.EventData.GameObjectData is set to the actor.
        /// </summary>
        /// <param name="actor">Actor.</param>
        protected virtual void OnSequenceEnd(Transform actor)
        {
            Fsm.EventData.GameObjectData = (actor != null) ? actor.gameObject : null;
            SendEvent("OnSequenceEnd");
        }

        /// <summary>
        /// Sent when a quest state or quest entry state changes.
        /// Fsm.EventData.StringData is set to the name of the quest.
        /// </summary>
        /// <param name="title">Quest title.</param>
        protected virtual void OnQuestStateChange(string title)
        {
            Fsm.EventData.StringData = title;
            SendEvent("OnQuestStateChange");
        }

        /// <summary>
        /// Sent when a quest's tracking is toggled on.
        /// Fsm.EventData.StringData is set to the name of the quest.
        /// </summary>
        /// <param name="title">Quest title.</param>
        protected virtual void OnQuestTrackingEnabled(string title)
        {
            Fsm.EventData.StringData = title;
            SendEvent("OnQuestTrackingEnabled");
        }

        /// <summary>
        /// Sent when a quest's tracking is toggled off.
        /// Fsm.EventData.StringData is set to the name of the quest.
        /// </summary>
        /// <param name="title">Quest title.</param>
        protected virtual void OnQuestTrackingDisabled(string title)
        {
            Fsm.EventData.StringData = title;
            SendEvent("OnQuestTrackingDisabled");
        }

        /// <summary>
        /// Sent when the Dialogue System is recording persistent data into the Lua
        /// environment, usually when saving a game or before changing levels.
        /// </summary>
        public virtual void OnRecordPersistentData()
        {
            SendEvent("OnRecordPersistentData");
        }

        /// <summary>
        /// Sent when the Dialogue System is applying persistent data to the scene
        /// from the Lua environment, usually when loading a game or after changing levels.
        /// </summary>
        public virtual void OnApplyPersistentData()
        {
            SendEvent("OnApplyPersistentData");
        }

        /// <summary>
        /// Sent prior to unloading a level, usually before changing levels.
        /// </summary>
        public virtual void OnLevelWillBeUnloaded()
        {
            SendEvent("OnLevelWillBeUnloaded");
        }

        /// <summary>
        /// Sent when the Dialogue System is paused.
        /// </summary>
        protected virtual void OnDialogueSystemPause()
        {
            SendEvent("OnDialogueSystemPause");
        }

        /// <summary>
        /// Sent when the Dialogue System is unpaused.
        /// </summary>
        protected virtual void OnDialogueSystemUnpause()
        {
            SendEvent("OnDialogueSystemUnpause");
        }

    }

}