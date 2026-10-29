using System;
using UnityEngine;

/// <summary>時間流逝演出排程。由服務餵入實際經過秒數，真正的時間規則仍由 TimeSystemModel 處理。</summary>
public sealed class TimePlaybackModel
{
    private readonly TimeSystemModel time;
    private readonly TimeConfig config;
    private int remaining;
    private int startingDay;
    private float interval;
    private float elapsed;
    private int generation;
    private bool ending;
    private Action<bool> callback;

    public bool IsTimeFlowing { get; private set; }
    public int RemainingMinis => remaining;
    public bool StopsAtNextDay { get; private set; } = true;
    public event Action OnTimeFlowStarted;
    // 取消也通知結束，以解除阻擋；只有正常完成才會發 OnTimeFlowCompleted。
    public event Action<bool> OnTimeFlowEnded;
    public event Action OnTimeFlowCompleted;

    public TimePlaybackModel(TimeSystemModel time, TimeConfig config)
    {
        this.time = time;
        this.config = config;
    }

    /// <summary>拒絕重疊播放。回傳是否接受；callback 在完成／取消時分別收到 true／false。</summary>
    public bool PlayAdvanceMinis(int minis, Action<bool> onFinished = null, bool withinDay = false,
        bool stopAtNextDay = true)
    {
        if (IsTimeFlowing || ending || minis < 0 ||
            (withinDay && !time.CanAdvanceMinisWithinDay(minis))) return false;
        if (minis == 0)
        {
            onFinished?.Invoke(true);
            return true;
        }
        float seconds = config.MiniPlaybackSeconds;
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) return false;
        remaining = minis;
        StopsAtNextDay = stopAtNextDay;
        startingDay = time.DayIndex;
        interval = seconds;
        elapsed = seconds; // 下一個 Update 推進第一個 Mini，之後每格停留指定秒數。
        callback = onFinished;
        generation++;
        IsTimeFlowing = true;
        Notify(OnTimeFlowStarted);
        return true;
    }

    public bool PlayAdvanceShortTime(Action<bool> onFinished = null)
        => PlayAdvanceMinis(Math.Max(1, config.ShortTimeMinis), onFinished);

    public bool PlayAdvanceMediumTime(Action<bool> onFinished = null)
        => PlayAdvanceMinis(Math.Max(1, config.MediumTimeMinis), onFinished);

    public bool PlayAdvanceLongTime(Action<bool> onFinished = null)
        => PlayAdvanceMinis(Math.Max(1, config.LongTimeMinis), onFinished);

    /// <summary>逐 Mini 播到往後第 slots 個 Slot 起點，不播放轉場。</summary>
    public bool PlaySlot(int slots = 1, Action<bool> onFinished = null)
        => time.TryGetMinisForSlotAdvance(slots, out int minis) &&
            PlayAdvanceMinis(minis, onFinished, stopAtNextDay: false);

    /// <summary>逐 Mini 播到往後第 phases 個 Phase 起點，不播放轉場。</summary>
    public bool PlayPhase(int phases = 1, Action<bool> onFinished = null)
        => time.TryGetMinisForPhaseAdvance(phases, out int minis) &&
            PlayAdvanceMinis(minis, onFinished, stopAtNextDay: false);

    /// <summary>逐 Mini 播到往後第 days 天起點，每次跨日照常結算，不播放轉場。</summary>
    public bool PlayDay(int days = 1, Action<bool> onFinished = null)
        => time.TryGetMinisForDayAdvance(days, out int minis) &&
            PlayAdvanceMinis(minis, onFinished, stopAtNextDay: false);

    /// <summary>一個畫面最多推進一格，不在卡頓時把多格一起跳過；最後一格也停留完整間隔。</summary>
    public void Tick(float unscaledDeltaTime)
    {
        if (!IsTimeFlowing) return;
        elapsed += Math.Max(0, unscaledDeltaTime);
        if (elapsed < interval) return;
        elapsed = 0;
        if (remaining == 0)
        {
            End(true);
            return;
        }
        int version = generation;
        try
        {
            time.AdvanceMinis(1);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Cancel();
            return;
        }
        // 時間事件可能導致讀檔或取消，舊排程不能再改動新排程。
        if (!IsTimeFlowing || generation != version) return;
        remaining--;
        // 舊 PlayAdvance 系列跨日即停；指定 Slot／Phase／Day 落點的播放繼續到目標。
        if (StopsAtNextDay && time.DayIndex != startingDay) remaining = 0;
    }

    /// <summary>停在已到達的位置，不回復時間，也不觸發到達事件。</summary>
    public void Cancel()
    {
        if (IsTimeFlowing) End(false);
    }

    private void End(bool completed)
    {
        IsTimeFlowing = false;
        remaining = 0;
        generation++;
        var finished = callback;
        callback = null;
        ending = true;
        try
        {
            // 所有結束 UnityEvent 先執行完，再讓 TimeLoopTrigger 檢查最終位置。
            Notify(OnTimeFlowEnded, completed);
            if (completed) Notify(OnTimeFlowCompleted);
        }
        finally
        {
            ending = false;
        }
        Notify(finished, completed);
    }

    private static void Notify(Action handlers)
    {
        if (handlers == null) return;
        foreach (Action handler in handlers.GetInvocationList())
        {
            try { handler(); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
    }

    private static void Notify(Action<bool> handlers, bool value)
    {
        if (handlers == null) return;
        foreach (Action<bool> handler in handlers.GetInvocationList())
        {
            try { handler(value); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
    }
}
