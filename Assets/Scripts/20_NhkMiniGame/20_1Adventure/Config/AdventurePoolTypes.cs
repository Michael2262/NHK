using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 成功率算式模式。也讓 UI 知道這張牌看哪個屬性（可搭配不同美術素材）。
/// </summary>
public enum AdventureRateMode
{
    Social, // A + 社會性 * B
    Life,   // A + 生活力 * C
    Both    // A + 社會性 * B + 生活力 * C
}

/// <summary>大冒險結束原因。</summary>
public enum AdventureEndReason
{
    GoHome,       // 玩家主動回家
    ClearedByCard // 牌上的結束效果觸發（通常＝通關）
}

/// <summary>
/// 一張牌在「必有效果」跑完之後要怎麼收尾。由 AdventureController 讀取決定演出節奏。
/// </summary>
public enum AdventureOutcomeMode
{
    /// <summary>必有效果後，正常依成功率判定成敗，跑成功或失敗效果</summary>
    Judge,

    /// <summary>必有效果後就結束，完全不判定成敗（成功/失敗效果都不會跑）</summary>
    AlwaysOnly,

    /// <summary>必有效果後不擲骰，必定跑成功效果</summary>
    ForceSuccess
}

/// <summary>加權牌：Weight 越大越容易被抽到。</summary>
[Serializable]
public class AdventureWeightedCard
{
    public AdventureCardData Card;
    [Min(0)] public int Weight = 1;
}

/// <summary>
/// 里程分段牌池：里程落在 [MinMileage, MaxMileage] 時，從這段的加權牌表抽牌。
/// 例如「10 里程後變難」= 開一段 [10, 999] 放難牌。
/// </summary>
[Serializable]
public class AdventureMileageBand
{
    public int MinMileage = 0;
    public int MaxMileage = 999;
    public List<AdventureWeightedCard> Cards = new List<AdventureWeightedCard>();

    public bool Contains(int mileage) => mileage >= MinMileage && mileage <= MaxMileage;
}

/// <summary>
/// 指定里程強制發某張牌，優先於一般牌池。
/// 例如「第 8 里程必抽 Boss 牌」。
/// TriggerAtOrAbove 勾選後改為「里程 ≥ AtMileage 皆強制」，
/// 適合最終牌，避免 +2 里程一次跳過目標格。
/// </summary>
[Serializable]
public class AdventureForcedDraw
{
    public int AtMileage = 0;

    [Tooltip("勾選：里程 ≥ AtMileage 皆強制此牌（適合最終 Boss 牌）。\n不勾：里程 == AtMileage 才強制")]
    public bool TriggerAtOrAbove = false;

    public AdventureCardData Card;

    public bool Matches(int mileage) => TriggerAtOrAbove ? mileage >= AtMileage : mileage == AtMileage;
}
