using System;

/// <summary>
/// 統一的主角數值分級。四項核心數值共用同一個 enum，
/// 各自的門檻可獨立調整，但預設一致。
/// </summary>
public enum StatusGrade
{
    Low,      // 預設 0～49
    Medium,   // 預設 50～79
    High,     // 預設 80～99
    Extreme   // 預設 100
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
/// 職責：負責儲存與管理「主角自身」的核心數值。
/// Day 由 TimeSystemModel.DayIndex 統一管理，本 Model 不再持有。
/// </summary>
public class ProtagonistStatusModel
{
    // ───── 初始值 ─────
    public const int INITIAL_STRESS = 40;
    public const int INITIAL_LIFE_POWER = 5;
    public const int INITIAL_SOCIALITY = 5;
    public const int INITIAL_DEPENDENCY = 0;
    public const int INITIAL_MONEY = 0;
    public const int INITIAL_SKILL_POINTS = 0;

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

    // ───── 事件通知 ─────
    public event Action<int> OnStressChanged;       // delta
    public event Action<int> OnLifePowerChanged;    // delta
    public event Action<int> OnSocialityChanged;    // delta
    public event Action<int> OnDependencyChanged;   // delta
    public event Action<int> OnMoneyChanged;        // delta
    public event Action<int> OnSkillPointsChanged;  // delta

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
}