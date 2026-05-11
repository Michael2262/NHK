using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    [ActionCategory("Progress - Flags")]
    [Tooltip("通用標記添加：可選擇生命週期。")]
    public class AddProgressFlag : FsmStateAction
    {
        [RequiredField]
        [Tooltip("拖入旗標定義")]
        public ProgressFlagDefinition flagDef;

        [ObjectType(typeof(FlagLifetime))]
        public FsmEnum lifetime;

        public override void OnEnter()
        {
            if (flagDef != null)
                GameStatusService.Instance.ProgressFlags.AddFlag(flagDef.FlagID, (FlagLifetime)lifetime.Value);
            Finish();
        }
    }

    [ActionCategory("Progress - Flags")]
    [Tooltip("快捷：添加一個永久標記 (Persistent)。")]
    public class AddPersistentFlag : FsmStateAction
    {
        [RequiredField]
        [Tooltip("拖入旗標定義")]
        public ProgressFlagDefinition flagDef;

        public override void OnEnter()
        {
            if (flagDef != null)
                GameStatusService.Instance.ProgressFlags.AddPersistentFlag(flagDef.FlagID);
            Finish();
        }
    }

    [ActionCategory("Progress - Flags")]
    [Tooltip("移除標記或數值紀錄。")]
    public class RemoveProgressFlag : FsmStateAction
    {
        [RequiredField]
        [Tooltip("拖入旗標定義")]
        public ProgressFlagDefinition flagDef;

        public override void OnEnter()
        {
            if (flagDef != null)
                GameStatusService.Instance.ProgressFlags.RemoveFlag(flagDef.FlagID);
            Finish();
        }
    }
}
