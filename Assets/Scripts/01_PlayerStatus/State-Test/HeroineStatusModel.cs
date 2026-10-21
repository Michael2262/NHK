using System;

namespace NHK.StateTest
{
    /// <summary>一個實例代表一位女主角；測試期間由 Service 持有單一實例。</summary>
    public class HeroineStatusModel
    {
        // ==========================================================
        // 數值設定區：同類數值放在一起，修改後重新開始遊戲套用。
        // 初始值須位於上下限內。
        // ==========================================================

        // ───── 信賴 ─────
        public const int TrustMin = 0;
        public const int TrustMax = 999;
        public const int InitialTrust = 0;

        // ───── 心情 ─────
        public const int MoodMin = 0;
        public const int MoodMax = 10;
        public const int InitialMood = 5;

        // ───── 好感 ─────
        public const int AffinityMin = 0;
        public const int AffinityMax = 999;
        public const int InitialAffinity = 0;

        // ───── 性興味 ─────
        public const int LibidoMin = 0;
        public const int LibidoMax = 250;
        public const int InitialLibido = 0;

        // ==========================================================
        // 執行期間狀態
        // ==========================================================
        public int Trust { get; private set; } = InitialTrust;
        public int Mood { get; private set; } = InitialMood;
        public int Affinity { get; private set; } = InitialAffinity;
        public int Libido { get; private set; } = InitialLibido;

        /// <summary>狀態完成更新後通知；訂閱者讀取目前屬性。</summary>
        public event Action OnChanged;

        private static int Clamp(long value, int min, int max)
            => (int)Math.Max(min, Math.Min(max, value));

        private void NotifyIfChanged(int previous, int current)
        {
            if (previous != current) OnChanged?.Invoke();
        }

        public void SetTrust(int value)
        {
            int previous = Trust;
            Trust = Clamp(value, TrustMin, TrustMax);
            NotifyIfChanged(previous, Trust);
        }

        public void AddTrust(int amount) => SetTrust(Clamp((long)Trust + amount, TrustMin, TrustMax));

        public void SetMood(int value)
        {
            int previous = Mood;
            Mood = Clamp(value, MoodMin, MoodMax);
            NotifyIfChanged(previous, Mood);
        }

        public void AddMood(int amount) => SetMood(Clamp((long)Mood + amount, MoodMin, MoodMax));

        public void SetAffinity(int value)
        {
            int previous = Affinity;
            Affinity = Clamp(value, AffinityMin, AffinityMax);
            NotifyIfChanged(previous, Affinity);
        }

        public void AddAffinity(int amount) => SetAffinity(Clamp((long)Affinity + amount, AffinityMin, AffinityMax));

        public void SetLibido(int value)
        {
            int previous = Libido;
            Libido = Clamp(value, LibidoMin, LibidoMax);
            NotifyIfChanged(previous, Libido);
        }

        public void AddLibido(int amount) => SetLibido(Clamp((long)Libido + amount, LibidoMin, LibidoMax));

        /// <summary>還原測試初始值；整批重置時由 Service 統一通知刷新。</summary>
        public void NewGame(bool notify = true)
        {
            Trust = InitialTrust;
            Mood = InitialMood;
            Affinity = InitialAffinity;
            Libido = InitialLibido;
            if (notify) OnChanged?.Invoke();
        }
    }
}
