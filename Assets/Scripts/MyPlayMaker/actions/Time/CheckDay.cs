using System;
using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    [ActionCategory("Time")]
    [Tooltip("依目前遊戲時間的星期，送出週一到週日對應的事件；不推進時間。")]
    public class CheckDay : FsmStateAction
    {
        [Tooltip("週一時發送。")]
        public FsmEvent mondayEvent;

        [Tooltip("週二時發送。")]
        public FsmEvent tuesdayEvent;

        [Tooltip("週三時發送。")]
        public FsmEvent wednesdayEvent;

        [Tooltip("週四時發送。")]
        public FsmEvent thursdayEvent;

        [Tooltip("週五時發送。")]
        public FsmEvent fridayEvent;

        [Tooltip("週六時發送。")]
        public FsmEvent saturdayEvent;

        [Tooltip("週日時發送。")]
        public FsmEvent sundayEvent;

        [Tooltip("時間系統未就緒時發送。")]
        public FsmEvent failedEvent;

        [UIHint(UIHint.Variable)]
        [Tooltip("儲存今天星期幾（選填）：0=週日、1=週一…6=週六；時間系統未就緒時寫入 -1。")]
        public FsmInt storeDayOfWeek;

        [Tooltip("每幀重新判斷；關閉時只在進入 State 時判斷一次。")]
        public bool everyFrame;

        public override void Reset()
        {
            mondayEvent = null;
            tuesdayEvent = null;
            wednesdayEvent = null;
            thursdayEvent = null;
            fridayEvent = null;
            saturdayEvent = null;
            sundayEvent = null;
            failedEvent = null;
            storeDayOfWeek = new FsmInt { UseVariable = true };
            everyFrame = false;
        }

        public override void OnEnter()
        {
            DoCheck();
            if (!everyFrame) Finish();
        }

        public override void OnUpdate()
        {
            if (everyFrame) DoCheck();
        }

        private void DoCheck()
        {
            var model = GameStatusService.Instance?.Time;
            if (model == null)
            {
                if (storeDayOfWeek != null && !storeDayOfWeek.IsNone)
                    storeDayOfWeek.Value = -1;
                Debug.LogWarning("[CheckDay] 時間系統尚未初始化。");
                Fsm.Event(failedEvent);
                return;
            }

            DayOfWeek today = model.CurrentDayOfWeek;
            if (storeDayOfWeek != null && !storeDayOfWeek.IsNone)
                storeDayOfWeek.Value = (int)today;

            FsmEvent dayEvent = today switch
            {
                DayOfWeek.Monday => mondayEvent,
                DayOfWeek.Tuesday => tuesdayEvent,
                DayOfWeek.Wednesday => wednesdayEvent,
                DayOfWeek.Thursday => thursdayEvent,
                DayOfWeek.Friday => fridayEvent,
                DayOfWeek.Saturday => saturdayEvent,
                DayOfWeek.Sunday => sundayEvent,
                _ => failedEvent
            };

            Fsm.Event(dayEvent);
        }
    }

    [ActionCategory("Time")]
    [Tooltip("判斷目前遊戲時間是假日或平日，送出對應事件。假日為週六、週日；不推進時間。")]
    public class CheckHoliday : FsmStateAction
    {
        [Tooltip("假日（週六、週日）時發送。")]
        public FsmEvent holidayEvent;

        [Tooltip("平日（週一到週五）時發送。")]
        public FsmEvent weekdayEvent;

        [Tooltip("時間系統未就緒時發送，不觸發假日或平日事件。")]
        public FsmEvent failedEvent;

        [UIHint(UIHint.Variable)]
        [Tooltip("儲存是否為假日（選填）：true=假日、false=平日；時間系統未就緒時寫入 false，並送出 failedEvent。")]
        public FsmBool storeResult;

        [Tooltip("每幀重新判斷；關閉時只在進入 State 時判斷一次。")]
        public bool everyFrame;

        public override void Reset()
        {
            holidayEvent = null;
            weekdayEvent = null;
            failedEvent = null;
            storeResult = new FsmBool { UseVariable = true };
            everyFrame = false;
        }

        public override void OnEnter()
        {
            DoCheck();
            if (!everyFrame) Finish();
        }

        public override void OnUpdate()
        {
            if (everyFrame) DoCheck();
        }

        private void DoCheck()
        {
            var model = GameStatusService.Instance?.Time;
            if (model == null)
            {
                if (storeResult != null && !storeResult.IsNone)
                    storeResult.Value = false;
                Debug.LogWarning("[CheckHoliday] 時間系統尚未初始化。");
                Fsm.Event(failedEvent);
                return;
            }

            bool isHoliday = model.IsWeekend;
            if (storeResult != null && !storeResult.IsNone)
                storeResult.Value = isHoliday;

            Fsm.Event(isHoliday ? holidayEvent : weekdayEvent);
        }
    }
}
