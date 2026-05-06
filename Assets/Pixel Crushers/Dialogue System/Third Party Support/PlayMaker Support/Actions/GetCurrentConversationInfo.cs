using System;
using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.DialogueSystem.PlayMaker
{

    [ActionCategory("Dialogue System")]
    [HutongGames.PlayMaker.TooltipAttribute("Gets info about the currently active conversation.")]
    public class GetCurrentConversationInfo : FsmStateAction
    {

        [UIHint(UIHint.Variable)]
        [HutongGames.PlayMaker.TooltipAttribute("Store the conversation title in a String variable")]
        public FsmString conversationTitle;

        [UIHint(UIHint.Variable)]
        [HutongGames.PlayMaker.TooltipAttribute("Store the conversation ID in an Int variable")]
        public FsmInt conversationID;

        [UIHint(UIHint.Variable)]
        [HutongGames.PlayMaker.TooltipAttribute("Store the dialogue entry ID in an Int variable")]
        public FsmInt entryID;

        [UIHint(UIHint.Variable)]
        [HutongGames.PlayMaker.TooltipAttribute("Store the conversation actor name in a String variable")]
        public FsmString actorName;

        [UIHint(UIHint.Variable)]
        [HutongGames.PlayMaker.TooltipAttribute("Store the conversation conversant name in a String variable")]
        public FsmString conversantName;

        [UIHint(UIHint.Variable)]
        [HutongGames.PlayMaker.TooltipAttribute("Store the current node's speaker name in a String variable")]
        public FsmString speakerName;

        [UIHint(UIHint.Variable)]
        [HutongGames.PlayMaker.TooltipAttribute("Store the current node's listener name in a String variable")]
        public FsmString listenerName;

        [UIHint(UIHint.Variable)]
        [HutongGames.PlayMaker.TooltipAttribute("Store the speaker's actor ID in an Int variable")]
        public FsmInt speakerID;

        [UIHint(UIHint.Variable)]
        [HutongGames.PlayMaker.TooltipAttribute("Store the listener's actor ID in an Intvariable")]
        public FsmInt listenerID;

        public override void Reset()
        {
            conversationTitle = null;
            conversationID = null;
            entryID = null;
            actorName = null;
            conversantName = null;
            speakerName = null;
            listenerName = null;
            speakerID = null;
            listenerID = null;
        }

        public override void OnEnter()
        {
            var isActive = DialogueManager.isConversationActive;
            var state = isActive ? DialogueManager.currentConversationState : null;
            if (conversationTitle != null)
            {
                var conversation = isActive ? DialogueManager.MasterDatabase.GetConversation(state.subtitle.dialogueEntry.conversationID) : null;
                conversationTitle.Value = (conversation != null) ? conversation.Title : string.Empty;
            }
            if (conversationID != null) conversationID.Value = isActive ? state.subtitle.dialogueEntry.conversationID : 0;
            if (entryID != null) entryID.Value = isActive ? state.subtitle.dialogueEntry.id : 0;
            if (actorName != null) actorName.Value = DialogueLua.GetVariable("Actor").AsString;
            if (conversantName != null) conversantName.Value = DialogueLua.GetVariable("Conversant").AsString;
            if (speakerName != null) speakerName.Value = (state != null) ? state.subtitle.speakerInfo.Name : string.Empty;
            if (listenerName != null) listenerName.Value = (state != null) ? state.subtitle.listenerInfo.Name : string.Empty;
            if (speakerID != null) speakerID.Value = (state != null) ? state.subtitle.speakerInfo.id : 0;
            if (listenerID != null) listenerID.Value = (state != null) ? state.subtitle.listenerInfo.id : 0;
            Finish();
        }

    }

}