using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Time/Daily Schedule Config", fileName = "DailyScheduleConfig")]
public class DailyScheduleConfig : ScriptableObject
{
    [Tooltip("所有排程規則列表")]
    public List<ScheduleEntry> Entries = new List<ScheduleEntry>();
}

[Serializable]
public class ScheduleEntry
{
    [Header("── 規則基本設定 ──────────────────────────")]
    [Tooltip("此規則的識別名稱（僅供編輯器辨識用）")]
    public string EntryName = "New Schedule Entry";

    [Tooltip("控制此規則是否啟動的 Flag。留空則視為「永遠啟用」。")]
    public ProgressFlagDefinition EnableFlag;

    [Header("── 觸發條件 ─────────────────────────────")]
    [Tooltip("此規則適用於哪些日子類型")]
    public DayTypeFilter DayType = DayTypeFilter.Both;

    [Tooltip("觸發的 Phase 索引（0=白天、1=黃昏…，依 TimeConfig 而定）")]
    public int TriggerPhaseIndex = 0;

    [Tooltip("觸發的 Slot 編號（從 1 開始，填 0 表示 Phase 進入時立即觸發）")]
    public int TriggerSlotInPhase = 1;

    [Header("── 執行動作 ─────────────────────────────")]
    [Tooltip("此規則要執行的動作類型")]
    public ScheduleActionType ActionType = ScheduleActionType.SetFlag;

    // --- SetFlag 專屬 ---
    [Tooltip("[SetFlag] 要操作的 Flag（拖入 Flag_Xxx 檔案）")]
    public ProgressFlagDefinition TargetFlag;

    [Tooltip("[SetFlag] true = 開啟 Flag；false = 關閉（RemoveFlag）")]
    public bool FlagState = true;

    [Tooltip("[SetFlag] 開啟時 Flag 的生命週期\n" +
             "• Persistent：永久存在，不自動消失\n" +
             "• Slot：下一個時間段（Slot）推進後自動消失\n" +
             "• Phase：進入下一個時段（Phase）後自動消失\n" +
             "• Day：隔天（DayPassed）後自動消失\n" +
             "• Temp：切換場景後消失")]
    public ScheduleFlagLifetime FlagLifetime = ScheduleFlagLifetime.Persistent;

    // --- SetValue 專屬 ---
    [Tooltip("[SetValue] 要設定的 Value（拖入 Val_Xxx 檔案）")]
    public ProgressValueDefinition TargetValue;

    [Tooltip("[SetValue] 要設定的數值")]
    public int TargetValueAmount = 0;

    // ── 執行期屬性（Processor 使用）──
    public string EnableFlagID => EnableFlag != null ? EnableFlag.FlagID : null;
    public string TargetFlagID => TargetFlag != null ? TargetFlag.FlagID : null;
    public string TargetValueID => TargetValue != null ? TargetValue.FlagID : null;
}

// ── 列舉 ────────────────────────────────────────────────────

public enum DayTypeFilter
{
    Weekday,    // 只在平日觸發
    Weekend,    // 只在假日觸發
    Both        // 平日與假日都觸發
}

public enum ScheduleActionType
{
    SetFlag,    // 開啟或關閉一個 Flag
    SetValue    // 設定一個 Value 為特定數值
}

/// <summary>
/// 排程開啟 Flag 的生命週期。
/// 需搭配 GameStatusService.SubscribeToEvents() 中的自動清除機制：
///   - Slot  flags → 由 OnTimeSlotAdvanced 事件觸發 ClearSlotFlags()
///   - Phase flags → 由 OnPhaseChanged 事件觸發 ClearPhaseFlags()
///   - Day   flags → 由 HandleDayPassed() 觸發 ClearDayFlags()
/// </summary>
public enum ScheduleFlagLifetime
{
    /// <summary>永久存在，不自動消失</summary>
    Persistent,

    /// <summary>下一個 Slot 推進後自動消失（對應 FlagLifetime.UntilNextSlot）</summary>
    Slot,

    /// <summary>進入下一個 Phase 後自動消失（對應 FlagLifetime.UntilNextPhase）</summary>
    Phase,

    /// <summary>隔天後自動消失（對應 FlagLifetime.UntilNextDay）</summary>
    Day,

    /// <summary>切換場景後消失（對應 FlagLifetime.Scene）</summary>
    Temp
}