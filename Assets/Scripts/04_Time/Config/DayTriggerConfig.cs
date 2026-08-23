using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Progress/Day Trigger Config", fileName = "DayTriggerConfig")]
public class DayTriggerConfig : ScriptableObject
{
    [Tooltip("所有日期觸發規則列表")]
    public List<DayTriggerEntry> Entries = new List<DayTriggerEntry>();
}

[Serializable]
public class DayTriggerEntry
{
    [Header("── 規則基本設定 ──────────────────────────")]
    [Tooltip("此規則的識別名稱（僅供編輯器辨識用）")]
    public string EntryName = "New Day Trigger";

    [Header("── 觸發條件 ─────────────────────────────")]
    [Tooltip("在第幾天觸發（對應 TimeSystemModel.DayIndex，從 1 開始）")]
    public int TriggerDayIndex = 1;

    [Tooltip("觸發的 Phase 索引（0=白天、1=黃昏…，依 TimeConfig 而定）\n填 -1 表示當天任何 Phase 都不觸發，改為隔天開始時（DayPassed）立即觸發")]
    public int TriggerPhaseIndex = 0;

    [Tooltip("觸發的 Slot 編號（從 1 開始）\n填 0 表示進入此 Phase 時立即觸發")]
    public int TriggerSlotInPhase = 1;

    [Header("── 執行動作 ─────────────────────────────")]
    [Tooltip("要操作的 Flag（拖入 Flag_Xxx 檔案）\n" +
             "可留空：若只想在特定日期單純發公告，不動任何 Flag，則留空並只填下方 AlertKey。")]
    public ProgressFlagDefinition TargetFlag;

    [Tooltip("true = 開啟 Flag；false = 關閉（RemoveFlag）")]
    public bool FlagState = true;

    [Tooltip("開啟時 Flag 的生命週期\n" +
             "• Persistent：永久存在，不自動消失\n" +
             "• Slot：下一個時間段（Slot）推進後自動消失\n" +
             "• Phase：進入下一個時段（Phase）後自動消失\n" +
             "• Day：隔天（DayPassed）後自動消失\n" +
             "• Temp：切換場景後消失")]
    public ScheduleFlagLifetime FlagLifetime = ScheduleFlagLifetime.Persistent;

    [Header("── 觸發時的公告 (Alert，選填) ────────────")]
    [Tooltip("觸發此規則時，經由 AlertToastManager 顯示的系統公告（與 Rules 的達成公告同一套）。\n" +
             "填 Localization Key（找不到 Key 時會直接顯示原文）；留空則不顯示公告。\n" +
             "可只填此欄、不指定 TargetFlag，用來在特定日期單純以 key 呼叫公告。")]
    public string AlertKey;

    // 執行期屬性
    public string TargetFlagID => TargetFlag != null ? TargetFlag.FlagID : null;

    /// <summary>TriggerPhaseIndex == -1 表示在 DayPassed 時立即觸發，不等待 Phase/Slot。</summary>
    public bool TriggerOnDayStart => TriggerPhaseIndex < 0;
}