using System;
using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.DialogueSystem.PlayMaker {
	
	[ActionCategory("Dialogue System")]
	[HutongGames.PlayMaker.TooltipAttribute("Tells the active conversation's dialogue UI to select a response. Typically used in an FSM that responds to the OnConversationResponseMenu event.")]
	public class SelectResponse : FsmStateAction {

        [RequiredField]
        [HutongGames.PlayMaker.TooltipAttribute("The index of the response to select (starting from zero).")]
        public FsmInt index;
		
		public override void Reset() {
            index = new FsmInt();
		}
		
		public override void OnEnter() {
            (DialogueManager.DialogueUI as AbstractDialogueUI).OnClick(DialogueManager.CurrentConversationState.pcResponses[index.Value]);
			Finish();
		}
		
	}
	
}