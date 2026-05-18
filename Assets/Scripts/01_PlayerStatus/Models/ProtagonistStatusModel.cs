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
/// 社會性狀態。用於 UI 顯示與事件條件判斷。越高越好。
/// </summary>
public enum ProtagonistSocialityState
{
    Low,     // 0～30：幾乎無法面對外界
    Medium,  // 31～59：勉強能應對
    High     // 60～100：能正常社交
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
/// 職責：負責儲存與管理「主角自身」的核心數值與行為統計。
/// NHK 版：保留舊類名，但核心改為壓力、生活力、社會性、依賴度。
/// Money / SkillPoints 依需求保留。
/// </summary>
public class ProtagonistStatusModel
{
    // ───── 初始值 ─────
    public const int INITIAL_DAY = 1;
    public const int INITIAL_STRESS = 40;
    public const int INITIAL_LIFE_POWER = 5;
    public const int INITIAL_SOCIALITY = 5;
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

    public const int SOCIALITY_MEDIUM_THRESHOLD = 31;   // 31 以上為 Medium
    public const int SOCIALITY_HIGH_THRESHOLD = 60;     // 60 以上為 High

    public const int DEPENDENCY_MEDIUM_THRESHOLD = 40;
    public const int DEPENDENCY_HIGH_THRESHOLD = 60;
    public const int DEPENDENCY_EXTREME_THRESHOLD = 80;

    // ───── 核心狀態 ─────
    public int Day { get; private set; } = INITIAL_DAY;

    /// <summary>壓力：0～100。越高越危險，100 可觸發崩潰事件。</summary>
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

    // ───── 事件通知 ─────
    public event Action<int> OnDayChanged;
    public event Action<int> OnStressChanged;       // delta
    public event Action<int> OnLifePowerChanged;    // delta
    public event Action<int> OnSocialityChanged;    // delta
    public event Action<int> OnDependencyChanged;   // delta
    public event Action<int> OnMoneyChanged;        // delta
    public event Action<int> OnSkillPointsChanged;  // delta

    public event Action<ProtagonistStressState, ProtagonistStressState> OnStressStateChanged;
    public event Action<ProtagonistLifeState, ProtagonistLifeState> OnLifeStateChanged;
    public event Action<ProtagonistSocialityState, ProtagonistSocialityState> OnSocialityStateChanged;
    public event Action<ProtagonistDependencyState, ProtagonistDependencyState> OnDependencyStateChanged;

    // ───── 初始化 / 存讀檔 ─────
    public void NewGame()
    {
        Day = INITIAL_DAY;
        Stress = INITIAL_STRESS;
        LifePower = INITIAL_LIFE_POWER;
        Sociality = INITIAL_SOCIALITY;
        Dependency = INITIAL_DEPENDENCY;
        Money = INITIAL_MONEY;
        SkillPoints = INITIAL_SKILL_POINTS;

        NotifyAllCoreValues();
    }

    public ProtagonistSaveData ToSaveData()
    {
        return new ProtagonistSaveData
        {
            Day = Day,
            Stress = Stress,
            LifePower = LifePower,
            Sociality = Sociality,
            Dependency = Dependency,
            Money = Money,
            SkillPoints = SkillPoints
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
        Sociality = ClampStatus(data.Sociality);
        Dependency = ClampStatus(data.Dependency);
        Money = Math.Max(0, data.Money);
        SkillPoints = Math.Max(0, data.SkillPoints);

        NotifyAllCoreValues();
    }

    private void NotifyAllCoreValues()
    {
        OnDayChanged?.Invoke(Day);
        OnStressChanged?.Invoke(0);
        OnLifePowerChanged?.Invoke(0);
        OnSocialityChanged?.Invoke(0);
        OnDependencyChanged?.Invoke(0);
        OnMoneyChanged?.Invoke(0);
        OnSkillPointsChanged?.Invoke(0);
    }

    // ───── 每日流程（預留，未來可加邏輯） ─────
    public void OnDayStart()
    {
    }

    public void OnDayEnd()
    {
    }

    public void NextDay()
    {
        OnDayEnd();
        Day++;
        OnDayChanged?.Invoke(Day);
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
        ProtagonistStressState prevState = GetStressState();
        Stress = ClampStatus(Stress + delta);

        if (Stress == prevValue) return;

        OnStressChanged?.Invoke(Stress - prevValue);

        ProtagonistStressState newState = GetStressState();
        if (newState != prevState)
            OnStressStateChanged?.Invoke(prevState, newState);
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

    public void AddSociality(int delta)
    {
        if (delta == 0) return;

        int prevValue = Sociality;
        ProtagonistSocialityState prevState = GetSocialityState();
        Sociality = ClampStatus(Sociality + delta);

        if (Sociality == prevValue) return;

        OnSocialityChanged?.Invoke(Sociality - prevValue);

        ProtagonistSocialityState newState = GetSocialityState();
        if (newState != prevState)
            OnSocialityStateChanged?.Invoke(prevState, newState);
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

    public ProtagonistSocialityState GetSocialityState()
    {
        if (Sociality >= SOCIALITY_HIGH_THRESHOLD) return ProtagonistSocialityState.High;
        if (Sociality >= SOCIALITY_MEDIUM_THRESHOLD) return ProtagonistSocialityState.Medium;
        return ProtagonistSocialityState.Low;
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
    public bool IsSocialityHigh() => Sociality >= SOCIALITY_HIGH_THRESHOLD;
    public bool IsSocialityLow() => Sociality < SOCIALITY_MEDIUM_THRESHOLD;
    public bool IsDependencyHigh() => Dependency >= DEPENDENCY_HIGH_THRESHOLD;
    public bool IsDependencyExtreme() => Dependency >= DEPENDENCY_EXTREME_THRESHOLD;

    // ───── 內部工具 ─────
    private static int ClampStatus(int value)
    {
        if (value < MIN_STATUS_VALUE) return MIN_STATUS_VALUE;
        if (value > MAX_STATUS_VALUE) return MAX_STATUS_VALUE;
        return value;
    }
}