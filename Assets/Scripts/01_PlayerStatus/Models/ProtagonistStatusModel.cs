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
    public const int ROOM_MESS_LEVEL_MAX = 25;
    public const int ROOM_MESS_LEVEL_MIN = 0;
    public const int INITIAL_ROOM_MESS_LEVEL = ROOM_MESS_LEVEL_MAX;
    public const float ROOM_MESS_DAILY_GAIN_CHANCE = 0.5f;

    // ───── BodyDirtyLevel（身體髒污度） ─────
    // 與 RoomMessLevel 平行：0～25，越高代表身體越髒。顯示時反向換算成「整潔度百分比」。
    public const int BODY_DIRTY_LEVEL_MAX = 25;
    public const int BODY_DIRTY_LEVEL_MIN = 0;
    public const int INITIAL_BODY_DIRTY_LEVEL = BODY_DIRTY_LEVEL_MAX;

    // ───── 數值範圍 ─────
    // 通用範圍（LifePower / Sociality 使用）
    public const int MIN_STATUS_VALUE = 0;
    public const int MAX_STATUS_VALUE = 100;

    // Stress 獨立範圍：可累積超過分級門檻（門檻維持 50/80/100，但實際數值上限放寬到 150）
    public const int STRESS_MIN_VALUE = 0;
    public const int STRESS_MAX_VALUE = 150;

    // Dependency 獨立範圍：同上
    public const int DEPENDENCY_MIN_VALUE = 0;
    public const int DEPENDENCY_MAX_VALUE = 150;

    /// <summary>每日依賴度自動上升量（換日時套用，參照 HeroineStatusModel.LibidoDailyDecay 的模式）。</summary>
    public const int DEPENDENCY_DAILY_GAIN = 5;

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

    /// <summary>壓力：0～150。越高越危險（分級門檻仍維持 50/80/100）。</summary>
    public int Stress { get; private set; } = INITIAL_STRESS;

    /// <summary>生活力：0～100。越高代表越能維持正常生活。</summary>
    public int LifePower { get; private set; } = INITIAL_LIFE_POWER;

    /// <summary>社會性：0～100。越高代表越能面對外界與正常社交。</summary>
    public int Sociality { get; private set; } = INITIAL_SOCIALITY;

    /// <summary>依賴度：0～150。主角對妹妹的心理與生活依賴（分級門檻仍維持 50/80/100）。每日自動上升 DEPENDENCY_DAILY_GAIN。</summary>
    public int Dependency { get; private set; } = INITIAL_DEPENDENCY;

    /// <summary>保留資源：金錢。</summary>
    public int Money { get; private set; } = INITIAL_MONEY;

    /// <summary>保留資源：技能點。</summary>
    public int SkillPoints { get; private set; } = INITIAL_SKILL_POINTS;

    /// <summary>射精次數。每日重製回 Max。可加超過 Max，但不可扣低於 Min。<= 0 時為耗盡狀態。</summary>
    public int ShootTimes { get; private set; } = INITIAL_SHOOT_TIMES;

    /// <summary>射精耗盡狀態：ShootTimes <= 0 即為 true。</summary>
    public bool IsOverShoot => ShootTimes <= 0;

    /// <summary>房間髒亂度：0～25。每日結束自動 +1。越高代表房間越亂。</summary>
    public int RoomMessLevel { get; private set; } = INITIAL_ROOM_MESS_LEVEL;

    /// <summary>
    /// 房間整潔度百分比：0～100（純衍生、不佔狀態、不進存檔）。
    /// 由髒亂度反向換算：髒亂 Min（0）= 整潔 100%，髒亂 Max（25）= 整潔 0%。
    /// 範圍為 25 時剛好整除（每點髒亂 = 4%），無四捨五入誤差。
    /// </summary>
    public int RoomCleanPercent
    {
        get
        {
            int range = ROOM_MESS_LEVEL_MAX - ROOM_MESS_LEVEL_MIN;
            if (range <= 0) return 100;
            return (ROOM_MESS_LEVEL_MAX - RoomMessLevel) * 100 / range;
        }
    }

    /// <summary>身體髒污度：0～25。越高代表身體越髒（顯示時反向換算成整潔度）。</summary>
    public int BodyDirtyLevel { get; private set; } = INITIAL_BODY_DIRTY_LEVEL;

    /// <summary>
    /// 身體整潔度百分比：0～100（純衍生、不佔狀態、不進存檔）。
    /// 由髒污度反向換算：髒污 Min（0）= 整潔 100%，髒污 Max（25）= 整潔 0%。
    /// </summary>
    public int BodyCleanPercent
    {
        get
        {
            int range = BODY_DIRTY_LEVEL_MAX - BODY_DIRTY_LEVEL_MIN;
            if (range <= 0) return 100;
            return (BODY_DIRTY_LEVEL_MAX - BodyDirtyLevel) * 100 / range;
        }
    }

    /// <summary>狀態不好：true 時「增加壓力會再多 +1、減少壓力會少 -1」。換日時自動解除。</summary>
    public bool BadHealthy { get; private set; } = false;

    // ───── 事件通知 ─────
    public event Action<int> OnStressChanged;       // delta
    public event Action<int> OnLifePowerChanged;    // delta
    public event Action<int> OnSocialityChanged;    // delta
    public event Action<int> OnDependencyChanged;   // delta
    public event Action<int> OnMoneyChanged;        // delta
    public event Action<int> OnSkillPointsChanged;  // delta
    public event Action<int> OnShootTimesChanged;   // delta
    public event Action<int> OnRoomMessLevelChanged; // delta
    public event Action<int> OnBodyDirtyLevelChanged; // delta
    public event Action<bool> OnBadHealthyChanged;   // 新狀態值

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
        BodyDirtyLevel = INITIAL_BODY_DIRTY_LEVEL;
        BadHealthy = false;

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
            RoomMessLevel = RoomMessLevel,
            BodyDirtyLevel = BodyDirtyLevel,
            BadHealthy = BadHealthy
        };
    }

    public void LoadFromSaveData(ProtagonistSaveData data)
    {
        if (data == null)
        {
            NewGame();
            return;
        }

        Stress = ClampStress(data.Stress);
        LifePower = ClampStatus(data.LifePower);
        Sociality = ClampStatus(data.Sociality);
        Dependency = ClampDependency(data.Dependency);
        Money = Math.Max(0, data.Money);
        SkillPoints = Math.Max(0, data.SkillPoints);
        ShootTimes = Math.Max(SHOOT_TIMES_MIN, Math.Min(data.ShootTimes, int.MaxValue));
        RoomMessLevel = ClampRoomMessLevel(data.RoomMessLevel);
        BodyDirtyLevel = ClampBodyDirtyLevel(data.BodyDirtyLevel); // 舊存檔缺欄位時反序列化為 0（= 身體最乾淨）
        BadHealthy = data.BadHealthy; // 舊存檔沒有此欄位時，Newtonsoft 預設為 false

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
        OnBodyDirtyLevelChanged?.Invoke(0);
        OnBadHealthyChanged?.Invoke(BadHealthy);
    }

    // ───── 每日流程 ─────
    public void OnDayStart()
    {
        ResetShootTimes();
        DisableBadHealthy(); // 「狀態不好」換日自動解除
    }

    public void OnDayEnd()
    {
        if (UnityEngine.Random.value < ROOM_MESS_DAILY_GAIN_CHANCE)
        {
            AddRoomMessLevel(1);

        }

        //AddBodyDirtyLevel(1); // 與房間同步：每日結束身體也會變髒一點。若不想要可移除此行。

        ApplyDependencyDailyGain();
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
        ApplyStressDelta(ApplyBadHealthyModifier(delta));
    }

    /// <summary>
    /// 「狀態不好」的壓力修正：增加壓力時再 +1、減少壓力時少 -1（兩者皆為 delta + 1）。
    /// 例：+5 → +6；-5 → -4；-1 → 0（等於這次減壓無效）。
    /// </summary>
    private int ApplyBadHealthyModifier(int delta)
    {
        if (!BadHealthy || delta == 0) return delta;
        return delta + 1;
    }

    /// <summary>實際套用壓力變化（不經過 BadHealthy 修正）。SetStress 走這裡以確保精確設定。</summary>
    private void ApplyStressDelta(int delta)
    {
        if (delta == 0) return;

        int prevValue = Stress;
        StatusGrade prevGrade = GetStressGrade();
        Stress = ClampStress(Stress + delta);

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
        ApplyStressDelta(ClampStress(value) - Stress);
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
        Dependency = ClampDependency(Dependency + delta);

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
        AddDependency(ClampDependency(value) - Dependency);
    }

    /// <summary>每日依賴度上升。由每日流程 (OnDayEnd) 呼叫。</summary>
    public void ApplyDependencyDailyGain()
    {
        AddDependency(DEPENDENCY_DAILY_GAIN);
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

    // ───── BodyDirtyLevel（身體髒污度） ─────

    /// <summary>查詢目前身體髒污度。</summary>
    public int CheckBodyDirtyLevel() => BodyDirtyLevel;

    /// <summary>增加髒污度。封頂於 Max，不會超過。</summary>
    public void AddBodyDirtyLevel(int delta)
    {
        if (delta == 0) return;
        int prev = BodyDirtyLevel;
        BodyDirtyLevel = ClampBodyDirtyLevel(BodyDirtyLevel + delta);
        if (BodyDirtyLevel != prev) OnBodyDirtyLevelChanged?.Invoke(BodyDirtyLevel - prev);
    }

    /// <summary>降低髒污度（delta 為正數）。保底於 Min，不會低於。</summary>
    public void ReduceBodyDirtyLevel(int delta)
    {
        if (delta <= 0) return;
        AddBodyDirtyLevel(-delta);
    }

    /// <summary>直接設定髒污度，會夾在 Min~Max。</summary>
    public void SetBodyDirtyLevel(int value)
    {
        int prev = BodyDirtyLevel;
        BodyDirtyLevel = ClampBodyDirtyLevel(value);
        if (BodyDirtyLevel != prev) OnBodyDirtyLevelChanged?.Invoke(BodyDirtyLevel - prev);
    }

    // ───── BadHealthy（狀態不好） ─────

    /// <summary>開啟「狀態不好」。開啟期間：增加壓力再 +1、減少壓力少 -1。</summary>
    public void EnableBadHealthy() => SetBadHealthy(true);

    /// <summary>解除「狀態不好」。換日 (OnDayStart) 也會自動呼叫。</summary>
    public void DisableBadHealthy() => SetBadHealthy(false);

    /// <summary>直接設定「狀態不好」。狀態有變化時觸發 OnBadHealthyChanged。</summary>
    public void SetBadHealthy(bool value)
    {
        if (BadHealthy == value) return;
        BadHealthy = value;
        OnBadHealthyChanged?.Invoke(BadHealthy);
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

    private static int ClampStress(int value)
    {
        if (value < STRESS_MIN_VALUE) return STRESS_MIN_VALUE;
        if (value > STRESS_MAX_VALUE) return STRESS_MAX_VALUE;
        return value;
    }

    private static int ClampDependency(int value)
    {
        if (value < DEPENDENCY_MIN_VALUE) return DEPENDENCY_MIN_VALUE;
        if (value > DEPENDENCY_MAX_VALUE) return DEPENDENCY_MAX_VALUE;
        return value;
    }

    private static int ClampRoomMessLevel(int value)
    {
        if (value < ROOM_MESS_LEVEL_MIN) return ROOM_MESS_LEVEL_MIN;
        if (value > ROOM_MESS_LEVEL_MAX) return ROOM_MESS_LEVEL_MAX;
        return value;
    }

    private static int ClampBodyDirtyLevel(int value)
    {
        if (value < BODY_DIRTY_LEVEL_MIN) return BODY_DIRTY_LEVEL_MIN;
        if (value > BODY_DIRTY_LEVEL_MAX) return BODY_DIRTY_LEVEL_MAX;
        return value;
    }
}
