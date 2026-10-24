using UnityEngine;

[AddComponentMenu("Game/API/Value Trigger Bridge API")]
public class ValueTriggerBridgeAPI : MonoBehaviour
{
    // ==========================================
    // Money / SkillPoints
    // ==========================================

    public void ExecuteReduceMoney(int amount)
    {
        var p = GameStatusService.Instance.Protagonist;
        bool success = p.TryReduceMoney(amount);
        Debug.Log($"[BridgeAPI] 執行扣除金錢 {amount}, 結果: {success}");
    }

    public void ExecuteAddMoney(int amount)
    {
        var p = GameStatusService.Instance.Protagonist;
        p.AddMoney(amount);
        Debug.Log($"[BridgeAPI] 執行增加金錢 {amount}");
    }

    public void ExecuteReduceSkillPoints(int amount)
    {
        var p = GameStatusService.Instance.Protagonist;
        bool success = p.TryReduceSkillPoints(amount);
        Debug.Log($"[BridgeAPI] 執行扣除技能點 {amount}, 結果: {success}");
    }

    public void ExecuteAddSkillPoints(int amount)
    {
        var p = GameStatusService.Instance.Protagonist;
        p.AddSkillPoints(amount);
        Debug.Log($"[BridgeAPI] 執行增加技能點 {amount}");
    }

    // ==========================================
    // NHK Core Values
    // ==========================================

    public void ExecuteAddRoomMessLevel(int amount)
    {
        var p = GameStatusService.Instance.Protagonist;
        p.AddRoomMessLevel(amount);
        Debug.Log($"[BridgeAPI] 執行增加房間髒亂度 {amount}");
    }

    public void ExecuteReduceRoomMessLevel(int amount)
    {
        var p = GameStatusService.Instance.Protagonist;
        p.ReduceRoomMessLevel(amount);
        Debug.Log($"[BridgeAPI] 執行降低房間髒亂度 {amount}");
    }

    // ==========================================
    //
    // ==========================================

    public void ExecuteAdvanceTime(int slots)
    {
        var t = GameStatusService.Instance.Time;
        t.AdvanceTime(slots);
        Debug.Log($"[BridgeAPI] 執行時間推進 {slots} 格");
    }

    public void ExecuteMoneyAndTime(int moneyAmount, int timeSlots)
    {
        ExecuteReduceMoney(moneyAmount);
        ExecuteAdvanceTime(timeSlots);
    }

    // ---- 批次推進（允許跨日，不觸發睡覺演出）----

    /// <summary>推進多個階段(Phase)，允許跨日。</summary>
    public void ExecuteAdvancePhases(int phases)
    {
        var t = GameStatusService.Instance.Time;
        t.AdvancePhases(phases);
        Debug.Log($"[BridgeAPI] 執行階段推進 {phases} 個 Phase");
    }

    /// <summary>推進多個 Day，落在目標日的第一階段第一格。</summary>
    public void ExecuteAdvanceDays(int days)
    {
        var t = GameStatusService.Instance.Time;
        t.AdvanceDays(days);
        Debug.Log($"[BridgeAPI] 執行日期推進 {days} 天");
    }

    /// <summary>推進到下一個週末（星期六，今日不算）。</summary>
    public void ExecuteAdvanceToNextWeekend()
    {
        var t = GameStatusService.Instance.Time;
        t.AdvanceToNextWeekend();
        Debug.Log("[BridgeAPI] 推進到下一個週末（星期六）");
    }

    /// <summary>
    /// 推進到下一個指定星期幾（今日不算）。
    /// dayOfWeek：0=星期日, 1=星期一, ... 6=星期六（對應 System.DayOfWeek）。
    /// </summary>
    public void ExecuteAdvanceToNextDayOfWeek(int dayOfWeek)
    {
        if (dayOfWeek < 0 || dayOfWeek > 6)
        {
            Debug.LogWarning($"[BridgeAPI] 無效的星期值 {dayOfWeek}（應為 0~6）");
            return;
        }

        var t = GameStatusService.Instance.Time;
        var target = (System.DayOfWeek)dayOfWeek;
        t.AdvanceToNextDayOfWeek(target);
        Debug.Log($"[BridgeAPI] 推進到下一個星期 {target}");
    }

    // ==========================================
    // Legacy compatibility
    // ==========================================
    [System.Obsolete("NHK no longer uses Action. This only advances time.")]
    public void ExecuteReduceAction(int amount) => Debug.LogWarning("[BridgeAPI] ExecuteReduceAction ignored in NHK.");

    [System.Obsolete("NHK no longer uses Action. This only advances time.")]
    public void ExecuteActionAndTime(int actionAmount, int timeSlots) => ExecuteAdvanceTime(timeSlots);

    public void ExecuteReduceStamina(int amount) {
        if (amount > 0) GameStatusService.Instance?.Protagonist?.ForceAddStamina(-amount);
    }

    public void ExecuteAddStamina(int amount) {
        GameStatusService.Instance?.Protagonist?.AddStamina(amount);
    }

    public void ExecuteAddStaminaMax(int amount) {
        GameStatusService.Instance?.Protagonist?.AddStaminaMax(amount);
    }
}
