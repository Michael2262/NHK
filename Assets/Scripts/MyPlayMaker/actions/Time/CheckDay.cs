using System;
using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  CheckDay — 判斷今天的日子                                          ║
    // ║  mode = DayType：判斷今天是平日 / 假日                              ║
    // ║  mode = SpecificDayOfWeek：判斷今天是不是指定星期                   ║
    // ╚════════════════════════════════════════════════════════════════════╝
    [ActionCategory("Time")]
    [Tooltip("判斷今天是平日/假日，或今天是否為指定星期。送出 true/false 事件。")]
    public class CheckDay : FsmStateAction
    {
        public enum CheckMode
        {
            DayType,            // 判斷平日 / 假日
            SpecificDayOfWeek   // 判斷是否為指定星期
        }

        public enum DayType
        {
            Weekday,    // 平日
            Holiday     // 假日
        }

        [Tooltip("判斷模式")]
        public CheckMode mode = CheckMode.DayType;

        [Tooltip("（mode = DayType 時）要比對的日子類型")]
        public DayType dayType = DayType.Weekday;

        [Tooltip("（mode = SpecificDayOfWeek 時）要比對的星期")]
        public DayOfWeek targetDayOfWeek = DayOfWeek.Monday;

        [Tooltip("條件成立時發送")]
        public FsmEvent trueEvent;

        [Tooltip("條件不成立時發送")]
        public FsmEvent falseEvent;

        [UIHint(UIHint.Variable)]
        [Tooltip("將比較結果存入此變數")]
        public FsmBool storeResult;

        [UIHint(UIHint.Variable)]
        [Tooltip("將今天星期幾(0=週日…6=週六)存入此變數")]
        public FsmInt storeDayOfWeek;

        public bool everyFrame;

        public override void Reset()
        {
            mode = CheckMode.DayType;
            dayType = DayType.Weekday;
            targetDayOfWeek = DayOfWeek.Monday;
            trueEvent = null;
            falseEvent = null;
            storeResult = null;
            storeDayOfWeek = null;
            everyFrame = false;
        }

        public override void OnEnter()
        {
            DoCheck();
            if (!everyFrame) Finish();
        }

        public override void OnUpdate()
        {
            DoCheck();
        }

        private void DoCheck()
        {
            var service = GameStatusService.Instance;
            if (service == null)
            {
                Debug.LogError("[CheckDay] 找不到 GameStatusService！");
                Fsm.Event(falseEvent);
                return;
            }

            var model = service.Time;

            if (!storeDayOfWeek.IsNone)
                storeDayOfWeek.Value = (int)model.CurrentDayOfWeek;

            bool result = mode switch
            {
                CheckMode.DayType => dayType == DayType.Holiday ? model.IsWeekend : !model.IsWeekend,
                CheckMode.SpecificDayOfWeek => model.CurrentDayOfWeek == targetDayOfWeek,
                _ => false
            };

            if (!storeResult.IsNone)
                storeResult.Value = result;

            Fsm.Event(result ? trueEvent : falseEvent);
        }
    }
}
