using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    [ActionCategory("router")]
    [Tooltip("依初始機率分流至 Normal 或 Rare。Normal 啟動共用倒數，倒數內一律 Rare，且不延長倒數。每次進入 State 執行一次。")]
    public class TimedNormalRareRouter : FsmStateAction
    {
        [Tooltip("相同選項共用倒數，包含不同 FSM。GrabHand 與 GrabFoot 各自獨立。切幕、新遊戲或讀檔清除，不存檔。")]
        public TimedRouterTimerId timerId;

        [RequiredField]
        [Tooltip("沒有倒數時的 Rare 機率（0～100%），Normal 為剩餘機率。")]
        public FsmFloat rareChance;

        [RequiredField]
        [Tooltip("抽到 Normal 時啟動的倒數秒數；0 表示不啟動。跟隨 timeScale，暫停時不計時。共用倒數以啟動者的秒數為準。")]
        public FsmFloat duration;

        [RequiredField]
        [Tooltip("抽到 Normal 時發送的事件；發送前已啟動倒數。")]
        public FsmEvent normalEvent;

        [RequiredField]
        [Tooltip("抽到 Rare 或仍在共用倒數內時發送的事件。")]
        public FsmEvent rareEvent;

        public override void Reset()
        {
            timerId = TimedRouterTimerId.GrabHand;
            rareChance = 10f;
            duration = 5f;
            normalEvent = null;
            rareEvent = null;
        }

        public override void OnEnter()
        {
            string error = ErrorCheck();
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError("[TimedNormalRareRouter] " + error);
                Finish();
                return;
            }

            var service = GameStatusService.Instance;
            if (service == null || service.TimedRouters == null)
            {
                Debug.LogError("[TimedNormalRareRouter] GameStatusService 或分流 Model 尚未初始化。");
                Finish();
                return;
            }

            bool isRare = service.TimedRouters.Route(timerId, rareChance.Value, duration.Value);
            Fsm.Event(isRare ? rareEvent : normalEvent);
            Finish();
        }

        public override string ErrorCheck()
        {
            if (!System.Enum.IsDefined(typeof(TimedRouterTimerId), timerId))
                return "請選擇有效的 Timer Id。";
            if (rareChance == null || rareChance.IsNone || float.IsNaN(rareChance.Value)
                || rareChance.Value < 0f || rareChance.Value > 100f)
                return "Rare Chance 必須介於 0 到 100。";
            if (duration == null || duration.IsNone || float.IsNaN(duration.Value)
                || float.IsInfinity(duration.Value) || duration.Value < 0f)
                return "Duration 必須為有限的非負秒數。";
            if (normalEvent == null || string.IsNullOrEmpty(normalEvent.Name)
                || rareEvent == null || string.IsNullOrEmpty(rareEvent.Name))
                return "請分別設定 Normal Event 與 Rare Event。";
            if (normalEvent.Name == rareEvent.Name)
                return "Normal Event 與 Rare Event 必須使用不同事件。";
            return null;
        }
    }
}
