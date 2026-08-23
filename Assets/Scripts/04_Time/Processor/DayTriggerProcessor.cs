using UnityEngine;

// ============================================================
// DayTriggerProcessor
// 職責：在指定的 Day + Phase + Slot 自動開啟或關閉 Flag。
//
// 開啟時支援五種生命週期：
//   Persistent → FlagLifetime.Persistent（永久，不自動消失）
//   Slot       → FlagLifetime.UntilNextSlot（下一個 Slot 推進後自動消失）
//   Phase      → FlagLifetime.UntilNextPhase（進入下一個 Phase 後自動消失）
//   Day        → FlagLifetime.UntilNextDay（隔天後自動消失）
//   Temp       → FlagLifetime.Scene（切換場景後消失）
//
// 有兩條觸發路徑：
//   A) TriggerPhaseIndex < 0（填 -1）
//      → 在 HandleDayPassed() 時立即觸發，不等待 Phase/Slot
//      → 呼叫：NotifyDayPassed()
//
//   B) TriggerPhaseIndex >= 0
//      → 等待到達指定的 Day + Phase + Slot 才觸發
//      → 呼叫：NotifySlotAdvanced()  （放在 ClearSlotFlags 後）
//              NotifyPhaseChanged()  （放在 ClearPhaseFlags 後）
//
// GameStatusService 需要加的呼叫：
//   HandleDayPassed()        → _dayTriggerProcessor?.NotifyDayPassed();
//   OnTimeSlotAdvanced lambda → _dayTriggerProcessor?.NotifySlotAdvanced();
//   OnPhaseChanged lambda     → _dayTriggerProcessor?.NotifyPhaseChanged();
// ============================================================
public class DayTriggerProcessor
{
    private readonly TimeSystemModel _timeModel;
    private readonly ProgressFlagModel _flagModel;
    private readonly DayTriggerConfig _config;

    public DayTriggerProcessor(
        TimeSystemModel timeModel,
        ProgressFlagModel flagModel,
        DayTriggerConfig config)
    {
        _timeModel = timeModel;
        _flagModel = flagModel;
        _config = config;
    }

    // ── 公開通知介面 ─────────────────────────────────────────

    /// <summary>
    /// 由 GameStatusService.HandleDayPassed() 呼叫。
    /// 處理 TriggerPhaseIndex == -1（當天開始立即觸發）的規則。
    /// </summary>
    public void NotifyDayPassed()
    {
        if (_config == null || _config.Entries == null) return;

        foreach (var entry in _config.Entries)
        {
            if (!entry.TriggerOnDayStart) continue;
            if (_timeModel.DayIndex != entry.TriggerDayIndex) continue;

            ExecuteEntry(entry);
        }
    }

    /// <summary>
    /// 由 GameStatusService 在 ClearSlotFlags() 之後呼叫。
    /// 處理指定 Phase + Slot 觸發的規則（TriggerSlotInPhase != 0）。
    /// </summary>
    public void NotifySlotAdvanced()
    {
        if (_config == null || _config.Entries == null) return;

        foreach (var entry in _config.Entries)
        {
            if (entry.TriggerOnDayStart) continue;
            if (entry.TriggerSlotInPhase == 0) continue;
            if (_timeModel.DayIndex != entry.TriggerDayIndex) continue;
            if (_timeModel.CurrentPhaseIndex != entry.TriggerPhaseIndex) continue;
            if (_timeModel.CurrentSlotInPhase != entry.TriggerSlotInPhase) continue;

            ExecuteEntry(entry);
        }
    }

    /// <summary>
    /// 由 GameStatusService 在 ClearPhaseFlags() 之後呼叫。
    /// 處理 Phase 進入時觸發的規則（TriggerSlotInPhase == 0）。
    /// </summary>
    public void NotifyPhaseChanged()
    {
        if (_config == null || _config.Entries == null) return;

        foreach (var entry in _config.Entries)
        {
            if (entry.TriggerOnDayStart) continue;
            if (entry.TriggerSlotInPhase != 0) continue;
            if (_timeModel.DayIndex != entry.TriggerDayIndex) continue;
            if (_timeModel.CurrentPhaseIndex != entry.TriggerPhaseIndex) continue;

            ExecuteEntry(entry);
        }
    }

    // ── 核心執行 ─────────────────────────────────────────────

    private void ExecuteEntry(DayTriggerEntry entry)
    {
        bool hasFlag = !string.IsNullOrEmpty(entry.TargetFlagID);
        bool hasAlert = !string.IsNullOrEmpty(entry.AlertKey);

        // 兩者都沒填，這條規則等於沒設定，直接跳過並提醒。
        if (!hasFlag && !hasAlert)
        {
            Debug.LogWarning($"[DayTrigger]「{entry.EntryName}」未指定 TargetFlag 也未填 AlertKey，跳過。");
            return;
        }

        // ── Flag 操作（選填）──────────────────────────
        if (hasFlag)
        {
            if (!entry.FlagState)
            {
                _flagModel.RemoveFlag(entry.TargetFlagID);
                Debug.Log($"[DayTrigger] Day {_timeModel.DayIndex} " +
                          $"Phase {_timeModel.CurrentPhaseIndex} Slot {_timeModel.CurrentSlotInPhase}" +
                          $"：關閉 Flag「{entry.TargetFlagID}」");
            }
            else
            {
                // 將 ScheduleFlagLifetime 對應到 ProgressFlagModel 的 FlagLifetime
                FlagLifetime lifetime = entry.FlagLifetime switch
                {
                    ScheduleFlagLifetime.Persistent => FlagLifetime.Persistent,
                    ScheduleFlagLifetime.Slot => FlagLifetime.UntilNextSlot,
                    ScheduleFlagLifetime.Phase => FlagLifetime.UntilNextPhase,
                    ScheduleFlagLifetime.Day => FlagLifetime.UntilNextDay,
                    ScheduleFlagLifetime.Temp => FlagLifetime.Scene,
                    _ => FlagLifetime.Persistent
                };

                _flagModel.AddFlag(entry.TargetFlagID, lifetime);
                Debug.Log($"[DayTrigger] Day {_timeModel.DayIndex} " +
                          $"Phase {_timeModel.CurrentPhaseIndex} Slot {_timeModel.CurrentSlotInPhase}" +
                          $"：開啟 Flag「{entry.TargetFlagID}」(Lifetime={entry.FlagLifetime})");
            }
        }

        // ── 公告（選填，經由 StoryManager → AlertToastManager）─────
        if (hasAlert)
        {
            ShowAlert(entry);
        }
    }

    /// <summary>
    /// 觸發時顯示系統公告（選填；AlertKey 留空則不會走到這裡）。
    /// 沿用 StoryManager.ShowLocalizedMessage —— 內部會走 AlertToastManager 堆疊公告，
    /// 找不到管理器時 fallback 回原生 Alert，與 Rules 的達成公告同一套機制。
    /// </summary>
    private void ShowAlert(DayTriggerEntry entry)
    {
        if (StoryManager.Instance == null)
        {
            Debug.LogWarning(
                $"[DayTrigger]「{entry.EntryName}」想顯示公告「{entry.AlertKey}」，" +
                "但找不到 StoryManager 實例。");
            return;
        }

        StoryManager.Instance.ShowLocalizedMessage(entry.AlertKey);
    }
}