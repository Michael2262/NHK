// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.PlayMakerSupport
{

    [ActionCategory("Pixel Crushers Common")]
    [HutongGames.PlayMaker.Tooltip("Sets the current language for localization.")]
    public class SetCurrentTextTableLanguage : FsmStateAction
    {
        public TextTable textTable;

        [HutongGames.PlayMaker.Tooltip("The language name (leave blank to use Language ID). Text Table must be assigned.")]
        public FsmString languageName = new FsmString();

        [HutongGames.PlayMaker.Tooltip("The language ID (leave -1 to use Language Name). Text Table doesn't have to be assigned.")]
        public FsmInt languageID = new FsmInt(-1);

        public override void Reset()
        {
            languageName = null;
            languageID = null;
        }

        public override void OnEnter()
        {
            if (languageID != null && languageID.Value != -1)
            {
                TextTable.currentLanguageID = languageID.Value;
            }
            else if (languageName != null && !string.IsNullOrEmpty(languageName.Value) && textTable != null)
            {
                TextTable.currentLanguageID = textTable.GetLanguageID(languageName.Value);
            }
            Finish();
        }

    }

}