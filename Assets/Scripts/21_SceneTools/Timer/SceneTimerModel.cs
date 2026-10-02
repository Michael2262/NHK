using System;
using System.Collections.Generic;

public enum TimerStartMode
{
    Interrupt = 0,
    Skip = 1,
    Queue = 2,
    Priority = 3
}

/// <summary>場景內的倒數與佇列。不依賴 Unity、不存檔，由 Controller 推進時間。</summary>
public sealed class SceneTimerModel
{
    /// <summary>獨立的唯讀狀態快照；取得快照不會推進時間或變更佇列。</summary>
    public sealed class TimerSnapshot
    {
        public string CurrentID { get; }
        public double Remaining { get; }
        public float Duration { get; }
        public bool Paused { get; }
        public IReadOnlyList<string> QueuedIDs { get; }
        public bool IsIdle => CurrentID == null && QueuedIDs.Count == 0;

        internal TimerSnapshot(string currentID, double remaining, float duration, bool paused, string[] queuedIDs)
        {
            CurrentID = currentID;
            Remaining = Math.Max(0d, remaining);
            Duration = duration;
            Paused = paused;
            QueuedIDs = Array.AsReadOnly(queuedIDs);
        }
    }

    public bool TryGetTimerSnapshot(TimerId id, out TimerSnapshot snapshot)
    {
        snapshot = null;
        if (!timers.TryGetValue(id, out var timer)) return false;
        var queuedIDs = new string[timer.Queue.Count];
        for (int i = 0; i < queuedIDs.Length; i++) queuedIDs[i] = timer.Queue[i].Id;
        snapshot = new TimerSnapshot(timer.Current?.Behavior.Id, timer.Current?.Remaining ?? 0d,
            timer.Current?.Behavior.Duration ?? 0f, timer.Paused, queuedIDs);
        return true;
    }

    public sealed class Behavior
    {
        public readonly string Id;
        public readonly TimerId Timer;
        public readonly float Duration;
        public readonly TimerStartMode Mode;
        public readonly Action Completed;

        public Behavior(string id, TimerId timer, float duration, TimerStartMode mode, Action completed)
        {
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim())
                throw new ArgumentException("行為 ID 不可空白或含前後空格。");
            if (!Enum.IsDefined(typeof(TimerId), timer))
                throw new ArgumentException("未知的計時器。");
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
                throw new ArgumentException("倒數秒數必須為有限的正數。");
            if (!Enum.IsDefined(typeof(TimerStartMode), mode))
                throw new ArgumentException("未知的啟動方式。");
            Id = id;
            Timer = timer;
            Duration = duration;
            Mode = mode;
            Completed = completed;
        }
    }

    private sealed class Run
    {
        public readonly Behavior Behavior;
        public double Remaining;
        public Run(Behavior behavior) { Behavior = behavior; Remaining = behavior.Duration; }
    }

    private sealed class Timer
    {
        public Run Current;
        public bool Paused;
        public readonly List<Behavior> Queue = new List<Behavior>();
    }

    private readonly Dictionary<string, Behavior> behaviors = new Dictionary<string, Behavior>(StringComparer.Ordinal);
    private readonly Dictionary<TimerId, Timer> timers = new Dictionary<TimerId, Timer>();
    private bool ticking;

    public SceneTimerModel(IEnumerable<Behavior> definitions)
    {
        foreach (TimerId id in Enum.GetValues(typeof(TimerId)))
            if (!timers.ContainsKey(id)) timers.Add(id, new Timer());
        foreach (Behavior behavior in definitions)
        {
            if (behavior == null || behaviors.ContainsKey(behavior.Id))
                throw new ArgumentException("行為設定不可為空，且 ID 不可重複。");
            behaviors.Add(behavior.Id, behavior);
        }
    }

    public bool ContainsID(string id) { return id != null && behaviors.ContainsKey(id); }

    /// <summary>閒置才為 true；暫停中的工作仍佔用計時器。</summary>
    public bool CheckTimer(TimerId id = TimerId.Main)
    {
        Timer timer;
        return timers.TryGetValue(id, out timer) && timer.Current == null && timer.Queue.Count == 0;
    }

    public bool CheckID(string id)
    {
        Behavior behavior;
        if (id == null || !behaviors.TryGetValue(id, out behavior)) return false;
        Timer timer = timers[behavior.Timer];
        return (timer.Current != null && timer.Current.Behavior == behavior) || timer.Queue.Contains(behavior);
    }

    /// <summary>有效啟動請求先恢復所屬計時器；相同 ID 不重複執行或排隊。</summary>
    public void StartTimer(string id)
    {
        Behavior behavior;
        if (id == null || !behaviors.TryGetValue(id, out behavior)) return;
        Timer timer = timers[behavior.Timer];
        timer.Paused = false;
        if (CheckID(id)) return;
        if (timer.Current == null) timer.Current = new Run(behavior);
        else
        {
            switch (behavior.Mode)
            {
                case TimerStartMode.Interrupt: timer.Current = new Run(behavior); break;
                case TimerStartMode.Queue: timer.Queue.Add(behavior); break;
                case TimerStartMode.Priority: timer.Queue.Insert(0, behavior); break;
                case TimerStartMode.Skip: break;
            }
        }
    }

    public void PauseTimer(TimerId id = TimerId.Main)
    {
        Timer timer;
        if (timers.TryGetValue(id, out timer)) timer.Paused = true;
    }

    public void CancelTimer(TimerId id = TimerId.Main)
    {
        Timer timer;
        if (!timers.TryGetValue(id, out timer)) return;
        timer.Current = null;
        timer.Queue.Clear();
        timer.Paused = false;
    }

    public void CancelAllTimers()
    {
        foreach (TimerId id in timers.Keys) CancelTimer(id);
    }

    public void Tick(float deltaTime)
    {
        if (ticking || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
        ticking = true;
        try
        {
            // 先扣除所有既有工作的時間，再派送事件，避免事件新建的工作被扣到本幀時間。
            var due = new List<KeyValuePair<Timer, Run>>();
            foreach (Timer timer in timers.Values)
            {
                if (timer.Paused || timer.Current == null) continue;
                timer.Current.Remaining -= deltaTime;
                if (timer.Current.Remaining <= 0d)
                    due.Add(new KeyValuePair<Timer, Run>(timer, timer.Current));
            }
            foreach (var item in due)
            {
                Timer timer = item.Key;
                if (timer.Paused || timer.Current != item.Value) continue;
                timer.Current = null;
                try { item.Value.Behavior.Completed?.Invoke(); }
                finally { StartNext(timer); }
            }
            // 完成事件中暫停的空閒計時器，恢復後也能繼續佇列。
            foreach (Timer timer in timers.Values) StartNext(timer);
        }
        finally { ticking = false; }
    }

    private static void StartNext(Timer timer)
    {
        if (timer.Paused || timer.Current != null || timer.Queue.Count == 0) return;
        Behavior next = timer.Queue[0];
        timer.Queue.RemoveAt(0);
        timer.Current = new Run(next);
    }
}
