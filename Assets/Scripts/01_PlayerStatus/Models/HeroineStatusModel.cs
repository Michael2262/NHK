using System;

/// <summary>正式女主角數值；每個實例代表一位角色，由 GameStatusService.Heroines 持有。</summary>
public class HeroineStatusModel
{
    // ==========================================================
    // 可調數值設定：同類放在一起。初始值於建立角色／新遊戲時套用。
    // ==========================================================

    // ───── Awake（獨立浮點數值；指定 Phase 每消耗一格扣除，換日重設）─────
    public const float AwakeMin = 0f;
    public const float AwakeMax = 120f;
    public const float InitialAwake = 120f;
    public const int AwakeDecayPhaseIndex = 2;
    public const float AwakeDecayPerSlot = 5f;
    public const float AwakeDailyResetValue = 120f;

    // ───── 睡眠狀態（所有女主角共用）─────
    // 各區間含下限、不含下一狀態下限；最上層包含 AwakeMax。
    // Wake：     [SleepStateWakeMin, AwakeMax]                     預設 100～120（含上限）
    // Dazed：    [SleepStateDazedMin, SleepStateWakeMin)            預設 80～<100
    // HalfAwake：[SleepStateHalfAwakeMin, SleepStateDazedMin)       預設 60～<80
    // Sleep：    [SleepStateSleepMin, SleepStateHalfAwakeMin)       預設 40～<60
    // SoundSleep：[SleepStateSoundSleepMin, SleepStateSleepMin)     預設 20～<40
    // DeepSleep：[AwakeMin, SleepStateSoundSleepMin)                預設 0～<20
    // 調整時保持 AwakeMin < SoundSleep < Sleep < HalfAwake < Dazed < Wake <= AwakeMax。
    // 共用相鄰邊界，避免小數區間出現空隙或重疊。
    public const float SleepStateWakeMin = 100f;
    public const float SleepStateDazedMin = 80f;
    public const float SleepStateHalfAwakeMin = 60f;
    public const float SleepStateSleepMin = 40f;
    public const float SleepStateSoundSleepMin = 20f;

    // ───── 信賴 ─────
    public const int TrustMin = 0;
    public const int TrustMax = 999;
    public const int InitialTrust = 0;

    // ───── 好感 ─────
    public const int AffinityMin = 0;
    public const int AffinityMax = 999;
    public const int InitialAffinity = 0;

    // ───── 心情 ─────
    public const int MoodMin = 0;
    public const int MoodMax = 10;
    public const int InitialMood = 5;

    // ───── 性興味（獨立數值，不做每日衰減）─────
    public const int InterestMin = 0;
    public const int InterestMax = 999;
    public const int InitialInterest = 0;

    // ───── 性欲 ─────
    public const int LibidoMin = 0;
    public const int LibidoMax = 250;
    public const int InitialLibido = 0;
    public const int LibidoDailyDecay = 2; // 當日不曾實際增加才衰減

    // ───── 興奮 ─────
    public const int ExcitementMin = 0;
    public const int ExcitementMax = 100;
    public const int InitialExcitement = 0;
    public const int ExcitementInHeatDailyRetentionMax = 50; // 發情中跨日最多保留值

    // ───── 高潮 ─────
    public const int OrgasmMin = 0;
    public const int OrgasmLimit = 100;
    public const int InitialOrgasm = 0;

    // ───── 次數與狀態 ─────
    public const int InitialHCount = 0;
    public const bool InitialIsInHeat = false;
    public const HeroineEmotionCardType InitialEmotion = HeroineEmotionCardType.Angry;

    // ───── 生理週期（遊戲規則）─────
    public const int DefaultInitialCycleDay = 7;
    public const int PeriodDays = 5;
    public const int AfterPeriodBaseDays = 4;
    public const int AfterPeriodVariation = 2;
    public const int OvulationDays = 7;
    public const int AfterOvulationDays = 6;
    public const int BeforePeriodDays = 6;
    public const int BeforePregnantDays = 9;
    public const float LowPregnancyProbability = 0.05f;
    public const float HighPregnancyProbability = 0.20f;

    private static readonly Random CycleRandom = new Random();
    private readonly int initialCycleDay;

    // ==========================================================
    // 角色資料與執行期間狀態
    // ==========================================================
    public string HeroineID { get; }
    public string NameTextKey { get; }
    public string Name { get; }
    public HeroineEmotionCardType CurrentEmotion { get; private set; } = InitialEmotion;
    public bool IsInHeat { get; private set; } = InitialIsInHeat;
    public int HCount { get; private set; } = InitialHCount;
    public int OrgasmMax => OrgasmLimit;
    /// <summary>每位角色獨立記錄，存檔保留；讀檔不算數值增加。</summary>
    public bool LibidoIncreasedToday { get; private set; }
    public HeroineStatisticsModel Statistics { get; private set; } = new HeroineStatisticsModel();
    public event Action OnChanged;
    public event Action<HeroineEmotionCardType> OnCurrentEmotionChanged;
    public event Action<bool> OnInHeatChanged;
    public event Action<int> OnHCountChanged;
    public event Action<HeroineEmotionCardType> OnEmotionChanged
    {
        add => OnCurrentEmotionChanged += value;
        remove => OnCurrentEmotionChanged -= value;
    }
    public float Awake { get; private set; } = InitialAwake;
    public event Action<float> OnAwakeChanged;
    /// <summary>由 Awake 即時計算；讀檔還原 Awake 後即可取得正確狀態，不另存檔。</summary>
    public HeroineSleepState CurrentSleepState => ResolveSleepState(Awake);
    /// <summary>一般數值操作跨越睡眠區間時通知新狀態；讀檔由 OnGameStatusLoaded 統一刷新。</summary>
    public event Action<HeroineSleepState> OnCurrentSleepStateChanged;
    public int Trust { get; private set; } = InitialTrust;
    public event Action<int> OnTrustChanged;
    public int Affinity { get; private set; } = InitialAffinity;
    public event Action<int> OnAffinityChanged;
    public int Mood { get; private set; } = InitialMood;
    public event Action<int> OnMoodChanged;
    public int Interest { get; private set; } = InitialInterest;
    public event Action<int> OnInterestChanged;
    public int Libido { get; private set; } = InitialLibido;
    public event Action<int> OnLibidoChanged;
    public int Excitement { get; private set; } = InitialExcitement;
    public event Action<int> OnExcitementValueChanged;
    public int Orgasm { get; private set; } = InitialOrgasm;
    public event Action<int> OnOrgasmChanged;

    public HeroineStatusModel(HeroineStat stat, HeroineStatusConfig config)
    {
        if (stat == null) throw new ArgumentNullException(nameof(stat));
        HeroineID = stat.ID;
        NameTextKey = stat.NameTextKey;
        Name = stat.Name;
        initialCycleDay = stat.InitialCycleDay > 0 ? stat.InitialCycleDay : DefaultInitialCycleDay;
        InitializeCycle();
    }

    private static int Clamp(long value, int min, int max)
        => (int)Math.Max(min, Math.Min(max, value));

    // ==========================================================
    // 數值操作：整數先以 long 計算；Awake 先以 double 計算，再限制範圍。
    // ==========================================================

    public float GetAwake() => Awake;
    public void AddAwake(float amount)
    {
        if (float.IsNaN(amount)) return;
        SetAwake(ClampAwake((double)Awake + amount));
    }
    public void SetAwake(float value)
    {
        if (float.IsNaN(value)) return;
        value = ClampAwake(value);
        if (Awake == value) return;
        var oldSleepState = CurrentSleepState;
        Awake = value;
        var newSleepState = CurrentSleepState;
        if (oldSleepState != newSleepState)
            OnCurrentSleepStateChanged?.Invoke(newSleepState);
        OnAwakeChanged?.Invoke(value);
        OnChanged?.Invoke();
    }

    public HeroineSleepState GetCurrentSleepState() => CurrentSleepState;

    private static HeroineSleepState ResolveSleepState(float awake)
    {
        if (awake >= SleepStateWakeMin) return HeroineSleepState.Wake;
        if (awake >= SleepStateDazedMin) return HeroineSleepState.Dazed;
        if (awake >= SleepStateHalfAwakeMin) return HeroineSleepState.HalfAwake;
        if (awake >= SleepStateSleepMin) return HeroineSleepState.Sleep;
        if (awake >= SleepStateSoundSleepMin) return HeroineSleepState.SoundSleep;
        return HeroineSleepState.DeepSleep;
    }

    // 運算與讀檔共用範圍限制；無限值截至邊界，存檔 NaN 回到初始值。
    private static float ClampAwake(double value)
        => double.IsNaN(value) ? InitialAwake : (float)Math.Max(AwakeMin, Math.Min(AwakeMax, value));

    public int GetTrust() => Trust;
    public void AddTrust(int amount) => SetTrust(Clamp((long)Trust + amount, TrustMin, TrustMax));
    public void SetTrust(int value)
    {
        value = Clamp(value, TrustMin, TrustMax);
        if (Trust == value) return;
        Trust = value;
        OnTrustChanged?.Invoke(value);
        OnChanged?.Invoke();
    }

    public int GetAffinity() => Affinity;
    public void AddAffinity(int amount) => SetAffinity(Clamp((long)Affinity + amount, AffinityMin, AffinityMax));
    public void SetAffinity(int value)
    {
        value = Clamp(value, AffinityMin, AffinityMax);
        if (Affinity == value) return;
        Affinity = value;
        OnAffinityChanged?.Invoke(value);
        OnChanged?.Invoke();
    }

    public int GetMood() => Mood;
    public void AddMood(int amount) => SetMood(Clamp((long)Mood + amount, MoodMin, MoodMax));
    public void SetMood(int value)
    {
        value = Clamp(value, MoodMin, MoodMax);
        if (Mood == value) return;
        Mood = value;
        OnMoodChanged?.Invoke(value);
        OnChanged?.Invoke();
    }

    public int GetInterest() => Interest;
    public void AddInterest(int amount) => SetInterest(Clamp((long)Interest + amount, InterestMin, InterestMax));
    public void SetInterest(int value)
    {
        value = Clamp(value, InterestMin, InterestMax);
        if (Interest == value) return;
        Interest = value;
        OnInterestChanged?.Invoke(value);
        OnChanged?.Invoke();
    }

    public int GetLibido() => Libido;
    public void AddLibido(int amount) => SetLibido(Clamp((long)Libido + amount, LibidoMin, LibidoMax));
    public void SetLibido(int value)
    {
        value = Clamp(value, LibidoMin, LibidoMax);
        if (Libido == value) return;
        if (value > Libido) LibidoIncreasedToday = true;
        Libido = value;
        OnLibidoChanged?.Invoke(value);
        OnChanged?.Invoke();
    }

    public int GetExcitement() => Excitement;
    public void AddExcitement(int amount) => SetExcitement(Clamp((long)Excitement + amount, ExcitementMin, ExcitementMax));
    public void SetExcitement(int value)
    {
        value = Clamp(value, ExcitementMin, ExcitementMax);
        if (Excitement == value) return;
        Excitement = value;
        OnExcitementValueChanged?.Invoke(value);
        OnChanged?.Invoke();
    }

    public int GetOrgasm() => Orgasm;
    public void AddOrgasm(int amount) => SetOrgasm(Clamp((long)Orgasm + amount, OrgasmMin, OrgasmMax));
    public void SetOrgasm(int value)
    {
        value = Clamp(value, OrgasmMin, OrgasmMax);
        if (Orgasm == value) return;
        Orgasm = value;
        OnOrgasmChanged?.Invoke(value);
        OnChanged?.Invoke();
    }

    public void ReduceOrgasm(int amount) => SetOrgasm(Clamp((long)Orgasm - amount, OrgasmMin, OrgasmMax));
    public void ResetOrgasm() => SetOrgasm(0);
    public void AddHCount(int amount = 1) => SetHCount(Clamp((long)HCount + amount, 0, int.MaxValue));
    public void SetHCount(int value)
    {
        value = Math.Max(0, value);
        if (HCount == value) return;
        HCount = value;
        OnHCountChanged?.Invoke(value);
        OnChanged?.Invoke();
    }

    public HeroineEmotionCardType GetCurrentEmotion() => CurrentEmotion;
    public void SetCurrentEmotion(HeroineEmotionCardType emotion)
    {
        if (!Enum.IsDefined(typeof(HeroineEmotionCardType), emotion) || CurrentEmotion == emotion) return;
        CurrentEmotion = emotion;
        OnCurrentEmotionChanged?.Invoke(emotion);
        OnChanged?.Invoke();
    }

    public void SetInHeat(bool value)
    {
        if (IsInHeat == value) return;
        IsInHeat = value;
        OnInHeatChanged?.Invoke(value);
        OnChanged?.Invoke();
    }

    /// <summary>每實際消耗一格結算一次；使用消耗前的 Phase，包含離開該 Phase 的最後一格。</summary>
    public void ApplyAwakeSlotDecay(int consumedPhaseIndex)
    {
        if (consumedPhaseIndex == AwakeDecayPhaseIndex)
            AddAwake(-AwakeDecayPerSlot);
    }

    // ───── 跨日結算：由 GameStatusService 對每位角色呼叫一次 ─────
    public void ApplyAwakeDailyReset() => SetAwake(AwakeDailyResetValue);

    public void ApplyLibidoDecay()
    {
        bool skipDecay = LibidoIncreasedToday;
        // 在廣播事件前清除，事件中若增加 Libido，會正確記為新一天的增加。
        LibidoIncreasedToday = false;
        if (!skipDecay) AddLibido(-LibidoDailyDecay);
    }

    public void ApplyExcitementDailyReset()
        => SetExcitement(IsInHeat ? Math.Min(Excitement, ExcitementInHeatDailyRetentionMax) : 0);
    public void ApplyOrgasmDailyReset() => SetOrgasm(0);

    // ───── 存讀檔：還原時不逐項通知，由 Service 統一刷新 ─────
    public HeroineSaveData ToSaveData() => new HeroineSaveData
    {
        Emotion = CurrentEmotion,
        Awake = Awake,
        Trust = Trust, Affinity = Affinity, Mood = Mood, Interest = Interest, Libido = Libido,
        LibidoIncreasedToday = LibidoIncreasedToday,
        Excitement = Excitement, Orgasm = Orgasm, HCount = HCount,
        IsInHeat = IsInHeat, StatisticsData = Statistics.ToSaveData(),
        CycleData = new HeroineCycleSaveData
        {
            CurrentCycleDay = CurrentCycleDay,
            AfterPeriodDays = AfterPeriodDays,
            NextAfterPeriodDays = NextAfterPeriodDays,
            Pregnancy = Pregnancy,
            EarlyPregnancyDaysElapsed = EarlyPregnancyDaysElapsed,
            PregnancyProbability = PregnancyProbability
        }
    };

    public void LoadFromSaveData(HeroineSaveData data)
    {
        if (data == null) return;
        CurrentEmotion = Enum.IsDefined(typeof(HeroineEmotionCardType), data.Emotion) ? data.Emotion : InitialEmotion;
        Awake = ClampAwake(data.Awake);
        Trust = Clamp(data.Trust, TrustMin, TrustMax);
        Affinity = Clamp(data.Affinity, AffinityMin, AffinityMax);
        Mood = Clamp(data.Mood, MoodMin, MoodMax);
        Interest = Clamp(data.Interest, InterestMin, InterestMax);
        Libido = Clamp(data.Libido, LibidoMin, LibidoMax);
        LibidoIncreasedToday = data.LibidoIncreasedToday;
        Excitement = Clamp(data.Excitement, ExcitementMin, ExcitementMax);
        Orgasm = Clamp(data.Orgasm, OrgasmMin, OrgasmMax);
        HCount = Math.Max(0, data.HCount);
        IsInHeat = data.IsInHeat;
        Statistics = new HeroineStatisticsModel();
        if (data.StatisticsData != null) Statistics.LoadFromSaveData(data.StatisticsData);
        LoadCycle(data.CycleData);
    }

    // ───── 處女系統仍為預留接口 ─────
    public VirginityState Virginity => VirginityState.Virgin;
    public event Action<VirginityState> OnVirginityChanged;
    public void SetVirginity(VirginityState state, string firstPartnerId = null) => OnVirginityChanged?.Invoke(state);

    // ───── 生理週期：自然週期日與懷孕進度分開保存，解除時可原位恢復 ─────
    public int CurrentCycleDay { get; private set; }
    /// <summary>本輪月經後天數，整輪固定，避免重抽造成週期位置改變。</summary>
    public int AfterPeriodDays { get; private set; }
    /// <summary>離開月經後時重抽；下一輪開始時套用。</summary>
    public int NextAfterPeriodDays { get; private set; }
    public int CycleLength => PeriodDays + AfterPeriodDays + OvulationDays + AfterOvulationDays + BeforePeriodDays;
    public bool IsCyclePaused => Pregnancy != PregnancyState.NotPregnant;
    public PregnancyState Pregnancy { get; private set; }
    public int EarlyPregnancyDaysElapsed { get; private set; }
    public MenstrualCyclePhase CurrentCyclePhase => Pregnancy == PregnancyState.EarlyPregnancy
        ? MenstrualCyclePhase.BeforePregnant
        : Pregnancy == PregnancyState.Pregnant ? MenstrualCyclePhase.Pregnant : GetNaturalCyclePhase();

    /// <summary>當日機率（0～1），可手動設定；初始化、換日及切換階段時依階段重設。</summary>
    public float PregnancyProbability { get; private set; }

    /// <summary>覆寫當日機率，例如 0.8 代表 80%；跨日後恢復階段預設值。</summary>
    public void SetPregnancyProbability(float value)
    {
        if (float.IsNaN(value)) return;
        value = Math.Max(0f, Math.Min(1f, value));
        if (PregnancyProbability == value) return;
        PregnancyProbability = value;
        OnChanged?.Invoke();
    }

    private float GetPhasePregnancyProbability()
    {
        switch (CurrentCyclePhase)
        {
            case MenstrualCyclePhase.AfterPeriod:
            case MenstrualCyclePhase.AfterOvulation: return LowPregnancyProbability;
            case MenstrualCyclePhase.Ovulation: return HighPregnancyProbability;
            default: return 0f;
        }
    }

    /// <summary>自然階段從 1 起算；受精顯示已經過天數（0～9），懷孕不累計天數。</summary>
    public int CurrentPhaseDay => IsCyclePaused ? EarlyPregnancyDaysElapsed
        : CurrentCycleDay - GetPhaseStartDay(GetNaturalCyclePhase()) + 1;

    public event Action<int> OnCycleDayChanged;
    public event Action<MenstrualCyclePhase> OnCyclePhaseChanged;
    public event Action<OvulationDayType> OnOvulationDayTypeChanged;
    public event Action<PregnancyState> OnPregnancyStateChanged;
    public event Action<bool> OnConceiveAttempted;

    private static int RollAfterPeriodDays()
        => CycleRandom.Next(AfterPeriodBaseDays - AfterPeriodVariation, AfterPeriodBaseDays + AfterPeriodVariation + 1);

    private void InitializeCycle()
    {
        AfterPeriodDays = RollAfterPeriodDays();
        NextAfterPeriodDays = AfterPeriodDays;
        CurrentCycleDay = (initialCycleDay - 1) % CycleLength + 1;
        // 初始位置已度過月經後時，亦準備好下一輪的天數。
        if (CurrentCycleDay > PeriodDays + AfterPeriodDays) NextAfterPeriodDays = RollAfterPeriodDays();
        Pregnancy = PregnancyState.NotPregnant;
        EarlyPregnancyDaysElapsed = 0;
        // 必須先確定週期日與階段，才設定初始機率；初始化不發送事件。
        PregnancyProbability = GetPhasePregnancyProbability();
    }

    private void LoadCycle(HeroineCycleSaveData data)
    {
        if (data == null)
        {
            InitializeCycle();
            return;
        }

        AfterPeriodDays = Clamp(data.AfterPeriodDays, AfterPeriodBaseDays - AfterPeriodVariation, AfterPeriodBaseDays + AfterPeriodVariation);
        NextAfterPeriodDays = Clamp(data.NextAfterPeriodDays, AfterPeriodBaseDays - AfterPeriodVariation, AfterPeriodBaseDays + AfterPeriodVariation);
        CurrentCycleDay = Clamp(data.CurrentCycleDay, 1, CycleLength);
        Pregnancy = Enum.IsDefined(typeof(PregnancyState), data.Pregnancy) ? data.Pregnancy : PregnancyState.NotPregnant;
        EarlyPregnancyDaysElapsed = Pregnancy == PregnancyState.NotPregnant ? 0
            : Clamp(data.EarlyPregnancyDaysElapsed, 0, BeforePregnantDays);
        if (Pregnancy == PregnancyState.EarlyPregnancy && EarlyPregnancyDaysElapsed >= BeforePregnantDays)
            Pregnancy = PregnancyState.Pregnant;
        // 舊檔缺少機率時依已還原的階段補值；明確存入的 0 仍須保留。
        PregnancyProbability = data.PregnancyProbability.HasValue && !float.IsNaN(data.PregnancyProbability.Value)
            ? Math.Max(0f, Math.Min(1f, data.PregnancyProbability.Value))
            : GetPhasePregnancyProbability();
    }

    private MenstrualCyclePhase GetNaturalCyclePhase()
    {
        if (CurrentCycleDay <= PeriodDays) return MenstrualCyclePhase.Period;
        if (CurrentCycleDay <= PeriodDays + AfterPeriodDays) return MenstrualCyclePhase.AfterPeriod;
        if (CurrentCycleDay <= PeriodDays + AfterPeriodDays + OvulationDays) return MenstrualCyclePhase.Ovulation;
        if (CurrentCycleDay <= PeriodDays + AfterPeriodDays + OvulationDays + AfterOvulationDays) return MenstrualCyclePhase.AfterOvulation;
        return MenstrualCyclePhase.BeforePeriod;
    }

    private int GetPhaseStartDay(MenstrualCyclePhase phase)
    {
        switch (phase)
        {
            case MenstrualCyclePhase.Period: return 1;
            case MenstrualCyclePhase.AfterPeriod: return PeriodDays + 1;
            case MenstrualCyclePhase.Ovulation: return PeriodDays + AfterPeriodDays + 1;
            case MenstrualCyclePhase.AfterOvulation: return PeriodDays + AfterPeriodDays + OvulationDays + 1;
            case MenstrualCyclePhase.BeforePeriod: return PeriodDays + AfterPeriodDays + OvulationDays + AfterOvulationDays + 1;
            default: throw new ArgumentOutOfRangeException(nameof(phase));
        }
    }

    public void AdvanceCycleDay()
    {
        if (Pregnancy == PregnancyState.Pregnant)
        {
            // 懷孕雖然凍結週期，當日手動設定的機率仍須在換日時重設。
            SetPregnancyProbability(GetPhasePregnancyProbability());
            return;
        }
        var oldPhase = CurrentCyclePhase;
        var oldPregnancy = Pregnancy;
        var oldDayType = GetCurrentOvulationDayType();
        int oldDay = CurrentCycleDay;

        if (Pregnancy == PregnancyState.EarlyPregnancy)
        {
            EarlyPregnancyDaysElapsed++;
            if (EarlyPregnancyDaysElapsed >= BeforePregnantDays) Pregnancy = PregnancyState.Pregnant;
        }
        else
        {
            CurrentCycleDay++;
            if (CurrentCycleDay == PeriodDays + AfterPeriodDays + 1)
                NextAfterPeriodDays = RollAfterPeriodDays();
            if (CurrentCycleDay > CycleLength)
            {
                CurrentCycleDay = 1;
                AfterPeriodDays = NextAfterPeriodDays;
            }
        }

        NotifyCycleChanged(oldDay, oldPhase, oldPregnancy, oldDayType);
    }

    /// <summary>手動選擇自然階段時跳到該階段第 1 天；解除懷孕並原位恢復請用 ClearPregnancy。</summary>
    public void SetCyclePhase(MenstrualCyclePhase phase)
    {
        if (!Enum.IsDefined(typeof(MenstrualCyclePhase), phase)) return;
        if (phase == MenstrualCyclePhase.BeforePregnant) { SetPregnancyState(PregnancyState.EarlyPregnancy); return; }
        if (phase == MenstrualCyclePhase.Pregnant) { SetPregnancyState(PregnancyState.Pregnant); return; }

        var oldPhase = CurrentCyclePhase;
        var oldPregnancy = Pregnancy;
        var oldDayType = GetCurrentOvulationDayType();
        int oldDay = CurrentCycleDay;
        int newDay = GetPhaseStartDay(phase);
        // 手動跳過月經後的終點亦只刷新一次；同階段重設不會重抽。
        if (oldDay <= PeriodDays + AfterPeriodDays && newDay > PeriodDays + AfterPeriodDays)
            NextAfterPeriodDays = RollAfterPeriodDays();
        CurrentCycleDay = newDay;
        Pregnancy = PregnancyState.NotPregnant;
        EarlyPregnancyDaysElapsed = 0;
        NotifyCycleChanged(oldDay, oldPhase, oldPregnancy, oldDayType);
    }

    public void ClearPregnancy() => SetPregnancyState(PregnancyState.NotPregnant);

    public void SetPregnancyState(PregnancyState state)
    {
        if (!Enum.IsDefined(typeof(PregnancyState), state) || Pregnancy == state) return;
        var oldPhase = CurrentCyclePhase;
        var oldPregnancy = Pregnancy;
        var oldDayType = GetCurrentOvulationDayType();
        Pregnancy = state;
        EarlyPregnancyDaysElapsed = state == PregnancyState.Pregnant ? BeforePregnantDays : 0;
        NotifyCycleChanged(CurrentCycleDay, oldPhase, oldPregnancy, oldDayType);
    }

    private void NotifyCycleChanged(int oldDay, MenstrualCyclePhase oldPhase, PregnancyState oldPregnancy, OvulationDayType oldDayType)
    {
        // 先依更新後的階段重設機率，再通知觀察者，避免事件中讀到舊階段的值。
        // 每次換日都會執行，即使仍處於同一階段也會清除當日覆寫。
        PregnancyProbability = GetPhasePregnancyProbability();
        if (CurrentCycleDay != oldDay) OnCycleDayChanged?.Invoke(CurrentCycleDay);
        if (Pregnancy != oldPregnancy) OnPregnancyStateChanged?.Invoke(Pregnancy);
        if (CurrentCyclePhase != oldPhase) OnCyclePhaseChanged?.Invoke(CurrentCyclePhase);
        if (GetCurrentOvulationDayType() != oldDayType) OnOvulationDayTypeChanged?.Invoke(GetCurrentOvulationDayType());
        OnChanged?.Invoke();
    }

    public OvulationDayType GetCurrentOvulationDayType()
    {
        switch (CurrentCyclePhase)
        {
            case MenstrualCyclePhase.AfterPeriod:
            case MenstrualCyclePhase.AfterOvulation: return OvulationDayType.Normal;
            case MenstrualCyclePhase.Ovulation: return OvulationDayType.Danger;
            default: return OvulationDayType.Safe;
        }
    }

    /// <summary>依當下機率嘗試受精；partnerId 保留供舊呼叫相容，目前不記錄對象。</summary>
    public bool TryConceive(string partnerId = null)
    {
        bool conceived = !IsCyclePaused && PregnancyProbability > 0f && CycleRandom.NextDouble() < PregnancyProbability;
        if (conceived) SetPregnancyState(PregnancyState.EarlyPregnancy);
        OnConceiveAttempted?.Invoke(conceived);
        return conceived;
    }
}
