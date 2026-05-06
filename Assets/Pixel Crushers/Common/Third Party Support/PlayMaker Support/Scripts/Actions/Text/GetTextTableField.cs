// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using HutongGames.PlayMaker;

namespace PixelCrushers.PlayMakerSupport
{

    [ActionCategory("Pixel Crushers Common")]
    [HutongGames.PlayMaker.Tooltip("Looks up the current localized version of a text table field.")]
    public class GetTextTableField : FsmStateAction
    {
        public TextTable textTable;

        [RequiredField]
        [HutongGames.PlayMaker.Tooltip("The field to look up.")]
        public FsmString fieldName = new FsmString();

        [RequiredField]
        [UIHint(UIHint.Variable)]
        [HutongGames.PlayMaker.Tooltip("Store the result in a String variable.")]
        public FsmString storeResult = new FsmString();

        public override void Reset()
        {
            textTable = null;
            fieldName = new FsmString();
            storeResult.Value = string.Empty;
        }

        public override void OnEnter()
        {
            storeResult.Value = (textTable != null && !string.IsNullOrEmpty(fieldName.Value))
                ? textTable.GetFieldText(fieldName.Value) : string.Empty;
            Finish();
        }

    }

}