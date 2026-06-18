using UnityEngine;
using PixelCrushers.DialogueSystem;
using System;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
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