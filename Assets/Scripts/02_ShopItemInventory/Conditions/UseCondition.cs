using UnityEngine;

// ==========================================================
// 自用條件：抽象基底
// ==========================================================

/// <summary>
/// 自用條件的抽象基底。
/// 判斷主角「是否能使用」這個道具或選項。
/// NHK 版：以 Stress / LifePower / Sociality / Dependency / Money / SkillPoints 為主。
/// </summary>
[System.Serializable]
public abstract class UseCondition
{
    [Tooltip("條件不符時的提示訊息 Key（對應 Text Table 中的 Field Name）")]
    public string FailMessageKey = "";

    public abstract bool IsMet(ProtagonistStatusModel protagonist);
}

// ==========================================================
// NHK 條件
// ==========================================================

[System.Serializable]
public class StressAtMostCondition : UseCondition
{
    [Tooltip("壓力必須小於等於此值")]
    public int MaxStress = 100;

    public override bool IsMet(ProtagonistStatusModel protagonist)
    {
        return protagonist != null && protagonist.Stress <= MaxStress;
    }
}

[System.Serializable]
public class LifePowerAtLeastCondition : UseCondition
{
    [Tooltip("生活力必須大於等於此值")]
    public int MinLifePower = 0;

    public override bool IsMet(ProtagonistStatusModel protagonist)
    {
        return protagonist != null && protagonist.LifePower >= MinLifePower;
    }
}

[System.Serializable]
public class SocialityAtLeastCondition : UseCondition
{
    [Tooltip("社會性必須大於等於此值")]
    public int MinSociality = 0;

    public override bool IsMet(ProtagonistStatusModel protagonist)
    {
        return protagonist != null && protagonist.Sociality >= MinSociality;
    }
}

[System.Serializable]
public class DependencyAtLeastCondition : UseCondition
{
    [Tooltip("依賴度必須大於等於此值")]
    public int MinDependency = 0;

    public override bool IsMet(ProtagonistStatusModel protagonist)
    {
        return protagonist != null && protagonist.Dependency >= MinDependency;
    }
}

[System.Serializable]
public class MoneyAtLeastCondition : UseCondition
{
    [Tooltip("金錢必須大於等於此值")]
    public int RequiredMoney = 0;

    public override bool IsMet(ProtagonistStatusModel protagonist)
    {
        return protagonist != null && protagonist.Money >= RequiredMoney;
    }
}

[System.Serializable]
public class SkillPointsAtLeastCondition : UseCondition
{
    [Tooltip("技能點必須大於等於此值")]
    public int RequiredSkillPoints = 0;

    public override bool IsMet(ProtagonistStatusModel protagonist)
    {
        return protagonist != null && protagonist.SkillPoints >= RequiredSkillPoints;
    }
}

// ==========================================================
// 舊版相容條件
// ==========================================================

/// <summary>
/// 舊版精力劑條件。NHK 已移除 EnergyDrink。
/// 為避免 Inspector / 既有 SO 參照爆掉，暫時保留 class，預設永遠通過。
/// 建議之後改用 StressAtMostCondition / LifePowerAtLeastCondition 等 NHK 條件。
/// </summary>
[System.Serializable]
public class EnergyDrinkChargesCondition : UseCondition
{
    [Tooltip("舊版欄位，NHK 不再使用。保留僅為相容。")]
    public int RequiredCharges = 1;

    public override bool IsMet(ProtagonistStatusModel protagonist)
    {
        return true;
    }
}

// ==========================================================
// Flag 條件
// ==========================================================

[System.Serializable]
public class RequireFlagCondition : UseCondition
{
    [Tooltip("必須已開啟的 Flag 定義檔")]
    public ProgressFlagDefinition RequiredFlag;

    public override bool IsMet(ProtagonistStatusModel protagonist)
    {
        if (RequiredFlag == null) return true;
        return GameStatusService.Instance != null &&
               GameStatusService.Instance.ProgressFlags != null &&
               GameStatusService.Instance.ProgressFlags.Contains(RequiredFlag.FlagID);
    }
}

[System.Serializable]
public class RequireFlagAbsentCondition : UseCondition
{
    [Tooltip("必須尚未開啟的 Flag 定義檔")]
    public ProgressFlagDefinition ForbiddenFlag;

    public override bool IsMet(ProtagonistStatusModel protagonist)
    {
        if (ForbiddenFlag == null) return true;
        return GameStatusService.Instance == null ||
               GameStatusService.Instance.ProgressFlags == null ||
               !GameStatusService.Instance.ProgressFlags.Contains(ForbiddenFlag.FlagID);
    }
}
