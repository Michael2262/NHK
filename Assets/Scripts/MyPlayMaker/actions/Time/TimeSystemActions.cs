using System;
using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    [ActionCategory("Time")]
    [Tooltip("比較今天的遊戲天數（Day X）與指定天數，送出 true/false 事件。不比較日曆日期，也不推進時間。")]
    public class CheckDate : FsmStateAction
    {
        public enum CompareType
        {
            Equal,
            NotEqual,
            GreaterThan,
            GreaterOrEqual,
            LessThan,
            LessOrEqual
        }

        [RequiredField]
        [Tooltip("要比較的遊戲天數，例如 3 表示 Day 3。")]
        public FsmInt targetDay;

        [Tooltip("比較方式：目前 Day 與目標 Day 比較，預設為相等。")]
        public CompareType compareType = CompareType.Equal;

        [Tooltip("條件成立時發送。")]
        public FsmEvent trueEvent;

        [Tooltip("條件不成立、時間系統未就緒或未指定目標天數時發送。")]
        public FsmEvent falseEvent;

        [UIHint(UIHint.Variable)]
        [Tooltip("儲存目前遊戲天數（選填）；時間系統未就緒時寫入 0。")]
        public FsmInt storeCurrentDay;

        [UIHint(UIHint.Variable)]
        [Tooltip("儲存比較結果（選填）。")]
        public FsmBool storeResult;

        [Tooltip("每幀重新判斷；關閉時只在進入 State 時判斷一次。")]
        public bool everyFrame;

        public override void Reset()
        {
            targetDay = 1;
            compareType = CompareType.Equal;
            trueEvent = null;
            falseEvent = null;
            storeCurrentDay = new FsmInt { UseVariable = true };
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
            if (storeCurrentDay != null && !storeCurrentDay.IsNone)
                storeCurrentDay.Value = model != null ? model.DayIndex : 0;

            bool result = false;
            if (model == null || targetDay == null || targetDay.IsNone)
            {
                Debug.LogWarning("[CheckDate] 時間系統尚未初始化或未指定目標天數。");
            }
            else
            {
                int currentDay = model.DayIndex;
                int target = targetDay.Value;
                result = compareType switch
                {
                    CompareType.Equal => currentDay == target,
                    CompareType.NotEqual => currentDay != target,
                    CompareType.GreaterThan => currentDay > target,
                    CompareType.GreaterOrEqual => currentDay >= target,
                    CompareType.LessThan => currentDay < target,
                    CompareType.LessOrEqual => currentDay <= target,
                    _ => false
                };
            }

            if (storeResult != null && !storeResult.IsNone)
                storeResult.Value = result;

            Fsm.Event(result ? trueEvent : falseEvent);
        }
    }

    /// <summary>共用等待與回呼處理，離開 State 後忽略舊回呼；全域播放會持續到結束或明確取消。</summary>
    public abstract class TimePlaybackActionBase : FsmStateAction
    {
        [Tooltip("整段播放完成後送出。")]
        public FsmEvent finishedEvent;
        [Tooltip("播放被拒絕或取消時送出。")]
        public FsmEvent failedEvent;
        private int generation;
        private bool done;
        private bool succeeded;

        public override void Reset()
        {
            finishedEvent = null;
            failedEvent = null;
        }

        protected void Play(int minis, bool shortTime = false, bool withinDay = false,
            Func<TimePlaybackModel, Action<bool>, bool> preset = null)
        {
            int version = ++generation;
            done = false;
            succeeded = false;
            Action<bool> callback = result =>
            {
                if (generation != version) return;
                succeeded = result;
                done = true;
            };
            var playback = GameStatusService.Instance?.TimePlayback;
            bool accepted = playback != null && (preset != null
                ? preset(playback, callback)
                : shortTime
                ? playback.PlayAdvanceShortTime(callback)
                : playback.PlayAdvanceMinis(minis, callback, withinDay));
            if (!accepted) done = true;
            CompleteIfReady();
        }

        public override void OnUpdate() => CompleteIfReady();
        public override void OnExit() => generation++;

        private void CompleteIfReady()
        {
            if (!done) return;
            done = false;
            Fsm.Event(succeeded ? finishedEvent : failedEvent);
            Finish();
        }
    }

    [ActionCategory("Time")]
    [Tooltip("逐格播放指定 Mini，等待播放完成。每格秒數由 TimeConfig 決定，跨日後停在隔天起點。")]
    public class PlayAdvanceMinis : TimePlaybackActionBase
    {
        [RequiredField] public FsmInt minis;
        public override void Reset() { base.Reset(); minis = 1; }
        public override void OnEnter() => Play(minis == null || minis.IsNone ? -1 : minis.Value);
    }

    [ActionCategory("Time")]
    [Tooltip("逐格播放指定 Mini，等待播放完成；當日時間不足時拒絕整次播放。")]
    public class PlayAdvanceMinisWithinDay : TimePlaybackActionBase
    {
        [RequiredField] public FsmInt minis;
        public override void Reset() { base.Reset(); minis = 1; }
        public override void OnEnter() => Play(minis == null || minis.IsNone ? -1 : minis.Value, withinDay: true);
    }

    [ActionCategory("Time")]
    [Tooltip("逐格播放一小段時間並等待完成；Mini 數量與每格秒數都由 TimeConfig 決定。")]
    public class PlayAdvanceShortTime : TimePlaybackActionBase
    {
        public override void OnEnter() => Play(0, shortTime: true);
    }

    [ActionCategory("Time")]
    [Tooltip("逐格播放一段中等時間並等待完成；Mini 數量與每格秒數都由 TimeConfig 決定。")]
    public class PlayAdvanceMediumTime : TimePlaybackActionBase
    {
        public override void OnEnter() => Play(0, preset: (playback, done) => playback.PlayAdvanceMediumTime(done));
    }

    [ActionCategory("Time")]
    [Tooltip("逐格播放一大段時間並等待完成；Mini 數量與每格秒數都由 TimeConfig 決定。")]
    public class PlayAdvanceLongTime : TimePlaybackActionBase
    {
        public override void OnEnter() => Play(0, preset: (playback, done) => playback.PlayAdvanceLongTime(done));
    }

    [ActionCategory("Time")]
    [Tooltip("直接推進指定 Mini。跨日後停在隔天起點，捨棄剩餘數量，不播放轉場。")]
    public class AdvanceMinis : FsmStateAction
    {
        [RequiredField]
        [Tooltip("推進數量，必須為非負整數；0 表示不推進。")]
        public FsmInt minis;
        public FsmEvent finishedEvent;
        public FsmEvent failedEvent;

        public override void Reset()
        {
            minis = 1;
            finishedEvent = null;
            failedEvent = null;
        }

        public override void OnEnter()
        {
            var model = GameStatusService.Instance?.Time;
            if (model == null || minis == null || minis.IsNone || minis.Value < 0)
            {
                Debug.LogWarning("[AdvanceMinis] 時間系統未就緒或 Mini 數量無效。");
                Fsm.Event(failedEvent);
                Finish();
                return;
            }
            model.AdvanceMinis(minis.Value);
            Fsm.Event(finishedEvent);
            Finish();
        }
    }

    [ActionCategory("Time")]
    [Tooltip("直接推進指定 Mini，但不允許跨日。當日不足時不推進，送出 failedEvent。")]
    public class AdvanceMinisWithinDay : FsmStateAction
    {
        [RequiredField]
        public FsmInt minis;
        public FsmEvent finishedEvent;
        public FsmEvent failedEvent;

        public override void Reset()
        {
            minis = 1;
            finishedEvent = null;
            failedEvent = null;
        }

        public override void OnEnter()
        {
            var model = GameStatusService.Instance?.Time;
            bool succeeded = model != null && minis != null && !minis.IsNone && model.TryAdvanceMinis(minis.Value);
            if (!succeeded)
                Debug.LogWarning("[AdvanceMinisWithinDay] 時間系統未就緒、數量無效或今日剩餘時間不足。");
            Fsm.Event(succeeded ? finishedEvent : failedEvent);
            Finish();
        }
    }

    [ActionCategory("Time")]
    [Tooltip("推進一小段時間。Mini 數量統一由 TimeConfig 設定（預設 6）；跨日後停在隔天起點，不播放轉場。")]
    public class AdvanceShortTime : FsmStateAction
    {
        public FsmEvent finishedEvent;
        public FsmEvent failedEvent;

        public override void Reset()
        {
            finishedEvent = null;
            failedEvent = null;
        }

        public override void OnEnter()
        {
            var model = GameStatusService.Instance?.Time;
            if (model == null)
            {
                Debug.LogWarning("[AdvanceShortTime] 時間系統尚未初始化。");
                Fsm.Event(failedEvent);
                Finish();
                return;
            }
            model.AdvanceShortTime();
            Fsm.Event(finishedEvent);
            Finish();
        }
    }

    [ActionCategory("Time")]
    [Tooltip("推進中段時間。Mini 數量統一由 TimeConfig 設定（預設 12）；跨日後停在隔天起點，不播放轉場。")]
    public class AdvanceMediumTime : FsmStateAction
    {
        public FsmEvent finishedEvent;
        public FsmEvent failedEvent;

        public override void Reset()
        {
            finishedEvent = null;
            failedEvent = null;
        }

        public override void OnEnter()
        {
            var model = GameStatusService.Instance?.Time;
            if (model == null)
            {
                Debug.LogWarning("[AdvanceMediumTime] 時間系統尚未初始化。");
                Fsm.Event(failedEvent);
                Finish();
                return;
            }
            model.AdvanceMediumTime();
            Fsm.Event(finishedEvent);
            Finish();
        }
    }

    [ActionCategory("Time")]
    [Tooltip("推進長段時間。Mini 數量統一由 TimeConfig 設定（預設 24）；跨日後停在隔天起點，不播放轉場。")]
    public class AdvanceLongTime : FsmStateAction
    {
        public FsmEvent finishedEvent;
        public FsmEvent failedEvent;

        public override void Reset()
        {
            finishedEvent = null;
            failedEvent = null;
        }

        public override void OnEnter()
        {
            var model = GameStatusService.Instance?.Time;
            if (model == null)
            {
                Debug.LogWarning("[AdvanceLongTime] 時間系統尚未初始化。");
                Fsm.Event(failedEvent);
                Finish();
                return;
            }
            model.AdvanceLongTime();
            Fsm.Event(finishedEvent);
            Finish();
        }
    }

    [ActionCategory("Time")]
    [Tooltip("取得目前 Slot 的 Mini 進度、總量與今日剩餘量。進度從 0 開始；不推進時間。")]
    public class GetMiniProgress : FsmStateAction
    {
        [UIHint(UIHint.Variable)] public FsmInt currentMini;
        [UIHint(UIHint.Variable)] public FsmInt minisInSlot;
        [UIHint(UIHint.Variable)] public FsmInt remainingMinisInDay;
        public FsmEvent finishedEvent;
        public FsmEvent failedEvent;

        public override void Reset()
        {
            currentMini = new FsmInt { UseVariable = true };
            minisInSlot = new FsmInt { UseVariable = true };
            remainingMinisInDay = new FsmInt { UseVariable = true };
            finishedEvent = null;
            failedEvent = null;
        }

        public override void OnEnter()
        {
            var model = GameStatusService.Instance?.Time;
            if (model == null)
            {
                Debug.LogWarning("[GetMiniProgress] 時間系統尚未初始化。");
                Fsm.Event(failedEvent);
                Finish();
                return;
            }
            if (currentMini != null && !currentMini.IsNone) currentMini.Value = model.CurrentMiniInSlot;
            if (minisInSlot != null && !minisInSlot.IsNone) minisInSlot.Value = model.MinisInCurrentSlot;
            if (remainingMinisInDay != null && !remainingMinisInDay.IsNone) remainingMinisInDay.Value = model.GetRemainingMinisInDay();
            Fsm.Event(finishedEvent);
            Finish();
        }
    }

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
