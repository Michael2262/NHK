using System;
using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.DialogueSystem.PlayMaker {
	
	[ActionCategory("Dialogue System")]
	[HutongGames.PlayMaker.TooltipAttribute("Makes an NPC bark.")]
	public class Bark : FsmStateAction {

		public enum BarkSource { Conversation, Text }
		public BarkSource barkSource = BarkSource.Conversation;

		[HutongGames.PlayMaker.TooltipAttribute("Bark this text")]
		public FsmString barkText;

		[HutongGames.PlayMaker.TooltipAttribute("Play this sequence with the bark")]
		public FsmString barkSequence;

		[HutongGames.PlayMaker.TooltipAttribute("The conversation containing the bark lines")]
		public FsmString conversation;
		
		[RequiredField]
		[HutongGames.PlayMaker.TooltipAttribute("The character speaking the bark")]
		public FsmGameObject speaker;
		
		[HutongGames.PlayMaker.TooltipAttribute("The character being barked at (optional)")]
		public FsmGameObject listener;
		
		public override void Reset() {
			barkSource = BarkSource.Conversation;
			if (barkText != null) barkText.Value = string.Empty;
			if (barkSequence != null) barkSequence.Value = string.Empty;
			if (conversation != null) conversation.Value = string.Empty;
			if (speaker != null) speaker.Value = null;
			if (listener != null) listener.Value = null;
		}
		
		public override void OnEnter() {
			Transform speakerTransform = ((speaker != null) && (speaker.Value != null)) ? speaker.Value.transform : null;
			Transform listenerTransform = ((listener != null) && (listener.Value != null)) ? listener.Value.transform : null;
			if (speakerTransform == null) Debug.LogWarning(string.Format("{0}: PlayMaker Action Bark - speaker is null", DialogueDebug.Prefix));
			switch (barkSource)
			{
				case BarkSource.Conversation:
					string conversationTitle = (conversation != null) ? conversation.Value : string.Empty;
					if (string.IsNullOrEmpty(conversationTitle)) Debug.LogWarning(string.Format("{0}: PlayMaker Action Bark - conversation title is blank", DialogueDebug.Prefix));
					if (listenerTransform != null)
					{
						DialogueManager.Bark(conversationTitle, speakerTransform, listenerTransform);
					}
					else
					{
						DialogueManager.Bark(conversationTitle, speakerTransform);
					}
					break;
				case BarkSource.Text:
					string barkString = (barkText != null) ? barkText.Value : string.Empty;
					string sequence = (barkSequence != null) ? barkSequence.Value : string.Empty;
					if (string.IsNullOrEmpty(barkString)) Debug.LogWarning(string.Format("{0}: PlayMaker Action Bark - Bark Text is blank", DialogueDebug.Prefix));
					DialogueManager.BarkString(barkString, speakerTransform, listenerTransform, sequence);
					break;
			}
			Finish();
		}
		
	}
}
