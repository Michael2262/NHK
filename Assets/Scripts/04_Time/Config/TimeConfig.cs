using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Game/Time/Time Config")]
public class TimeConfig : ScriptableObject
{
    [Header("時間階段定義")]
    [Tooltip("四個主要時間階段的名稱，順序即為時間演進的順序 (0-3)")]
    public List<string> PhaseNames = new List<string> { "白天", "黃昏", "晚上", "深夜" };

    [Header("每個階段的時段數量")]
    [Tooltip("請確保順序與 PhaseNames 一致。例如：白天=4, 黃昏=3, 晚上=3, 深夜=3")]
    public List<int> SlotsPerPhase = new List<int> { 4, 3, 3, 3 };

    // ============================================================
    // 【新增】轉場顏色設定
    // ============================================================
    [Header("轉場顏色設定")]
    [Tooltip("每個階段的轉場 Fade 顏色，順序與 PhaseNames 一致")]
    public List<Color> PhaseFadeColors = new List<Color>
    {
        Color.white,    // Phase 0: 白天 → 白色
        Color.white,    // Phase 1: 黃昏 → 白色 (或可改成暖橘色)
        Color.black,    // Phase 2: 晚上 → 黑色
        Color.black     // Phase 3: 深夜 → 黑色
    };

    // ============================================================
    // 週系統配置
    // ============================================================
    [Header("週系統配置")]
    [Tooltip("每週的總天數 (例如 7)，供 WeekIndex 計算用")]
    public int DaysPerWeek = 7;
    [Tooltip("新遊戲 Day 1（StartDayIndex 那天）是星期幾，預設星期一。\n" +
             "假日(IsWeekend)直接綁定星期六、星期日，不再使用獨立的假日天數設定。")]
    public System.DayOfWeek StartDayOfWeek = System.DayOfWeek.Monday;

    // ============================================================
    // 新遊戲初始時間
    // ============================================================
    [Header("新遊戲初始時間")]
    [Tooltip("新遊戲開始的月份 (1-12)")]
    public int StartMonth = 7;
    [Tooltip("新遊戲開始的日期 (1-31)")]
    public int StartDayOfMonth = 1;
    [Tooltip("新遊戲開始的階段索引 (0=白天, 1=黃昏...)")]
    public int StartPhaseIndex = 0;
    [Tooltip("新遊戲開始的階段內時段 (1-N)")]
    public int StartSlotInPhase = 1;
    [Tooltip("新遊戲從第幾天開始計數 (通常為 1)")]
    public int StartDayIndex = 1;

    // ============================================================
    // 公用方法
    // ============================================================

    /// <summary>
    /// 獲取指定階段的總時段數。
    /// </summary>
    public int GetTotalSlots(int phaseIndex)
    {
        if (phaseIndex >= 0 && phaseIndex < SlotsPerPhase.Count)
        {
            return SlotsPerPhase[phaseIndex];
        }
        return 0;
    }

    /// <summary>
    /// 【新增】獲取指定階段的轉場顏色。
    /// </summary>
    public Color GetPhaseFadeColor(int phaseIndex)
    {
        if (phaseIndex >= 0 && phaseIndex < PhaseFadeColors.Count)
        {
            return PhaseFadeColors[phaseIndex];
        }
        // 預設回傳白色
        return Color.white;
    }
}