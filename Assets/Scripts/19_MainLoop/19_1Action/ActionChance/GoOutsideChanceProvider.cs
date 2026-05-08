using UnityEngine;

/// <summary>
/// 「嘗試出門」成功率計算器。
///
/// 公式：
///   基礎成功率 = 生活力 × 1.5 − 社會恐懼  （視為百分比）
///   若壓力 > 50，成功率減半
///   若壓力 > 80，成功率除以 3
///
/// 最終結果會被 minChance / maxChance 夾住。
/// </summary>
public class GoOutsideChanceProvider : ActionChanceProvider
{
    [Header("嘗試出門 — 公式參數")]
    [Tooltip("生活力的權重倍率。預設 1.5。")]
    public float lifePowerWeight = 1.5f;

    [Tooltip("社會恐懼的權重倍率。預設 1.0。")]
    public float socialFearWeight = 1.0f;

    [Tooltip("壓力超過此值時，成功率減半。")]
    public int stressHalfThreshold = 50;

    [Tooltip("壓力超過此值時，成功率除以 3（優先於減半）。")]
    public int stressThirdThreshold = 80;

    protected override float CalculateChance(ProtagonistStatusModel p)
    {
        // 基礎成功率（百分比轉 0~1）
        float chance = (p.LifePower * lifePowerWeight - p.SocialFear * socialFearWeight) / 100f;

        // 壓力懲罰：> 80 優先判定（除以 3），否則 > 50 減半
        if (p.Stress > stressThirdThreshold)
            chance /= 3f;
        else if (p.Stress > stressHalfThreshold)
            chance /= 2f;

        return chance;
    }

    protected override float GetFallbackChance() => 0.3f;

    protected override string BuildDebugSummary(ProtagonistStatusModel p, float chance)
    {
        float raw = (p.LifePower * lifePowerWeight - p.SocialFear * socialFearWeight) / 100f;
        return $"GoOutside: raw={raw:P0}, final={chance:P0}, " +
               $"LifePower={p.LifePower}, SocialFear={p.SocialFear}, Stress={p.Stress}";
    }
}
