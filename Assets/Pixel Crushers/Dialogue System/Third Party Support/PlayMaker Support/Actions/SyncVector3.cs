using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.DialogueSystem.PlayMaker
{

    [ActionCategory("Dialogue System")]
    [HutongGames.PlayMaker.Tooltip("Syncs a Vector3 variable between PlayMaker and the Dialogue System's Variable[] Lua table.")]
    public class SyncVector3 : FsmStateAction
    {

        [RequiredField]
        [HutongGames.PlayMaker.Tooltip("The name of the variable in the Dialogue System")]
        public FsmString dialogueSystemVariable;

        [UIHint(UIHint.Variable)]
        [HutongGames.PlayMaker.Tooltip("The PlayMaker variable")]
        public FsmVector3 playMakerVariable;

        [RequiredField]
        [HutongGames.PlayMaker.Tooltip("If ticked, copy PlayMaker value to Dialogue System; if unticked, copy Dialogue System value to PlayMaker")]
        public FsmBool toDialogueSystem;

        [HutongGames.PlayMaker.Tooltip("Repeat every frame while the state is active")]
        public bool everyFrame;

        public override void Reset()
        {
            if (dialogueSystemVariable != null) dialogueSystemVariable.Value = string.Empty;
            playMakerVariable = new FsmVector3 { UseVariable = true };
            toDialogueSystem = null;
            everyFrame = false;
        }

        public override string ErrorCheck()
        {
            bool valid = (dialogueSystemVariable != null) && (playMakerVariable != null);
            return valid ? base.ErrorCheck() : "Assign Dialogue System and PlayMaker variables.";
        }

        public override void OnEnter()
        {
            Sync();
            if (!everyFrame) Finish();
        }

        public override void OnUpdate()
        {
            if (everyFrame)
            {
                Sync();
            }
            else
            {
                Finish();
            }
        }

        private void Sync()
        {
            if (dialogueSystemVariable == null || string.IsNullOrEmpty(dialogueSystemVariable.Value))
            {
                LogWarning(DialogueDebug.Prefix + ": Dialogue System Variable isn't assigned or is blank.");
            }
            else if (playMakerVariable == null)
            {
                LogWarning(DialogueDebug.Prefix + ": PlayMaker Variable isn't assigned or is blank.");
            }
            else
            {
                if (toDialogueSystem != null && toDialogueSystem.Value == true)
                {
                    DialogueLua.SetVariable(dialogueSystemVariable.Value, DialogueSystemPlayMakerTools.Vector3ToString(playMakerVariable.Value));
                }
                else
                {
                    playMakerVariable.Value = DialogueSystemPlayMakerTools.StringToVector3(DialogueLua.GetVariable(dialogueSystemVariable.Value).AsString);
                }
            }
        }

    }

}
