// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.PlayMakerSupport
{

    [ActionCategory("Pixel Crushers Common")]
    [HutongGames.PlayMaker.Tooltip("Gets the current language for localization.")]
    public class GetCurrentTextTableLanguage : FsmStateAction
    {
        public TextTable textTable;

        [UIHint(UIHint.Variable)]
        [HutongGames.PlayMaker.Tooltip("Store the language name result in a String variable. Text Table must be assigned.")]
        public FsmString storeLanguageName = null;

        [UIHint(UIHint.Variable)]
        [HutongGames.PlayMaker.Tooltip("Store the language ID result in an Int variable. Text Table doesn't have to be assigned.")]
        public FsmInt storeLanguageID = null;

        public override void Reset()
        {
            storeLanguageName = null;
            storeLanguageID = null;
        }

        public override void OnEnter()
        {
            var languageID = TextTable.currentLanguageID;
            if (storeLanguageID != null) storeLanguageID.Value = languageID;
            if (storeLanguageName != null && textTable != null) storeLanguageName.Value = textTable.GetLanguageName(languageID);
            Finish();
        }

    }

}