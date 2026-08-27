using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>抽牌的類別。Normal＝普通事件（墊底 fallback）；Special＝特色/判定；Quest＝任務。</summary>
public enum AdventureCardCategory
{
    Normal,
    Special,
    Quest
}

/// <summary>
/// 一個「有機率、可 gating」的牌池（Special / Quest 共用）。
/// Normal 池不用這個 —— 它是墊底 fallback，直接放 List。
/// </summary>
[Serializable]
public class AdventureCardPool
{
    public List<AdventureCardData> Cards = new List<AdventureCardData>();

    [Tooltip("每次散步抽到這個池的機率(%)，依第幾次散步查表。\n" +
             "index 0 = 第 1 次…超過表格長度沿用最後一筆。")]
    public List<float> ChancePerAction = new List<float>();

    [Tooltip("勾選：一趟只要出過一次這個池的牌，之後就不再出（機率視為 0）")]
    public bool OnlyOncePerRun = false;

    [Tooltip("勾選：本輪不重複抽到同一張（同一趟每張最多出一次；全出過了就不再出）")]
    public bool NoRepeatInRun = false;

    /// <summary>有沒有可用牌（過濾 null 後）。</summary>
    public bool HasCards => Cards != null && Cards.Exists(c => c != null);

    /// <summary>排除 exclude 後是否還有可抽的牌。</summary>
    public bool HasDrawable(ICollection<AdventureCardData> exclude)
        => Cards != null && Cards.Exists(c => c != null && (exclude == null || !exclude.Contains(c)));

    /// <summary>取第 (actionIndex+1) 次散步的機率(%)。超過表格長度沿用最後一筆。</summary>
    public float GetChance(int actionIndex)
    {
        if (ChancePerAction == null || ChancePerAction.Count == 0) return 0f;
        if (actionIndex < 0) actionIndex = 0;
        if (actionIndex >= ChancePerAction.Count) actionIndex = ChancePerAction.Count - 1;
        return ChancePerAction[actionIndex];
    }

    /// <summary>從池中隨機抽一張（排除 exclude；無可抽回傳 null）。</summary>
    public AdventureCardData PickRandom(ICollection<AdventureCardData> exclude)
    {
        if (Cards == null || Cards.Count == 0) return null;
        var valid = Cards.FindAll(c => c != null && (exclude == null || !exclude.Contains(c)));
        if (valid.Count == 0) return null;
        return valid[UnityEngine.Random.Range(0, valid.Count)];
    }
}

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
