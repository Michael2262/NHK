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

    public void ExecuteAddStress(int amount)
    {
        var p = GameStatusService.Instance.Protagonist;
        p.AddStress(amount);
        Debug.Log($"[BridgeAPI] 執行增加壓力 {amount}");
    }

    public void ExecuteReduceStress(int amount)
    {
        var p = GameStatusService.Instance.Protagonist;
        p.ReduceStress(amount);
        Debug.Log($"[BridgeAPI] 執行降低壓力 {amount}");
    }

    public void ExecuteAddLifePower(int amount)
    {
        var p = GameStatusService.Instance.Protagonist;
        p.AddLifePower(amount);
        Debug.Log($"[BridgeAPI] 執行增加生活力 {amount}");
    }

    public void ExecuteReduceLifePower(int amount)
    {
        var p = GameStatusService.Instance.Protagonist;
        p.ReduceLifePower(amount);
        Debug.Log($"[BridgeAPI] 執行降低生活力 {amount}");
    }

    public void ExecuteAddSociality(int amount)
    {
        var p = GameStatusService.Instance.Protagonist;
        p.AddSociality(amount);
        Debug.Log($"[BridgeAPI] 執行增加社會性 {amount}");
    }

    public void ExecuteReduceSociality(int amount)
    {
        var p = GameStatusService.Instance.Protagonist;
        p.ReduceSociality(amount);
        Debug.Log($"[BridgeAPI] 執行降低社會性 {amount}");
    }

    public void ExecuteAddDependency(int amount)
    {
        var p = GameStatusService.Instance.Protagonist;
        p.AddDependency(amount);
        Debug.Log($"[BridgeAPI] 執行增加依賴度 {amount}");
    }

    public void ExecuteReduceDependency(int amount)
    {
        var p = GameStatusService.Instance.Protagonist;
        p.ReduceDependency(amount);
        Debug.Log($"[BridgeAPI] 執行降低依賴度 {amount}");
    }

    // ==========================================
    // Time
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

    // ==========================================
    // Legacy compatibility
    // ==========================================
    [System.Obsolete("NHK no longer uses Action. This only advances time.")]
    public void ExecuteReduceAction(int amount) => Debug.LogWarning("[BridgeAPI] ExecuteReduceAction ignored in NHK.");

    [System.Obsolete("NHK no longer uses Action. This only advances time.")]
    public void ExecuteActionAndTime(int actionAmount, int timeSlots) => ExecuteAdvanceTime(timeSlots);

    [System.Obsolete("NHK uses LifePower instead of Stamina.")]
    public void ExecuteReduceStamina(int amount) => ExecuteReduceLifePower(amount);

    [System.Obsolete("NHK uses LifePower instead of Stamina.")]
    public void ExecuteAddStamina(int amount) => ExecuteAddLifePower(amount);

    [System.Obsolete("NHK no longer uses StaminaMax. This call is ignored.")]
    public void ExecuteAddStaminaMax(int amount) => Debug.LogWarning("[BridgeAPI] ExecuteAddStaminaMax ignored in NHK.");
}
