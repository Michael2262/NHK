using UnityEngine;

/// <summary>
/// NHK 版主角數值變化 UnityEvent 橋接 API。
/// 保留原檔名 / class 名，供 Inspector 的 UnityEvent 拖曳使用。
/// 
/// 核心：Stress / LifePower / Sociality / Dependency / Money / SkillPoints / Time / Daily Flags / Counters。
/// </summary>
[AddComponentMenu("Game/API/Protagonist Bridge API")]
public class ProtagonistBridgeAPI : MonoBehaviour
{
    private ProtagonistStatusModel P => GameStatusService.Instance?.Protagonist;

    // ==========================================
    // NHK Core Values
    // ==========================================

    public void AddStress(int amount) => P?.AddStress(amount);
    public void ReduceStress(int amount) => P?.ReduceStress(amount);
    public void SetStress(int value) => P?.SetStress(value);

    public void AddLifePower(int amount) => P?.AddLifePower(amount);
    public void ReduceLifePower(int amount) => P?.ReduceLifePower(amount);
    public void SetLifePower(int value) => P?.SetLifePower(value);

    public void AddSociality(int amount) => P?.AddSociality(amount);
    public void ReduceSociality(int amount) => P?.ReduceSociality(amount);
    public void SetSociality(int value) => P?.SetSociality(value);

    public void AddDependency(int amount) => P?.AddDependency(amount);
    public void ReduceDependency(int amount) => P?.ReduceDependency(amount);
    public void SetDependency(int value) => P?.SetDependency(value);

    public void ApplyStatusChange(int stressDelta, int lifePowerDelta, int socialityDelta, int dependencyDelta)
    {
        P?.ApplyStatusChange(new ProtagonistStatusChange(stressDelta, lifePowerDelta, socialityDelta, dependencyDelta));
    }

    // ==========================================
    // Money / SkillPoints
    // ==========================================

    public void ReduceMoney(int amount)
    {
        bool success = P?.TryReduceMoney(amount) ?? false;
        if (!success) Debug.LogWarning($"[ProtagonistBridgeAPI] Money 不足，無法扣除 {amount}");
    }

    public void AddMoney(int amount) => P?.AddMoney(amount);
    public void SetMoney(int amount) => P?.SetMoney(amount);

    public void ReduceSkillPoints(int amount)
    {
        bool success = P?.TryReduceSkillPoints(amount) ?? false;
        if (!success) Debug.LogWarning($"[ProtagonistBridgeAPI] SkillPoints 不足，無法扣除 {amount}");
    }

    public void AddSkillPoints(int amount) => P?.AddSkillPoints(amount);
    public void SetSkillPoints(int amount) => P?.SetSkillPoints(amount);

    // ==========================================
    // Daily Flags
    // ==========================================

    public void MarkBathedToday(bool value = true) => P?.MarkBathedToday(value);
    public void MarkCleanedRoomToday(bool value = true) => P?.MarkCleanedRoomToday(value);
    public void MarkHadMealToday(bool value = true) => P?.MarkHadMealToday(value);
    public void MarkCheckedMailToday(bool value = true) => P?.MarkCheckedMailToday(value);
    public void MarkRepliedFamilyToday(bool value = true) => P?.MarkRepliedFamilyToday(value);
    public void MarkIgnoredPhoneToday(bool value = true) => P?.MarkIgnoredPhoneToday(value);
    public void MarkEscapedToday(bool value = true) => P?.MarkEscapedToday(value);
    public void MarkGoneOutsideSucceeded() => P?.MarkGoneOutsideToday(true);
    public void MarkGoneOutsideFailed() => P?.MarkGoneOutsideToday(false);
    public void MarkStressCollapsedToday(bool value = true) => P?.MarkStressCollapsedToday(value);

    // ==========================================
    // Long-term Counters
    // ==========================================

    public void AddNightExtend1SuccessCount(int amount = 1) => P?.AddNightExtend1SuccessCount(amount);
    public void AddNightExtend2SuccessCount(int amount = 1) => P?.AddNightExtend2SuccessCount(amount);
    public void AddStayOverCount(int amount = 1) => P?.AddStayOverCount(amount);

    // ==========================================
    // Time
    // ==========================================

    public void AdvanceTime(int slots)
    {
        var t = GameStatusService.Instance?.Time;
        if (t == null)
        {
            Debug.LogWarning("[ProtagonistBridgeAPI] TimeSystemModel 尚未初始化，無法推進時間。");
            return;
        }
        t.AdvanceTime(slots);
    }

    // ==========================================
    // Legacy Compatibility Wrappers
    // ==========================================
    // 這些方法保留是為了避免舊場景 UnityEvent 綁定直接斷掉。
    // NHK 版不再有 Stamina / Spirit / Action / ShootTimes / Suspicion / EnergyDrink。

    [System.Obsolete("NHK uses LifePower instead of Stamina. Use AddLifePower / ReduceLifePower instead.")]
    public void ReduceStamina(float amount) => ReduceLifePower(Mathf.RoundToInt(amount));
    [System.Obsolete("NHK uses LifePower instead of Stamina. Use AddLifePower / ReduceLifePower instead.")]
    public void AddStamina(float amount) => AddLifePower(Mathf.RoundToInt(amount));
    [System.Obsolete("NHK uses LifePower instead of Stamina. Use SetLifePower instead.")]
    public void SetStaminaToZero() => SetLifePower(0);
    [System.Obsolete("NHK uses LifePower instead of Stamina. Use SetLifePower instead.")]
    public void SetStaminaToMax() => SetLifePower(100);
    public void ReduceStamina(int amount) => ReduceStamina((float)amount);
    public void AddStamina(int amount) => AddStamina((float)amount);

    [System.Obsolete("NHK no longer uses StaminaMax. This call is ignored.")]
    public void AddStaminaMax(float amount) => Debug.LogWarning("[ProtagonistBridgeAPI] AddStaminaMax ignored in NHK.");
    public void AddStaminaMax(int amount) => AddStaminaMax((float)amount);
    [System.Obsolete("NHK no longer uses StaminaMax. This call is ignored.")]
    public void SetStaminaMax(float value) => Debug.LogWarning("[ProtagonistBridgeAPI] SetStaminaMax ignored in NHK.");

    [System.Obsolete("NHK no longer uses ShootTimes. This call is ignored.")]
    public void ReduceShootTimes(int amount) => Debug.LogWarning("[ProtagonistBridgeAPI] ReduceShootTimes ignored in NHK.");
    [System.Obsolete("NHK no longer uses ShootTimes. This call is ignored.")]
    public void AddShootTimes(int amount) => Debug.LogWarning("[ProtagonistBridgeAPI] AddShootTimes ignored in NHK.");
    [System.Obsolete("NHK no longer uses ShootTimes. This call is ignored.")]
    public void SetShootTimes(int value) => Debug.LogWarning("[ProtagonistBridgeAPI] SetShootTimes ignored in NHK.");
    [System.Obsolete("NHK no longer uses ShootTimes depletion. This call is ignored.")]
    public void AddShootPhysicalDepletionPenalty(int amount) => Debug.LogWarning("[ProtagonistBridgeAPI] AddShootPhysicalDepletionPenalty ignored in NHK.");
    [System.Obsolete("NHK no longer uses ShootTimes depletion. This call is ignored.")]
    public void SetShootItemDepletion(int value) => Debug.LogWarning("[ProtagonistBridgeAPI] SetShootItemDepletion ignored in NHK.");

    [System.Obsolete("NHK no longer uses ExcuseCharges. This call is ignored.")]
    public void ReduceExcuseCharge(int amount = 1) => Debug.LogWarning("[ProtagonistBridgeAPI] ReduceExcuseCharge ignored in NHK.");
    [System.Obsolete("NHK no longer uses ExcuseCharges. This call is ignored.")]
    public void AddExcuseCharge(int amount = 1) => Debug.LogWarning("[ProtagonistBridgeAPI] AddExcuseCharge ignored in NHK.");

    [System.Obsolete("NHK uses Stress reduction for rest. Use ReduceStress instead.")]
    public void Rest(int slots)
    {
        int reduce = Mathf.Max(0, slots) * 10;
        ReduceStress(reduce);
        Debug.Log($"[ProtagonistBridgeAPI] Legacy Rest({slots}) mapped to ReduceStress({reduce}).");
    }

    [System.Obsolete("NHK no longer uses RestRecoveryPerSlot. This call is ignored.")]
    public void SetRestRecoveryPerSlot(int value) => Debug.LogWarning("[ProtagonistBridgeAPI] SetRestRecoveryPerSlot ignored in NHK.");
    [System.Obsolete("NHK no longer uses RestRecoveryPerSlot. This call is ignored.")]
    public void AddRestRecoveryPerSlot(int delta) => Debug.LogWarning("[ProtagonistBridgeAPI] AddRestRecoveryPerSlot ignored in NHK.");

    [System.Obsolete("NHK no longer uses EnergyDrink. This call is ignored.")]
    public void TryReduceEnergyDrink(int amount = 1) => Debug.LogWarning("[ProtagonistBridgeAPI] TryReduceEnergyDrink ignored in NHK.");
    [System.Obsolete("NHK no longer uses EnergyDrink. This call is ignored.")]
    public void RefillEnergyDrink() => Debug.LogWarning("[ProtagonistBridgeAPI] RefillEnergyDrink ignored in NHK.");
    [System.Obsolete("NHK no longer uses EnergyDrink. This call is ignored.")]
    public void SetEnergyDrinkMax(int value) => Debug.LogWarning("[ProtagonistBridgeAPI] SetEnergyDrinkMax ignored in NHK.");
}
