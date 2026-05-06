using System;
using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.DialogueSystem.PlayMaker
{

    [ActionCategory("Dialogue System")]
    [HutongGames.PlayMaker.TooltipAttribute("Gets the title of the most recently-ended conversation.")]
    public class GetLastConversationEnded: FsmStateAction
    {

        [UIHint(UIHint.Variable)]
        [HutongGames.PlayMaker.TooltipAttribute("Store the conversation title in a String variable")]
        public FsmString conversationTitle;

        public override void Reset()
        {
            conversationTitle = null;
        }

        public override void OnEnter()
        {
            if (conversationTitle != null) conversationTitle.Value = DialogueManager.lastConversationEnded;
            Finish();
        }

    }

}