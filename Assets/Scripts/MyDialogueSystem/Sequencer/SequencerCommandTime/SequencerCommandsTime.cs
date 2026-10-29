using UnityEngine;
using PixelCrushers.DialogueSystem;
using System;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>auto（預設，亦接受 true）完成後前進；wait 完成後等點擊；false 不加 Delay 與繼續鈕鎖。</summary>
    public abstract class SequencerCommandTimePlaybackBase : SequencerCommand
    {
        protected enum PlaybackMode { Auto, Wait, Legacy }

        private bool heldContinueButtonLock;
        private ConversationView playbackView;
        private Subtitle playbackSubtitle;
        private bool waitingForContinue;
        private bool continueButtonRequested;
        private NhkAutoDialogueBridge pausedAutoDialogue;
        private int pausedAutoGeneration;
        private bool advanceOnNextUpdate;

        protected PlaybackMode GetPlaybackMode(int parameterIndex)
        {
            string value = GetParameter(parameterIndex, "auto").Trim().ToLowerInvariant();
            switch (value)
            {
                case "":
                case "auto":
                case "true":
                    return PlaybackMode.Auto;
                case "wait":
                    return PlaybackMode.Wait;
                case "false":
                    return PlaybackMode.Legacy;
                default:
                    Debug.LogWarning($"[時間流逝] 未知模式「{value}」，請使用 auto、true、wait 或 false；本次使用 auto。");
                    return PlaybackMode.Auto;
            }
        }

        protected void Play(int minis, bool shortTime = false, bool withinDay = false,
            Func<TimePlaybackModel, Action<bool>, bool> preset = null, PlaybackMode mode = PlaybackMode.Auto)
        {
            bool withDelay = mode != PlaybackMode.Legacy;
            // 只控制這個指令所屬的對話句；獨立播放的 Sequence 不取得對話控制權。
            if (withDelay)
            {
                var view = DialogueManager.conversationView;
                if (view != null && view.sequencer == sequencer)
                {
                    playbackView = view;
                    playbackSubtitle = DialogueManager.currentConversationState?.subtitle;
                }
            }
            var service = GameStatusService.Instance;
            var playback = service?.TimePlayback;
            // 播放開始事件可能改動設定，先保存與本次演出相同的秒數及跨日上限。
            float miniSeconds = withDelay ? (service?.TimeConfig?.MiniPlaybackSeconds ?? 0f) : 0f;
            long minisUntilNextDay = withDelay ? (long)(service?.Time?.GetRemainingMinisInDay() ?? -1) + 1L : 0L;
            Action<bool> done = completed =>
            {
                if (this == null || !isPlaying) return;
                if (completed && mode == PlaybackMode.Auto && IsPlaybackLineCurrent())
                {
                    // 零格可能在 Awake 內同步完成，等到 Update 再前進，避免重入尚未解析完的 Sequence。
                    advanceOnNextUpdate = true;
                    return;
                }
                if (completed && mode == PlaybackMode.Wait && IsPlaybackLineCurrent() &&
                    playbackView.dialogueUI is AbstractDialogueUI)
                {
                    waitingForContinue = true;
                    // 這一句明確要求玩家確認。暫停 Auto，離開當句時再恢復。
                    var auto = NhkAutoDialogueBridge.Find();
                    if (auto != null && auto.IsActive)
                    {
                        auto.DisableAutoDialogue();
                        pausedAutoDialogue = auto;
                        pausedAutoGeneration = auto.Generation;
                    }
                    ReleaseContinueButtonLock();
                    ShowContinueButtonWhenUnlocked();
                    // 保留正在執行的命令，即使 Continue Mode 是 Optional 也不會自動跳句。
                    // 玩家點擊會走原生 FinishSubtitle / Sequencer.Stop，再清除此命令。
                    return;
                }
                ReleaseContinueButtonLock();
                if (IsPlaybackLineCurrent()) playbackView.SetupContinueButton();
                Stop();
            };
            bool accepted = playback != null && (preset != null
                ? preset(playback, done)
                : shortTime
                ? playback.PlayAdvanceShortTime(done)
                : playback.PlayAdvanceMinis(minis, done, withinDay));
            if (!accepted)
            {
                Debug.LogWarning("[時間流逝] 無法開始：系統未就緒、已有播放、參數無效或今日時間不足。");
                Stop();
                return;
            }

            // 只有成功且仍在播放時才加鎖及 Delay；零格直接依模式處理完成，取消則結束指令。
            if (!withDelay || this == null || !isPlaying || !playback.IsTimeFlowing || sequencer == null) return;
            // 與 ContinueButtonLock(on) 使用相同 API；即使每格秒數為 0，仍鎖到逐幀播放結束。
            heldContinueButtonLock = true;
            NhkUISubtitlePanel.PushContinueButtonLock();
            long playedMinis = playback.StopsAtNextDay
                ? Math.Min((long)playback.RemainingMinis, minisUntilNextDay)
                : playback.RemainingMinis;
            float seconds = playedMinis * miniSeconds;
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0f) return;

            // 與演出同時開始，不在演出結束後再等一次；實際逐幀播放仍由完成回呼保底等待。
            // 使用原生 Delay 的時間模式，也保留它與同句其他等待命令並行的行為。
            sequencer.PlayCommand("Delay", false, 0f, null, null,
                seconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        private bool IsPlaybackLineCurrent()
        {
            return playbackView != null && playbackSubtitle != null &&
                DialogueManager.isConversationActive && DialogueManager.conversationView == playbackView &&
                ReferenceEquals(DialogueManager.currentConversationState?.subtitle, playbackSubtitle);
        }

        private void ShowContinueButtonWhenUnlocked()
        {
            if (NhkUISubtitlePanel.IsContinueButtonLocked)
            {
                continueButtonRequested = false;
                return;
            }
            if (continueButtonRequested) return;
            continueButtonRequested = true;
            // 明確刷新當句按鈕，不更改整段對話的 Continue Mode。
            if (playbackView.dialogueUI is AbstractDialogueUI ui)
                ui.ShowContinueButton(playbackSubtitle);
        }

        protected void Update()
        {
            if (advanceOnNextUpdate && isPlaying)
            {
                advanceOnNextUpdate = false;
                ReleaseContinueButtonLock();
                Stop();
                // 直接完成當句，不受原本 Always / Optional 模式影響，也不修改後續句子的設定。
                // 再次核對句子，避免舊演出完成後誤跳過新的節點。
                if (IsPlaybackLineCurrent()) playbackView.HandleContinueButtonClick();
                return;
            }
            if (!waitingForContinue || !isPlaying) return;
            if (!IsPlaybackLineCurrent())
            {
                waitingForContinue = false;
                RestoreAutoDialogue();
                Stop();
                return;
            }
            // 等待期間若玩家主動重新開啟 Auto，就尊重新的操作，交還自動播放。
            if (NhkAutoDialogueBridge.Find()?.IsActive == true)
            {
                waitingForContinue = false;
                pausedAutoDialogue = null;
                Stop();
                return;
            }
            // 同句若另有演出鎖，等最後一層解除才重顯，避免按鈕再次消失後無人刷新。
            ShowContinueButtonWhenUnlocked();
        }

        protected void OnConversationLineEnd(Subtitle subtitle)
        {
            if (!ReferenceEquals(subtitle, playbackSubtitle)) return;
            advanceOnNextUpdate = false;
            waitingForContinue = false;
            ReleaseContinueButtonLock();
            RestoreAutoDialogue();
            Stop();
        }

        private void RestoreAutoDialogue()
        {
            var auto = pausedAutoDialogue;
            pausedAutoDialogue = null;
            // 下一句開始前恢復；對話關閉或模式已被其他操作變更時不重新開啟。
            if (auto != null && !auto.IsActive && auto.Generation == pausedAutoGeneration &&
                DialogueManager.isConversationActive && DialogueManager.conversationView == playbackView)
                auto.EnableAutoDialogue();
        }

        private void ReleaseContinueButtonLock()
        {
            if (!heldContinueButtonLock) return;
            heldContinueButtonLock = false;
            // 等同 ContinueButtonLock(off)，只解除本指令的一層鎖，不影響其他演出。
            NhkUISubtitlePanel.PopContinueButtonLock();
        }

        protected void OnDestroy()
        {
            // 對話被強制結束或切場景時，完成回呼可能尚未執行，先解除本次鎖定。
            ReleaseContinueButtonLock();
            RestoreAutoDialogue();
        }
    }

    /// <summary>PlayAdvanceMinis([數量=1], [模式=auto])，模式接受 auto / true / wait / false，跨日後停在隔天起點。</summary>
    public class SequencerCommandPlayAdvanceMinis : SequencerCommandTimePlaybackBase
    {
        public void Awake() => Play(GetParameterAsInt(0, 1), mode: GetPlaybackMode(1));
    }

    /// <summary>PlayAdvanceMinisWithinDay([數量=1], [模式=auto])，當日不足時不播放。</summary>
    public class SequencerCommandPlayAdvanceMinisWithinDay : SequencerCommandTimePlaybackBase
    {
        public void Awake() => Play(GetParameterAsInt(0, 1), withinDay: true, mode: GetPlaybackMode(1));
    }

    /// <summary>PlayAdvanceShortTime([模式=auto])，使用 Config 的短時間 Mini 數量。</summary>
    public class SequencerCommandPlayAdvanceShortTime : SequencerCommandTimePlaybackBase
    {
        public void Awake() => Play(0, shortTime: true, mode: GetPlaybackMode(0));
    }

    /// <summary>PlayAdvanceMediumTime([模式=auto])，使用 Config 的中段時間 Mini 數量。</summary>
    public class SequencerCommandPlayAdvanceMediumTime : SequencerCommandTimePlaybackBase
    {
        public void Awake() => Play(0, preset: (playback, done) => playback.PlayAdvanceMediumTime(done),
            mode: GetPlaybackMode(0));
    }

    /// <summary>PlayAdvanceLongTime([模式=auto])，使用 Config 的長段時間 Mini 數量。</summary>
    public class SequencerCommandPlayAdvanceLongTime : SequencerCommandTimePlaybackBase
    {
        public void Awake() => Play(0, preset: (playback, done) => playback.PlayAdvanceLongTime(done),
            mode: GetPlaybackMode(0));
    }

    /// <summary>PlaySlot([數量=1], [模式=auto])：逐 Mini 播到往後第 N 個 Slot 起點，允許跨日、不轉場。</summary>
    public class SequencerCommandPlaySlot : SequencerCommandTimePlaybackBase
    {
        public void Awake() => Play(0,
            preset: (playback, done) => playback.PlaySlot(GetParameterAsInt(0, 1), done),
            mode: GetPlaybackMode(1));
    }

    /// <summary>PlayPhase([數量=1], [模式=auto])：逐 Mini 播到往後第 N 個 Phase 起點，允許跨日、不轉場。</summary>
    public class SequencerCommandPlayPhase : SequencerCommandTimePlaybackBase
    {
        public void Awake() => Play(0,
            preset: (playback, done) => playback.PlayPhase(GetParameterAsInt(0, 1), done),
            mode: GetPlaybackMode(1));
    }

    /// <summary>PlayDay([數量=1], [模式=auto])：逐 Mini 播到 N 天後的起點，不轉場。</summary>
    public class SequencerCommandPlayDay : SequencerCommandTimePlaybackBase
    {
        public void Awake() => Play(0,
            preset: (playback, done) => playback.PlayDay(GetParameterAsInt(0, 1), done),
            mode: GetPlaybackMode(1));
    }

    /// <summary>AdvanceMinis([數量=1])：直接推進 Mini，跨日後停在隔天起點。</summary>
    public class SequencerCommandAdvanceMinis : SequencerCommand
    {
        public void Awake()
        {
            int minis = GetParameterAsInt(0, 1);
            var model = GameStatusService.Instance?.Time;
            if (model == null)
                Debug.LogError("[AdvanceMinis] 時間系統尚未初始化。");
            else if (minis < 0)
                Debug.LogWarning("[AdvanceMinis] Mini 數量不可為負數。");
            else
                model.AdvanceMinis(minis);
            Stop();
        }
    }

    /// <summary>AdvanceMinisWithinDay([數量=1])：當日不足時不推進，並記錄警告。</summary>
    public class SequencerCommandAdvanceMinisWithinDay : SequencerCommand
    {
        public void Awake()
        {
            int minis = GetParameterAsInt(0, 1);
            var model = GameStatusService.Instance?.Time;
            if (model == null)
                Debug.LogError("[AdvanceMinisWithinDay] 時間系統尚未初始化。");
            else if (!model.TryAdvanceMinis(minis))
                Debug.LogWarning($"[AdvanceMinisWithinDay] 無法推進 {minis} Mini：數量無效或今日剩餘時間不足。");
            Stop();
        }
    }

    /// <summary>AdvanceShortTime()：數量由 TimeConfig 統一設定，跨日後停在隔天起點。</summary>
    public class SequencerCommandAdvanceShortTime : SequencerCommand
    {
        public void Awake()
        {
            var model = GameStatusService.Instance?.Time;
            if (model == null)
                Debug.LogError("[AdvanceShortTime] 時間系統尚未初始化。");
            else
                model.AdvanceShortTime();
            Stop();
        }
    }

    /// <summary>AdvanceMediumTime()：數量由 TimeConfig 統一設定，跨日後停在隔天起點。</summary>
    public class SequencerCommandAdvanceMediumTime : SequencerCommand
    {
        public void Awake()
        {
            var model = GameStatusService.Instance?.Time;
            if (model == null)
                Debug.LogError("[AdvanceMediumTime] 時間系統尚未初始化。");
            else
                model.AdvanceMediumTime();
            Stop();
        }
    }

    /// <summary>AdvanceLongTime()：數量由 TimeConfig 統一設定，跨日後停在隔天起點。</summary>
    public class SequencerCommandAdvanceLongTime : SequencerCommand
    {
        public void Awake()
        {
            var model = GameStatusService.Instance?.Time;
            if (model == null)
                Debug.LogError("[AdvanceLongTime] 時間系統尚未初始化。");
            else
                model.AdvanceLongTime();
            Stop();
        }
    }

    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  1. AdvanceSlot — 推進 N 格時段                                   ║
    // ╠════════════════════════════════════════════════════════════════════╣
    // ║  語法：AdvanceSlot([格數], [是否轉場])                             ║
    // ║                                                                    ║
    // ║  參數：                                                            ║
    // ║  - 格數       (選填, 預設 1)     要推進的時段格數                   ║
    // ║  - 是否轉場   (選填, 預設 false) true=輕量轉場 / false=直接切       ║
    // ║                                                                    ║
    // ║  範例：                                                            ║
    // ║  AdvanceSlot()          → 推進 1 格，不轉場                        ║
    // ║  AdvanceSlot(2)         → 推進 2 格，不轉場                        ║
    // ║  AdvanceSlot(1, true)   → 推進 1 格，帶輕量轉場                    ║
    // ║  AdvanceSlot(3, true)   → 推進 3 格，帶輕量轉場                    ║
    // ╚════════════════════════════════════════════════════════════════════╝
    public class SequencerCommandAdvanceSlot : SequencerCommand
    {
        public void Awake()
        {
            int slots = GetParameterAsInt(0, 1);
            bool withTransition = GetParameterAsBool(1, false);

            if (slots <= 0) { Stop(); return; }

            var service = GameStatusService.Instance;
            if (service == null)
            {
                Debug.LogError("[AdvanceSlot] 找不到 GameStatusService！");
                Stop();
                return;
            }

            var model = service.Time;

            if (!model.CanAdvanceWithinDay(slots))
            {
                Debug.LogWarning($"[AdvanceSlot] 剩餘時段不足，無法推進 {slots} 格。");
                Stop();
                return;
            }

            if (withTransition)
            {
                SceneController.PerformSlotTransition(
                    onMidTransition: () =>
                    {
                        model.AdvanceTime(slots);
                        Debug.Log($"[AdvanceSlot] 輕量轉場推進 {slots} 格完成。");
                    },
                    onComplete: () => Stop()
                );
            }
            else
            {
                model.AdvanceTime(slots);
                Debug.Log($"[AdvanceSlot] 直接推進 {slots} 格完成。");
                Stop();
            }
        }
    }

    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  2. AdvancePhase — 推進到下一個時段階段                            ║
    // ╠════════════════════════════════════════════════════════════════════╣
    // ║  語法：AdvancePhase([是否轉場])                                    ║
    // ║                                                                    ║
    // ║  參數：                                                            ║
    // ║  - 是否轉場   (選填, 預設 false) true=輕量轉場 / false=直接切       ║
    // ║                                                                    ║
    // ║  行為：                                                            ║
    // ║  自動計算從目前 Slot 到下一個 Phase 開頭需要多少格，一次推進。       ║
    // ║  若已在最後 Phase，不會跨日，會印出警告。                           ║
    // ║                                                                    ║
    // ║  範例：                                                            ║
    // ║  AdvancePhase()        → 跳到下一階段，不轉場                      ║
    // ║  AdvancePhase(true)    → 跳到下一階段，帶輕量轉場                  ║
    // ╚════════════════════════════════════════════════════════════════════╝
    public class SequencerCommandAdvancePhase : SequencerCommand
    {
        public void Awake()
        {
            bool withTransition = GetParameterAsBool(0, false);

            var service = GameStatusService.Instance;
            if (service == null)
            {
                Debug.LogError("[AdvancePhase] 找不到 GameStatusService！");
                Stop();
                return;
            }

            var model = service.Time;
            var config = service.TimeConfig;

            int currentPhase = model.CurrentPhaseIndex;
            int totalPhases = config.PhaseNames.Count;

            if (currentPhase >= totalPhases - 1)
            {
                Debug.LogWarning("[AdvancePhase] 已在最後階段，無法推進（不允許自動跨日）。");
                Stop();
                return;
            }

            int remainingInPhase = config.GetTotalSlots(currentPhase) - model.CurrentSlotInPhase;
            int slotsNeeded = remainingInPhase + 1;

            if (!model.CanAdvanceWithinDay(slotsNeeded))
            {
                Debug.LogWarning("[AdvancePhase] 時段不足以推進到下一階段。");
                Stop();
                return;
            }

            if (withTransition)
            {
                SceneController.PerformSlotTransition(
                    onMidTransition: () =>
                    {
                        model.AdvanceTime(slotsNeeded);
                        Debug.Log($"[AdvancePhase] 輕量轉場推進 {slotsNeeded} 格，進入 Phase {currentPhase + 1}。");
                    },
                    onComplete: () => Stop()
                );
            }
            else
            {
                model.AdvanceTime(slotsNeeded);
                Debug.Log($"[AdvancePhase] 直接推進 {slotsNeeded} 格，進入 Phase {currentPhase + 1}。");
                Stop();
            }
        }
    }

    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  3. AdvanceDay — 跳到隔天                                         ║
    // ╠════════════════════════════════════════════════════════════════════╣
    // ║  語法：AdvanceDay([是否轉場])                                      ║
    // ║                                                                    ║
    // ║  參數：                                                            ║
    // ║  - 是否轉場   (選填, 預設 true) true=隔天演出 / false=直接跳日      ║
    // ║                                                                    ║
    // ║  行為：                                                            ║
    // ║  - true  → 呼叫 SkipToNextDay()，觸發 OnDaySkipRequested，         ║
    // ║            SceneController 執行 DayTransitionUI 或簡易轉場。       ║
    // ║  - false → 直接跳到隔天第一格，不做任何視覺效果。                   ║
    // ║                                                                    ║
    // ║  範例：                                                            ║
    // ║  AdvanceDay()          → 帶隔天轉場演出                            ║
    // ║  AdvanceDay(true)      → 帶隔天轉場演出                            ║
    // ║  AdvanceDay(false)     → 直接跳日，不做轉場                        ║
    // ╚════════════════════════════════════════════════════════════════════╝
    public class SequencerCommandAdvanceDay : SequencerCommand
    {
        public void Awake()
        {
            bool withTransition = GetParameterAsBool(0, true);

            var service = GameStatusService.Instance;
            if (service == null)
            {
                Debug.LogError("[AdvanceDay] 找不到 GameStatusService！");
                Stop();
                return;
            }

            var model = service.Time;

            if (withTransition)
            {
                StartCoroutine(WaitForDaySkipRoutine(model));
            }
            else
            {
                int remainingSlots = model.GetRemainingSlotsInDay();
                model.AdvanceTime(remainingSlots + 1);
                Debug.Log("[AdvanceDay] 直接跳到隔天完成。");
                Stop();
            }
        }

        private System.Collections.IEnumerator WaitForDaySkipRoutine(TimeSystemModel model)
        {
            bool dayPassed = false;

            Action onDayPassed = null;
            onDayPassed = () =>
            {
                dayPassed = true;
                model.OnDayPassed -= onDayPassed;
            };
            model.OnDayPassed += onDayPassed;

            model.SkipToNextDay();

            float timeout = 15f;
            float elapsed = 0f;
            while (!dayPassed && elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!dayPassed)
            {
                model.OnDayPassed -= onDayPassed;
                Debug.LogWarning("[AdvanceDay] 隔天演出等待超時（15秒）！強制結束指令。");
            }
            else
            {
                Debug.Log("[AdvanceDay] 隔天轉場演出完成。");
            }

            Stop();
        }
    }

    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  4. AdvanceTimeAndChangeScene — 轉場 + 切時間 + 切地點            ║
    // ╠════════════════════════════════════════════════════════════════════╣
    // ║  語法：AdvanceTimeAndChangeScene(場景名, 推進類型, [量],            ║
    // ║                                 [轉場色Phase], [更新Scenario])     ║
    // ║                                                                    ║
    // ║  參數：                                                            ║
    // ║  - 場景名稱     (必填)  目標場景名稱                               ║
    // ║  - 推進類型     (必填)  "slot" / "phase" / "day"                   ║
    // ║  - 推進量       (選填, 預設 1) 僅 slot 模式有效                     ║
    // ║  - 轉場色Phase  (選填, 預設 -1)                                    ║
    // ║      -2 = 直接轉場（無淡入淡出）                                   ║
    // ║      -1 = 依照當前 Phase 自動決定                                  ║
    // ║       0 = 白天, 1 = 黃昏, 2 = 晚上, 3 = 深夜                      ║
    // ║  - 更新Scenario (選填, 預設 true)                                  ║
    // ║      true = 呼叫 ScenarioManager.ChangeLocation()                 ║
    // ║                                                                    ║
    // ║  【v3 改動】                                                       ║
    // ║  所有模式現在統一透過 SceneController 的 API 執行場景切換，         ║
    // ║  不再自行管理場景載入/卸載。確保 ReadyHandlers 一定會被執行。       ║
    // ║                                                                    ║
    // ║  範例：                                                            ║
    // ║  AdvanceTimeAndChangeScene(Stage2, slot, 2)                        ║
    // ║    → 推進 2 格 + 轉場到 Stage2                                     ║
    // ║                                                                    ║
    // ║  AdvanceTimeAndChangeScene(Stage2, phase)                          ║
    // ║    → 跳到下一階段 + 轉場到 Stage2                                  ║
    // ║                                                                    ║
    // ║  AdvanceTimeAndChangeScene(Stage2, day)                            ║
    // ║    → 跳到隔天 + 隔天演出 + 切到 Stage2                             ║
    // ║                                                                    ║
    // ║  AdvanceTimeAndChangeScene(Stage2, slot, 1, -2)                    ║
    // ║    → 推進 1 格 + 直接切場景（無淡入淡出）                          ║
    // ║                                                                    ║
    // ║  AdvanceTimeAndChangeScene(Stage2, slot, 1, 2, false)              ║
    // ║    → 推進 1 格 + 夜間轉場到 Stage2，不更新邏輯地點                 ║
    // ╚════════════════════════════════════════════════════════════════════╝
    public class SequencerCommandAdvanceTimeAndChangeScene : SequencerCommand
    {
        private const int IMMEDIATE_TRANSITION = -2;

        public void Awake()
        {
            // ─── 參數解析 ───
            string targetScene = GetParameter(0);
            string advanceType = GetParameter(1, "slot").ToLower();
            int slotAmount = GetParameterAsInt(2, 1);
            int fadePhaseIndex = GetParameterAsInt(3, -1);
            bool updateScenario = GetParameterAsBool(4, true);

            // ─── 安全檢查 ───
            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogWarning("[AdvanceTimeAndChangeScene] 場景名稱為空！");
                Stop();
                return;
            }

            var service = GameStatusService.Instance;
            if (service == null)
            {
                Debug.LogError("[AdvanceTimeAndChangeScene] 找不到 GameStatusService！");
                Stop();
                return;
            }

            if (SceneController.Instance == null)
            {
                Debug.LogError("[AdvanceTimeAndChangeScene] 找不到 SceneController！");
                Stop();
                return;
            }

            var model = service.Time;
            var config = service.TimeConfig;

            // ─── 計算推進格數 ───
            int slotsToAdvance = 0;
            bool isDaySkip = false;

            switch (advanceType)
            {
                case "slot":
                    slotsToAdvance = slotAmount;
                    if (!model.CanAdvanceWithinDay(slotsToAdvance))
                    {
                        Debug.LogWarning($"[AdvanceTimeAndChangeScene] 剩餘時段不足 ({slotsToAdvance} 格)。");
                        Stop();
                        return;
                    }
                    break;

                case "phase":
                    int curPhase = model.CurrentPhaseIndex;
                    if (curPhase >= config.PhaseNames.Count - 1)
                    {
                        Debug.LogWarning("[AdvanceTimeAndChangeScene] 已在最後階段。");
                        Stop();
                        return;
                    }
                    slotsToAdvance = (config.GetTotalSlots(curPhase) - model.CurrentSlotInPhase) + 1;
                    if (!model.CanAdvanceWithinDay(slotsToAdvance))
                    {
                        Debug.LogWarning("[AdvanceTimeAndChangeScene] 時段不足以推進到下一階段。");
                        Stop();
                        return;
                    }
                    break;

                case "day":
                    isDaySkip = true;
                    break;

                default:
                    Debug.LogWarning($"[AdvanceTimeAndChangeScene] 未知推進類型: {advanceType}");
                    Stop();
                    return;
            }

            // ─── 更新邏輯地點 ───
            if (updateScenario && ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.ChangeLocation(targetScene);
            }

            // ─── 登記入口 ID ───
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.SetNextSceneEntry("DefaultEntry");
            }

            // ─── 執行 ───
            if (isDaySkip)
            {
                // day 模式：先推進日期，再透過 SceneController 統一處理隔天演出 + 場景切換
                int remaining = model.GetRemainingSlotsInDay();
                model.AdvanceTime(remaining + 1);

                SceneController.PerformDayTransitionThenChangeScene(targetScene);

                // 等待場景完全就緒（含 ReadyHandlers 全部完成）
                bool fullyReady = false;
                Action onReady = null;
                onReady = () =>
                {
                    fullyReady = true;
                    SceneController.OnSceneFullyReady -= onReady;
                };
                SceneController.OnSceneFullyReady += onReady;

                StartCoroutine(WaitAndStop(() => fullyReady, 15f, "隔天跳日"));
            }
            else if (fadePhaseIndex == IMMEDIATE_TRANSITION)
            {
                // 直接模式：推進時間 + 無淡入淡出切場景（帶回呼的新 API）
                model.AdvanceTime(slotsToAdvance);

                bool fullyReady = false;
                SceneController.ChangeSceneImmediate(targetScene, () => { fullyReady = true; });

                StartCoroutine(WaitAndStop(() => fullyReady, 10f,
                    $"直接推進 {slotsToAdvance} 格 + 切到 {targetScene}"));
            }
            else
            {
                // 轉場模式：推進時間 + 帶淡入淡出切場景（帶回呼的新 API）
                model.AdvanceTime(slotsToAdvance);

                bool fullyReady = false;
                SceneController.ChangeScene(targetScene, fadePhaseIndex, () => { fullyReady = true; });

                StartCoroutine(WaitAndStop(() => fullyReady, 10f,
                    $"轉場推進 {slotsToAdvance} 格 + 切到 {targetScene}"));
            }
        }

        /// <summary>
        /// 等待條件成立後呼叫 Stop()，附帶超時保護。
        /// </summary>
        private System.Collections.IEnumerator WaitAndStop(
            Func<bool> condition, float timeout, string logLabel)
        {
            float elapsed = 0f;
            while (!condition() && elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!condition())
            {
                Debug.LogWarning($"[AdvanceTimeAndChangeScene] {logLabel} 等待超時（{timeout}s）！");
            }
            else
            {
                Debug.Log($"[AdvanceTimeAndChangeScene] {logLabel} 完成。");
            }

            Stop();
        }
    }

    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  5. AdvanceToPhase — 到達下一個「指定階段」                        ║
    // ╠════════════════════════════════════════════════════════════════════╣
    // ║  語法：AdvanceToPhase(階段索引)                                     ║
    // ║                                                                    ║
    // ║  參數：                                                            ║
    // ║  - 階段索引 (必填)  0=白天, 1=黃昏, 2=晚上, 3=深夜                 ║
    // ║                                                                    ║
    // ║  行為：                                                            ║
    // ║  跳到「下一個」該階段的第一格，允許跨日（不做隔天演出）。           ║
    // ║  若目前已在該階段 → 跳到下一輪（通常為隔天同階段）。                ║
    // ║                                                                    ║
    // ║  範例：                                                            ║
    // ║  AdvanceToPhase(2)   → 跳到下一個「晚上」開頭                      ║
    // ╚════════════════════════════════════════════════════════════════════╝
    public class SequencerCommandAdvanceToPhase : SequencerCommand
    {
        public void Awake()
        {
            int targetPhase = GetParameterAsInt(0, -1);

            var service = GameStatusService.Instance;
            if (service == null)
            {
                Debug.LogError("[AdvanceToPhase] 找不到 GameStatusService！");
                Stop();
                return;
            }

            service.Time.AdvanceToNextPhase(targetPhase);
            Debug.Log($"[AdvanceToPhase] 已推進到下一個 Phase {targetPhase}。");
            Stop();
        }
    }

    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  6. AdvanceToWeekday — 到達下一個「指定星期幾」                    ║
    // ╠════════════════════════════════════════════════════════════════════╣
    // ║  語法：AdvanceToWeekday(星期)                                       ║
    // ║                                                                    ║
    // ║  參數：                                                            ║
    // ║  - 星期 (必填)  英文名（Monday）或數字（0=週日 … 6=週六）         ║
    // ║                                                                    ║
    // ║  行為：                                                            ║
    // ║  跳到「下一個」該星期，今日不算，落在該日第一階段第一格。           ║
    // ║  （直接跳日，不做隔天演出）                                        ║
    // ║                                                                    ║
    // ║  範例：                                                            ║
    // ║  AdvanceToWeekday(Monday)  → 跳到下一個星期一                      ║
    // ║  AdvanceToWeekday(1)       → 同上                                  ║
    // ╚════════════════════════════════════════════════════════════════════╝
    public class SequencerCommandAdvanceToWeekday : SequencerCommand
    {
        public void Awake()
        {
            string dayArg = GetParameter(0);

            var service = GameStatusService.Instance;
            if (service == null)
            {
                Debug.LogError("[AdvanceToWeekday] 找不到 GameStatusService！");
                Stop();
                return;
            }

            if (!TryParseDayOfWeek(dayArg, out DayOfWeek target))
            {
                Debug.LogWarning($"[AdvanceToWeekday] 無法解析星期參數 \"{dayArg}\"。可用：Sunday~Saturday 或 0~6。");
                Stop();
                return;
            }

            service.Time.AdvanceToNextDayOfWeek(target);
            Debug.Log($"[AdvanceToWeekday] 已推進到下一個 {target}。");
            Stop();
        }

        private bool TryParseDayOfWeek(string arg, out DayOfWeek result)
        {
            result = DayOfWeek.Sunday;
            if (string.IsNullOrEmpty(arg)) return false;

            if (int.TryParse(arg, out int num))
            {
                if (num < 0 || num > 6) return false;
                result = (DayOfWeek)num;
                return true;
            }

            return Enum.TryParse(arg, true, out result);
        }
    }

    // ╔════════════════════════════════════════════════════════════════════╗
    // ║  7. AdvanceToWeekend — 到達下一個「週末」                          ║
    // ╠════════════════════════════════════════════════════════════════════╣
    // ║  語法：AdvanceToWeekend()                                           ║
    // ║                                                                    ║
    // ║  行為：                                                            ║
    // ║  跳到「下一個」週末（以星期六為起點，今日不算），                   ║
    // ║  落在該日第一階段第一格。（直接跳日，不做隔天演出）                 ║
    // ║                                                                    ║
    // ║  範例：                                                            ║
    // ║  AdvanceToWeekend()  → 跳到下一個星期六                            ║
    // ╚════════════════════════════════════════════════════════════════════╝
    public class SequencerCommandAdvanceToWeekend : SequencerCommand
    {
        public void Awake()
        {
            var service = GameStatusService.Instance;
            if (service == null)
            {
                Debug.LogError("[AdvanceToWeekend] 找不到 GameStatusService！");
                Stop();
                return;
            }

            service.Time.AdvanceToNextWeekend();
            Debug.Log("[AdvanceToWeekend] 已推進到下一個週末（星期六）。");
            Stop();
        }
    }
}
