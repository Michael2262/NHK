using System;
using System.Collections.Generic;

/// <summary>
/// 管理 Normal / Rare 分流及共用到期時間。不存檔，也不需要每幀更新。
/// </summary>
public sealed class TimedNormalRareRouterModel
{
    private readonly Dictionary<TimedRouterTimerId, double> expiresAt
        = new Dictionary<TimedRouterTimerId, double>();
    private readonly Func<double> getTime;
    private readonly Func<float> getRandomValue;

    public TimedNormalRareRouterModel(Func<double> getTime, Func<float> getRandomValue)
    {
        this.getTime = getTime ?? throw new ArgumentNullException(nameof(getTime));
        this.getRandomValue = getRandomValue ?? throw new ArgumentNullException(nameof(getRandomValue));
    }

    /// <summary>
    /// true 為 Rare，false 為 Normal。Normal 會在回傳前啟動倒數，
    /// 確保同幀內其他 Action 也能讀到；Rare 不啟動或延長倒數。
    /// </summary>
    public bool Route(TimedRouterTimerId timerId, float rareChance, float duration)
    {
        if (!Enum.IsDefined(typeof(TimedRouterTimerId), timerId))
            throw new ArgumentOutOfRangeException(nameof(timerId), "未知的計時種類。");
        if (float.IsNaN(rareChance) || rareChance < 0f || rareChance > 100f)
            throw new ArgumentOutOfRangeException(nameof(rareChance), "Rare 機率必須介於 0 到 100。");
        if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0f)
            throw new ArgumentOutOfRangeException(nameof(duration), "倒數秒數必須為有限的非負數。");

        double now = getTime();
        if (expiresAt.TryGetValue(timerId, out double deadline) && now < deadline)
            return true;

        expiresAt.Remove(timerId);

        // 明確處理端點，避免亂數回傳 1 時讓 100% 機率落入 Normal。
        bool isRare = rareChance >= 100f
            || (rareChance > 0f && getRandomValue() < rareChance / 100f);
        if (!isRare && duration > 0f)
            expiresAt[timerId] = now + duration;

        return isRare;
    }

    /// <summary>切幕、新遊戲或讀檔時清除全部暫存倒數，恢復各 Action 的初始機率。</summary>
    public void ClearTimers()
    {
        expiresAt.Clear();
    }
}
