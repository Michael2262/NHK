using System;

/// <summary>正式女主角數值；每個實例代表一位角色，由 GameStatusService.Heroines 持有。</summary>
public class HeroineStatusModel
{
    // ==========================================================
    // 可調數值設定：同類放在一起。初始值於建立角色／新遊戲時套用。
    // ==========================================================

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

    // ───── 性興味 ─────
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
    public int Trust { get; private set; } = InitialTrust;
    public event Action<int> OnTrustChanged;
    public int Affinity { get; private set; } = InitialAffinity;
    public event Action<int> OnAffinityChanged;
    public int Mood { get; private set; } = InitialMood;
    public event Action<int> OnMoodChanged;
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
    }

    private static int Clamp(long value, int min, int max)
        => (int)Math.Max(min, Math.Min(max, value));

    // ==========================================================
    // 數值操作：先以 long 計算，超出範圍時截斷，避免 int 溢位。
    // ==========================================================

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

    // ───── 跨日結算：由 GameStatusService 對每位角色呼叫一次 ─────
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
        Trust = Trust, Affinity = Affinity, Mood = Mood, Libido = Libido,
        LibidoIncreasedToday = LibidoIncreasedToday,
        Excitement = Excitement, Orgasm = Orgasm, HCount = HCount,
        IsInHeat = IsInHeat, StatisticsData = Statistics.ToSaveData()
    };

    public void LoadFromSaveData(HeroineSaveData data)
    {
        if (data == null) return;
        CurrentEmotion = Enum.IsDefined(typeof(HeroineEmotionCardType), data.Emotion) ? data.Emotion : InitialEmotion;
        Trust = Clamp(data.Trust, TrustMin, TrustMax);
        Affinity = Clamp(data.Affinity, AffinityMin, AffinityMax);
        Mood = Clamp(data.Mood, MoodMin, MoodMax);
        Libido = Clamp(data.Libido, LibidoMin, LibidoMax);
        LibidoIncreasedToday = data.LibidoIncreasedToday;
        Excitement = Clamp(data.Excitement, ExcitementMin, ExcitementMax);
        Orgasm = Clamp(data.Orgasm, OrgasmMin, OrgasmMax);
        HCount = Math.Max(0, data.HCount);
        IsInHeat = data.IsInHeat;
        Statistics = new HeroineStatisticsModel();
        if (data.StatisticsData != null) Statistics.LoadFromSaveData(data.StatisticsData);
    }

    // ───── 未來系統預留接口：目前不保存處女、週期或懷孕進度 ─────
    public VirginityState Virginity => VirginityState.Virgin;
    public int CurrentCycleDay => 1;
    public bool IsCyclePaused => true;
    public PregnancyState Pregnancy => PregnancyState.NotPregnant;
    public int EarlyPregnancyDaysElapsed => 0;
    public event Action<VirginityState> OnVirginityChanged;
    public event Action<int> OnCycleDayChanged;
    public event Action<OvulationDayType> OnOvulationDayTypeChanged;
    public event Action<PregnancyState> OnPregnancyStateChanged;
    public event Action<bool> OnConceiveAttempted;
    public void SetVirginity(VirginityState state, string firstPartnerId = null) => OnVirginityChanged?.Invoke(state);
    public void AdvanceCycleDay() => OnCycleDayChanged?.Invoke(CurrentCycleDay);
    public OvulationDayType GetCurrentOvulationDayType() => OvulationDayType.Safe;
    public bool TryConceive(string partnerId = null)
    {
        OnConceiveAttempted?.Invoke(false);
        return false;
    }
}
