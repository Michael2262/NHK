using System;
using System.Collections.Generic;
using UnityEngine;

// 職責：保存及管理遊戲內的日期、時段和週狀態。
public class TimeSystemModel
{
    // ═════════ 狀態屬性 ═════════

    // Day/Week/Date
    public int DayIndex { get; private set; } = 1;     // 遊戲從 Day 1 開始計數
    public int WeekIndex => (DayIndex - 1) / _cfg.DaysPerWeek + 1; // 依據 TimeConfig
    public bool IsWeekend { get; private set; }

    // Game Date (日曆日期)
    public DateTime GameDate { get; private set; }     // 從 7 月 1 日開始

    // Time Slots
    public int CurrentPhaseIndex { get; private set; } // 0=Day, 1=Dusk, 2=Night, 3=LateNight
    public int CurrentSlotInPhase { get; private set; } = 1; // 目前時段是當前階段的第幾格 (從 1 開始)

    // ═════════ 事件宣告 ═════════
    public event Action<int> OnTimeSlotAdvanced;       // 告知外界時段已前進 (delta)
    public event Action OnPhaseChanged;                 // 告知外界進入新的階段 (例如：從白天進入黃昏)
    public event Action OnDayPassed;                   // 告知外界新的一天開始 (例如：Day 1 -> Day 2)

    // ============================================================
    // 【新增】隔天演出請求事件
    // ============================================================
    /// <summary>
    /// 當呼叫 SkipToNextDay() 時觸發，通知外部系統執行隔天轉場演出。
    /// 參數 Action 是「演出完成後要執行的回呼」，用於實際推進日期。
    /// </summary>
    public event Action<Action> OnDaySkipRequested;

    // ═════════ 依賴注入 ═════════
    private readonly TimeConfig _cfg;

    public TimeSystemModel(TimeConfig config)
    {
        _cfg = config;
        LoadInitialState();
        UpdateWeekendStatus();
    }

    // ==========================================================
    // 檢查
    // ==========================================================

    /// <summary>
    /// 判斷傳入的時段數是否能在「不跨日」的情況下執行完畢。
    /// </summary>
    public bool CanAdvanceWithinDay(int slots)
    {
        if (slots < 0) return false;
        if (slots == 0) return true;
        return slots <= GetRemainingSlotsInDay();
    }

    // ==========================================================
    // 動作
    // ==========================================================

    /// <summary>
    /// 一般動作使用：嘗試推進時間。
    /// 如果消耗量會導致「跨日」，則回傳 false 且不執行。
    /// </summary>
    public bool TryAdvanceTime(int slots)
    {
        if (slots < 0) return false;
        if (slots == 0) return true;

        if (slots > GetRemainingSlotsInDay())
        {
            Debug.Log("[TimeSystem] 剩餘時間不足以執行此動作（不允許自動跨日）。");
            return false;
        }

        AdvanceTime(slots);
        return true;
    }

    /// <summary>
    /// 【修改】特殊觸發（睡覺）：請求隔天轉場演出。
    /// 實際的日期推進會在演出完成後才執行。
    /// </summary>
    public void SkipToNextDay()
    {
        Debug.Log("[TimeSystem] SkipToNextDay 被呼叫，準備請求隔天演出...");

        // 如果有訂閱者（SceneController），則發送事件並等待演出完成
        if (OnDaySkipRequested != null)
        {
            // 傳入一個回呼，讓演出系統在完成後呼叫
            OnDaySkipRequested.Invoke(ExecuteActualDaySkip);
        }
        else
        {
            // 如果沒有訂閱者，直接執行（向下相容）
            Debug.LogWarning("[TimeSystem] 沒有系統監聽 OnDaySkipRequested，直接執行日期推進。");
            ExecuteActualDaySkip();
        }
    }

    /// <summary>
    /// 【新增】實際執行日期跳轉的內部方法。
    /// 由演出系統在壓幕期間呼叫。
    /// </summary>
    private void ExecuteActualDaySkip()
    {
        CurrentPhaseIndex = 0;       // 回到第一階段 (例如：早上)
        CurrentSlotInPhase = 1;      // 回到第一格

        AdvanceDay();                // 執行日期 +1 與週末判定

        // 通知 UI 更新
        OnPhaseChanged?.Invoke();
        OnTimeSlotAdvanced?.Invoke(0);

        Debug.Log("[TimeSystem] 玩家已上床睡覺，跳至隔天。");
    }

    // ==========================================================
    // 核心邏輯：推進時間 (由 TimeSystemManager 呼叫)
    // ==========================================================

    /// <summary>
    /// 根據玩家的動作，推進時間時段。
    /// </summary>
    public void AdvanceTime(int slots)
    {
        if (slots == 0) return;

        int totalSlotsInCurrentPhase = _cfg.GetTotalSlots(CurrentPhaseIndex);

        while (slots > 0)
        {
            // 1. 在當前階段內推進
            if (CurrentSlotInPhase < totalSlotsInCurrentPhase)
            {
                int remainingSlotsInPhase = totalSlotsInCurrentPhase - CurrentSlotInPhase;
                int slotsToAdvance = Math.Min(slots, remainingSlotsInPhase);

                CurrentSlotInPhase += slotsToAdvance;
                slots -= slotsToAdvance;

                if (slots > 0 && CurrentSlotInPhase == totalSlotsInCurrentPhase)
                {
                    // 已經填滿當前階段，準備進入下一階段
                }
                else if (slots == 0)
                {
                    // 推進完成
                    break;
                }
            }
            // 2. 進入下一階段
            else if (CurrentPhaseIndex < _cfg.PhaseNames.Count - 1)
            {
                CurrentPhaseIndex++;
                CurrentSlotInPhase = 1;
                slots -= 1;
                OnPhaseChanged?.Invoke();
                totalSlotsInCurrentPhase = _cfg.GetTotalSlots(CurrentPhaseIndex);
            }
            // 3. 進入新的一天
            else
            {
                CurrentPhaseIndex = 0;
                CurrentSlotInPhase = 1;
                slots -= 1;
                AdvanceDay();
                break;
            }
        }

        OnTimeSlotAdvanced?.Invoke(slots);
    }

    // ==========================================================
    // 日期推進邏輯
    // ==========================================================

    private void AdvanceDay()
    {
        DayIndex++;
        GameDate = GameDate.AddDays(1);
        UpdateWeekendStatus();
        OnDayPassed?.Invoke();
    }

    private void UpdateWeekendStatus()
    {
        int dayOfWeek = (DayIndex - 1) % _cfg.DaysPerWeek;
        IsWeekend = dayOfWeek >= (_cfg.DaysPerWeek - _cfg.WeekendDays);
    }

    /// <summary>
    /// 計算從目前時段開始，到今天結束(最後一個階段的最後一格)總共還剩多少可用時段。
    /// </summary>
    public int GetRemainingSlotsInDay()
    {
        int totalSlotsInCurrentPhase = _cfg.GetTotalSlots(CurrentPhaseIndex);
        int remainingInCurrent = totalSlotsInCurrentPhase - CurrentSlotInPhase;

        int remainingInFuturePhases = 0;
        for (int i = CurrentPhaseIndex + 1; i < _cfg.PhaseNames.Count; i++)
        {
            remainingInFuturePhases += _cfg.GetTotalSlots(i);
        }

        return remainingInCurrent + remainingInFuturePhases;
    }

    // ==========================================================
    // 存檔與讀檔方法
    // ==========================================================

    /// <summary>
    /// NewGame，將 Model 數據重設為 Config 定義的初始狀態。
    /// </summary>
    public void LoadInitialState()
    {
        DayIndex = _cfg.StartDayIndex;

        try
        {
            GameDate = new DateTime(DateTime.Now.Year, _cfg.StartMonth, _cfg.StartDayOfMonth);
        }
        catch (Exception)
        {
            GameDate = new DateTime(DateTime.Now.Year, 1, 1);
        }

        CurrentPhaseIndex = _cfg.StartPhaseIndex;
        CurrentSlotInPhase = _cfg.StartSlotInPhase;

        UpdateWeekendStatus();

        OnDayPassed?.Invoke();
        OnPhaseChanged?.Invoke();
        OnTimeSlotAdvanced?.Invoke(0);
    }

    /// <summary>
    /// 導出 Model 的當前狀態至 TimeSaveData 結構。
    /// </summary>
    public TimeSaveData ToSaveData()
    {
        return new TimeSaveData
        {
            DayIndex = this.DayIndex,
            GameDate = this.GameDate.ToString("o"),
            CurrentPhaseIndex = this.CurrentPhaseIndex,
            CurrentSlotInPhase = this.CurrentSlotInPhase
        };
    }

    /// <summary>
    /// 從存檔數據中恢復 Model 的狀態。
    /// </summary>
    public void LoadFromSaveData(TimeSaveData data)
    {
        DayIndex = data.DayIndex;
        GameDate = DateTime.Parse(data.GameDate);
        CurrentPhaseIndex = data.CurrentPhaseIndex;
        CurrentSlotInPhase = data.CurrentSlotInPhase;

        Debug.Log($"[TimeSystemModel] LoadFromSaveData executed. New GameDate is: {GameDate.ToString("o")}");
    }

    /// <summary>
    /// 對外接口：重設為新遊戲狀態 (由 TimeSystemManager 呼叫)。
    /// </summary>
    public void NewGame()
    {
        LoadInitialState();
    }
}