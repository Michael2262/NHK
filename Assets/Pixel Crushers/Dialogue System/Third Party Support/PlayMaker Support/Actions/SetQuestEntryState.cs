using System;
using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.DialogueSystem.PlayMaker {
	
	[ActionCategory("Dialogue System")]
	[HutongGames.PlayMaker.TooltipAttribute("Sets the state of a quest entry in a quest.")]
	public class SetQuestEntryState : FsmStateAction {
		
		[RequiredField]
		[HutongGames.PlayMaker.TooltipAttribute("The name of the quest")]
		public FsmString questName;
		
		[RequiredField]
		[HutongGames.PlayMaker.TooltipAttribute("The quest entry number (from 1)")]
		public FsmInt entryNumber;
		
		[HutongGames.PlayMaker.TooltipAttribute("The quest state (unassigned, active, success, or failure)")]
		public FsmString state;

        [HutongGames.PlayMaker.TooltipAttribute("The quest state as a dropdown menu (used if State is blank)")]
        public QuestState stateDropdown;

        public override void Reset() {
			if (questName != null) questName.Value = string.Empty;
			if (entryNumber != null) entryNumber.Value = 0;
			if (state != null) state.Value = string.Empty;
		}
		
		public override void OnEnter() {
			if (PlayMakerTools.IsValueAssigned(questName))
			{
				var entryNum = Mathf.Max(1, entryNumber.Value);
				if (string.IsNullOrEmpty(state.Value))
				{
					QuestLog.SetQuestEntryState(questName.Value, entryNum, stateDropdown);
				}
				else
				{
					QuestLog.SetQuestEntryState(questName.Value, entryNum, QuestLog.StringToState(state.Value.ToLower()));
				}
			}
			else
			{
				LogError(string.Format("{0}: Quest Name is null or blank.", DialogueDebug.Prefix));
			}
			Finish();
		}
		
	}
	
}