using UnityEngine;

// ============================================================
// DailyScheduleProcessor
// 職責：依據 DailyScheduleConfig，在正確時機自動操作 ProgressFlagModel。
//
// Flag 生命週期完整支援五種：
//   Persistent → FlagLifetime.Persistent（永久，不自動消失）
//   Slot       → FlagLifetime.UntilNextSlot（下一個 Slot 推進後自動消失）
//   Phase      → FlagLifetime.UntilNextPhase（進入下一個 Phase 後自動消失）
//   Day        → FlagLifetime.UntilNextDay（隔天後自動消失）
//   Temp       → FlagLifetime.Scene（切換場景後消失）
//
// 自動清除依賴 GameStatusService.SubscribeToEvents() 中的事件接線：
//   OnTimeSlotAdvanced → ClearSlotFlags()
//   OnPhaseChanged     → ClearPhaseFlags()
//   HandleDayPassed    → ClearDayFlags()
//
// 使用方式：
//   建立：_scheduleProcessor = new DailyScheduleProcessor(Time, ProgressFlags, config);
//   呼叫（GameStatusService.SubscribeToEvents 裡）：
//     _scheduleProcessor?.NotifySlotAdvanced();   ← 放在任何需要評估 Slot 觸發的地方
//     _scheduleProcessor?.NotifyPhaseChanged();   ← 放在 ClearPhaseFlags() 之後
// ============================================================
public class DailyScheduleProcessor
{
    private readonly TimeSystemModel _timeModel;
    private readonly ProgressFlagModel _flagModel;
    private readonly DailyScheduleConfig _config;

    public DailyScheduleProcessor(
        TimeSystemModel timeModel,
        ProgressFlagModel flagModel,
        DailyScheduleConfig config)
    {
        _timeModel = timeModel;
        _flagModel = flagModel;
        _config = config;
    }

    // ── 公開通知介面（由 GameStatusService 呼叫）────────────

    /// <summary>
    /// 由 GameStatusService 在時間推進後呼叫。
    /// 評估所有設定在「特定 Phase + Slot」的規則。
    /// </summary>
    public void NotifySlotAdvanced()
    {
        if (_config == null || _config.Entries == null) return;

        foreach (var entry in _config.Entries)
        {
            if (entry.TriggerSlotInPhase == 0) continue;
            if (_timeModel.CurrentPhaseIndex != entry.TriggerPhaseIndex) continue;
            if (_timeModel.CurrentSlotInPhase != entry.TriggerSlotInPhase) continue;

            TryExecuteEntry(entry);
        }
    }

    /// <summary>
    /// 由 GameStatusService 在 ClearPhaseFlags() 之後呼叫。
    /// 評估所有設定在「Phase 進入時（TriggerSlotInPhase == 0）」的規則。
    /// </summary>
    public void NotifyPhaseChanged()
    {
        if (_config == null || _config.Entries == null) return;

        foreach (var entry in _config.Entries)
        {
            if (entry.TriggerSlotInPhase != 0) continue;
            if (_timeModel.CurrentPhaseIndex != entry.TriggerPhaseIndex) continue;

            TryExecuteEntry(entry);
        }
    }

    // ── 核心邏輯 ────────────────────────────────────────────

    private void TryExecuteEntry(ScheduleEntry entry)
    {
        if (!MatchesDayType(entry.DayType)) return;

        if (entry.EnableFlagID != null)
            if (!_flagModel.Contains(entry.EnableFlagID)) return;

        ExecuteAction(entry);

        Debug.Log($"[DailySchedule] 觸發「{entry.EntryName}」" +
                  $" @ Phase={_timeModel.CurrentPhaseIndex}, Slot={_timeModel.CurrentSlotInPhase}");
    }

    private bool MatchesDayType(DayTypeFilter filter) => filter switch
    {
        DayTypeFilter.Weekday => !_timeModel.IsWeekend,
        DayTypeFilter.Weekend => _timeModel.IsWeekend,
        _ => true
    };

    private void ExecuteAction(ScheduleEntry entry)
    {
        switch (entry.ActionType)
        {
            case ScheduleActionType.SetFlag: ExecuteSetFlag(entry); break;
            case ScheduleActionType.SetValue: ExecuteSetValue(entry); break;
            default:
                Debug.LogWarning($"[DailySchedule] 未知的 ActionType: {entry.ActionType}");
                break;
        }
    }

    private void ExecuteSetFlag(ScheduleEntry entry)
    {
        if (string.IsNullOrEmpty(entry.TargetFlagID))
        {
            Debug.LogWarning($"[DailySchedule]「{entry.EntryName}」TargetFlag 未指定，跳過。");
            return;
        }

        if (!entry.FlagState)
        {
            _flagModel.RemoveFlag(entry.TargetFlagID);
            Debug.Log($"[DailySchedule] 關閉 Flag: {entry.TargetFlagID}");
            return;
        }

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
        Debug.Log($"[DailySchedule] 開啟 Flag: {entry.TargetFlagID} (Lifetime={entry.FlagLifetime})");
    }

    private void ExecuteSetValue(ScheduleEntry entry)
    {
        if (string.IsNullOrEmpty(entry.TargetValueID))
        {
            Debug.LogWarning($"[DailySchedule]「{entry.EntryName}」TargetValue 未指定，跳過。");
            return;
        }

        _flagModel.SetValue(entry.TargetValueID, entry.TargetValueAmount);
        Debug.Log($"[DailySchedule] 設定 Value: {entry.TargetValueID} = {entry.TargetValueAmount}");
    }
}