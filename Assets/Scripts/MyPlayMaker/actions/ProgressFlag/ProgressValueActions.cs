using HutongGames.PlayMaker;

namespace MyGame.Actions
{
    [ActionCategory("Progress - Variables")]
    [Tooltip("設定進度數值變數。")]
    public class SetProgressValue : FsmStateAction
    {
        [RequiredField] public FsmString key;
        [RequiredField] public FsmInt value;

        public override void OnEnter()
        {
            GameStatusService.Instance.ProgressFlags.SetValue(key.Value, value.Value);
            Finish();
        }
    }

    [ActionCategory("Progress - Variables")]
    [Tooltip("獲取進度數值並存入 PlayMaker 變數。")]
    public class GetProgressValue : FsmStateAction
    {
        [RequiredField] public FsmString key;
        [RequiredField][UIHint(UIHint.Variable)] public FsmInt storeValue;

        public override void OnEnter()
        {
            storeValue.Value = GameStatusService.Instance.ProgressFlags.GetValue(key.Value);
            Finish();
        }
    }

    [ActionCategory("Progress - Variables")]
    [Tooltip("增加或減少進度數值。")]
    public class AddProgressValue : FsmStateAction
    {
        [RequiredField] public FsmString key;
        [RequiredField] public FsmInt amount;

        public override void OnEnter()
        {
            GameStatusService.Instance.ProgressFlags.AddValue(key.Value, amount.Value);
            Finish();
        }
    }
}