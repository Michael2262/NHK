using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    [ActionCategory("router")]
    [Tooltip("啟動共用倒數，供 TimedNormalRareRouter 判斷。再次執行會從設定秒數重新開始。每次進入 State 執行一次，啟動後立即完成，不等待倒數結束。")]
    public class StartRouterTimer : FsmStateAction
    {
        [Tooltip("與分流 Action 選擇相同選項即可共用倒數，包含不同 FSM。切幕、新遊戲或讀檔清除，不存檔。")]
        public TimedRouterTimerId timerId;

        [RequiredField]
        [Tooltip("倒數秒數，預設 5 秒。0 秒立即清除該計時器。跟隨 timeScale，暫停時不計時。")]
        public FsmFloat duration;

        public override void Reset()
        {
            timerId = TimedRouterTimerId.GrabHand;
            duration = 5f;
        }

        public override void OnEnter()
        {
            string error = ErrorCheck();
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError("[StartRouterTimer] " + error);
                Finish();
                return;
            }

            var service = GameStatusService.Instance;
            if (service == null || service.TimedRouters == null)
            {
                Debug.LogError("[StartRouterTimer] GameStatusService 或分流 Model 尚未初始化。");
                Finish();
                return;
            }

            service.TimedRouters.StartTimer(timerId, duration.Value);
            Finish();
        }

        public override string ErrorCheck()
        {
            if (!System.Enum.IsDefined(typeof(TimedRouterTimerId), timerId))
                return "請選擇有效的 Timer Id。";
            if (duration == null || duration.IsNone || float.IsNaN(duration.Value)
                || float.IsInfinity(duration.Value) || duration.Value < 0f)
                return "Duration 必須為有限的非負秒數。";
            return null;
        }
    }
}
