using System;

/// <summary>正式主角數值；可調設定集中於頂部，存讀檔由 Service 統一管理。</summary>
public class ProtagonistStatusModel
{
    // ==========================================================
    // 數值設定區：同類數值放在一起，修改後重新開始遊戲套用。
    // 初始值須位於上下限內；上限與每日恢復量請使用非負數。
    // ForceAdd 仍忽略遊戲上下限；int 的儲存限制不是平衡參數。
    // ==========================================================

    // ───── 體力 ─────
    public const int StaminaMin = 0;                 // 一般 Set／Add 的下限
    public const int InitialStaminaMax = 25;          // 初始上限；開局體力補滿
    public const int StaminaDailyRecovery = 35;       // 非負體力每日恢復量
    public const int NegativeStaminaDailyReset = 10;  // 負體力換日直接設為此值，不再加恢復量

    // ───── 射精次數 ─────
    public const int ShootTimesMin = 0;              // 一般 Set／Add 的下限
    public const int InitialShootTimesMax = 1;       // 初始上限；開局補滿
    public const int ShootTimesDailyRecovery = 1;   // 尚未耗盡時每日恢復量
    public const int ExhaustedShootTimesDailyReset = 1; // 當前值 <= 0 時直接恢復至此值，仍受上限限制

    // ───── 邪念 ─────
    public const int LustMin = 0;
    public const int LustMax = 100;
    public const int InitialLust = 0;

    // ───── 貼紙 ─────
    public const int StickerCountMin = 0;
    public const int StickerCountMax = 999;
    public const int InitialStickerCount = 0;

    // ───── 金錢 ─────
    public const int InitialMoney = 1000;              // 開局金錢；有效範圍 0～999999

    // ───── 技能點 ─────
    public const int InitialSkillPoints = 0;

    // ───── 射精感 ─────
    public const int SemenMin = 0;
    public const int SemenLimit = 100;
    public const int InitialSemen = 0;

    // ───── 房間髒亂 ─────
    public const int RoomMessLevelMin = 0;
    public const int RoomMessLevelMax = 25;
    public const int InitialRoomMessLevel = 25;
    public const int RoomMessDailyGain = 1;
    public const float RoomMessDailyGainChance = 0.5f;

    // ==========================================================
    // 執行期間狀態：上面的設定決定初始值，下方方法負責變更。
    // ==========================================================
    public int StaminaMax { get; private set; } = InitialStaminaMax;
    public int Stamina { get; private set; } = InitialStaminaMax;
    public int ShootTimesMax { get; private set; } = InitialShootTimesMax;
    public int ShootTimes { get; private set; } = InitialShootTimesMax;
    public int Lust { get; private set; } = InitialLust;
    public int StickerCount { get; private set; } = InitialStickerCount;
    public int Money { get; private set; } = ClampMoney(InitialMoney);

    public int SkillPoints { get; private set; } = InitialSkillPoints;
    public int Semen { get; private set; } = InitialSemen;
    public int SemenMax => SemenLimit;
    public int RoomMessLevel { get; private set; } = InitialRoomMessLevel;
    public bool IsOverShoot => ShootTimes <= 0;

    /// <summary>狀態完成更新後通知；訂閱者讀取目前屬性。</summary>
    public event Action OnChanged;

    // 保留既有事件語意：金錢、技能點、射精次數、房間髒亂傳變化量；射精感傳最新值。
    public event Action<int> OnMoneyChanged;
    public event Action<int> OnSkillPointsChanged;
    public event Action<int> OnShootTimesChanged;
    public event Action<int> OnRoomMessLevelChanged;
    public event Action<int> OnSemenChanged;

    public ProtagonistStatusModel() => NewGame(notify: false);

    // 先以 long 計算，避免 int 加法溢位導致正負反轉。
    private static int Clamp(long value, int min, int max)
        => (int)Math.Max(min, Math.Min(max, value));

    private void NotifyIfChanged(int previous, int current, Action<int> deltaEvent = null)
    {
        if (previous == current) return;
        // ForceAdd 可橫跨整個 int 範圍；舊 delta 事件以 int 可表示範圍限制。
        deltaEvent?.Invoke(Clamp((long)current - previous, int.MinValue, int.MaxValue));
        OnChanged?.Invoke();
    }

    /// <summary>增加上限不補體力；降低上限時僅裁切超出部分，保留負值。</summary>
    public void SetStaminaMax(int value)
    {
        value = Math.Max(StaminaMin, value);
        if (value == StaminaMax) return;
        if (value < StaminaMax)
            Stamina = Math.Min(Stamina, value);
        StaminaMax = value;
        OnChanged?.Invoke();
    }

    public void AddStaminaMax(int amount)
        => SetStaminaMax(Clamp((long)StaminaMax + amount, StaminaMin, int.MaxValue));

    public void SetStamina(int value)
    {
        int previous = Stamina;
        Stamina = Clamp(value, StaminaMin, StaminaMax);
        NotifyIfChanged(previous, Stamina);
    }

    public void AddStamina(int amount)
        => SetStamina(Clamp((long)Stamina + amount, StaminaMin, StaminaMax));

    /// <summary>忽略遊戲上下限，僅受 int 可表示範圍限制。</summary>
    public void ForceAddStamina(int amount)
    {
        int previous = Stamina;
        Stamina = Clamp((long)Stamina + amount, int.MinValue, int.MaxValue);
        NotifyIfChanged(previous, Stamina);
    }

    /// <summary>增加上限不補次數；降低上限時僅裁切超出部分，保留負值。</summary>
    public void SetShootTimesMax(int value)
    {
        value = Math.Max(ShootTimesMin, value);
        if (value == ShootTimesMax) return;
        int previous = ShootTimes;
        if (value < ShootTimesMax)
            ShootTimes = Math.Min(ShootTimes, value);
        ShootTimesMax = value;
        if (previous != ShootTimes) OnShootTimesChanged?.Invoke(Clamp((long)ShootTimes - previous, int.MinValue, int.MaxValue));
        OnChanged?.Invoke();
    }

    public void AddShootTimesMax(int amount)
        => SetShootTimesMax(Clamp((long)ShootTimesMax + amount, ShootTimesMin, int.MaxValue));

    public void SetShootTimes(int value)
    {
        int previous = ShootTimes;
        ShootTimes = Clamp(value, ShootTimesMin, ShootTimesMax);
        NotifyIfChanged(previous, ShootTimes, OnShootTimesChanged);
    }

    public void AddShootTimes(int amount)
        => SetShootTimes(Clamp((long)ShootTimes + amount, ShootTimesMin, ShootTimesMax));

    /// <summary>忽略遊戲上下限，僅受 int 可表示範圍限制。</summary>
    public void ForceAddShootTimes(int amount)
    {
        int previous = ShootTimes;
        ShootTimes = Clamp((long)ShootTimes + amount, int.MinValue, int.MaxValue);
        NotifyIfChanged(previous, ShootTimes, OnShootTimesChanged);
    }

    public void SetLust(int value)
    {
        int previous = Lust;
        Lust = Clamp(value, LustMin, LustMax);
        NotifyIfChanged(previous, Lust);
    }

    public void AddLust(int amount) => SetLust(Clamp((long)Lust + amount, LustMin, LustMax));

    public void SetStickerCount(int value)
    {
        int previous = StickerCount;
        StickerCount = Clamp(value, StickerCountMin, StickerCountMax);
        NotifyIfChanged(previous, StickerCount);
    }

    public void AddStickerCount(int amount)
        => SetStickerCount(Clamp((long)StickerCount + amount, StickerCountMin, StickerCountMax));

    // 金錢上下限固定於此，不列入頂部的可調設定區。
    private static int ClampMoney(long value) => Clamp(value, 0, 999999);

    public void SetMoney(int value)
    {
        int previous = Money;
        Money = ClampMoney(value);
        NotifyIfChanged(previous, Money, OnMoneyChanged);
    }

    /// <summary>正數增加、負數扣除；先以 long 相加，避免整數溢位。</summary>
    public void AddMoney(int amount) => SetMoney(ClampMoney((long)Money + amount));

    public bool CanReduceMoney(int cost) => cost >= 0 && Money >= cost;
    public bool TryReduceMoney(int cost)
    {
        if (!CanReduceMoney(cost)) return false;
        AddMoney(-cost);
        return true;
    }

    public void SetSkillPoints(int value)
    {
        int previous = SkillPoints;
        SkillPoints = Math.Max(0, value);
        NotifyIfChanged(previous, SkillPoints, OnSkillPointsChanged);
    }
    public void AddSkillPoints(int amount) => SetSkillPoints(Clamp((long)SkillPoints + amount, 0, int.MaxValue));
    public void ReduceSkillPoints(int amount) { if (amount > 0) AddSkillPoints(-amount); }
    public bool CanReduceSkillPoints(int cost) => cost >= 0 && SkillPoints >= cost;
    public bool TryReduceSkillPoints(int cost)
    {
        if (!CanReduceSkillPoints(cost)) return false;
        AddSkillPoints(-cost);
        return true;
    }

    public int GetSemen() => Semen;
    public void SetSemen(int value)
    {
        value = Clamp(value, SemenMin, SemenLimit);
        if (Semen == value) return;
        Semen = value;
        OnSemenChanged?.Invoke(Semen);
        OnChanged?.Invoke();
    }
    public void AddSemen(int amount) => SetSemen(Clamp((long)Semen + amount, SemenMin, SemenLimit));
    public void ApplySemenDailyReset() => SetSemen(SemenMin);

    public int CheckRoomMessLevel() => RoomMessLevel;
    public void SetRoomMessLevel(int value)
    {
        int previous = RoomMessLevel;
        RoomMessLevel = Clamp(value, RoomMessLevelMin, RoomMessLevelMax);
        NotifyIfChanged(previous, RoomMessLevel, OnRoomMessLevelChanged);
    }
    public void AddRoomMessLevel(int amount)
        => SetRoomMessLevel(Clamp((long)RoomMessLevel + amount, RoomMessLevelMin, RoomMessLevelMax));
    public void ReduceRoomMessLevel(int amount) { if (amount > 0) AddRoomMessLevel(-amount); }

    /// <summary>日終保留房間隨機變髒；日初恢復體力、射精次數並清空射精感。</summary>
    public void NextDay()
    {
        OnDayEnd();
        OnDayStart();
    }

    public void OnDayEnd()
    {
        if (UnityEngine.Random.value < RoomMessDailyGainChance)
            AddRoomMessLevel(RoomMessDailyGain);
    }

    public void OnDayStart()
    {
        int previousStamina = Stamina;
        int previousShootTimes = ShootTimes;
        int previousSemen = Semen;
        Stamina = Stamina < 0
            ? Clamp(NegativeStaminaDailyReset, StaminaMin, StaminaMax)
            : Clamp((long)Stamina + StaminaDailyRecovery, StaminaMin, StaminaMax);
        ShootTimes = ShootTimes <= 0
            ? Clamp(ExhaustedShootTimesDailyReset, ShootTimesMin, ShootTimesMax)
            : Clamp((long)ShootTimes + ShootTimesDailyRecovery, ShootTimesMin, ShootTimesMax);
        Semen = SemenMin;
        // 完成整批變更後才通知；避免讀到一半的日初狀態。
        if (previousShootTimes != ShootTimes)
            OnShootTimesChanged?.Invoke(Clamp((long)ShootTimes - previousShootTimes, int.MinValue, int.MaxValue));
        if (previousSemen != Semen) OnSemenChanged?.Invoke(Semen);
        if (previousStamina != Stamina || previousShootTimes != ShootTimes || previousSemen != Semen)
            OnChanged?.Invoke();
    }

    /// <summary>開局補滿體力與射精次數；整批初始化由 Service 統一通知刷新。</summary>
    public void NewGame(bool notify = true)
    {
        StaminaMax = Math.Max(StaminaMin, InitialStaminaMax);
        Stamina = StaminaMax;
        ShootTimesMax = Math.Max(ShootTimesMin, InitialShootTimesMax);
        ShootTimes = ShootTimesMax;
        Lust = Clamp(InitialLust, LustMin, LustMax);
        StickerCount = Clamp(InitialStickerCount, StickerCountMin, StickerCountMax);
        Money = ClampMoney(InitialMoney);
        SkillPoints = Math.Max(0, InitialSkillPoints);
        Semen = Clamp(InitialSemen, SemenMin, SemenLimit);
        RoomMessLevel = Clamp(InitialRoomMessLevel, RoomMessLevelMin, RoomMessLevelMax);
        if (notify)
        {
            OnMoneyChanged?.Invoke(0);
            OnSkillPointsChanged?.Invoke(0);
            OnShootTimesChanged?.Invoke(0);
            OnRoomMessLevelChanged?.Invoke(0);
            OnSemenChanged?.Invoke(Semen);
            OnChanged?.Invoke();
        }
    }

    public ProtagonistSaveData ToSaveData() => new ProtagonistSaveData
    {
        StaminaMax = StaminaMax, Stamina = Stamina,
        ShootTimesMax = ShootTimesMax, ShootTimes = ShootTimes,
        Lust = Lust, StickerCount = StickerCount,
        Money = Money, SkillPoints = SkillPoints, Semen = Semen, RoomMessLevel = RoomMessLevel
    };

    /// <summary>靜默還原；完成所有 Model 載入後由 OnGameStatusLoaded 統一刷新。</summary>
    public void LoadFromSaveData(ProtagonistSaveData data)
    {
        if (data == null) { NewGame(notify: false); return; }
        StaminaMax = Math.Max(StaminaMin, data.StaminaMax);
        ShootTimesMax = Math.Max(ShootTimesMin, data.ShootTimesMax);
        // ForceAdd 的負值及超上限值都是合法狀態，讀檔不得裁切。
        Stamina = data.Stamina;
        ShootTimes = data.ShootTimes;
        Lust = Clamp(data.Lust, LustMin, LustMax);
        StickerCount = Clamp(data.StickerCount, StickerCountMin, StickerCountMax);
        Money = ClampMoney(data.Money);
        SkillPoints = Math.Max(0, data.SkillPoints);
        Semen = Clamp(data.Semen, SemenMin, SemenLimit);
        RoomMessLevel = Clamp(data.RoomMessLevel, RoomMessLevelMin, RoomMessLevelMax);
    }
}
