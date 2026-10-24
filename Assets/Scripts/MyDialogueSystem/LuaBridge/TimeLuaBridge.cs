using System;
using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// 將 TimeSystemModel 的日期、時段與 Mini 查詢／操作橋接到 Dialogue System 的 Lua 環境。
/// 查詢用於 Conditions，推進用於 Script。掛載於 GameStatusService 同一個 GameObject 上。
///
/// 註：「假日 / 平日」沿用遊戲既有定義（TimeSystemModel.IsWeekend），
/// 目前定義為星期六、星期日是假日。
/// </summary>
public class TimeLuaBridge : MonoBehaviour
{
    void OnEnable()
    {
        RegisterLuaFunctions();
    }

    void OnDisable()
    {
        UnregisterLuaFunctions();
    }

    // ==========================================================
    // Lua 函數註冊 / 取消註冊
    // ==========================================================

    private void RegisterLuaFunctions()
    {
        Lua.RegisterFunction("GetDay", this, typeof(TimeLuaBridge).GetMethod("GetDay"));
        Lua.RegisterFunction("DayOfWeek", this, typeof(TimeLuaBridge).GetMethod("GetDayOfWeek"));
        Lua.RegisterFunction("IsDayOfWeek", this, typeof(TimeLuaBridge).GetMethod("IsDayOfWeek"));
        Lua.RegisterFunction("IsWeekend", this, typeof(TimeLuaBridge).GetMethod("IsWeekend"));
        Lua.RegisterFunction("IsWeekday", this, typeof(TimeLuaBridge).GetMethod("IsWeekday"));
        Lua.RegisterFunction("GetPhase", this, typeof(TimeLuaBridge).GetMethod("GetPhase"));
        Lua.RegisterFunction("IsPhase", this, typeof(TimeLuaBridge).GetMethod("IsPhase"));
        Lua.RegisterFunction("GetSlot", this, typeof(TimeLuaBridge).GetMethod("GetSlot"));
        Lua.RegisterFunction(nameof(GetMini), this, typeof(TimeLuaBridge).GetMethod(nameof(GetMini)));
        Lua.RegisterFunction(nameof(GetMinisInCurrentSlot), this, typeof(TimeLuaBridge).GetMethod(nameof(GetMinisInCurrentSlot)));
        Lua.RegisterFunction(nameof(GetRemainingMinisInDay), this, typeof(TimeLuaBridge).GetMethod(nameof(GetRemainingMinisInDay)));
        Lua.RegisterFunction(nameof(CanAdvanceMinisWithinDay), this, typeof(TimeLuaBridge).GetMethod(nameof(CanAdvanceMinisWithinDay)));
        Lua.RegisterFunction(nameof(AdvanceMinis), this, typeof(TimeLuaBridge).GetMethod(nameof(AdvanceMinis)));
        Lua.RegisterFunction(nameof(AdvanceMinisWithinDay), this, typeof(TimeLuaBridge).GetMethod(nameof(AdvanceMinisWithinDay)));
        Lua.RegisterFunction(nameof(AdvanceShortTime), this, typeof(TimeLuaBridge).GetMethod(nameof(AdvanceShortTime)));
        Lua.RegisterFunction(nameof(AdvanceMediumTime), this, typeof(TimeLuaBridge).GetMethod(nameof(AdvanceMediumTime)));
        Lua.RegisterFunction(nameof(AdvanceLongTime), this, typeof(TimeLuaBridge).GetMethod(nameof(AdvanceLongTime)));
        Lua.RegisterFunction(nameof(PlayAdvanceMinis), this, typeof(TimeLuaBridge).GetMethod(nameof(PlayAdvanceMinis)));
        Lua.RegisterFunction(nameof(PlayAdvanceMinisWithinDay), this, typeof(TimeLuaBridge).GetMethod(nameof(PlayAdvanceMinisWithinDay)));
        Lua.RegisterFunction(nameof(PlayAdvanceShortTime), this, typeof(TimeLuaBridge).GetMethod(nameof(PlayAdvanceShortTime)));
        Lua.RegisterFunction(nameof(PlayAdvanceMediumTime), this, typeof(TimeLuaBridge).GetMethod(nameof(PlayAdvanceMediumTime)));
        Lua.RegisterFunction(nameof(PlayAdvanceLongTime), this, typeof(TimeLuaBridge).GetMethod(nameof(PlayAdvanceLongTime)));
        Lua.RegisterFunction(nameof(IsTimeFlowing), this, typeof(TimeLuaBridge).GetMethod(nameof(IsTimeFlowing)));
        Lua.RegisterFunction(nameof(CancelTimeFlow), this, typeof(TimeLuaBridge).GetMethod(nameof(CancelTimeFlow)));

        Debug.Log("TimeLuaBridge: Lua functions registered.");
    }

    private void UnregisterLuaFunctions()
    {
        Lua.UnregisterFunction("GetDay");
        Lua.UnregisterFunction("DayOfWeek");
        Lua.UnregisterFunction("IsDayOfWeek");
        Lua.UnregisterFunction("IsWeekend");
        Lua.UnregisterFunction("IsWeekday");
        Lua.UnregisterFunction("GetPhase");
        Lua.UnregisterFunction("IsPhase");
        Lua.UnregisterFunction("GetSlot");
        Lua.UnregisterFunction(nameof(GetMini));
        Lua.UnregisterFunction(nameof(GetMinisInCurrentSlot));
        Lua.UnregisterFunction(nameof(GetRemainingMinisInDay));
        Lua.UnregisterFunction(nameof(CanAdvanceMinisWithinDay));
        Lua.UnregisterFunction(nameof(AdvanceMinis));
        Lua.UnregisterFunction(nameof(AdvanceMinisWithinDay));
        Lua.UnregisterFunction(nameof(AdvanceShortTime));
        Lua.UnregisterFunction(nameof(AdvanceMediumTime));
        Lua.UnregisterFunction(nameof(AdvanceLongTime));
        Lua.UnregisterFunction(nameof(PlayAdvanceMinis));
        Lua.UnregisterFunction(nameof(PlayAdvanceMinisWithinDay));
        Lua.UnregisterFunction(nameof(PlayAdvanceShortTime));
        Lua.UnregisterFunction(nameof(PlayAdvanceMediumTime));
        Lua.UnregisterFunction(nameof(PlayAdvanceLongTime));
        Lua.UnregisterFunction(nameof(IsTimeFlowing));
        Lua.UnregisterFunction(nameof(CancelTimeFlow));

        Debug.Log("TimeLuaBridge: Lua functions unregistered.");
    }

    // ==========================================================
    // Mini 相關 Lua 函數
    // ==========================================================

    private TimeSystemModel MiniModel
    {
        get
        {
            var model = GameStatusService.Instance?.Time;
            if (model == null) Debug.LogWarning("[TimeLuaBridge] 時間系統尚未初始化。");
            return model;
        }
    }

    // Lua 的 Play 函數只回傳是否接受，不阻塞對話。需要等待完成時使用 Sequencer Play 指令。
    public bool PlayAdvanceMinis(double minis)
    {
        return TryParseMiniCount(minis, out int count) &&
            (GameStatusService.Instance?.TimePlayback?.PlayAdvanceMinis(count) ?? false);
    }

    public bool PlayAdvanceMinisWithinDay(double minis)
    {
        return TryParseMiniCount(minis, out int count) &&
            (GameStatusService.Instance?.TimePlayback?.PlayAdvanceMinis(count, withinDay: true) ?? false);
    }

    public bool PlayAdvanceShortTime() => GameStatusService.Instance?.TimePlayback?.PlayAdvanceShortTime() ?? false;
    public bool PlayAdvanceMediumTime() => GameStatusService.Instance?.TimePlayback?.PlayAdvanceMediumTime() ?? false;
    public bool PlayAdvanceLongTime() => GameStatusService.Instance?.TimePlayback?.PlayAdvanceLongTime() ?? false;
    public bool IsTimeFlowing() => GameStatusService.Instance?.TimePlayback?.IsTimeFlowing ?? false;
    public void CancelTimeFlow() => GameStatusService.Instance?.TimePlayback?.Cancel();

    // Lua 數字為 double，拒絕小數、負數、非有限值及超出 int 範圍的數量。
    private static bool TryParseMiniCount(double value, out int count)
    {
        count = 0;
        if (double.IsNaN(value) || double.IsInfinity(value) ||
            value < 0 || value > int.MaxValue || value != Math.Truncate(value))
        {
            Debug.LogWarning("[TimeLuaBridge] Mini 數量必須是 int 範圍內的非負整數。");
            return false;
        }
        count = (int)value;
        return true;
    }

    /// <summary>目前 Slot 已推進的 Mini 數量，從 0 開始；例：GetMini() >= 3。</summary>
    public double GetMini() => MiniModel?.CurrentMiniInSlot ?? 0;
    public double GetMinisInCurrentSlot() => MiniModel?.MinisInCurrentSlot ?? 0;
    public double GetRemainingMinisInDay() => MiniModel?.GetRemainingMinisInDay() ?? 0;

    /// <summary>唯讀條件：例 CanAdvanceMinisWithinDay(3)。</summary>
    public bool CanAdvanceMinisWithinDay(double minis)
    {
        if (!TryParseMiniCount(minis, out int count)) return false;
        return MiniModel?.CanAdvanceMinisWithinDay(count) ?? false;
    }

    /// <summary>Script：AdvanceMinis(3)。跨日後停在隔天起點；true 表示指令已接受。</summary>
    public bool AdvanceMinis(double minis)
    {
        if (!TryParseMiniCount(minis, out int count)) return false;
        var model = MiniModel;
        if (model == null) return false;
        model.AdvanceMinis(count);
        return true;
    }

    /// <summary>Script：AdvanceMinisWithinDay(3)。不足時不推進並回傳 false。</summary>
    public bool AdvanceMinisWithinDay(double minis)
    {
        if (!TryParseMiniCount(minis, out int count)) return false;
        return MiniModel?.TryAdvanceMinis(count) ?? false;
    }

    /// <summary>Script：AdvanceShortTime()。由 Config 決定數量，跨日後停在隔天起點。</summary>
    public bool AdvanceShortTime()
    {
        var model = MiniModel;
        if (model == null) return false;
        model.AdvanceShortTime();
        return true;
    }

    /// <summary>Script：AdvanceMediumTime()。由 Config 決定數量，跨日後停在隔天起點。</summary>
    public bool AdvanceMediumTime()
    {
        var model = MiniModel;
        if (model == null) return false;
        model.AdvanceMediumTime();
        return true;
    }

    /// <summary>Script：AdvanceLongTime()。由 Config 決定數量，跨日後停在隔天起點。</summary>
    public bool AdvanceLongTime()
    {
        var model = MiniModel;
        if (model == null) return false;
        model.AdvanceLongTime();
        return true;
    }

    // ==========================================================
    // 日期 / 星期相關 Lua 函數
    // ==========================================================

    /// <summary>
    /// 取得目前是遊戲第幾天（TimeSystemModel.DayIndex）。
    /// Lua 用法: GetDay() >= 5  → 已到第 5 天以後
    /// </summary>
    public double GetDay()
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("TimeLuaBridge: GameStatusService not available.");
            return 1;
        }
        return GameStatusService.Instance.Time.DayIndex;
    }

    /// <summary>
    /// 取得今天星期幾的數字（對齊 System.DayOfWeek：0=週日, 1=週一 … 6=週六）。
    /// Lua 用法: DayOfWeek() == 1  → 今天是星期一
    /// </summary>
    public double GetDayOfWeek()
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("TimeLuaBridge: GameStatusService not available.");
            return 0;
        }
        return (int)GameStatusService.Instance.Time.CurrentDayOfWeek;
    }

    /// <summary>
    /// 判斷今天是否為指定星期。可傳英文名稱（"Monday"）或數字字串（"1"）。
    /// Lua 用法: IsDayOfWeek("Monday") 或 IsDayOfWeek("1")
    /// </summary>
    public bool IsDayOfWeek(string day)
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("TimeLuaBridge: GameStatusService not available.");
            return false;
        }

        DayOfWeek today = GameStatusService.Instance.Time.CurrentDayOfWeek;

        if (int.TryParse(day, out int dayNum))
            return (int)today == dayNum;

        if (Enum.TryParse(day, true, out DayOfWeek target))
            return today == target;

        Debug.LogWarning($"TimeLuaBridge: 無法解析星期參數 \"{day}\"。可用：Sunday~Saturday 或 0~6。");
        return false;
    }

    /// <summary>
    /// 今天是否為假日（星期六、星期日）。
    /// Lua 用法: IsWeekend()
    /// </summary>
    public bool IsWeekend()
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("TimeLuaBridge: GameStatusService not available.");
            return false;
        }
        return GameStatusService.Instance.Time.IsWeekend;
    }

    /// <summary>
    /// 今天是否為平日（= 非假日）。
    /// Lua 用法: IsWeekday()
    /// </summary>
    public bool IsWeekday()
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("TimeLuaBridge: GameStatusService not available.");
            return false;
        }
        return !GameStatusService.Instance.Time.IsWeekend;
    }

    // ==========================================================
    // Phase / Slot 相關 Lua 函數
    // ==========================================================

    /// <summary>
    /// 取得目前 Phase 索引（0=白天, 1=黃昏, 2=晚上, 3=深夜，依 TimeConfig.PhaseNames 而定）。
    /// Lua 用法: GetPhase() == 2  → 目前是晚上
    /// </summary>
    public double GetPhase()
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("TimeLuaBridge: GameStatusService not available.");
            return 0;
        }
        return GameStatusService.Instance.Time.CurrentPhaseIndex;
    }

    /// <summary>
    /// 判斷目前是否為指定 Phase。可傳數字字串（"2"）或 TimeConfig.PhaseNames 中的名稱（"晚上"）。
    /// Lua 用法: IsPhase("晚上") 或 IsPhase("2")
    /// </summary>
    public bool IsPhase(string phase)
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("TimeLuaBridge: GameStatusService not available.");
            return false;
        }

        int current = GameStatusService.Instance.Time.CurrentPhaseIndex;

        if (int.TryParse(phase, out int phaseNum))
            return current == phaseNum;

        var config = GameStatusService.Instance.TimeConfig;
        if (config != null)
        {
            int index = config.PhaseNames.IndexOf(phase);
            if (index >= 0)
                return current == index;
        }

        Debug.LogWarning($"TimeLuaBridge: 無法解析 Phase 參數 \"{phase}\"。可用：0~3 或 TimeConfig.PhaseNames 中的名稱。");
        return false;
    }

    /// <summary>
    /// 取得目前是當前 Phase 的第幾個 Slot（從 1 開始）。
    /// Lua 用法: GetSlot() == 1  → 剛進入這個 Phase 的第一格
    /// </summary>
    public double GetSlot()
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("TimeLuaBridge: GameStatusService not available.");
            return 1;
        }
        return GameStatusService.Instance.Time.CurrentSlotInPhase;
    }
}
