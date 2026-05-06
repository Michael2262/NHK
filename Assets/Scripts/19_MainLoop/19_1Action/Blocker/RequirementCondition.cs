using System;
using UnityEngine;

/// <summary>
/// 阻擋條件的檢查類型
/// </summary>
public enum RequirementType
{
    AffinityLevel,     // 特定女主角的 BaseAffinityLevel >= RequiredLevel
    ExcitementLevel,   // 特定女主角的 BaseExcitementLevel >= RequiredLevel
    LewdnessLevel,     // 特定女主角的 LewdnessLevel >= RequiredLevel
    FlagOn,            // 特定 Flag 存在（開）
    FlagOff            // 特定 Flag 不存在（關）
}

[Serializable]
public class RequirementCondition
{
    [Tooltip("條件類型")]
    public RequirementType Type;

    [Tooltip("女主角 ID（僅 AffinityLevel / ExcitementLevel / LewdnessLevel 類型需要）")]
    public string HeroineID;

    [Tooltip("所需等級（僅 AffinityLevel / ExcitementLevel / LewdnessLevel 類型需要）")]
    public int RequiredLevel;

    [Tooltip("Flag ID（僅 FlagOn / FlagOff 類型需要）")]
    public string FlagID;

    /// <summary> 判斷此條件是否符合 </summary>
    public bool IsMet()
    {
        var svc = GameStatusService.Instance;
        if (svc == null) return false;

        switch (Type)
        {
            case RequirementType.AffinityLevel:
                return GetHeroineValue(svc, RequirementType.AffinityLevel) >= RequiredLevel;

            case RequirementType.ExcitementLevel:
                return GetHeroineValue(svc, RequirementType.ExcitementLevel) >= RequiredLevel;

            case RequirementType.LewdnessLevel:
                return GetHeroineValue(svc, RequirementType.LewdnessLevel) >= RequiredLevel;

            case RequirementType.FlagOn:
                return svc.ProgressFlags != null && svc.ProgressFlags.Contains(FlagID);

            case RequirementType.FlagOff:
                return svc.ProgressFlags != null && !svc.ProgressFlags.Contains(FlagID);
        }
        return false;
    }

    private int GetHeroineValue(GameStatusService svc, RequirementType t)
    {
        if (svc.Heroines == null || string.IsNullOrEmpty(HeroineID)) return 0;
        if (!svc.Heroines.TryGetValue(HeroineID, out var h) || h == null) return 0;
        switch (t)
        {
            case RequirementType.AffinityLevel: return h.BaseAffinityLevel;
            case RequirementType.ExcitementLevel: return h.BaseExcitementLevel;
            case RequirementType.LewdnessLevel: return h.LewdnessLevel;
        }
        return 0;
    }

    /// <summary> 此條件是否為「等級類」（需要在遮擋圖顯示所需 LV）</summary>
    public bool IsLevelType =>
        Type == RequirementType.AffinityLevel ||
        Type == RequirementType.ExcitementLevel ||
        Type == RequirementType.LewdnessLevel;

    /// <summary>
    /// 取得此條件對應的 textTable field key（用於顯示類型名稱，如「好感度/興奮度/開發度」）。
    /// Flag 類型回傳 null，代表不需要改變類型文字。
    /// </summary>
    public string GetTypeTextKey()
    {
        switch (Type)
        {
            case RequirementType.AffinityLevel: return "Affinity";
            case RequirementType.ExcitementLevel: return "Excitement";
            case RequirementType.LewdnessLevel: return "Lewdness";
            default: return null;
        }
    }
}