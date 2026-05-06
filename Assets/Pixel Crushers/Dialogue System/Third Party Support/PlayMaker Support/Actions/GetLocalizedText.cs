using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.DialogueSystem.PlayMaker
{

    [ActionCategory("Dialogue System")]
    [HutongGames.PlayMaker.TooltipAttribute("Gets the value of a text table field.")]
    public class GetLocalizedText : FsmStateAction
    {

        [RequiredField]
        [HutongGames.PlayMaker.TooltipAttribute("The text table")]
        public TextTable textTable;

        [RequiredField]
        [HutongGames.PlayMaker.TooltipAttribute("The field in the table")]
        public FsmString field;

        [UIHint(UIHint.Variable)]
        [HutongGames.PlayMaker.TooltipAttribute("Store the result in a String variable")]
        public FsmString storeResult;

        public override void Reset()
        {
            textTable = null;
            if (field != null) field.Value = string.Empty;
            storeResult = null;
        }

        public override void OnEnter()
        {
            if (textTable == null && DialogueManager.DisplaySettings.localizationSettings.textTable == null)
            {
                LogError(string.Format("{0}: Text table is null. Assign one to this action or the Dialogue Manager.", DialogueDebug.Prefix));
            }
            else if ((field == null) || (string.IsNullOrEmpty(field.Value)))
            {
                LogError(string.Format("{0}: Field is null or blank.", DialogueDebug.Prefix));
            }
            else
            {
                var table = textTable ?? DialogueManager.DisplaySettings.localizationSettings.textTable;
                if (!table.HasField(field.Value))
                {
                    LogError(string.Format("{0}: Text table {1} does not contain a field '{2}'. (Field must match exactly, including case.)", new string[] { DialogueDebug.Prefix, table.name, field.Value }));
                }
                else
                {
                    if (storeResult != null) storeResult.Value = table.GetFieldTextForLanguage(field.Value, Localization.language);
                }
            }
            Finish();
        }

    }

}