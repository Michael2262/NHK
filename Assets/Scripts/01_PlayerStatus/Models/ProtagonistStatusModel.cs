using System;

/// <summary>
/// 壓力狀態。用於 UI 顯示與事件條件判斷。
/// </summary>
public enum ProtagonistStressState
{
    Calm,       // 0～20：平穩
    Uneasy,     // 21～40：不安
    Irritated,  // 41～60：焦躁
    Strained,   // 61～79：快撐不住
    Critical,   // 80～99：危險
    Collapsed   // 100：崩潰
}

/// <summary>
/// 生活力狀態。用於 UI 顯示與事件條件判斷。
/// </summary>
public enum ProtagonistLifeState
{
    VeryLow,   // 0～25：極差
    Unstable,  // 26～49：不穩
    Stable,    // 50～69：穩定
    Healthy    // 70～100：健康
}

/// <summary>
/// 社會恐懼狀態。用於 UI 顯示與事件條件判斷。
/// </summary>
public enum ProtagonistSocialFearState
{
    Low,     // 0～40
    Medium,  // 41～69
    High     // 70～100
}

/// <summary>
/// 依賴度狀態。用於 UI 顯示與事件條件判斷。
/// </summary>
public enum ProtagonistDependencyState
{
    Low,      // 0～39
    Medium,   // 40～59
    High,     // 60～79
    Extreme   // 80～100
}

/// <summary>
/// 一次性套用主角數值變化用的資料結構。
/// </summary>
public struct ProtagonistStatusChange
{
    public int stressDelta;
    public int lifePowerDelta;
    public int socialFearDelta;
    public int dependencyDelta;

    public ProtagonistStatusChange(int stressDelta, int lifePowerDelta, int socialFearDelta, int dependencyDelta)
    {
        this.stressDelta = stressDelta;
        this.lifePowerDelta = lifePowerDelta;
        this.socialFearDelta = socialFearDelta;
        this.dependencyDelta = dependencyDelta;
    }
}

/// <summary>
/// 職責：負責儲存與管理「主角自身」的核心數值與行為統計。
/// NHK 版：保留舊類名，但核心改為壓力、生活力、社會恐懼、依賴度。
/// Money / SkillPoints 依需求保留。
/// </summary>
public class ProtagonistStatusModel
{
    // ───── 初始值 ─────
    public const int INITIAL_DAY = 1;
    public const int INITIAL_STRESS = 40;
    public const int INITIAL_LIFE_POWER = 5;
    public const int INITIAL_SOCIAL_FEAR = 70;
    public const int INITIAL_DEPENDENCY = 0;
    public const int INITIAL_MONEY = 0;
    public const int INITIAL_SKILL_POINTS = 0;

    // ───── 門檻 ─────
    public const int MIN_STATUS_VALUE = 0;
    public const int MAX_STATUS_VALUE = 100;

    public const int STRESS_UNEASY_THRESHOLD = 21;
    public const int STRESS_IRRITATED_THRESHOLD = 41;
    public const int STRESS_STRAINED_THRESHOLD = 61;
    public const int STRESS_CRITICAL_THRESHOLD = 80;
    public const int STRESS_COLLAPSE_THRESHOLD = 100;

    public const int LIFE_VERY_LOW_MAX = 25;
    public const int LIFE_STABLE_THRESHOLD = 50;
    public const int LIFE_HEALTHY_THRESHOLD = 70;

    public const int SOCIAL_FEAR_MEDIUM_THRESHOLD = 41;
    public const int SOCIAL_FEAR_HIGH_THRESHOLD = 70;

    public const int DEPENDENCY_MEDIUM_THRESHOLD = 40;
    public const int DEPENDENCY_HIGH_THRESHOLD = 60;
    public const int DEPENDENCY_EXTREME_THRESHOLD = 80;

    // ───── 核心狀態 ─────
    public int Day { get; private set; } = INITIAL_DAY;

    /// <summary>壓力：0～100。越高越危險，100 可觸發崩潰事件。</summary>
    public int Stress { get; private set; } = INITIAL_STRESS;

    /// <summary>生活力：0～100。越高代表越能維持正常生活。</summary>
    public int LifePower { get; private set; } = INITIAL_LIFE_POWER;

    /// <summary>社會恐懼：0～100。越高越害怕外界。</summary>
    public int SocialFear { get; private set; } = INITIAL_SOCIAL_FEAR;

    /// <summary>依賴度：0～100。主角對妹妹的心理與生活依賴。</summary>
    public int Dependency { get; private set; } = INITIAL_DEPENDENCY;

    /// <summary>保留資源：金錢。</summary>
    public int Money { get; private set; } = INITIAL_MONEY;

    /// <summary>保留資源：技能點。</summary>
    public int SkillPoints { get; private set; } = INITIAL_SKILL_POINTS;

    // ───── 每日狀態：每日開始時重置 ─────
    public bool HasBathedToday { get; private set; }
    public bool HasCleanedRoomToday { get; private set; }
    public bool HasHadMealToday { get; private set; }
    public bool HasCheckedMailToday { get; private set; }
    public bool HasRepliedFamilyToday { get; private set; }
    public bool HasIgnoredPhoneToday { get; private set; }
    public bool HasGoneOutsideToday { get; private set; }
    public bool SucceededGoingOutsideToday { get; private set; }
    public bool FailedGoingOutsideToday { get; private set; }
    public bool HasEscapedToday { get; private set; }
    public bool StressCollapsedToday { get; private set; }

    // ───── 長期統計 ─────
    public int CollapseCount { get; private set; }
    public int OutsideSuccessCount { get; private set; }
    public int OutsideFailCount { get; private set; }
    public int ConsecutiveNoBathDays { get; private set; }
    public int ConsecutiveEscapeDays { get; private set; }
    public int DaysImprovedLife { get; private set; }
    public int DaysIgnoredReality { get; private set; }
    public int NightExtend1SuccessCount { get; private set; }
    public int NightExtend2SuccessCount { get; private set; }
    public int StayOverCount { get; private set; }

    // ───── 事件通知 ─────
    public event Action<int> OnDayChanged;
    public event Action<int> OnStressChanged;       // delta
    public event Action<int> OnLifePowerChanged;    // delta
    public event Action<int> OnSocialFearChanged;   // delta
    public event Action<int> OnDependencyChanged;   // delta
    public event Action<int> OnMoneyChanged;        // delta
    public event Action<int> OnSkillPointsChanged;  // delta

    public event Action<ProtagonistStressState, ProtagonistStressState> OnStressStateChanged;
    public event Action<ProtagonistLifeState, ProtagonistLifeState> OnLifeStateChanged;
    public event Action<ProtagonistSocialFearState, ProtagonistSocialFearState> OnSocialFearStateChanged;
    public event Action<ProtagonistDependencyState, ProtagonistDependencyState> OnDependencyStateChanged;

    public event Action OnStressCollapsed;
    public event Action OnDailyFlagsReset;
    public event Action OnLongTermCountersChanged;

    // ───── 初始化 / 存讀檔 ─────
    public void NewGame()
    {
        Day = INITIAL_DAY;
        Stress = INITIAL_STRESS;
        LifePower = INITIAL_LIFE_POWER;
        SocialFear = INITIAL_SOCIAL_FEAR;
        Dependency = INITIAL_DEPENDENCY;
        Money = INITIAL_MONEY;
        SkillPoints = INITIAL_SKILL_POINTS;

        ResetDailyFlags();
        ResetLongTermCounters();
        NotifyAllCoreValues();
    }

    public ProtagonistSaveData ToSaveData()
    {
        return new ProtagonistSaveData
        {
            Day = Day,
            Stress = Stress,
            LifePower = LifePower,
            SocialFear = SocialFear,
            Dependency = Dependency,
            Money = Money,
            SkillPoints = SkillPoints,

            HasBathedToday = HasBathedToday,
            HasCleanedRoomToday = HasCleanedRoomToday,
            HasHadMealToday = HasHadMealToday,
            HasCheckedMailToday = HasCheckedMailToday,
            HasRepliedFamilyToday = HasRepliedFamilyToday,
            HasIgnoredPhoneToday = HasIgnoredPhoneToday,
            HasGoneOutsideToday = HasGoneOutsideToday,
            SucceededGoingOutsideToday = SucceededGoingOutsideToday,
            FailedGoingOutsideToday = FailedGoingOutsideToday,
            HasEscapedToday = HasEscapedToday,
            StressCollapsedToday = StressCollapsedToday,

            CollapseCount = CollapseCount,
            OutsideSuccessCount = OutsideSuccessCount,
            OutsideFailCount = OutsideFailCount,
            ConsecutiveNoBathDays = ConsecutiveNoBathDays,
            ConsecutiveEscapeDays = ConsecutiveEscapeDays,
            DaysImprovedLife = DaysImprovedLife,
            DaysIgnoredReality = DaysIgnoredReality,
            NightExtend1SuccessCount = NightExtend1SuccessCount,
            NightExtend2SuccessCount = NightExtend2SuccessCount,
            StayOverCount = StayOverCount
        };
    }

    public void LoadFromSaveData(ProtagonistSaveData data)
    {
        if (data == null)
        {
            NewGame();
            return;
        }

        Day = Math.Max(INITIAL_DAY, data.Day);
        Stress = ClampStatus(data.Stress);
        LifePower = ClampStatus(data.LifePower);
        SocialFear = ClampStatus(data.SocialFear);
        Dependency = ClampStatus(data.Dependency);
        Money = Math.Max(0, data.Money);
        SkillPoints = Math.Max(0, data.SkillPoints);

        HasBathedToday = data.HasBathedToday;
        HasCleanedRoomToday = data.HasCleanedRoomToday;
        HasHadMealToday = data.HasHadMealToday;
        HasCheckedMailToday = data.HasCheckedMailToday;
        HasRepliedFamilyToday = data.HasRepliedFamilyToday;
        HasIgnoredPhoneToday = data.HasIgnoredPhoneToday;
        HasGoneOutsideToday = data.HasGoneOutsideToday;
        SucceededGoingOutsideToday = data.SucceededGoingOutsideToday;
        FailedGoingOutsideToday = data.FailedGoingOutsideToday;
        HasEscapedToday = data.HasEscapedToday;
        StressCollapsedToday = data.StressCollapsedToday;

        CollapseCount = Math.Max(0, data.CollapseCount);
        OutsideSuccessCount = Math.Max(0, data.OutsideSuccessCount);
        OutsideFailCount = Math.Max(0, data.OutsideFailCount);
        ConsecutiveNoBathDays = Math.Max(0, data.ConsecutiveNoBathDays);
        ConsecutiveEscapeDays = Math.Max(0, data.ConsecutiveEscapeDays);
        DaysImprovedLife = Math.Max(0, data.DaysImprovedLife);
        DaysIgnoredReality = Math.Max(0, data.DaysIgnoredReality);
        NightExtend1SuccessCount = Math.Max(0, data.NightExtend1SuccessCount);
        NightExtend2SuccessCount = Math.Max(0, data.NightExtend2SuccessCount);
        StayOverCount = Math.Max(0, data.StayOverCount);

        NotifyAllCoreValues();
        OnLongTermCountersChanged?.Invoke();
    }

    private void NotifyAllCoreValues()
    {
        OnDayChanged?.Invoke(Day);
        OnStressChanged?.Invoke(0);
        OnLifePowerChanged?.Invoke(0);
        OnSocialFearChanged?.Invoke(0);
        OnDependencyChanged?.Invoke(0);
        OnMoneyChanged?.Invoke(0);
        OnSkillPointsChanged?.Invoke(0);
    }

    // ───── 每日流程 ─────
    public void OnDayStart()
    {
        ResetDailyFlags();
    }

    public void OnDayEnd()
    {
        UpdateLongTermCountersFromDailyFlags();
    }

    public void NextDay()
    {
        OnDayEnd();
        Day++;
        OnDayChanged?.Invoke(Day);
        OnDayStart();
    }

    public void ResetDailyFlags()
    {
        HasBathedToday = false;
        HasCleanedRoomToday = false;
        HasHadMealToday = false;
        HasCheckedMailToday = false;
        HasRepliedFamilyToday = false;
        HasIgnoredPhoneToday = false;
        HasGoneOutsideToday = false;
        SucceededGoingOutsideToday = false;
        FailedGoingOutsideToday = false;
        HasEscapedToday = false;
        StressCollapsedToday = false;

        OnDailyFlagsReset?.Invoke();
    }

    public void ResetLongTermCounters()
    {
        CollapseCount = 0;
        OutsideSuccessCount = 0;
        OutsideFailCount = 0;
        ConsecutiveNoBathDays = 0;
        ConsecutiveEscapeDays = 0;
        DaysImprovedLife = 0;
        DaysIgnoredReality = 0;
        NightExtend1SuccessCount = 0;
        NightExtend2SuccessCount = 0;
        StayOverCount = 0;

        OnLongTermCountersChanged?.Invoke();
    }

    private void UpdateLongTermCountersFromDailyFlags()
    {
        if (StressCollapsedToday) CollapseCount++;

        if (SucceededGoingOutsideToday) OutsideSuccessCount++;
        if (FailedGoingOutsideToday) OutsideFailCount++;

        ConsecutiveNoBathDays = HasBathedToday ? 0 : ConsecutiveNoBathDays + 1;
        ConsecutiveEscapeDays = HasEscapedToday ? ConsecutiveEscapeDays + 1 : 0;

        if (HasBathedToday || HasCleanedRoomToday || HasHadMealToday || SucceededGoingOutsideToday)
            DaysImprovedLife++;

        if (HasIgnoredPhoneToday || HasEscapedToday)
            DaysIgnoredReality++;

        OnLongTermCountersChanged?.Invoke();
    }

    // ───── 主角核心數值方法 ─────
    public void ApplyStatusChange(ProtagonistStatusChange change)
    {
        AddStress(change.stressDelta);
        AddLifePower(change.lifePowerDelta);
        AddSocialFear(change.socialFearDelta);
        AddDependency(change.dependencyDelta);
    }

    public void AddStress(int delta)
    {
        if (delta == 0) return;

        int prevValue = Stress;
        ProtagonistStressState prevState = GetStressState();
        Stress = ClampStatus(Stress + delta);

        if (Stress == prevValue) return;

        OnStressChanged?.Invoke(Stress - prevValue);

        ProtagonistStressState newState = GetStressState();
        if (newState != prevState)
            OnStressStateChanged?.Invoke(prevState, newState);

        if (Stress >= STRESS_COLLAPSE_THRESHOLD && prevValue < STRESS_COLLAPSE_THRESHOLD)
        {
            StressCollapsedToday = true;
            OnStressCollapsed?.Invoke();
        }
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
        ProtagonistLifeState prevState = GetLifeState();
        LifePower = ClampStatus(LifePower + delta);

        if (LifePower == prevValue) return;

        OnLifePowerChanged?.Invoke(LifePower - prevValue);

        ProtagonistLifeState newState = GetLifeState();
        if (newState != prevState)
            OnLifeStateChanged?.Invoke(prevState, newState);
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

    public void AddSocialFear(int delta)
    {
        if (delta == 0) return;

        int prevValue = SocialFear;
        ProtagonistSocialFearState prevState = GetSocialFearState();
        SocialFear = ClampStatus(SocialFear + delta);

        if (SocialFear == prevValue) return;

        OnSocialFearChanged?.Invoke(SocialFear - prevValue);

        ProtagonistSocialFearState newState = GetSocialFearState();
        if (newState != prevState)
            OnSocialFearStateChanged?.Invoke(prevState, newState);
    }

    public void ReduceSocialFear(int delta)
    {
        if (delta <= 0) return;
        AddSocialFear(-delta);
    }

    public void SetSocialFear(int value)
    {
        AddSocialFear(ClampStatus(value) - SocialFear);
    }

    public void AddDependency(int delta)
    {
        if (delta == 0) return;

        int prevValue = Dependency;
        ProtagonistDependencyState prevState = GetDependencyState();
        Dependency = ClampStatus(Dependency + delta);

        if (Dependency == prevValue) return;

        OnDependencyChanged?.Invoke(Dependency - prevValue);

        ProtagonistDependencyState newState = GetDependencyState();
        if (newState != prevState)
            OnDependencyStateChanged?.Invoke(prevState, newState);
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

    // ───── Money / SkillPoints：依需求保留 ─────
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

    // ───── 每日狀態標記方法 ─────
    public void MarkBathedToday(bool value = true) => HasBathedToday = value;
    public void MarkCleanedRoomToday(bool value = true) => HasCleanedRoomToday = value;
    public void MarkHadMealToday(bool value = true) => HasHadMealToday = value;
    public void MarkCheckedMailToday(bool value = true) => HasCheckedMailToday = value;
    public void MarkRepliedFamilyToday(bool value = true) => HasRepliedFamilyToday = value;
    public void MarkIgnoredPhoneToday(bool value = true) => HasIgnoredPhoneToday = value;
    public void MarkEscapedToday(bool value = true) => HasEscapedToday = value;

    public void MarkGoneOutsideToday(bool succeeded)
    {
        HasGoneOutsideToday = true;
        SucceededGoingOutsideToday = succeeded;
        FailedGoingOutsideToday = !succeeded;
    }

    public void MarkStressCollapsedToday(bool value = true)
    {
        StressCollapsedToday = value;
    }

    // ───── 長期統計手動加算：供事件系統使用 ─────
    public void AddNightExtend1SuccessCount(int amount = 1)
    {
        if (amount == 0) return;
        NightExtend1SuccessCount = Math.Max(0, NightExtend1SuccessCount + amount);
        OnLongTermCountersChanged?.Invoke();
    }

    public void AddNightExtend2SuccessCount(int amount = 1)
    {
        if (amount == 0) return;
        NightExtend2SuccessCount = Math.Max(0, NightExtend2SuccessCount + amount);
        OnLongTermCountersChanged?.Invoke();
    }

    public void AddStayOverCount(int amount = 1)
    {
        if (amount == 0) return;
        StayOverCount = Math.Max(0, StayOverCount + amount);
        OnLongTermCountersChanged?.Invoke();
    }

    // ───── 狀態查詢 ─────
    public ProtagonistStressState GetStressState()
    {
        if (Stress >= STRESS_COLLAPSE_THRESHOLD) return ProtagonistStressState.Collapsed;
        if (Stress >= STRESS_CRITICAL_THRESHOLD) return ProtagonistStressState.Critical;
        if (Stress >= STRESS_STRAINED_THRESHOLD) return ProtagonistStressState.Strained;
        if (Stress >= STRESS_IRRITATED_THRESHOLD) return ProtagonistStressState.Irritated;
        if (Stress >= STRESS_UNEASY_THRESHOLD) return ProtagonistStressState.Uneasy;
        return ProtagonistStressState.Calm;
    }

    public ProtagonistLifeState GetLifeState()
    {
        if (LifePower <= LIFE_VERY_LOW_MAX) return ProtagonistLifeState.VeryLow;
        if (LifePower >= LIFE_HEALTHY_THRESHOLD) return ProtagonistLifeState.Healthy;
        if (LifePower >= LIFE_STABLE_THRESHOLD) return ProtagonistLifeState.Stable;
        return ProtagonistLifeState.Unstable;
    }

    public ProtagonistSocialFearState GetSocialFearState()
    {
        if (SocialFear >= SOCIAL_FEAR_HIGH_THRESHOLD) return ProtagonistSocialFearState.High;
        if (SocialFear >= SOCIAL_FEAR_MEDIUM_THRESHOLD) return ProtagonistSocialFearState.Medium;
        return ProtagonistSocialFearState.Low;
    }

    public ProtagonistDependencyState GetDependencyState()
    {
        if (Dependency >= DEPENDENCY_EXTREME_THRESHOLD) return ProtagonistDependencyState.Extreme;
        if (Dependency >= DEPENDENCY_HIGH_THRESHOLD) return ProtagonistDependencyState.High;
        if (Dependency >= DEPENDENCY_MEDIUM_THRESHOLD) return ProtagonistDependencyState.Medium;
        return ProtagonistDependencyState.Low;
    }

    public bool IsStressCritical() => Stress >= STRESS_CRITICAL_THRESHOLD;
    public bool IsStressCollapsed() => Stress >= STRESS_COLLAPSE_THRESHOLD;
    public bool IsLifeVeryLow() => LifePower <= LIFE_VERY_LOW_MAX;
    public bool IsLifeStable() => LifePower >= LIFE_STABLE_THRESHOLD;
    public bool IsLifeHealthy() => LifePower >= LIFE_HEALTHY_THRESHOLD;
    public bool IsSocialFearHigh() => SocialFear >= SOCIAL_FEAR_HIGH_THRESHOLD;
    public bool IsSocialFearLow() => SocialFear < SOCIAL_FEAR_MEDIUM_THRESHOLD;
    public bool IsDependencyHigh() => Dependency >= DEPENDENCY_HIGH_THRESHOLD;
    public bool IsDependencyExtreme() => Dependency >= DEPENDENCY_EXTREME_THRESHOLD;

    // ───── 外界行動成功率輔助 ─────
    public int GetFaceRealityScore()
    {
        // 給事件系統使用的粗略評分：越高代表越可能成功面對外界。
        return 100 - SocialFear + (LifePower / 2) - (Stress / 3);
    }

    public bool CanLikelyFaceReality(int threshold = 50)
    {
        return GetFaceRealityScore() >= threshold;
    }

    private static int ClampStatus(int value)
    {
        if (value < MIN_STATUS_VALUE) return MIN_STATUS_VALUE;
        if (value > MAX_STATUS_VALUE) return MAX_STATUS_VALUE;
        return value;
    }
}
