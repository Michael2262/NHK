using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{

    /// <summary>
    /// Implements sequencer command FSMEvent(event, [subject, [fsm]]).
    /// </summary>
    public class SequencerCommandFSMEvent : SequencerCommand
    {

        public void Start()
        {
            string eventName = GetParameter(0);
            bool all = string.Equals(GetParameter(1), "all", System.StringComparison.OrdinalIgnoreCase);
            Transform subject = all ? null : GetSubject(1, Sequencer.Speaker);
            string fsmName = GetParameter(2);
            if (string.IsNullOrEmpty(eventName))
            {
                if (DialogueDebug.LogWarnings) Debug.LogWarning(string.Format("{0}: FSMEvent(): event name is empty", DialogueDebug.Prefix));
            }
            else if (!all && (subject == null))
            {
                if (DialogueDebug.LogWarnings) Debug.LogWarning(string.Format("{0}: FSMEvent({1}, {2}, {3}): subject is null", DialogueDebug.Prefix, eventName, GetParameter(1), fsmName));
            }
            else
            {
                if (DialogueDebug.LogInfo) Debug.Log(string.Format("{0}: FSMEvent({1}, {2}, {3}) sending event to FSM(s)", DialogueDebug.Prefix, eventName, subject.name, fsmName));
                if (all)
                {
                    // Send event to all GameObjects in scene:
                    PixelCrushers.DialogueSystem.PlayMaker.DialogueSystemPlayMakerTools.SendEventToAllFSMs(eventName, fsmName);
                }
                else
                {
                    // Send event to specified subject:
                    PixelCrushers.DialogueSystem.PlayMaker.DialogueSystemPlayMakerTools.SendEventToFSMs(subject, eventName, fsmName);
                }
            }
            Stop();
        }

    }

}
