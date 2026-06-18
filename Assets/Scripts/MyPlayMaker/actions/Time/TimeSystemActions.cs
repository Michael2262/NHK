using System;
using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  AdvanceSlot — 推進 N 格時段                                       ║
    // ║  對應 Sequencer 指令：AdvanceSlot(格數, 是否轉場)                   ║
    // ╚════════════════════════════════════════════════════════════════════╝
    [ActionCategory("Time")]
    [Tooltip("推進 N 格時段。可選擇是否帶輕量轉場。剩餘時段不足時不推進並送出 failedEvent。")]
    public class AdvanceSlot : FsmStateAction
    {
        [Tooltip("要推進的時段格數（預設 1）")]
        public FsmInt slots;

        [Tooltip("true = 帶輕量轉場 / false = 直接切")]
        public FsmBool withTransition;

        [Tooltip("推進完成時送出的事件（選填）")]
        public FsmEvent finishedEvent;

        [Tooltip("剩餘時段不足、無法推進時送出的事件（選填）")]
        public FsmEvent failedEvent;

        private bool _waiting;
        private bool _done;

        public override void Reset()
        {
            slots = 1;
            withTransition = false;
            finishedEvent = null;
            failedEvent = null;
        }

        public override void OnEnter()
        {
            _waiting = false;
            _done = false;

            int count = slots.Value;
            if (count <= 0) { Finish(); return; }

            var service = GameStatusService.Instance;
            if (service == null)
            {
                Debug.LogError("[AdvanceSlot] 找不到 GameStatusService！");
                Fail();
                return;
            }

            var model = service.Time;

            if (!model.CanAdvanceWithinDay(count))
            {
                Debug.LogWarning($"[AdvanceSlot] 剩餘時段不足，無法推進 {count} 格。");
                Fail();
                return;
            }

            if (withTransition.Value)
            {
                _waiting = true;
                SceneController.PerformSlotTransition(
                    onMidTransition: () =>
                    {
                        model.AdvanceTime(count);
                        Debug.Log($"[AdvanceSlot] 輕量轉場推進 {count} 格完成。");
                    },
                    onComplete: () => _done = true);
            }
            else
            {
                model.AdvanceTime(count);
                Debug.Log($"[AdvanceSlot] 直接推進 {count} 格完成。");
                Succeed();
            }
        }

        public override void OnUpdate()
        {
            if (_waiting && _done) Succeed();
        }

        private void Succeed()
        {
            Fsm.Event(finishedEvent);
            Finish();
        }

        private void Fail()
        {
            Fsm.Event(failedEvent);
            Finish();
        }
    }

    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  AdvancePhase — 推進到下一個時段階段                               ║
    // ║  對應 Sequencer 指令：AdvancePhase(是否轉場)                        ║
    // ╚════════════════════════════════════════════════════════════════════╝
    [ActionCategory("Time")]
    [Tooltip("自動計算到下一個 Phase 開頭需要的格數並一次推進。已在最後階段時不跨日，送出 failedEvent。")]
    public class AdvancePhase : FsmStateAction
    {
        [Tooltip("true = 帶輕量轉場 / false = 直接切")]
        public FsmBool withTransition;

        [Tooltip("推進完成時送出的事件（選填）")]
        public FsmEvent finishedEvent;

        [Tooltip("已在最後階段或時段不足、無法推進時送出的事件（選填）")]
        public FsmEvent failedEvent;

        private bool _waiting;
        private bool _done;

        public override void Reset()
        {
            withTransition = false;
            finishedEvent = null;
            failedEvent = null;
        }

        public override void OnEnter()
        {
            _waiting = false;
            _done = false;

            var service = GameStatusService.Instance;
            if (service == null)
            {
                Debug.LogError("[AdvancePhase] 找不到 GameStatusService！");
                Fail();
                return;
            }

            var model = service.Time;
            var config = service.TimeConfig;

            int currentPhase = model.CurrentPhaseIndex;
            int totalPhases = config.PhaseNames.Count;

            if (currentPhase >= totalPhases - 1)
            {
                Debug.LogWarning("[AdvancePhase] 已在最後階段，無法推進（不允許自動跨日）。");
                Fail();
                return;
            }

            int remainingInPhase = config.GetTotalSlots(currentPhase) - model.CurrentSlotInPhase;
            int slotsNeeded = remainingInPhase + 1;

            if (!model.CanAdvanceWithinDay(slotsNeeded))
            {
                Debug.LogWarning("[AdvancePhase] 時段不足以推進到下一階段。");
                Fail();
                return;
            }

            if (withTransition.Value)
            {
                _waiting = true;
                SceneController.PerformSlotTransition(
                    onMidTransition: () =>
                    {
                        model.AdvanceTime(slotsNeeded);
                        Debug.Log($"[AdvancePhase] 輕量轉場推進 {slotsNeeded} 格，進入 Phase {currentPhase + 1}。");
                    },
                    onComplete: () => _done = true);
            }
            else
            {
                model.AdvanceTime(slotsNeeded);
                Debug.Log($"[AdvancePhase] 直接推進 {slotsNeeded} 格，進入 Phase {currentPhase + 1}。");
                Succeed();
            }
        }

        public override void OnUpdate()
        {
            if (_waiting && _done) Succeed();
        }

        private void Succeed()
        {
            Fsm.Event(finishedEvent);
            Finish();
        }

        private void Fail()
        {
            Fsm.Event(failedEvent);
            Finish();
        }
    }

    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  AdvanceDay — 跳到隔天                                             ║
    // ║  對應 Sequencer 指令：AdvanceDay(是否轉場)                          ║
    // ╚════════════════════════════════════════════════════════════════════╝
    [ActionCategory("Time")]
    [Tooltip("跳到隔天。true = 走 SkipToNextDay 觸發隔天演出 / false = 直接跳到隔天第一格。")]
    public class AdvanceDay : FsmStateAction
    {
        private const float TIMEOUT = 15f;

        [Tooltip("true = 隔天演出 / false = 直接跳日")]
        public FsmBool withTransition;

        [Tooltip("跳日完成時送出的事件（選填）")]
        public FsmEvent finishedEvent;

        [Tooltip("隔天演出等待超時時送出的事件（選填）")]
        public FsmEvent failedEvent;

        private TimeSystemModel _model;
        private Action _onDayPassed;
        private bool _waiting;
        private bool _dayPassed;
        private float _elapsed;

        public override void Reset()
        {
            withTransition = true;
            finishedEvent = null;
            failedEvent = null;
        }

        public override void OnEnter()
        {
            _waiting = false;
            _dayPassed = false;
            _elapsed = 0f;

            var service = GameStatusService.Instance;
            if (service == null)
            {
                Debug.LogError("[AdvanceDay] 找不到 GameStatusService！");
                Fsm.Event(failedEvent);
                Finish();
                return;
            }

            _model = service.Time;

            if (withTransition.Value)
            {
                _onDayPassed = () => _dayPassed = true;
                _model.OnDayPassed += _onDayPassed;
                _waiting = true;
                _model.SkipToNextDay();
            }
            else
            {
                int remainingSlots = _model.GetRemainingSlotsInDay();
                _model.AdvanceTime(remainingSlots + 1);
                Debug.Log("[AdvanceDay] 直接跳到隔天完成。");
                Fsm.Event(finishedEvent);
                Finish();
            }
        }

        public override void OnUpdate()
        {
            if (!_waiting) return;

            if (_dayPassed)
            {
                Debug.Log("[AdvanceDay] 隔天轉場演出完成。");
                Unsubscribe();
                Fsm.Event(finishedEvent);
                Finish();
                return;
            }

            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed >= TIMEOUT)
            {
                Debug.LogWarning($"[AdvanceDay] 隔天演出等待超時（{TIMEOUT}秒）！強制結束。");
                Unsubscribe();
                Fsm.Event(failedEvent);
                Finish();
            }
        }

        public override void OnExit()
        {
            // 狀態被外部事件強制離開時，確保不殘留訂閱
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (_model != null && _onDayPassed != null)
            {
                _model.OnDayPassed -= _onDayPassed;
                _onDayPassed = null;
            }
            _waiting = false;
        }
    }

    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  AdvanceToPhase — 到達下一個「指定階段」                           ║
    // ║  對應 Sequencer 指令：AdvanceToPhase(階段索引)                      ║
    // ╚════════════════════════════════════════════════════════════════════╝
    [ActionCategory("Time")]
    [Tooltip("跳到下一個指定 Phase 的第一格（允許跨日，不做隔天演出）。0=白天,1=黃昏,2=晚上,3=深夜。")]
    public class AdvanceToPhase : FsmStateAction
    {
        [RequiredField]
        [Tooltip("目標階段索引：0=白天, 1=黃昏, 2=晚上, 3=深夜")]
        public FsmInt targetPhase;

        [Tooltip("推進完成時送出的事件（選填）")]
        public FsmEvent finishedEvent;

        [Tooltip("索引無效或系統未就緒時送出的事件（選填）")]
        public FsmEvent failedEvent;

        public override void Reset()
        {
            targetPhase = 0;
            finishedEvent = null;
            failedEvent = null;
        }

        public override void OnEnter()
        {
            var service = GameStatusService.Instance;
            if (service == null)
            {
                Debug.LogError("[AdvanceToPhase] 找不到 GameStatusService！");
                Fsm.Event(failedEvent);
                Finish();
                return;
            }

            int phaseCount = service.TimeConfig.PhaseNames.Count;
            int target = targetPhase.Value;
            if (target < 0 || target >= phaseCount)
            {
                Debug.LogWarning($"[AdvanceToPhase] 無效的 Phase 索引 {target}（應為 0~{phaseCount - 1}）。");
                Fsm.Event(failedEvent);
                Finish();
                return;
            }

            service.Time.AdvanceToNextPhase(target);
            Debug.Log($"[AdvanceToPhase] 已推進到下一個 Phase {target}。");
            Fsm.Event(finishedEvent);
            Finish();
        }
    }

    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  AdvanceToWeekday — 到達下一個「指定星期幾」                       ║
    // ║  對應 Sequencer 指令：AdvanceToWeekday(星期)                        ║
    // ╚════════════════════════════════════════════════════════════════════╝
    [ActionCategory("Time")]
    [Tooltip("跳到下一個指定星期幾（今日不算，落在該日第一階段第一格，不做隔天演出）。")]
    public class AdvanceToWeekday : FsmStateAction
    {
        [RequiredField]
        [ObjectType(typeof(DayOfWeek))]
        [Tooltip("目標星期幾")]
        public FsmEnum targetDay;

        [Tooltip("推進完成時送出的事件（選填）")]
        public FsmEvent finishedEvent;

        [Tooltip("系統未就緒時送出的事件（選填）")]
        public FsmEvent failedEvent;

        public override void Reset()
        {
            targetDay = DayOfWeek.Monday;
            finishedEvent = null;
            failedEvent = null;
        }

        public override void OnEnter()
        {
            var service = GameStatusService.Instance;
            if (service == null)
            {
                Debug.LogError("[AdvanceToWeekday] 找不到 GameStatusService！");
                Fsm.Event(failedEvent);
                Finish();
                return;
            }

            var target = (DayOfWeek)targetDay.Value;
            service.Time.AdvanceToNextDayOfWeek(target);
            Debug.Log($"[AdvanceToWeekday] 已推進到下一個 {target}。");
            Fsm.Event(finishedEvent);
            Finish();
        }
    }

    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  AdvanceToWeekend — 到達下一個「週末」                             ║
    // ║  對應 Sequencer 指令：AdvanceToWeekend()                            ║
    // ╚════════════════════════════════════════════════════════════════════╝
    [ActionCategory("Time")]
    [Tooltip("跳到下一個週末（以星期六為起點，今日不算，落在該日第一階段第一格，不做隔天演出）。")]
    public class AdvanceToWeekend : FsmStateAction
    {
        [Tooltip("推進完成時送出的事件（選填）")]
        public FsmEvent finishedEvent;

        [Tooltip("系統未就緒時送出的事件（選填）")]
        public FsmEvent failedEvent;

        public override void Reset()
        {
            finishedEvent = null;
            failedEvent = null;
        }

        public override void OnEnter()
        {
            var service = GameStatusService.Instance;
            if (service == null)
            {
                Debug.LogError("[AdvanceToWeekend] 找不到 GameStatusService！");
                Fsm.Event(failedEvent);
                Finish();
                return;
            }

            service.Time.AdvanceToNextWeekend();
            Debug.Log("[AdvanceToWeekend] 已推進到下一個週末（星期六）。");
            Fsm.Event(finishedEvent);
            Finish();
        }
    }
}
