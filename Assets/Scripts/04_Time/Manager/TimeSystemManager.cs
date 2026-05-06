using UnityEngine;
using System;
using System.Collections.Generic;

// 職責：管理時間系統的流程邏輯、協調 Model 的推進，並對外提供時間操作接口。
public class TimeSystemManager
{
    private readonly TimeSystemModel _model;
    private readonly TimeConfig _config; // 新增：用於獲取時間名稱和週配置

    
    private event Action _onPhaseChanged;
    public event Action OnPhaseChanged
    {
        add { _onPhaseChanged += value; }
        remove { _onPhaseChanged -= value; }
    }

    private event Action _onDayPassed;
    public event Action OnDayPassed
    {
        add { _onDayPassed += value; }
        remove { _onDayPassed -= value; }
    }

    public event Action<int> OnTimeSlotAdvanced;
    public TimeSystemManager(TimeSystemModel model, TimeConfig config)
    {
        _model = model;
        _config = config;

        
        _model.OnPhaseChanged += HandlePhaseChanged;
        _model.OnDayPassed += HandleDayPassed;
        _model.OnTimeSlotAdvanced += (delta) => OnTimeSlotAdvanced?.Invoke(delta);
    }

    // ───── 核心方法 ─────

    /// <summary>
    /// 對外接口：根據玩家動作，推進時間時段。
    /// </summary>
    /// <param name="slots">要消耗的時段數量 (0-3)</param>
    public void AdvanceTime(int slots)
    {
        if (slots == 0) return;

        // Model 負責計算和更新內部狀態
        _model.AdvanceTime(slots);

        // 額外的 Manager 邏輯可以在這裡添加
    }
    /// <summary>
    /// 對外接口：重設 Model 為新遊戲狀態 (由 GameStatusService 呼叫)。
    /// </summary>
    public void NewGame()
    {
        _model.NewGame();
    }

    /// <summary>
    /// 消耗一個時段（通常用於小遊戲結束後）。
    /// </summary>
    public void ConsumeOneSlot()
    {
        // 呼叫您提供的 TimeSystemModel 中的 AdvanceTime
        _model.AdvanceTime(1);
    }

    // ───── 查詢方法 ─────

    /// <summary>
    /// 獲取當前遊戲時間的顯示字串。
    /// </summary>
    public string GetCurrentTimeDisplay()
    {
        string phaseName = _config.PhaseNames[_model.CurrentPhaseIndex];
        return $"{_model.GameDate:M/d} ({phaseName} - Slot {_model.CurrentSlotInPhase})";
    }

    // ───── 事件處理器 ─────

    private void HandlePhaseChanged()
    {
        // Manager 可以執行一些與場景相關的邏輯（例如：播放背景音樂變化）
        // Debug.Log($"Phase Changed to: {_config.PhaseNames[_model.CurrentPhaseIndex]}");

        // 轉發事件給外部系統
        _onPhaseChanged?.Invoke();
    }

    private void HandleDayPassed()
    {
        // 這裡可以加入 Manager 級別的邏輯
        // Debug.Log($"Day Passed: Day {_model.DayIndex}");

        // 轉發事件給外部系統 (GameStatusService 會訂閱這個事件來協調 NextDay())
        _onDayPassed?.Invoke();
    }
}