using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    [ActionCategory("Progress - Variables")]
    [Tooltip("設定進度數值變數。")]
    public class SetProgressValue : FsmStateAction
    {
        [RequiredField]
        [Tooltip("拖入數值定義")]
        public ProgressValueDefinition valueDef;

        [RequiredField] public FsmInt value;

        public override void OnEnter()
        {
            if (valueDef != null)
                GameStatusService.Instance.ProgressFlags.SetValue(valueDef.FlagID, value.Value);
            Finish();
        }
    }

    [ActionCategory("Progress - Variables")]
    [Tooltip("獲取進度數值並存入 PlayMaker 變數。")]
    public class GetProgressValue : FsmStateAction
    {
        [RequiredField]
        [Tooltip("拖入數值定義")]
        public ProgressValueDefinition valueDef;

        [RequiredField][UIHint(UIHint.Variable)] public FsmInt storeValue;

        public override void OnEnter()
        {
            if (valueDef != null)
                storeValue.Value = GameStatusService.Instance.ProgressFlags.GetValue(valueDef.FlagID);
            Finish();
        }
    }

    [ActionCategory("Progress - Variables")]
    [Tooltip("增加或減少進度數值。")]
    public class AddProgressValue : FsmStateAction
    {
        [RequiredField]
        [Tooltip("拖入數值定義")]
        public ProgressValueDefinition valueDef;

        [RequiredField] public FsmInt amount;

        public override void OnEnter()
        {
            if (valueDef != null)
                GameStatusService.Instance.ProgressFlags.AddValue(valueDef.FlagID, amount.Value);
            Finish();
        }
    }
}
