using System;

/// <summary>小遊戲的分鐘時鐘。以 Lobby 的顯示日為起點，結束時才推進正式 Mini。</summary>
public sealed class BulletTimeModel
{
    private const int MinutesPerDay = 24 * 60;
    private readonly TimeSystemModel time;
    private readonly TimeConfig config;
    private readonly TimePlaybackModel playback;
    private TimeMappingSO mapping;
    private int startDay;
    private int displayMinutes;
    private float interval;
    private double elapsed;
    private bool settling;

    public bool IsActive => time.BulletTimeActive;
    public int DisplayDay => startDay + displayMinutes / MinutesPerDay;
    public int MinuteOfDay => displayMinutes % MinutesPerDay;
    public event Action DisplayChanged;

    public BulletTimeModel(TimeSystemModel time, TimeConfig config, TimePlaybackModel playback)
    {
        this.time = time;
        this.config = config;
        this.playback = playback;
    }

    public bool Begin(TimeMappingSO timeMapping, float secondsPerMinute)
    {
        if (IsActive || settling || playback.IsTimeFlowing || timeMapping == null ||
            secondsPerMinute < 0 || float.IsNaN(secondsPerMinute) || float.IsInfinity(secondsPerMinute))
            return false;

        int phase = time.CurrentPhaseIndex;
        int slot = time.CurrentSlotInPhase;
        if (!timeMapping.TryGetClockTime(phase, slot, time.CurrentMiniInSlot, out int hour, out int minute))
            return false;

        mapping = timeMapping;
        startDay = time.DayIndex + (mapping.ShouldShowNextDay(phase, slot) ? 1 : 0);
        displayMinutes = hour * 60 + minute;
        elapsed = 0;
        interval = secondsPerMinute;
        time.BulletTimeActive = true;
        DisplayChanged?.Invoke();
        return true;
    }

    public void Tick(float seconds)
    {
        if (!IsActive || settling || interval <= 0) return;
        elapsed += seconds;
        if (elapsed < interval) return;
        elapsed -= interval;
        // 一幀最多顯示一分鐘，卡頓時讓剩餘刻度在後續畫面播完。
        if (!AdvanceOneMinute()) interval = 0;
    }

    public bool AdvanceOneMinute()
    {
        if (!IsActive || settling || displayMinutes >= 2 * MinutesPerDay - 1) return false;
        displayMinutes++;
        DisplayChanged?.Invoke();
        return true;
    }

    public bool EndAtNearestMini() => TrySettle(displayMinutes, exact: false);

    public bool EndAtTime(string clock, bool nextDay)
    {
        if (!TimeMappingSO.TryParseClockTime(clock, out int hour, out int minute)) return false;
        int target = (nextDay ? MinutesPerDay : 0) + hour * 60 + minute;
        return TrySettle(target, exact: true);
    }

    private bool TrySettle(int target, bool exact)
    {
        if (!IsActive || settling || !TryFindDistance(target, exact, out int steps)) return false;
        settling = true;
        try
        {
            for (int i = 0; i < steps && IsActive; i++)
                time.AdvanceOneMiniCore();
            return IsActive;
        }
        finally
        {
            try { Cancel(); }
            finally { settling = false; }
        }
    }

    // 從正式位置向前查。跨正式日與跨午夜是兩個不同的邊界，依每格的顯示日比較。
    private bool TryFindDistance(int target, bool exact, out int bestSteps)
    {
        bestSteps = -1;
        int bestDifference = int.MaxValue;
        int dayOffset = time.DayIndex - startDay;
        int phase = time.CurrentPhaseIndex;
        int slot = time.CurrentSlotInPhase;
        int mini = time.CurrentMiniInSlot;
        for (int steps = 0; dayOffset <= 1; steps++)
        {
            int displayOffset = dayOffset + (mapping.ShouldShowNextDay(phase, slot) ? 1 : 0);
            if (displayOffset >= 0 && displayOffset <= 1 &&
                mapping.TryGetClockTime(phase, slot, mini, out int hour, out int minute))
            {
                int candidate = displayOffset * MinutesPerDay + hour * 60 + minute;
                int difference = Math.Abs(candidate - target);
                if (difference == 0)
                {
                    bestSteps = steps;
                    return true;
                }
                // 等距時保留沿正式流程先遇到的格子。
                if (!exact && difference < bestDifference)
                {
                    bestDifference = difference;
                    bestSteps = steps;
                }
            }

            if (++mini < config.GetMinisInSlot(phase, slot)) continue;
            mini = 0;
            if (++slot <= config.GetTotalSlots(phase)) continue;
            slot = 1;
            if (++phase < config.PhaseNames.Count) continue;
            phase = 0;
            dayOffset++;
        }
        return bestSteps >= 0;
    }

    public void Cancel()
    {
        if (!IsActive) return;
        time.BulletTimeActive = false;
        mapping = null;
        DisplayChanged?.Invoke();
    }
}
