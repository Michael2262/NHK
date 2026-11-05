using HutongGames.PlayMaker;

namespace MyGame.Actions
{
    [ActionCategory("Time")]
    [Tooltip("操作目前小遊戲的 BulletTime。結束操作會先結算正式時間，再完成 Action。")]
    public sealed class BulletTime : FsmStateAction
    {
        public BulletTimeOperation operation;
        [Tooltip("操作被拒絕時送出（選填）。")]
        public FsmEvent failedEvent;

        public override void Reset()
        {
            operation = BulletTimeOperation.Begin;
            failedEvent = null;
        }

        public override void OnEnter()
        {
            if (!BulletTimeBridge.TryExecuteCurrent(operation) && failedEvent != null)
                Fsm.Event(failedEvent);
            Finish();
        }
    }
}
