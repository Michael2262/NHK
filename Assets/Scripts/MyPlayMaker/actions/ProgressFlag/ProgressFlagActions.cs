using HutongGames.PlayMaker;

namespace MyGame.Actions
{
    [ActionCategory("Progress - Flags")]
    [Tooltip("通用標記添加：可選擇生命週期。")]
    public class AddProgressFlag : FsmStateAction
    {
        [RequiredField] public FsmString flag;
        [ObjectType(typeof(FlagLifetime))] public FsmEnum lifetime;

        public override void OnEnter()
        {
            GameStatusService.Instance.ProgressFlags.AddFlag(flag.Value, (FlagLifetime)lifetime.Value);
            Finish();
        }
    }

    [ActionCategory("Progress - Flags")]
    [Tooltip("快捷：添加一個永久標記 (Persistent)。")]
    public class AddPersistentFlag : FsmStateAction
    {
        [RequiredField] public FsmString flag;

        public override void OnEnter()
        {
            GameStatusService.Instance.ProgressFlags.AddPersistentFlag(flag.Value);
            Finish();
        }
    }

    [ActionCategory("Progress - Flags")]
    [Tooltip("移除標記或數值紀錄。")]
    public class RemoveProgressFlag : FsmStateAction
    {
        [RequiredField] public FsmString flag;

        public override void OnEnter()
        {
            GameStatusService.Instance.ProgressFlags.RemoveFlag(flag.Value);
            Finish();
        }
    }
}