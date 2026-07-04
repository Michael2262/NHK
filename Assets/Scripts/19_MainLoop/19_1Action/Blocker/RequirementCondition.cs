using System;
using UnityEngine;

/// <summary>
/// 阻擋條件的檢查類型。
/// Stat = 數值條件（用 UnlockStatType 指定要查哪個數值，與 ProgressUnlockRuleAsset 共用同一套定義）。
/// 注意：FlagOn / FlagOff 保留舊序列化編號 (3 / 4)，場景中既有的 Flag 條件不會壞。
/// </summary>
public enum RequirementType
{
    /// <summary> 數值條件：Stat 欄位指定數值，Op + RequiredLevel 指定比較 </summary>
    Stat = 0,
    /// <summary> 特定 Flag 存在（開） </summary>
    FlagOn = 3,
    /// <summary> 特定 Flag 不存在（關） </summary>
    FlagOff = 4
}

/// <summary>
/// 單一阻擋條件。數值類型沿用 ProgressUnlockCommon 的 UnlockStatType / ComparisonOp，
/// 與 ProgressUnlockRuleAsset 的條件系統同一份數值來源：
///   女主角數值 (Libido / Trust / HCount) 需填 HeroineID；
///   主角數值 (Stress / LifePower / Sociality / Dependency / RoomMessLevel) 忽略 HeroineID。
/// </summary>
[Serializable]
public class RequirementCondition
{
    [Tooltip("條件類型：Stat = 數值條件；FlagOn / FlagOff = 旗標條件")]
    public RequirementType Type = RequirementType.Stat;

    [Tooltip(
        "要檢查的數值（僅 Stat 類型生效）。\n" +
        "Libido / Trust / HCount 查下方 HeroineID 指定的女主角；\n" +
        "Stress / LifePower / Sociality / Dependency / RoomMessLevel 查主角。"
    )]
    public UnlockStatType Stat = UnlockStatType.Libido;

    [Tooltip("比較運算子（僅 Stat 類型生效）。例如 Stress 想要「低於門檻才解鎖」就選 Less / LessOrEqual。")]
    public ComparisonOp Op = ComparisonOp.GreaterOrEqual;

    [Tooltip("女主角 ID（僅 Stat 類型且數值為 Libido / Trust / HCount 時需要）")]
    public string HeroineID;

    [Tooltip("閾值（僅 Stat 類型需要）")]
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
            case RequirementType.Stat:
                HeroineStatusModel heroine = null;
                if (ProgressUnlockUtility.IsHeroineStat(Stat)
                    && svc.Heroines != null && !string.IsNullOrEmpty(HeroineID))
                {
                    svc.Heroines.TryGetValue(HeroineID, out heroine);
                }

                if (!ProgressUnlockUtility.TryGetStatValue(Stat, heroine, svc.Protagonist, out int value))
                    return false;
                return ProgressUnlockUtility.Compare(value, Op, RequiredLevel);

            case RequirementType.FlagOn:
                return svc.ProgressFlags != null && svc.ProgressFlags.Contains(FlagID);

            case RequirementType.FlagOff:
                return svc.ProgressFlags != null && !svc.ProgressFlags.Contains(FlagID);
        }
        return false;
    }

    /// <summary> 此條件是否為「數值類」（需要在遮擋圖顯示所需 LV / 閾值）</summary>
    public bool IsStatType => Type == RequirementType.Stat;

    /// <summary> 此條件是否為女主角數值（需要 HeroineID，Blocker 據此訂閱女主角事件）</summary>
    public bool IsHeroineStat => IsStatType && ProgressUnlockUtility.IsHeroineStat(Stat);

    /// <summary> 此條件是否為主角數值（Blocker 據此訂閱主角事件）</summary>
    public bool IsProtagonistStat => IsStatType && !ProgressUnlockUtility.IsHeroineStat(Stat);

    /// <summary>
    /// 取得此條件對應的 textTable field key（用於顯示類型名稱）。
    /// 與 ProgressUnlockRuleAsset.GetUIDisplayTypeKey 一致，直接用數值名稱當 key
    /// （Libido / Trust / HCount / Stress / …）。Flag 類型回傳 null，代表不需要改變類型文字。
    /// </summary>
    public string GetTypeTextKey()
        => IsStatType ? Stat.ToString() : null;

    /// <summary> 條件的文字摘要，例如 "Libido≥3"、"Flag[xxx]=On"（Debug 用）</summary>
    public string ToSummary()
    {
        switch (Type)
        {
            case RequirementType.Stat:
                return $"{Stat}{ProgressUnlockUtility.GetOpSymbol(Op)}{RequiredLevel}";
            case RequirementType.FlagOn:
                return $"Flag[{FlagID}]=On";
            case RequirementType.FlagOff:
                return $"Flag[{FlagID}]=Off";
            default:
                return Type.ToString();
        }
    }
}
