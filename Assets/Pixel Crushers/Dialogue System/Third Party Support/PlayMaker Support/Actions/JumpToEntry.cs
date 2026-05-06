using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.DialogueSystem.PlayMaker
{

    [ActionCategory("Dialogue System")]
    [HutongGames.PlayMaker.TooltipAttribute("Jumps the active conversation to a specific entry.")]
    public class JumpToEntry : FsmStateAction
    {

        [HutongGames.PlayMaker.TooltipAttribute("Conversation containing the dialogue entry. Leave blank to use active conversation. (This is the conversation originally started; not linked conversation if it crossed links.)")]
        public FsmString conversationTitle;

        [HutongGames.PlayMaker.TooltipAttribute("The starting dialogue entry ID. Leave at -1 to start at the beginning")]
        public FsmInt entryID;

        public override void Reset()
        {
            conversationTitle = new FsmString();
            entryID = new FsmInt();
            entryID.Value = -1;
        }

        public override void OnEnter()
        {
            if (!DialogueManager.isConversationActive)
            {
                LogError("Can't jump to entry " + entryID.Value + ". No conversation is active.");
            }
            else
            { 
                var id = (entryID != null) ? entryID.Value : -1;
                var conversation = (conversationTitle != null && !string.IsNullOrEmpty(conversationTitle.Value))
                    ? DialogueManager.masterDatabase.GetConversation(conversationTitle.Value)
                    : DialogueManager.masterDatabase.GetConversation(DialogueManager.lastConversationID);
                var entry = (conversation != null) ? conversation.GetDialogueEntry(id) : null;
                if (entry == null)
                {
                    LogError("Can't find entry  " + entryID.Value + " to jump to it.");
                }
                else
                {
                    var state = DialogueManager.conversationModel.GetState(entry);
                    DialogueManager.conversationController.GotoState(state);
                }
            }
            Finish();
        }

    }

}