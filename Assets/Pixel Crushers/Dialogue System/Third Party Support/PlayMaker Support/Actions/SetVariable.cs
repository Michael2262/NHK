using System;
using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.DialogueSystem.PlayMaker {
	
	[ActionCategory("Dialogue System")]
	[HutongGames.PlayMaker.TooltipAttribute("Sets the value of a Lua variable in the Variable[] table.")]
	public class SetVariable : FsmStateAction {
		
		[RequiredField]
		[HutongGames.PlayMaker.TooltipAttribute("The name of the variable")]
		public FsmString variableName;

		[HutongGames.PlayMaker.TooltipAttribute("The value of the variable as a string")]
		public FsmString stringValue = new FsmString { UseVariable = true };
		
		[HutongGames.PlayMaker.TooltipAttribute("The value of the variable as a float")]
		public FsmFloat floatValue = new FsmFloat { UseVariable = true };

        [HutongGames.PlayMaker.TooltipAttribute("The value of the variable as an int")]
        public FsmInt intValue = new FsmInt { UseVariable = true };

        [HutongGames.PlayMaker.TooltipAttribute("The value of the variable as a bool")]
		public FsmBool boolValue = new FsmBool { UseVariable = true };

        // Not adding Vector3 & arrays, since this slows down PlayMaker's editor too much when
        // there are several Get/SetVariable actions. Use Sync actions instead.

        //[HutongGames.PlayMaker.TooltipAttribute("The value of the variable as a Vector3")]
        //public FsmVector3 vector3Value = new FsmVector3 { UseVariable = true };

        //[HutongGames.PlayMaker.TooltipAttribute("The value of the variable as a string array")]
        //[ArrayEditor(VariableType.String)]
        //public FsmArray stringArrayValue = new FsmArray { UseVariable = true };

        //[HutongGames.PlayMaker.TooltipAttribute("The value of the variable as an int array")]
        //[ArrayEditor(VariableType.Int)]
        //public FsmArray intArrayValue = new FsmArray { UseVariable = true };

        public override void Reset() {
			if (variableName != null) variableName.Value = string.Empty;
			stringValue = new FsmString { UseVariable = true };
			floatValue = new FsmFloat { UseVariable = true };
            intValue = new FsmInt { UseVariable = true };
            boolValue = new FsmBool { UseVariable = true };
            //vector3Value = new FsmVector3 { UseVariable = true };
            //stringArrayValue = new FsmArray { UseVariable = true };
            //intArrayValue = new FsmArray { UseVariable = true };
        }

		public override string ErrorCheck() {
            bool anyValue = (stringValue != null) || (floatValue != null) || (boolValue != null); // || (vector3Value != null); || (stringArrayValue != null) || (intArrayValue != null);
			return anyValue ? base.ErrorCheck() : "Assign at least one value field.";
		}
		
		public override void OnEnter() {
			if ((variableName == null) || string.IsNullOrEmpty(variableName.Value)) {
				LogWarning(string.Format("{0}: Variable Name isn't assigned or is blank.", DialogueDebug.Prefix));
			} else {
				if ((stringValue != null) && !stringValue.IsNone) DialogueLua.SetVariable(variableName.Value, stringValue.Value);
				if ((floatValue != null) && !floatValue.IsNone) DialogueLua.SetVariable(variableName.Value, floatValue.Value);
                if ((intValue != null) && !intValue.IsNone) DialogueLua.SetVariable(variableName.Value, intValue.Value);
                if ((boolValue != null) && !boolValue.IsNone) DialogueLua.SetVariable(variableName.Value, boolValue.Value);
                //if ((vector3Value != null) && !vector3Value.IsNone) DialogueLua.SetVariable(variableName.Value, DialogueSystemPlayMakerTools.Vector3ToString(vector3Value.Value));
                //if ((stringArrayValue != null) && !stringArrayValue.IsNone) DialogueLua.SetVariable(variableName.Value, DialogueSystemPlayMakerTools.ArrayToString(stringArrayValue.Values));
                //if ((intArrayValue != null) && !intArrayValue.IsNone) DialogueLua.SetVariable(variableName.Value, DialogueSystemPlayMakerTools.ArrayToString(intArrayValue.Values));
            }
			Finish();
		}

    }
	
}
