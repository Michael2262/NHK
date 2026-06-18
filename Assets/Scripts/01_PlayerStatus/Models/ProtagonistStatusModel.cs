using System;



/// <summary>
/// 一次性套用主角數值變化用的資料結構。
/// </summary>
public struct ProtagonistStatusChange
{
    public int stressDelta;
    public int lifePowerDelta;
    public int socialityDelta;
    public int dependencyDelta;

    public ProtagonistStatusChange(int stressDelta, int lifePowerDelta, int socialityDelta, int dependencyDelta)
    {
        this.stressDelta = stressDelta;
        this.lifePowerDelta = lifePowerDelta;
        this.socialityDelta = socialityDelta;
        this.dependencyDelta = dependencyDelta;
    }
}

/// <summary>
/// 職責：負責儲存與管理「主角自身」的核心數值。
/// Day 由 TimeSystemModel.DayIndex 統一管理，本 Model 不再持有。
/// </summary>
public class ProtagonistStatusModel
{
    // ───── 初始值 ─────
    public const int INITIAL_STRESS = 40;
    public const int INITIAL_LIFE_POWER = 0;
    public const int INITIAL_SOCIALITY = 0;
    public const int INITIAL_DEPENDENCY = 0;
    public const int INITIAL_MONEY = 0;
    public const int INITIAL_SKILL_POINTS = 0;

    // ───── ShootTimes ─────
    public const int SHOOT_TIMES_MAX = 3;
    public const int SHOOT_TIMES_MIN = -5;
    public const int INITIAL_SHOOT_TIMES = SHOOT_TIMES_MAX;

    // ───── RoomMessLevel（房間髒亂度） ─────
    public const int ROOM_MESS_LEVEL_MAX = 10;
    public const int ROOM_MESS_LEVEL_MIN = 0;
    public const int INITIAL_ROOM_MESS_LEVEL = ROOM_MESS_LEVEL_MAX;

    // ───── 數值範圍 ─────
    public const int MIN_STATUS_VALUE = 0;
    public const int MAX_STATUS_VALUE = 100;

    // ───── 分級門檻（各數值可獨立調整，預設一致） ─────
    // Stress
    public const int STRESS_MEDIUM_THRESHOLD = 50;
    public const int STRESS_HIGH_THRESHOLD = 80;
    public const int STRESS_EXTREME_THRESHOLD = 100;

    // LifePower
    public const int LIFE_MEDIUM_THRESHOLD = 50;
    public const int LIFE_HIGH_THRESHOLD = 80;
    public const int LIFE_EXTREME_THRESHOLD = 100;

    // Sociality
    public const int SOCIALITY_MEDIUM_THRESHOLD = 50;
    public const int SOCIALITY_HIGH_THRESHOLD = 80;
    public const int SOCIALITY_EXTREME_THRESHOLD = 100;

    // Dependency
    public const int DEPENDENCY_MEDIUM_THRESHOLD = 50;
    public const int DEPENDENCY_HIGH_THRESHOLD = 80;
    public const int DEPENDENCY_EXTREME_THRESHOLD = 100;

    // ───── 核心狀態 ─────

    /// <summary>壓力：0～100。越高越危險。</summary>
    public int Stress { get; private set; } = INITIAL_STRESS;

    /// <summary>生活力：0～100。越高代表越能維持正常生活。</summary>
    public int LifePower { get; private set; } = INITIAL_LIFE_POWER;

    /// <summary>社會性：0～100。越高代表越能面對外界與正常社交。</summary>
    public int Sociality { get; private set; } = INITIAL_SOCIALITY;

    /// <summary>依賴度：0～100。主角對妹妹的心理與生活依賴。</summary>
    public int Dependency { get; private set; } = INITIAL_DEPENDENCY;

    /// <summary>保留資源：金錢。</summary>
    public int Money { get; private set; } = INITIAL_MONEY;

    /// <summary>保留資源：技能點。</summary>
    public int SkillPoints { get; private set; } = INITIAL_SKILL_POINTS;

    /// <summary>射精次數。每日重製回 Max。可加超過 Max，但不可扣低於 Min。<= 0 時為耗盡狀態。</summary>
    public int ShootTimes { get; private set; } = INITIAL_SHOOT_TIMES;

    /// <summary>射精耗盡狀態：ShootTimes <= 0 即為 true。</summary>
    public bool IsOverShoot => ShootTimes <= 0;

    /// <summary>房間髒亂度：0～10。每日結束自動 +1。越高代表房間越亂。</summary>
    public int RoomMessLevel { get; private set; } = INITIAL_ROOM_MESS_LEVEL;

    // ───── 事件通知 ─────
    public event Action<int> OnStressChanged;       // delta
    public event Action<int> OnLifePowerChanged;    // delta
    public event Action<int> OnSocialityChanged;    // delta
    public event Action<int> OnDependencyChanged;   // delta
    public event Action<int> OnMoneyChanged;        // delta
    public event Action<int> OnSkillPointsChanged;  // delta
    public event Action<int> OnShootTimesChanged;   // delta
    public event Action<int> OnRoomMessLevelChanged; // delta

    public event Action<StatusGrade, StatusGrade> OnStressGradeChanged;
    public event Action<StatusGrade, StatusGrade> OnLifeGradeChanged;
    public event Action<StatusGrade, StatusGrade> OnSocialityGradeChanged;
    public event Action<StatusGrade, StatusGrade> OnDependencyGradeChanged;

    // ───── 初始化 / 存讀檔 ─────
    public void NewGame()
    {
        Stress = INITIAL_STRESS;
        LifePower = INITIAL_LIFE_POWER;
        Sociality = INITIAL_SOCIALITY;
        Dependency = INITIAL_DEPENDENCY;
        Money = INITIAL_MONEY;
        SkillPoints = INITIAL_SKILL_POINTS;
        ShootTimes = INITIAL_SHOOT_TIMES;
        RoomMessLevel = INITIAL_ROOM_MESS_LEVEL;

        NotifyAllCoreValues();
    }

    public ProtagonistSaveData ToSaveData()
    {
        return new ProtagonistSaveData
        {
            Stress = Stress,
            LifePower = LifePower,
            Sociality = Sociality,
            Dependency = Dependency,
            Money = Money,
            SkillPoints = SkillPoints,
            ShootTimes = ShootTimes,
            RoomMessLevel = RoomMessLevel
        };
    }

    public void LoadFromSaveData(ProtagonistSaveData data)
    {
        if (data == null)
        {
            NewGame();
            return;
        }

        Stress = ClampStatus(data.Stress);
        LifePower = ClampStatus(data.LifePower);
        Sociality = ClampStatus(data.Sociality);
        Dependency = ClampStatus(data.Dependency);
        Money = Math.Max(0, data.Money);
        SkillPoints = Math.Max(0, data.SkillPoints);
        ShootTimes = Math.Max(SHOOT_TIMES_MIN, Math.Min(data.ShootTimes, int.MaxValue));
        RoomMessLevel = ClampRoomMessLevel(data.RoomMessLevel);

        NotifyAllCoreValues();
    }

    private void NotifyAllCoreValues()
    {
        OnStressChanged?.Invoke(0);
        OnLifePowerChanged?.Invoke(0);
        OnSocialityChanged?.Invoke(0);
        OnDependencyChanged?.Invoke(0);
        OnMoneyChanged?.Invoke(0);
        OnSkillPointsChanged?.Invoke(0);
        OnShootTimesChanged?.Invoke(0);
        OnRoomMessLevelChanged?.Invoke(0);
    }

    // ───── 每日流程 ─────
    public void OnDayStart()
    {
        ResetShootTimes();
    }

    public void OnDayEnd()
    {
        AddRoomMessLevel(1);
    }

    public void NextDay()
    {
        OnDayEnd();
        // Day 由 TimeSystemModel.DayIndex 管理，這裡不再遞增。
        OnDayStart();
    }

    // ───── 主角核心數值方法 ─────
    public void ApplyStatusChange(ProtagonistStatusChange change)
    {
        AddStress(change.stressDelta);
        AddLifePower(change.lifePowerDelta);
        AddSociality(change.socialityDelta);
        AddDependency(change.dependencyDelta);
    }

    public void AddStress(int delta)
    {
        if (delta == 0) return;

        int prevValue = Stress;
        StatusGrade prevGrade = GetStressGrade();
        Stress = ClampStatus(Stress + delta);

        if (Stress == prevValue) return;

        OnStressChanged?.Invoke(Stress - prevValue);

        StatusGrade newGrade = GetStressGrade();
        if (newGrade != prevGrade)
            OnStressGradeChanged?.Invoke(prevGrade, newGrade);
    }

    public void ReduceStress(int delta)
    {
        if (delta <= 0) return;
        AddStress(-delta);
    }

    public void SetStress(int value)
    {
        AddStress(ClampStatus(value) - Stress);
    }

    public void AddLifePower(int delta)
    {
        if (delta == 0) return;

        int prevValue = LifePower;
        StatusGrade prevGrade = GetLifeGrade();
        LifePower = ClampStatus(LifePower + delta);

        if (LifePower == prevValue) return;

        OnLifePowerChanged?.Invoke(LifePower - prevValue);

        StatusGrade newGrade = GetLifeGrade();
        if (newGrade != prevGrade)
            OnLifeGradeChanged?.Invoke(prevGrade, newGrade);
    }

    public void ReduceLifePower(int delta)
    {
        if (delta <= 0) return;
        AddLifePower(-delta);
    }

    public void SetLifePower(int value)
    {
        AddLifePower(ClampStatus(value) - LifePower);
    }

    public void AddSociality(int delta)
    {
        if (delta == 0) return;

        int prevValue = Sociality;
        StatusGrade prevGrade = GetSocialityGrade();
        Sociality = ClampStatus(Sociality + delta);

        if (Sociality == prevValue) return;

        OnSocialityChanged?.Invoke(Sociality - prevValue);

        StatusGrade newGrade = GetSocialityGrade();
        if (newGrade != prevGrade)
            OnSocialityGradeChanged?.Invoke(prevGrade, newGrade);
    }

    public void ReduceSociality(int delta)
    {
        if (delta <= 0) return;
        AddSociality(-delta);
    }

    public void SetSociality(int value)
    {
        AddSociality(ClampStatus(value) - Sociality);
    }

    public void AddDependency(int delta)
    {
        if (delta == 0) return;

        int prevValue = Dependency;
        StatusGrade prevGrade = GetDependencyGrade();
        Dependency = ClampStatus(Dependency + delta);

        if (Dependency == prevValue) return;

        OnDependencyChanged?.Invoke(Dependency - prevValue);

        StatusGrade newGrade = GetDependencyGrade();
        if (newGrade != prevGrade)
            OnDependencyGradeChanged?.Invoke(prevGrade, newGrade);
    }

    public void ReduceDependency(int delta)
    {
        if (delta <= 0) return;
        AddDependency(-delta);
    }

    public void SetDependency(int value)
    {
        AddDependency(ClampStatus(value) - Dependency);
    }

    // ───── Money / SkillPoints ─────
    public bool CanReduceMoney(int cost)
    {
        if (cost < 0) return false;
        if (cost == 0) return true;
        return Money >= cost;
    }

    public bool TryReduceMoney(int cost)
    {
        if (!CanReduceMoney(cost)) return false;
        AddMoney(-cost);
        return true;
    }

    public void AddMoney(int delta)
    {
        if (delta == 0) return;
        int prev = Money;
        Money = Math.Max(0, Money + delta);
        if (Money != prev) OnMoneyChanged?.Invoke(Money - prev);
    }

    public void SetMoney(int amount)
    {
        int prev = Money;
        Money = Math.Max(0, amount);
        if (Money != prev) OnMoneyChanged?.Invoke(Money - prev);
    }

    public void AddSkillPoints(int delta)
    {
        if (delta == 0) return;
        int prev = SkillPoints;
        SkillPoints = Math.Max(0, SkillPoints + delta);
        if (SkillPoints != prev) OnSkillPointsChanged?.Invoke(SkillPoints - prev);
    }

    public void ReduceSkillPoints(int delta)
    {
        if (delta <= 0) return;
        AddSkillPoints(-delta);
    }

    public bool CanReduceSkillPoints(int cost)
    {
        if (cost < 0) return false;
        if (cost == 0) return true;
        return SkillPoints >= cost;
    }

    public bool TryReduceSkillPoints(int cost)
    {
        if (!CanReduceSkillPoints(cost)) return false;
        AddSkillPoints(-cost);
        return true;
    }

    public void SetSkillPoints(int amount)
    {
        int prev = SkillPoints;
        SkillPoints = Math.Max(0, amount);
        if (SkillPoints != prev) OnSkillPointsChanged?.Invoke(SkillPoints - prev);
    }

    // ───── ShootTimes（射精次數） ─────

    /// <summary>查詢目前射精次數。</summary>
    public int CheckShootTimes() => ShootTimes;

    /// <summary>增加射精次數。可以超過 Max，無上限。</summary>
    public void AddShootTimes(int delta)
    {
        if (delta == 0) return;
        int prev = ShootTimes;
        ShootTimes += delta;
        if (ShootTimes != prev) OnShootTimesChanged?.Invoke(ShootTimes - prev);
    }

    /// <summary>
    /// 嘗試消耗射精次數（amount 為正數）。
    /// 若扣除後會低於 Min，回傳 false 且不扣除。
    /// </summary>
    public bool TryReduceShootTimes(int amount = 1)
    {
        if (amount <= 0) return true;
        if (ShootTimes - amount < SHOOT_TIMES_MIN) return false;

        int prev = ShootTimes;
        ShootTimes -= amount;
        if (ShootTimes != prev) OnShootTimesChanged?.Invoke(ShootTimes - prev);
        return true;
    }

    /// <summary>
    /// 重製射精次數回 Max。若目前超過 Max 也會被截回 Max。
    /// OnDayStart 會自動呼叫，也可由外部獨立呼叫。
    /// </summary>
    public void ResetShootTimes()
    {
        int prev = ShootTimes;
        ShootTimes = SHOOT_TIMES_MAX;
        if (ShootTimes != prev) OnShootTimesChanged?.Invoke(ShootTimes - prev);
    }

    // ───── RoomMessLevel（房間髒亂度） ─────

    /// <summary>查詢目前房間髒亂度。</summary>
    public int CheckRoomMessLevel() => RoomMessLevel;

    /// <summary>增加髒亂度。封頂於 Max，不會超過。</summary>
    public void AddRoomMessLevel(int delta)
    {
        if (delta == 0) return;
        int prev = RoomMessLevel;
        RoomMessLevel = ClampRoomMessLevel(RoomMessLevel + delta);
        if (RoomMessLevel != prev) OnRoomMessLevelChanged?.Invoke(RoomMessLevel - prev);
    }

    /// <summary>降低髒亂度（delta 為正數）。保底於 Min，不會低於。</summary>
    public void ReduceRoomMessLevel(int delta)
    {
        if (delta <= 0) return;
        AddRoomMessLevel(-delta);
    }

    /// <summary>直接設定髒亂度，會夾在 Min~Max。</summary>
    public void SetRoomMessLevel(int value)
    {
        int prev = RoomMessLevel;
        RoomMessLevel = ClampRoomMessLevel(value);
        if (RoomMessLevel != prev) OnRoomMessLevelChanged?.Invoke(RoomMessLevel - prev);
    }

    // ───── 分級查詢 ─────
    public StatusGrade GetStressGrade()
    {
        if (Stress >= STRESS_EXTREME_THRESHOLD) return StatusGrade.Extreme;
        if (Stress >= STRESS_HIGH_THRESHOLD) return StatusGrade.High;
        if (Stress >= STRESS_MEDIUM_THRESHOLD) return StatusGrade.Medium;
        return StatusGrade.Low;
    }

    public StatusGrade GetLifeGrade()
    {
        if (LifePower >= LIFE_EXTREME_THRESHOLD) return StatusGrade.Extreme;
        if (LifePower >= LIFE_HIGH_THRESHOLD) return StatusGrade.High;
        if (LifePower >= LIFE_MEDIUM_THRESHOLD) return StatusGrade.Medium;
        return StatusGrade.Low;
    }

    public StatusGrade GetSocialityGrade()
    {
        if (Sociality >= SOCIALITY_EXTREME_THRESHOLD) return StatusGrade.Extreme;
        if (Sociality >= SOCIALITY_HIGH_THRESHOLD) return StatusGrade.High;
        if (Sociality >= SOCIALITY_MEDIUM_THRESHOLD) return StatusGrade.Medium;
        return StatusGrade.Low;
    }

    public StatusGrade GetDependencyGrade()
    {
        if (Dependency >= DEPENDENCY_EXTREME_THRESHOLD) return StatusGrade.Extreme;
        if (Dependency >= DEPENDENCY_HIGH_THRESHOLD) return StatusGrade.High;
        if (Dependency >= DEPENDENCY_MEDIUM_THRESHOLD) return StatusGrade.Medium;
        return StatusGrade.Low;
    }

    // ───── Bool 快捷查詢 ─────
    public bool IsStressHigh() => Stress >= STRESS_HIGH_THRESHOLD;
    public bool IsStressExtreme() => Stress >= STRESS_EXTREME_THRESHOLD;

    public bool IsLifeLow() => LifePower < LIFE_MEDIUM_THRESHOLD;
    public bool IsLifeHigh() => LifePower >= LIFE_HIGH_THRESHOLD;

    public bool IsSocialityLow() => Sociality < SOCIALITY_MEDIUM_THRESHOLD;
    public bool IsSocialityHigh() => Sociality >= SOCIALITY_HIGH_THRESHOLD;

    public bool IsDependencyHigh() => Dependency >= DEPENDENCY_HIGH_THRESHOLD;
    public bool IsDependencyExtreme() => Dependency >= DEPENDENCY_EXTREME_THRESHOLD;

    // ───── 內部工具 ─────
    private static int ClampStatus(int value)
    {
        if (value < MIN_STATUS_VALUE) return MIN_STATUS_VALUE;
        if (value > MAX_STATUS_VALUE) return MAX_STATUS_VALUE;
        return value;
    }

    private static int ClampRoomMessLevel(int value)
    {
        if (value < ROOM_MESS_LEVEL_MIN) return ROOM_MESS_LEVEL_MIN;
        if (value > ROOM_MESS_LEVEL_MAX) return ROOM_MESS_LEVEL_MAX;
        return value;
    }
}