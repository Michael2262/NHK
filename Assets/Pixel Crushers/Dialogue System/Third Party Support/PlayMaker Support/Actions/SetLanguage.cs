using System;
using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.DialogueSystem.PlayMaker {
	
	[ActionCategory("Dialogue System")]
	[HutongGames.PlayMaker.TooltipAttribute("Set the language to use for localization.")]
	public class SetLanguage : FsmStateAction {
		
		[HutongGames.PlayMaker.TooltipAttribute("The current language to use")]
		public FsmString language;
		
		public override void Reset() {
			if (language != null) language.Value = string.Empty;
		}
		
		public override void OnEnter() {
			DialogueManager.SetLanguage((language != null) ? language.Value : string.Empty);
			Finish();
		}
		
	}
	
}