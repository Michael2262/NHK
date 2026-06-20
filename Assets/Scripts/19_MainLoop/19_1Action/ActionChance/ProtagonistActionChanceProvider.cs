using UnityEngine;

/// <summary>
/// 主角通用行動成功率計算器。
///
/// 公式：
///   基礎成功率 = 自定義基礎值 + 生活力 × X + 社會性 × Y + 依賴度 × Z  （視為百分比）
///   （自定義基礎值、X、Y、Z 預設皆為 0）
///
///   （可選）壓力懲罰：當 enableStressPenalty 為 true 且壓力 > stressThreshold 時，
///   成功率除以 stressDivisor。預設不啟動。
///
/// 最終結果會被 minChance / maxChance 夾住。
/// </summary>
public class ProtagonistActionChanceProvider : ActionChanceProvider
{
    [Header("主角行動 — 公式參數")]
    [Tooltip("自定義基礎值（百分比）。預設 0。")]
    public float customBase = 0f;

    [Tooltip("生活力的權重倍率 X。預設 0。")]
    public float lifePowerWeight = 0f;

    [Tooltip("社會性的權重倍率 Y。預設 0。")]
    public float socialityWeight = 0f;

    [Tooltip("依賴度的權重倍率 Z。預設 0。")]
    public float dependencyWeight = 0f;

    [Header("壓力懲罰（選用）")]
    [Tooltip("是否啟用壓力過大時降低成功率的功能。預設關閉。")]
    public bool enableStressPenalty = false;

    [Tooltip("壓力超過此值時，成功率除以 stressDivisor。預設 80。")]
    public int stressThreshold = 80;

    [Tooltip("壓力過大時的成功率除數。預設 2。")]
    public float stressDivisor = 2f;

    protected override float CalculateChance(ProtagonistStatusModel p)
    {
        // 基礎成功率（百分比轉 0~1）
        float chance = (customBase
                        + p.LifePower * lifePowerWeight
                        + p.Sociality * socialityWeight
                        + p.Dependency * dependencyWeight) / 100f;

        // 壓力懲罰（選用）
        if (enableStressPenalty && p.Stress > stressThreshold && stressDivisor != 0f)
            chance /= stressDivisor;

        return chance;
    }

    protected override float GetFallbackChance() => 0.3f;

    protected override string BuildDebugSummary(ProtagonistStatusModel p, float chance)
    {
        float raw = (customBase
                     + p.LifePower * lifePowerWeight
                     + p.Sociality * socialityWeight
                     + p.Dependency * dependencyWeight) / 100f;
        return $"ProtagonistAction: raw={raw:P0}, final={chance:P0}, " +
               $"LifePower={p.LifePower}, Sociality={p.Sociality}, Dependency={p.Dependency}, " +
               $"Stress={p.Stress}, StressPenalty={enableStressPenalty}";
    }
}
