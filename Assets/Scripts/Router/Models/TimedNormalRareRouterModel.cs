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
    /// 啟動指定計時器；已啟動時從現在重新計時。0 秒會立即清除該計時器。
    /// </summary>
    public void StartTimer(TimedRouterTimerId timerId, float duration)
    {
        if (!Enum.IsDefined(typeof(TimedRouterTimerId), timerId))
            throw new ArgumentOutOfRangeException(nameof(timerId), "未知的計時種類。");
        if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0f)
            throw new ArgumentOutOfRangeException(nameof(duration), "倒數秒數必須為有限的非負數。");

        if (duration == 0f)
            expiresAt.Remove(timerId);
        else
            expiresAt[timerId] = getTime() + duration;
    }

    /// <summary>
    /// true 為 Rare，false 為 Normal。倒數內強制 Rare，否則依初始機率抽選。
    /// 分流不會啟動、重啟或延長倒數。
    /// </summary>
    public bool Route(TimedRouterTimerId timerId, float rareChance)
    {
        if (!Enum.IsDefined(typeof(TimedRouterTimerId), timerId))
            throw new ArgumentOutOfRangeException(nameof(timerId), "未知的計時種類。");
        if (float.IsNaN(rareChance) || rareChance < 0f || rareChance > 100f)
            throw new ArgumentOutOfRangeException(nameof(rareChance), "Rare 機率必須介於 0 到 100。");

        double now = getTime();
        if (expiresAt.TryGetValue(timerId, out double deadline) && now < deadline)
            return true;

        expiresAt.Remove(timerId);

        // 明確處理端點，避免亂數回傳 1 時讓 100% 機率落入 Normal。
        return rareChance >= 100f
            || (rareChance > 0f && getRandomValue() < rareChance / 100f);
    }

    /// <summary>切幕、新遊戲或讀檔時清除全部暫存倒數，恢復各 Action 的初始機率。</summary>
    public void ClearTimers()
    {
        expiresAt.Clear();
    }
}
