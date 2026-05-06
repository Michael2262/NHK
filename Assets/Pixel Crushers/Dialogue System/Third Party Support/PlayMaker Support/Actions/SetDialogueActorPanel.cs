using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.DialogueSystem.PlayMaker
{

    [ActionCategory("Dialogue System")]
    [HutongGames.PlayMaker.TooltipAttribute("Sets a Dialogue Actor's subtitle panel number.")]
    public class SetDialogueActorPanel : FsmStateAction
    {

        [RequiredField]
        [HutongGames.PlayMaker.TooltipAttribute("GameObject with a Dialogue Actor component")]
        public FsmGameObject dialogueActor;

        [HutongGames.PlayMaker.TooltipAttribute("Subtitle panel number")]
        public SubtitlePanelNumber subtitlePanelNumber;

        [HutongGames.PlayMaker.TooltipAttribute("If Subtitle Panel Number is Custom, the custom panel to use")]
        public FsmGameObject customPanel;

        public override void Reset()
        {
            dialogueActor = null;
            subtitlePanelNumber = SubtitlePanelNumber.Default;
            customPanel = null;
        }

        public override void OnEnter()
        {
            var areParamsAssigned = (dialogueActor != null && dialogueActor.Value != null);
            if (!areParamsAssigned)
            {
                LogError(string.Format("{0}: You must assign the Dialogue Actor and a panel number or custom panel.", DialogueDebug.Prefix));
            }
            else
            {
                var dialogueActorComponent = DialogueActor.GetDialogueActorComponent(dialogueActor.Value.transform);
                if (dialogueActorComponent == null)
                {
                    LogError(string.Format("{0}: The Dialogue Actor GameObject doesn't have a Dialogue Actor component.", DialogueDebug.Prefix));
                }
                else
                {
                    dialogueActorComponent.standardDialogueUISettings.subtitlePanelNumber = subtitlePanelNumber;
                    if (subtitlePanelNumber == SubtitlePanelNumber.Custom)
                    {
                        if (customPanel.Value == null)
                        {
                            LogError(string.Format("{0}: You must assign Custom Panel.", DialogueDebug.Prefix));
                        }
                        else
                        {
                            var customSubtitlePanel = customPanel.Value.GetComponent<StandardUISubtitlePanel>();
                            if (customSubtitlePanel == null)
                            {
                                LogError(string.Format("{0}: The Custom Panel GameObject doesn't have a Standard UI Subtitle Panel component.", DialogueDebug.Prefix));
                            }
                            else
                            {
                                dialogueActorComponent.standardDialogueUISettings.customSubtitlePanel = customSubtitlePanel;
                                dialogueActorComponent.standardDialogueUISettings.subtitlePanelNumber = SubtitlePanelNumber.Custom;
                            }
                        }
                    }
                }
            }
            Finish();
        }

    }
}