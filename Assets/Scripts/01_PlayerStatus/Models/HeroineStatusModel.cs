using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// NHK 版女主角狀態模型。
/// 保留 HeroineStatusModel 類名與多女主角架構，但核心內容改為「情緒卡池」。
///
/// 核心責任：
/// 1. 保存女主角識別資訊。
/// 2. 管理固定張數的情緒卡池。
/// 3. CurrentEmotion 永遠自動等於目前卡池的主導情緒。
/// 4. 提供情緒分數與卡片替換規則。
/// 5. 保存 H 次數。
///
/// CurrentEmotion 規則：
/// - 不允許外部主動設定。
/// - 每當情緒卡池變動，自動重新計算。
/// - 數量最多者成為 CurrentEmotion。
/// - 若數量一樣多，取 tied emotions 中最近加入卡池的情緒。
/// </summary>
public class HeroineStatusModel
{
    // ─────────────────────────────────────────────────────────────
    // Core identity
    // ─────────────────────────────────────────────────────────────
    public string HeroineID { get; }
    public string NameTextKey { get; }
    public string Name { get; }

    // ─────────────────────────────────────────────────────────────
    // NHK core state
    // ─────────────────────────────────────────────────────────────
    public int EmotionDeckMaxCount { get; private set; }
    public HeroineEmotionCardType CurrentEmotion { get; private set; }
    public int HCount { get; private set; }

    private readonly HeroineStatusConfig cfg;
    private readonly HeroineStat sourceStat;
    private readonly List<HeroineEmotionCardSaveData> emotionDeck = new List<HeroineEmotionCardSaveData>();
    private int nextEmotionCardOrder;

    public IReadOnlyList<HeroineEmotionCardSaveData> EmotionDeck => emotionDeck;

    public HeroineStatisticsModel Statistics { get; private set; }
    public ProtagonistStatusEffectModel StatusEffectModel { get; private set; }

    // ─────────────────────────────────────────────────────────────
    // NHK events
    // ─────────────────────────────────────────────────────────────
    public event Action OnEmotionDeckChanged;
    public event Action<HeroineEmotionCardType> OnEmotionCardAdded;
    public event Action<HeroineEmotionCardType> OnEmotionCardRemoved;
    public event Action<HeroineEmotionCardType> OnDominantEmotionChanged;
    public event Action<HeroineEmotionCardType> OnEmotionChanged;
    public event Action<int> OnHCountChanged;

    // ─────────────────────────────────────────────────────────────
    // Compatibility events from previous project.
    // NHK 新功能請不要再依賴這些事件。
    // ─────────────────────────────────────────────────────────────
    public event Action<int> OnLewdnessChanged;
    public event Action<int> OnAffinityChanged;
    public event Action<int> OnExcitementLevelChanged;
    public event Action<int> OnExcitementChanged;
    public event Action<int> OnDiscomfortChanged;
    public event Action OnDiscomfortFull;
    public event Action<int> OnOrgasmChanged;
    public event Action OnOrgasmFull;
    public event Action<int> OnPersonalSuspicionChanged;
    public event Action OnPersonalSuspicionReachedMax;
    public event Action<VirginityState> OnVirginityChanged;
    public event Action<int> OnExcitementDecayAmountChanged;
    public event Action<bool> OnInHeatChanged;
    public event Action<bool> OnEnragedChanged;
    public event Action OnHeroineFlagsChanged;
    public event Action<int> OnAttackChanged;
    public event Action<int> OnDefenseChanged;
    public event Action<int> OnPatienceChanged;
    public event Action<int> OnStaminaChanged;
    public event Action<int> OnSpiritChanged;
    public event Action<int> OnTotalExcitementLevelChanged;
    public event Action<int> OnCycleDayChanged;
    public event Action<OvulationDayType> OnOvulationDayTypeChanged;
    public event Action<PregnancyState> OnPregnancyStateChanged;
    public event Action<bool> OnConceiveAttempted;

    public HeroineStatusModel(HeroineStat stat, HeroineStatusConfig config)
    {
        if (stat == null) throw new ArgumentNullException(nameof(stat));

        sourceStat = stat;
        HeroineID = stat.ID;
        NameTextKey = stat.NameTextKey;
        Name = stat.Name;
        cfg = config;
        EmotionDeckMaxCount = Mathf.Max(1, config != null ? config.EmotionDeckMaxCount : 10);

        CurrentEmotion = HeroineEmotionCardType.Angry;
        HCount = 0;

        Statistics = new HeroineStatisticsModel();
        StatusEffectModel = new ProtagonistStatusEffectModel();
        if (StatusEffectModel != null)
        {
            StatusEffectModel.OnEffectsChanged += RecalculateTotalStats;
        }

        InitializeDefaultEmotionDeck(stat);
    }

    // ─────────────────────────────────────────────────────────────
    // Emotion deck initialization
    // ─────────────────────────────────────────────────────────────
    public void InitializeDefaultEmotionDeck(HeroineStat stat = null)
    {
        HeroineEmotionCardType oldDominant = GetDominantEmotion();

        emotionDeck.Clear();
        nextEmotionCardOrder = 0;

        HeroineStat targetStat = stat ?? sourceStat;
        AddInitialCards(targetStat, HeroineEmotionCardType.Angry);
        AddInitialCards(targetStat, HeroineEmotionCardType.Shy);
        AddInitialCards(targetStat, HeroineEmotionCardType.Worried);
        AddInitialCards(targetStat, HeroineEmotionCardType.Maternal);
        AddInitialCards(targetStat, HeroineEmotionCardType.Relaxed);
        AddInitialCards(targetStat, HeroineEmotionCardType.Disappointed);

        NormalizeDeckSize();
        NotifyDeckChanged(oldDominant, forceEmotionNotify: true);
    }

    private void AddInitialCards(HeroineStat stat, HeroineEmotionCardType type)
    {
        int count = cfg != null ? cfg.GetInitialCardCount(stat, type) : GetFallbackInitialCount(type);
        for (int i = 0; i < count; i++) AddCardInternal(type);
    }

    private int GetFallbackInitialCount(HeroineEmotionCardType type)
    {
        switch (type)
        {
            case HeroineEmotionCardType.Angry: return 8;
            case HeroineEmotionCardType.Shy: return 2;
            default: return 0;
        }
    }

    private void NormalizeDeckSize()
    {
        while (emotionDeck.Count > EmotionDeckMaxCount)
        {
            RemoveOldestCardInternal();
        }

        // 如果設定總數少於上限，補生氣卡到滿，以維持「固定張數」規則。
        while (emotionDeck.Count < EmotionDeckMaxCount)
        {
            AddCardInternal(HeroineEmotionCardType.Angry);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Emotion deck query
    // ─────────────────────────────────────────────────────────────
    public int GetCardCount(HeroineEmotionCardType type)
    {
        return emotionDeck.Count(c => c.Type == type);
    }

    public int GetEmotionDeckCount() => emotionDeck.Count;

    public HeroineEmotionCardType GetDominantEmotion()
    {
        if (emotionDeck.Count == 0) return HeroineEmotionCardType.Angry;
        return GetDominantEmotionWithoutFallback();
    }

    private HeroineEmotionCardType GetDominantEmotionWithoutFallback()
    {
        if (emotionDeck.Count == 0) return HeroineEmotionCardType.Angry;

        int maxCount = emotionDeck
            .GroupBy(c => c.Type)
            .Max(g => g.Count());

        var tiedTypes = emotionDeck
            .GroupBy(c => c.Type)
            .Where(g => g.Count() == maxCount)
            .Select(g => g.Key)
            .ToHashSet();

        // 平手時取 tied emotions 中最近加入的卡。
        return emotionDeck
            .Where(c => tiedTypes.Contains(c.Type))
            .OrderByDescending(c => c.AddedOrder)
            .First()
            .Type;
    }

    public int GetIntimateMoodScore()
    {
        return GetCardCount(HeroineEmotionCardType.Shy)
             + GetCardCount(HeroineEmotionCardType.Relaxed)
             + GetCardCount(HeroineEmotionCardType.Maternal);
    }

    public int GetNegativeMoodScore()
    {
        return GetCardCount(HeroineEmotionCardType.Angry)
             + GetCardCount(HeroineEmotionCardType.Disappointed);
    }

    public int GetCareMoodScore()
    {
        return GetCardCount(HeroineEmotionCardType.Worried)
             + GetCardCount(HeroineEmotionCardType.Maternal);
    }

    public bool IsDisappointmentHigh() => GetCardCount(HeroineEmotionCardType.Disappointed) >= 3;
    public bool IsMaternalHigh() => GetCardCount(HeroineEmotionCardType.Maternal) >= 3;
    public bool IsShyHigh() => GetCardCount(HeroineEmotionCardType.Shy) >= 3;

    // ─────────────────────────────────────────────────────────────
    // Emotion deck mutation
    // ─────────────────────────────────────────────────────────────
    public void AddEmotionCard(HeroineEmotionCardType type)
    {
        ReplaceEmotionCard(type);
    }

    public void ReplaceEmotionCard(HeroineEmotionCardType newType)
    {
        HeroineEmotionCardType oldDominant = GetDominantEmotion();

        AddCardInternal(newType);
        OnEmotionCardAdded?.Invoke(newType);

        while (emotionDeck.Count > EmotionDeckMaxCount)
        {
            HeroineEmotionCardSaveData cardToRemove = FindCardToRemove(newType);
            RemoveCardInternal(cardToRemove);
        }

        NotifyDeckChanged(oldDominant);
    }

    public bool RemoveOneCardOfType(HeroineEmotionCardType type)
    {
        HeroineEmotionCardType oldDominant = GetDominantEmotion();
        var target = emotionDeck
            .Where(c => c.Type == type)
            .OrderBy(c => c.AddedOrder)
            .FirstOrDefault();

        if (target == null) return false;

        RemoveCardInternal(target);
        NotifyDeckChanged(oldDominant);
        return true;
    }

    public void RemoveOldestCard()
    {
        HeroineEmotionCardType oldDominant = GetDominantEmotion();
        RemoveOldestCardInternal();
        NotifyDeckChanged(oldDominant);
    }

    private void AddCardInternal(HeroineEmotionCardType type)
    {
        emotionDeck.Add(new HeroineEmotionCardSaveData
        {
            Type = type,
            AddedOrder = nextEmotionCardOrder++
        });
    }

    private void RemoveOldestCardInternal()
    {
        if (emotionDeck.Count == 0) return;
        var oldest = emotionDeck.OrderBy(c => c.AddedOrder).First();
        RemoveCardInternal(oldest);
    }

    private void RemoveCardInternal(HeroineEmotionCardSaveData card)
    {
        if (card == null) return;
        if (emotionDeck.Remove(card))
        {
            OnEmotionCardRemoved?.Invoke(card.Type);
        }
    }

    private HeroineEmotionCardSaveData FindCardToRemove(HeroineEmotionCardType newType)
    {
        foreach (var opposite in GetOppositeEmotionPriority(newType))
        {
            var target = emotionDeck
                .Where(c => c.Type == opposite)
                .OrderBy(c => c.AddedOrder)
                .FirstOrDefault();
            if (target != null) return target;
        }

        var groups = emotionDeck
            .GroupBy(c => c.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToList();

        foreach (var g in groups)
        {
            if (g.Type == newType && groups.Count > 1) continue;
            var target = emotionDeck
                .Where(c => c.Type == g.Type)
                .OrderBy(c => c.AddedOrder)
                .FirstOrDefault();
            if (target != null) return target;
        }

        return emotionDeck.OrderBy(c => c.AddedOrder).FirstOrDefault();
    }

    public HeroineEmotionCardType[] GetOppositeEmotionPriority(HeroineEmotionCardType newType)
    {
        switch (newType)
        {
            case HeroineEmotionCardType.Angry:
                return new[] { HeroineEmotionCardType.Relaxed, HeroineEmotionCardType.Shy, HeroineEmotionCardType.Maternal };
            case HeroineEmotionCardType.Shy:
                return new[] { HeroineEmotionCardType.Angry, HeroineEmotionCardType.Disappointed, HeroineEmotionCardType.Relaxed };
            case HeroineEmotionCardType.Worried:
                return new[] { HeroineEmotionCardType.Angry, HeroineEmotionCardType.Relaxed, HeroineEmotionCardType.Shy };
            case HeroineEmotionCardType.Maternal:
                return new[] { HeroineEmotionCardType.Angry, HeroineEmotionCardType.Disappointed, HeroineEmotionCardType.Relaxed };
            case HeroineEmotionCardType.Relaxed:
                return new[] { HeroineEmotionCardType.Angry, HeroineEmotionCardType.Worried, HeroineEmotionCardType.Disappointed };
            case HeroineEmotionCardType.Disappointed:
                return new[] { HeroineEmotionCardType.Relaxed, HeroineEmotionCardType.Shy, HeroineEmotionCardType.Maternal, HeroineEmotionCardType.Worried };
            default:
                return Array.Empty<HeroineEmotionCardType>();
        }
    }

    private void NotifyDeckChanged(HeroineEmotionCardType oldDominant, bool forceEmotionNotify = false)
    {
        OnEmotionDeckChanged?.Invoke();

        HeroineEmotionCardType newDominant = GetDominantEmotion();
        if (newDominant != oldDominant)
        {
            OnDominantEmotionChanged?.Invoke(newDominant);
        }

        UpdateCurrentEmotionFromDeck(forceEmotionNotify);
    }

    private void UpdateCurrentEmotionFromDeck(bool forceNotify = false)
    {
        HeroineEmotionCardType newEmotion = GetDominantEmotion();
        if (!forceNotify && CurrentEmotion == newEmotion) return;

        CurrentEmotion = newEmotion;
        OnEmotionChanged?.Invoke(CurrentEmotion);
    }

    // ─────────────────────────────────────────────────────────────
    // H count
    // ─────────────────────────────────────────────────────────────
    public void AddHCount(int amount = 1)
    {
        if (amount == 0) return;
        HCount = Mathf.Max(0, HCount + amount);
        OnHCountChanged?.Invoke(HCount);
    }

    public void SetHCount(int value)
    {
        value = Mathf.Max(0, value);
        if (HCount == value) return;
        HCount = value;
        OnHCountChanged?.Invoke(HCount);
    }

    // ─────────────────────────────────────────────────────────────
    // Save / load
    // ─────────────────────────────────────────────────────────────
    public HeroineSaveData ToSaveData()
    {
        return new HeroineSaveData
        {
            Emotion = CurrentEmotion,
            HCount = HCount,
            NextEmotionCardOrder = nextEmotionCardOrder,
            EmotionDeckData = emotionDeck
                .Select(c => new HeroineEmotionCardSaveData { Type = c.Type, AddedOrder = c.AddedOrder })
                .ToList(),
            StatisticsData = Statistics?.ToSaveData(),
            StatusEffectData = StatusEffectModel?.ToSaveData()
        };
    }

    public void LoadFromSaveData(HeroineSaveData data)
    {
        LoadFromSaveData(data, null);
    }

    public void LoadFromSaveData(HeroineSaveData data, StatusEffectDatabase effectDatabase)
    {
        if (data == null) return;

        HeroineEmotionCardType oldDominant = GetDominantEmotion();

        HCount = Mathf.Max(0, data.HCount);
        nextEmotionCardOrder = Mathf.Max(0, data.NextEmotionCardOrder);

        emotionDeck.Clear();
        if (data.EmotionDeckData != null && data.EmotionDeckData.Count > 0)
        {
            foreach (var card in data.EmotionDeckData)
            {
                if (card == null) continue;
                emotionDeck.Add(new HeroineEmotionCardSaveData { Type = card.Type, AddedOrder = card.AddedOrder });
                nextEmotionCardOrder = Mathf.Max(nextEmotionCardOrder, card.AddedOrder + 1);
            }
        }
        else
        {
            InitializeDefaultEmotionDeck(sourceStat);
        }

        NormalizeDeckSize();

        Statistics = new HeroineStatisticsModel();
        if (data.StatisticsData != null) Statistics.LoadFromSaveData(data.StatisticsData);

        StatusEffectModel = new ProtagonistStatusEffectModel();
        if (StatusEffectModel != null)
        {
            StatusEffectModel.OnEffectsChanged += RecalculateTotalStats;
        }
        if (data.StatusEffectData != null) StatusEffectModel.LoadFromSaveData(data.StatusEffectData, effectDatabase);

        OnHCountChanged?.Invoke(HCount);
        NotifyDeckChanged(oldDominant, forceEmotionNotify: true);
    }

    // ─────────────────────────────────────────────────────────────
    // Compatibility layer from previous project.
    // 這些成員只為了降低舊腳本編譯錯誤。NHK 新腳本請不要再使用。
    // ─────────────────────────────────────────────────────────────
    public int LewdnessLevel => GetCardCount(HeroineEmotionCardType.Shy);
    public int LewdnessExp => 0;
    public int BaseAffinityLevel => GetCardCount(HeroineEmotionCardType.Relaxed);
    public int BaseAffinityExp => 0;
    public int BaseExcitementLevel => GetCardCount(HeroineEmotionCardType.Shy);
    public int BaseExcitementExp => 0;
    public int TotalExcitementLevel => BaseExcitementLevel;

    public int Discomfort => 0;
    public int Orgasm => 0;
    public int DiscomfortMax => 100;
    public int OrgasmMax => 100;

    public int PersonalSuspicion => GetNegativeMoodScore();
    public int PersonalSuspicionMax => EmotionDeckMaxCount;
    public int SuspicionDecayAmount => 0;
    public int SuspicionReliefAmount => 0;
    public bool IsSuspicionAtMax => IsDisappointmentHigh();

    public VirginityState Virginity => VirginityState.Virgin;
    public int CurrentCycleDay => 1;
    public bool IsCyclePaused => true;
    public PregnancyState Pregnancy => PregnancyState.NotPregnant;
    public int EarlyPregnancyDaysElapsed => 0;

    public int BaseAttack => 0;
    public int BaseDefense => 0;
    public int TotalAttack => 0;
    public int TotalDefense => 0;
    public int Patience => 0;
    public int Stamina => 100;
    public int StaminaMax => 100;
    public int Spirit => 100;
    public int SpiritMax => 100;
    public int ExcitementDecayAmount => 0;
    public bool IsInHeat => false;
    public bool IsEnraged => false;

    public bool IsGoneForever => false;
    public int AbsentUntilDay => 0;
    public int AbsentUntilPhase => -1;

    public void RecalculateTotalStats()
    {
        OnTotalExcitementLevelChanged?.Invoke(TotalExcitementLevel);
    }

    public int GetCurrentLewdnessThreshold(int level) => 100;
    public int GetCurrentAffinityThreshold(int level) => 100;
    public int GetCurrentExcitementThreshold(int level) => 100;

    public void AddLewdnessExp(int delta)
    {
        if (delta > 0) ReplaceEmotionCard(HeroineEmotionCardType.Shy);
        else if (delta < 0) ReplaceEmotionCard(HeroineEmotionCardType.Angry);
        OnLewdnessChanged?.Invoke(delta);
    }
    public void ReduceLewdnessExp(int delta) => AddLewdnessExp(-delta);
    public void SetLewdness(int level, int exp = 0) => OnLewdnessChanged?.Invoke(level);

    public void AddAffinityExp(int delta)
    {
        if (delta > 0) ReplaceEmotionCard(HeroineEmotionCardType.Relaxed);
        else if (delta < 0) ReplaceEmotionCard(HeroineEmotionCardType.Disappointed);
        OnAffinityChanged?.Invoke(delta);
    }
    public void ReduceAffinityExp(int delta) => AddAffinityExp(-delta);
    public void SetAffinity(int level, int exp = 0) => OnAffinityChanged?.Invoke(level);
    public void SetAffinityLevelLock(int level, bool isLocked) { }
    public bool IsAffinityLevelLocked(int level) => false;

    public void AddExcitementExp(int delta)
    {
        if (delta > 0) ReplaceEmotionCard(HeroineEmotionCardType.Shy);
        else if (delta < 0) ReplaceEmotionCard(HeroineEmotionCardType.Relaxed);
        OnExcitementChanged?.Invoke(delta);
        OnExcitementLevelChanged?.Invoke(BaseExcitementLevel);
    }
    public void ReduceExcitementExp(int delta) => AddExcitementExp(-delta);
    public void SetExcitement(int level, int exp = 0)
    {
        OnExcitementLevelChanged?.Invoke(level);
        OnExcitementChanged?.Invoke(exp);
    }
    public void SetExcitementLevelLock(int level, bool isLocked) { }
    public bool IsExcitementLevelLocked(int level) => false;

    public void AddDiscomfort(int amount)
    {
        if (amount > 0) ReplaceEmotionCard(HeroineEmotionCardType.Angry);
        OnDiscomfortChanged?.Invoke(amount);
    }
    public void ReduceDiscomfort(int amount) => AddDiscomfort(-amount);
    public void ResetDiscomfort() => OnDiscomfortChanged?.Invoke(0);

    public void AddOrgasm(int amount)
    {
        OnOrgasmChanged?.Invoke(amount);
        if (amount > 0) AddHCount(1);
    }
    public void ReduceOrgasm(int amount) => AddOrgasm(-amount);
    public void ResetOrgasm() => OnOrgasmChanged?.Invoke(0);

    public void AddPersonalSuspicion(int amount)
    {
        if (amount > 0) ReplaceEmotionCard(HeroineEmotionCardType.Disappointed);
        OnPersonalSuspicionChanged?.Invoke(amount);
    }
    public void ReducePersonalSuspicion(int amount) => AddPersonalSuspicion(-amount);
    public void SetPersonalSuspicion(int value) => OnPersonalSuspicionChanged?.Invoke(value);
    public void SetPersonalSuspicionMax(int value) => OnPersonalSuspicionChanged?.Invoke(PersonalSuspicion);
    public void AddPersonalSuspicionMax(int value) => OnPersonalSuspicionChanged?.Invoke(PersonalSuspicion);
    public void SetSuspicionDecayAmount(int amount) => OnPersonalSuspicionChanged?.Invoke(PersonalSuspicion);
    public void AddSuspicionDecayAmount(int amount) => OnPersonalSuspicionChanged?.Invoke(PersonalSuspicion);
    public void ApplySuspicionDecay() { }
    public void ApplySuspicionRelief() { }

    public void SetVirginity(VirginityState state, string firstPartnerId = null) => OnVirginityChanged?.Invoke(state);
    public void SetExcitementDecayAmount(int amount) => OnExcitementDecayAmountChanged?.Invoke(amount);
    public void SetInHeat(bool value) => OnInHeatChanged?.Invoke(value);
    public void SetEnraged(bool value) => OnEnragedChanged?.Invoke(value);
    public void ApplyExcitementDecay() => OnExcitementChanged?.Invoke(0);

    public void AddAttack(int amount) => OnAttackChanged?.Invoke(amount);
    public void AddDefense(int amount) => OnDefenseChanged?.Invoke(amount);
    public void AddPatience(int amount) => OnPatienceChanged?.Invoke(amount);
    public void AddStamina(int amount) => OnStaminaChanged?.Invoke(amount);
    public void AddSpirit(int amount) => OnSpiritChanged?.Invoke(amount);

    public void SetAbsentForever() => OnHeroineFlagsChanged?.Invoke();
    public void SetAbsentUntil(int day, int phase = -1) => OnHeroineFlagsChanged?.Invoke();
    public void ClearAbsentStatus() => OnHeroineFlagsChanged?.Invoke();
    public bool IsCurrentlyAbsent(int currentDay, int currentPhase) => false;

    public void AdvanceCycleDay() => OnCycleDayChanged?.Invoke(CurrentCycleDay);
    public OvulationDayType GetCurrentOvulationDayType() => OvulationDayType.Safe;
    public bool TryConceive(string partnerId = null)
    {
        OnConceiveAttempted?.Invoke(false);
        return false;
    }
}
