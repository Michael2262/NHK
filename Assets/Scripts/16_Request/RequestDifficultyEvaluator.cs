using UnityEngine;

/// <summary>
/// Request 難度顯示的純 C# 計算工具。
/// 使用與 RequestRoll 相同的驅動值與成功率曲線，但不進行擲骰。
/// </summary>
public static class RequestDifficultyEvaluator
{
    /// <summary>
    /// 目前難度文字支援的主角驅動數值。
    /// </summary>
    public static bool IsSupportedProtagonistDriver(DriverStat driver)
    {
        return driver == DriverStat.Protagonist_LifePower
            || driver == DriverStat.Protagonist_Sociality
            || driver == DriverStat.Protagonist_Dependency;
    }

    /// <summary>
    /// 取得本次 Request 的有效成功率，不進行隨機擲骰。
    /// GuaranteedAbove 生效時回傳 100%。
    /// </summary>
    public static float ComputeEffectiveSuccessRate(
        RequestArchetype archetype,
        int driverValue,
        int bonus = 0)
    {
        if (archetype == null)
        {
            Debug.LogWarning("[RequestDifficultyEvaluator] archetype 為 null，無法計算難度。");
            return 0f;
        }

        int effectiveValue = driverValue + bonus;
        if (archetype.GuaranteedAbove && effectiveValue >= archetype.THigh)
            return 100f;

        return archetype.ComputeSuccessRate(effectiveValue);
    }

    /// <summary>
    /// 依集中設定將成功率轉成 Text Table Key。
    /// </summary>
    public static string ResolveTextTableKey(
        RequestDifficultyDisplayConfig config,
        float successRate)
    {
        if (config == null)
        {
            Debug.LogWarning("[RequestDifficultyEvaluator] display config 為 null，無法取得文字 Key。");
            return string.Empty;
        }

        float rate = Mathf.Clamp(successRate, 0f, 100f);
        if (rate >= config.EasyMinimumRate) return config.EasyKey;
        if (rate >= config.MediumMinimumRate) return config.MediumKey;
        return config.HardKey;
    }
}
