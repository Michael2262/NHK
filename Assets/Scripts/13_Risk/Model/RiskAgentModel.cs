using System;
using UnityEngine; // 為了 Max

/// <summary>
/// 職責：保存並管理「單一風險來源」(例如家人) 的「動態」狀態。
/// 包含：個人可疑度、狀態旗標 (例如 "隔天不在")。
/// </summary>
public class RiskAgentModel
{
    // --- 核心識別 ---
    public string AgentID { get; }

    // --- 動態數值 ---
    [Tooltip("此家人對玩家的個人可疑度")]
    public int PersonalSuspicion { get; private set; }

    // --- 動態旗標 (Status Flags) ---
    [Tooltip("是否永久消失")]
    public bool IsGoneForever { get; private set; }

    [Tooltip("暫時不在，直到此日期 (DayIndex)")]
    public int AbsentUntilDay { get; private set; }

    [Tooltip("暫時不在，直到此日期的此階段 (PhaseIndex)")]
    public int AbsentUntilPhase { get; private set; }

    // --- 事件宣告 ---
    public event Action<int> OnPersonalSuspicionChanged;
    public event Action OnStatusFlagsChanged; // 當 "不在" 狀態改變時通知 Manager

    /// <summary>
    /// 建構函式
    /// </summary>
    public RiskAgentModel(string agentID)
    {
        this.AgentID = agentID;
        // 狀態初始化
        NewGame();
    }

    // ==========================================================
    // 公開 API：修改狀態
    // ==========================================================

    /// <summary>
    /// 增加對此家人的個人可疑度
    /// </summary>
    public void AddPersonalSuspicion(int amount)
    {
        if (amount == 0) return;
        int oldValue = PersonalSuspicion;
        PersonalSuspicion = Math.Max(0, PersonalSuspicion + amount);
        int delta = PersonalSuspicion - oldValue;

        if (delta != 0)
            OnPersonalSuspicionChanged?.Invoke(delta);
    }

    /// <summary>
    /// 降低對此家人的個人可疑度
    /// </summary>
    public void ReducePersonalSuspicion(int amount) => AddPersonalSuspicion(-amount);

    /// <summary>
    /// 設定此家人永久消失
    /// </summary>
    public void SetAbsentForever()
    {
        if (IsGoneForever) return;
        IsGoneForever = true;
        OnStatusFlagsChanged?.Invoke();
    }

    /// <summary>
    /// 設定此家人不在，直到指定的時間點
    /// </summary>
    /// <param name="untilDay">持續到哪一天 (TimeSystemModel.DayIndex)</param>
    /// <param name="untilPhase">持續到哪一階段 (TimeSystemModel.CurrentPhaseIndex)</param>
    public void SetAbsentUntil(int untilDay, int untilPhase)
    {
        AbsentUntilDay = untilDay;
        AbsentUntilPhase = untilPhase;
        OnStatusFlagsChanged?.Invoke();
    }

    /// <summary>
    /// 清除暫時不在的狀態
    /// </summary>
    public void ClearAbsentStatus()
    {
        if (AbsentUntilDay == 0 && AbsentUntilPhase == -1) return; // 本來就沒事
        AbsentUntilDay = 0;
        AbsentUntilPhase = -1;
        OnStatusFlagsChanged?.Invoke();
    }

    // ==========================================================
    // 公開 API：查詢狀態
    // ==========================================================

    /// <summary>
    /// 檢查此家人在「當前時間」是否處於 "不在" 狀態
    /// </summary>
    public bool IsCurrentlyAbsent(int currentDay, int currentPhase)
    {
        if (IsGoneForever) return true;

        // 檢查是否在 "不在" 的日期之前
        if (currentDay < AbsentUntilDay) return true;

        // 檢查是否剛好在 "不在" 的最後一天，且還沒過 "不在" 的階段
        if (currentDay == AbsentUntilDay && currentPhase <= AbsentUntilPhase) return true;

        // 已經過了 "不在" 的時間
        return false;
    }

    // ==========================================================
    // 遊戲流程：NewGame / Save / Load
    // ==========================================================

    /// <summary>
    /// 重設為新遊戲狀態
    /// </summary>
    public void NewGame()
    {
        // 這裡我們假設所有家人一開始的可疑度都是 0
        // 如果你需要從 RiskDatabaseConfig 讀取初始可疑度，
        // 則需要修改 GameStatusService 的 InitializeRiskAgentModels 邏輯
        PersonalSuspicion = 0;
        IsGoneForever = false;
        AbsentUntilDay = 0;
        AbsentUntilPhase = -1; // -1 代表無效階段
    }

    /// <summary>
    // 導出存檔數據
    /// </summary>
    public RiskAgentSaveData ToSaveData()
    {
        return new RiskAgentSaveData
        {
            PersonalSuspicion = this.PersonalSuspicion,
            IsGoneForever = this.IsGoneForever,
            AbsentUntilDay = this.AbsentUntilDay,
            AbsentUntilPhase = this.AbsentUntilPhase
        };
    }

    /// <summary>
    /// 載入存檔數據
    /// </summary>
    public void LoadFromSaveData(RiskAgentSaveData data)
    {
        if (data == null)
        {
            NewGame(); // 如果存檔中沒有此資料，則重設
            return;
        }

        PersonalSuspicion = data.PersonalSuspicion;
        IsGoneForever = data.IsGoneForever;
        AbsentUntilDay = data.AbsentUntilDay;
        AbsentUntilPhase = data.AbsentUntilPhase;
    }
}