using UnityEngine;

/// <summary>
/// NHK 版主角數值變化 UnityEvent 橋接 API。
/// 保留原檔名 / class 名，供 Inspector 的 UnityEvent 拖曳使用。
/// 
/// 提供核心數值、金錢、技能點、時間、射精感、射精次數、房間／身體髒污度及狀態開關。
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
    // 射精感：範圍與變更通知由 Model 處理。
    // ==========================================

    public int GetSemen() => P?.GetSemen() ?? 0;
    public void SetSemen(int value) => P?.SetSemen(value);
    /// <summary>正數增加、負數減少射精感。</summary>
    public void AddSemen(int amount) => P?.AddSemen(amount);

    // ==========================================
    // 射精次數
    // ==========================================

    /// <summary>增加射精次數，沿用 Model 規則，可超過每日重設值。</summary>
    public void AddShootTimes(int amount) => P?.AddShootTimes(amount);

    /// <summary>以正數指定扣除次數；低於 Model 下限時不扣除並提示。</summary>
    public void ReduceShootTimes(int amount)
    {
        var model = P;
        if (model == null)
        {
            Debug.LogWarning("[ProtagonistBridgeAPI] 主角 Model 尚未初始化，無法扣除射精次數。", this);
            return;
        }

        if (!model.TryReduceShootTimes(amount))
        {
            Debug.LogWarning($"[ProtagonistBridgeAPI] 射精次數不足，無法扣除 {amount}；下限為 {ProtagonistStatusModel.SHOOT_TIMES_MIN}。", this);
        }
    }

    /// <summary>重設射精次數為 Model 的每日初始值，目前為 3。</summary>
    public void ResetShootTimes() => P?.ResetShootTimes();

    // ==========================================
    // 房間髒亂度 / 身體髒污度：範圍由 Model 處理。
    // ==========================================

    public void AddRoomMessLevel(int amount) => P?.AddRoomMessLevel(amount);
    /// <summary>以正數指定減少的房間髒亂度。</summary>
    public void ReduceRoomMessLevel(int amount) => P?.ReduceRoomMessLevel(amount);
    public void SetRoomMessLevel(int value) => P?.SetRoomMessLevel(value);

    public void AddBodyDirtyLevel(int amount) => P?.AddBodyDirtyLevel(amount);
    /// <summary>以正數指定減少的身體髒污度。</summary>
    public void ReduceBodyDirtyLevel(int amount) => P?.ReduceBodyDirtyLevel(amount);
    public void SetBodyDirtyLevel(int value) => P?.SetBodyDirtyLevel(value);

    // ==========================================
    // 狀態開關
    // ==========================================

    public void SetBadHealthy(bool value) => P?.SetBadHealthy(value);
    public void EnableBadHealthy() => SetBadHealthy(true);
    public void DisableBadHealthy() => SetBadHealthy(false);

    /// <summary>設定不良依賴；能否開啟及自動解除門檻由 Model 決定。</summary>
    public void SetBadDependency(bool value) => P?.SetBadDependency(value);
    public void EnableBadDependency() => SetBadDependency(true);
    public void DisableBadDependency() => SetBadDependency(false);
}
