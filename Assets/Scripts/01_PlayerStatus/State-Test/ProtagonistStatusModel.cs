using System;

namespace NHK.StateTest
{
    /// <summary>測試版主角數值；不參與正式存讀檔。</summary>
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

        // ───── 疲憊 ─────
        public const bool InitialIsExhausted = false;    // 初始狀態；目前由外部手動設定

        // ───── 貼紙 ─────
        public const int StickerCountMin = 0;
        public const int StickerCountMax = 999;          // 待領貼紙兌換也使用此持有上限
        public const int InitialStickerCount = 0;

        // ───── 達標未領貼紙 ─────
        public const int PendingStickerCountMin = 0;    // 維持 0，代表沒有待領貼紙
        public const int PendingStickerCountMax = 999;
        public const int InitialPendingStickerCount = 0;

        // ───── 金錢 ─────
        public const int InitialMoney = 0;              // 開局金錢；有效範圍 0～999999

        // ==========================================================
        // 執行期間狀態：上面的設定決定初始值，下方方法負責變更。
        // ==========================================================
        public int StaminaMax { get; private set; } = InitialStaminaMax;
        public int Stamina { get; private set; } = InitialStaminaMax;
        public int ShootTimesMax { get; private set; } = InitialShootTimesMax;
        public int ShootTimes { get; private set; } = InitialShootTimesMax;
        public int Lust { get; private set; } = InitialLust;
        public bool IsExhausted { get; private set; } = InitialIsExhausted;
        public int StickerCount { get; private set; } = InitialStickerCount;
        public int PendingStickerCount { get; private set; } = InitialPendingStickerCount;
        public int Money { get; private set; } = ClampMoney(InitialMoney);

        /// <summary>狀態完成更新後通知；訂閱者讀取目前屬性。</summary>
        public event Action OnChanged;

        // 先以 long 計算，避免 int 加法溢位導致正負反轉。
        private static int Clamp(long value, int min, int max)
            => (int)Math.Max(min, Math.Min(max, value));

        private void NotifyIfChanged(int previous, int current)
        {
            if (previous != current) OnChanged?.Invoke();
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
            if (value < ShootTimesMax)
                ShootTimes = Math.Min(ShootTimes, value);
            ShootTimesMax = value;
            OnChanged?.Invoke();
        }

        public void AddShootTimesMax(int amount)
            => SetShootTimesMax(Clamp((long)ShootTimesMax + amount, ShootTimesMin, int.MaxValue));

        public void SetShootTimes(int value)
        {
            int previous = ShootTimes;
            ShootTimes = Clamp(value, ShootTimesMin, ShootTimesMax);
            NotifyIfChanged(previous, ShootTimes);
        }

        public void AddShootTimes(int amount)
            => SetShootTimes(Clamp((long)ShootTimes + amount, ShootTimesMin, ShootTimesMax));

        /// <summary>忽略遊戲上下限，僅受 int 可表示範圍限制。</summary>
        public void ForceAddShootTimes(int amount)
        {
            int previous = ShootTimes;
            ShootTimes = Clamp((long)ShootTimes + amount, int.MinValue, int.MaxValue);
            NotifyIfChanged(previous, ShootTimes);
        }

        public void SetLust(int value)
        {
            int previous = Lust;
            Lust = Clamp(value, LustMin, LustMax);
            NotifyIfChanged(previous, Lust);
        }

        public void AddLust(int amount) => SetLust(Clamp((long)Lust + amount, LustMin, LustMax));

        public void SetIsExhausted(bool value)
        {
            if (IsExhausted == value) return;
            IsExhausted = value;
            OnChanged?.Invoke();
        }

        public void SetStickerCount(int value)
        {
            int previous = StickerCount;
            StickerCount = Clamp(value, StickerCountMin, StickerCountMax);
            NotifyIfChanged(previous, StickerCount);
        }

        public void AddStickerCount(int amount)
            => SetStickerCount(Clamp((long)StickerCount + amount, StickerCountMin, StickerCountMax));

        public void SetPendingStickerCount(int value)
        {
            int previous = PendingStickerCount;
            PendingStickerCount = Clamp(value, PendingStickerCountMin, PendingStickerCountMax);
            NotifyIfChanged(previous, PendingStickerCount);
        }

        public void AddPendingStickerCount(int amount)
            => SetPendingStickerCount(Clamp((long)PendingStickerCount + amount,
                PendingStickerCountMin, PendingStickerCountMax));

        /// <summary>盡量將待領貼紙轉入持有量；放不下的保留待領，沒有轉入時回傳 false。</summary>
        public bool RedeemPendingStickers()
        {
            int availableSpace = Math.Max(0, StickerCountMax - StickerCount);
            int redeemed = Math.Min(PendingStickerCount, availableSpace);
            if (redeemed <= 0) return false;

            // 兩個數值都完成更新後才通知，避免 UI 看到只更新一半的資料。
            StickerCount += redeemed;
            PendingStickerCount -= redeemed;
            OnChanged?.Invoke();
            return true;
        }

        // 金錢上下限固定於此，不列入頂部的可調設定區。
        private static int ClampMoney(long value) => Clamp(value, 0, 999999);

        public void SetMoney(int value)
        {
            int previous = Money;
            Money = ClampMoney(value);
            NotifyIfChanged(previous, Money);
        }

        /// <summary>正數增加、負數扣除；先以 long 相加，避免整數溢位。</summary>
        public void AddMoney(int amount) => SetMoney(ClampMoney((long)Money + amount));

        /// <summary>體力與射精次數依各自設定恢復；射精次數耗盡時直接設為恢復值，不累計負數欠額。</summary>
        public void NextDay()
        {
            int previousStamina = Stamina;
            int previousShootTimes = ShootTimes;
            Stamina = Stamina < 0
                ? Clamp(NegativeStaminaDailyReset, StaminaMin, StaminaMax)
                : Clamp((long)Stamina + StaminaDailyRecovery, StaminaMin, StaminaMax);
            ShootTimes = ShootTimes <= 0
                ? Clamp(ExhaustedShootTimesDailyReset, ShootTimesMin, ShootTimesMax)
                : Clamp((long)ShootTimes + ShootTimesDailyRecovery, ShootTimesMin, ShootTimesMax);
            if (previousStamina != Stamina || previousShootTimes != ShootTimes)
                OnChanged?.Invoke();
        }

        /// <summary>還原測試初始值；整批重置時由 Service 統一通知刷新。</summary>
        public void NewGame(bool notify = true)
        {
            StaminaMax = InitialStaminaMax;
            Stamina = StaminaMax;
            ShootTimesMax = InitialShootTimesMax;
            ShootTimes = ShootTimesMax;
            Lust = InitialLust;
            IsExhausted = InitialIsExhausted;
            StickerCount = InitialStickerCount;
            PendingStickerCount = InitialPendingStickerCount;
            Money = ClampMoney(InitialMoney);
            if (notify) OnChanged?.Invoke();
        }
    }
}
