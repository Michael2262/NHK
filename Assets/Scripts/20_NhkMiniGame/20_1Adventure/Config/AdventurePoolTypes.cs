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
    GoHome,   // 玩家主動回家
    ByEffect  // 牌上的 End Adventure 效果結束（通關/中止皆走這個）
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

// 抽牌改為「兩池 + 每次特色機率」，相關資料直接放在 AdventureDungeonData，
// 不再需要獨立的加權牌 / 分段 / 強制牌型別。
