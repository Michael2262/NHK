using System;
using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.DialogueSystem.PlayMaker
{

	[ActionCategory("Dialogue System")]
	[HutongGames.PlayMaker.TooltipAttribute("Gets the type of a Dialogue System Lua variable.")]
	public class GetVariableType : FsmStateAction
	{

		[RequiredField]
		[HutongGames.PlayMaker.TooltipAttribute("The name of the variable")]
		public FsmString variableName;

		public FsmEvent stringTypeEvent;
		public FsmEvent boolTypeEvent;
		public FsmEvent numberTypeEvent;
		public FsmEvent otherTypeEvent;

		public override void Reset()
		{
			if (variableName != null) variableName.Value = string.Empty;
		}

		public override void OnEnter()
		{
			if ((variableName == null) || (string.IsNullOrEmpty(variableName.Value)))
			{
				LogError(string.Format("{0}: Variable Name is null or blank.", DialogueDebug.Prefix));
			}
			else
			{
				var luaResult = DialogueLua.GetVariable(variableName.Value);
				if (luaResult.isBool)
				{
					Fsm.Event(boolTypeEvent);
				}
				else if (luaResult.isNumber)
                {
					Fsm.Event(numberTypeEvent);
				}
				else if (luaResult.isString)
				{
					Fsm.Event(stringTypeEvent);
				}
				else
                {
					Fsm.Event(otherTypeEvent);
                }
			}
			Finish();
		}

	}

}
