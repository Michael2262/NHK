using UnityEngine;
using System.Collections.Generic;
using HutongGames.PlayMaker;

namespace PixelCrushers.DialogueSystem.PlayMaker {
	
	[ActionCategory("Dialogue System")]
	[HutongGames.PlayMaker.TooltipAttribute("Gets the value of a Lua variable from the Variable[] table.")]
	public class GetVariable : FsmStateAction {
		
		[RequiredField]
		[HutongGames.PlayMaker.TooltipAttribute("The name of the variable")]
		public FsmString variableName;

		[UIHint(UIHint.Variable)]
		[HutongGames.PlayMaker.TooltipAttribute("The value of the variable as a string")]
		public FsmString storeStringResult;
		
		[UIHint(UIHint.Variable)]
		[HutongGames.PlayMaker.TooltipAttribute("The value of the variable as a float")]
		public FsmFloat storeFloatResult;

        [UIHint(UIHint.Variable)]
        [HutongGames.PlayMaker.TooltipAttribute("The value of the variable as an int")]
        public FsmInt storeIntResult;

        [UIHint(UIHint.Variable)]
		[HutongGames.PlayMaker.TooltipAttribute("The value of the variable as a bool")]
		public FsmBool storeBoolResult;

        // Not adding Vector3 & arrays, since this slows down PlayMaker's editor too much when
        // there are several Get/SetVariable actions. Use Sync actions instead.

        //[UIHint(UIHint.Variable)]
        //[HutongGames.PlayMaker.TooltipAttribute("The value of the variable as a Vector3")]
        //public FsmVector3 storeVector3Result;

        //[UIHint(UIHint.Variable)]
        //[HutongGames.PlayMaker.TooltipAttribute("The value of the variable as a string array")]
        //[ArrayEditor(VariableType.String)]
        //public FsmArray storeStringArrayResult;

        //[UIHint(UIHint.Variable)]
        //[HutongGames.PlayMaker.TooltipAttribute("The value of the variable as an int array")]
        //[ArrayEditor(VariableType.Int)]
        //public FsmArray storeIntArrayResult;

        [HutongGames.PlayMaker.TooltipAttribute("Repeat every frame while the state is active")]
		public bool everyFrame;
		
		public override void Reset() {
			if (variableName != null) variableName.Value = string.Empty;
			storeStringResult = null;
			storeFloatResult = null;
            storeIntResult = null;
			storeBoolResult = null;
            //storeVector3Result = null;
            //storeStringArrayResult = null;
            //storeIntArrayResult = null;
        }

		public override string ErrorCheck() {
            bool anyResultVariable = (storeStringResult != null) || (storeFloatResult != null) || (storeBoolResult != null); // || (storeVector3Result != null); || (storeStringArrayResult != null) || (storeIntArrayResult != null);
			return anyResultVariable ? base.ErrorCheck() : "Assign at least one store result variable.";
		}
		
		public override void OnEnter() {
			GetAndStore();
			if (!everyFrame) Finish();
		}
		
		public override void OnUpdate() {
			if (everyFrame) {
				GetAndStore();
			} else {
				Finish();
			}
		}
		
		private void GetAndStore() {
			if ((variableName == null) || string.IsNullOrEmpty(variableName.Value)) {
				LogWarning(string.Format("{0}: Variable Name isn't assigned or is blank.", DialogueDebug.Prefix));
			} else {
				Lua.Result luaResult = DialogueLua.GetVariable(variableName.Value);
				if (storeStringResult != null) storeStringResult.Value = luaResult.AsString;
				if (storeFloatResult != null) storeFloatResult.Value = luaResult.AsFloat;
                if (storeIntResult != null) storeIntResult.Value = luaResult.AsInt;
				if (storeBoolResult != null) storeBoolResult.Value = luaResult.AsBool;
                //if (storeVector3Result != null) storeVector3Result.Value = DialogueSystemPlayMakerTools.StringToVector3(luaResult.AsString);
                //if (storeStringArrayResult != null) storeStringArrayResult.Values = DialogueSystemPlayMakerTools.StringToArray(luaResult.AsString, VariableType.String);
                //if (storeIntArrayResult != null) storeIntArrayResult.Values = DialogueSystemPlayMakerTools.StringToArray(luaResult.AsString, VariableType.Int);
            }
		}

    }

}
