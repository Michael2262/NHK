using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 睡眠小遊戲的 Awake 操作入口；階段規則與緊急計時由內部純 C# Model 處理。
/// 指定女主角的 Awake 仍由 GameStatusService 上的正式 Model 保存。
/// </summary>
[DisallowMultipleComponent]
public class SleepGameAwakeController : MonoBehaviour
{
    [Header("連動角色")]
    [SerializeField] private string heroineID = "sister";

    [Header("各 Awake 階段設定（機率 0～1；增加值可用小數）")]
    [SerializeField] private SleepStageSettings stageSettings = new SleepStageSettings();

    [Header("緊急檢查")]
    [Tooltip("緊急期間，每次收到 TriggerValueChange 都重新計時，即使增加機率沒有通過。")]
    [Min(0.01f)] [SerializeField] private float emergencyIdleSeconds = 3f;
    [Tooltip("勾選時以實際經過秒數計時，不受 Time.timeScale 影響。")]
    [SerializeField] private bool useUnscaledTime = true;
    [Tooltip("緊急期間開啟此 Image 的 GameObject，其餘時間關閉。請使用獨立或子物件，不能指定本控制器所在物件或其父物件。")]
    [SerializeField] private Image emergencyCheckImage;

    [Header("緊急檢查事件")]
    [Tooltip("實際進入緊急檢查時觸發一次；緊急期間延長倒數不重複觸發。")]
    [SerializeField] private UnityEvent onEmergencyCheckStarted = new UnityEvent();
    [Tooltip("緊急檢查結束時觸發一次，包含逾時、手動重設、換角、讀檔或停用；原本未進入緊急狀態時不觸發。")]
    [SerializeField] private UnityEvent onEmergencyCheckEnded = new UnityEvent();

    public bool IsEmergencyChecking => changeModel.IsEmergencyChecking;
    public UnityEvent OnEmergencyCheckStarted => onEmergencyCheckStarted;
    public UnityEvent OnEmergencyCheckEnded => onEmergencyCheckEnded;

    private readonly AwakeChangeModel changeModel = new AwakeChangeModel();
    private GameStatusService service;
    private HeroineStatusModel heroine;
    private bool warnedInvalidImage;
    private double CurrentTime => useUnscaledTime ? Time.unscaledTimeAsDouble : Time.timeAsDouble;

    private void Awake()
    {
        changeModel.OnEmergencyCheckChanged += HandleEmergencyCheckChanged;
    }

    private void OnDestroy()
    {
        changeModel.OnEmergencyCheckChanged -= HandleEmergencyCheckChanged;
    }

    private void OnEnable()
    {
        RefreshBinding();
        ResetEmergencyCheck();
    }

    private void OnDisable()
    {
        if (service != null) service.OnGameStatusLoaded -= HandleGameStatusLoaded;
        service = null;
        heroine = null;
        ResetEmergencyCheck();
    }

    private void Update()
    {
        RefreshBinding();
        changeModel.ExpireIfIdle(CurrentTime);
        RefreshEmergencyImage();
    }

    /// <summary>
    /// 觸發一次數值變化，可由 Button／UnityEvent 呼叫。
    /// 整輪使用呼叫當下的睡眠階段；平時先判定增加，再判定是否進入緊急檢查。
    /// </summary>
    public void TriggerValueChange()
    {
        if (!isActiveAndEnabled) return;
        RefreshBinding();
        if (heroine == null)
        {
            Debug.LogWarning($"[SleepGameAwakeController] 找不到女主角：{heroineID}", this);
            return;
        }

        bool configured = changeModel.ApplyValueChange(heroine, stageSettings, CurrentTime, emergencyIdleSeconds);
        if (!configured)
            Debug.LogWarning("[SleepGameAwakeController] 目前 Awake 階段缺少設定，請檢查 Inspector。", this);
        RefreshEmergencyImage();
    }

    /// <summary>切換角色並清除本次小遊戲的緊急狀態。</summary>
    public void SetHeroineID(string id)
    {
        heroineID = id;
        if (isActiveAndEnabled) RefreshBinding();
    }

    /// <summary>只重設緊急狀態，不改變女主角的 Awake。</summary>
    public void ResetEmergencyCheck()
    {
        changeModel.Reset();
        RefreshEmergencyImage();
    }

    private void RefreshBinding()
    {
        var nextService = GameStatusService.Instance;
        if (service != nextService)
        {
            if (service != null) service.OnGameStatusLoaded -= HandleGameStatusLoaded;
            service = nextService;
            if (service != null) service.OnGameStatusLoaded += HandleGameStatusLoaded;
        }

        HeroineStatusModel nextHeroine = null;
        if (service != null && service.Heroines != null && !string.IsNullOrEmpty(heroineID))
            service.Heroines.TryGetValue(heroineID, out nextHeroine);
        if (ReferenceEquals(heroine, nextHeroine)) return;
        heroine = nextHeroine;
        ResetEmergencyCheck();
    }

    private void HandleGameStatusLoaded()
    {
        RefreshBinding();
        // 緊急狀態僅屬於場景內本次操作；讀檔／新遊戲不保留舊倒數。
        ResetEmergencyCheck();
    }

    private void HandleEmergencyCheckChanged(bool active)
    {
        // 先同步 Image；其啟停回呼若再次改變狀態，就不派送過期通知。
        int revision = changeModel.Revision;
        RefreshEmergencyImage();
        if (revision != changeModel.Revision) return;
        if (active) onEmergencyCheckStarted.Invoke();
        else onEmergencyCheckEnded.Invoke();
    }

    private void RefreshEmergencyImage()
    {
        if (emergencyCheckImage == null) return;
        if (transform.IsChildOf(emergencyCheckImage.transform))
        {
            if (!warnedInvalidImage)
                Debug.LogWarning("[SleepGameAwakeController] 緊急 Image 不能是控制器本身或父物件，否則關閉 Image 會連帶停用控制器。", this);
            warnedInvalidImage = true;
            return;
        }

        warnedInvalidImage = false;
        bool show = isActiveAndEnabled && changeModel.IsEmergencyChecking;
        if (emergencyCheckImage.gameObject.activeSelf != show)
            emergencyCheckImage.gameObject.SetActive(show);
    }

    [Serializable]
    private sealed class AwakeStageSettings
    {
        [Tooltip("平時完成增加判定後，進入緊急檢查的機率。0＝不觸發，1＝必定觸發。")]
        [Range(0f, 1f)] public float EmergencyCheckChance;
        [Tooltip("平時每次呼叫時，增加 Awake 的機率。")]
        [Range(0f, 1f)] public float NormalAwakeIncreaseChance;
        [Tooltip("平時增加判定通過後，加到正式 Awake 的數值。")]
        [Min(0f)] public float NormalAwakeIncrease;
        [Tooltip("緊急期間每次呼叫時，增加 Awake 的機率；預設 1＝必定增加。")]
        [Range(0f, 1f)] public float EmergencyAwakeIncreaseChance = 1f;
        [Tooltip("緊急期間增加判定通過後，加到正式 Awake 的數值。")]
        [Min(0f)] public float EmergencyAwakeIncrease;
    }

    [Serializable]
    private sealed class SleepStageSettings
    {
        [Header("深睡 DeepSleep")]
        public AwakeStageSettings DeepSleep = new AwakeStageSettings();
        [Header("熟睡 SoundSleep")]
        public AwakeStageSettings SoundSleep = new AwakeStageSettings();
        [Header("睡著 Sleep")]
        public AwakeStageSettings Sleep = new AwakeStageSettings();
        [Header("半夢半醒 HalfAwake")]
        public AwakeStageSettings HalfAwake = new AwakeStageSettings();
        [Header("迷糊 Dazed")]
        public AwakeStageSettings Dazed = new AwakeStageSettings();
        [Header("清醒 Wake")]
        public AwakeStageSettings Wake = new AwakeStageSettings();

        public AwakeStageSettings Get(HeroineSleepState state)
        {
            switch (state)
            {
                case HeroineSleepState.DeepSleep: return DeepSleep;
                case HeroineSleepState.SoundSleep: return SoundSleep;
                case HeroineSleepState.Sleep: return Sleep;
                case HeroineSleepState.HalfAwake: return HalfAwake;
                case HeroineSleepState.Dazed: return Dazed;
                case HeroineSleepState.Wake: return Wake;
                default: return null;
            }
        }
    }

    /// <summary>場景內的純 C# 狀態；時間由元件傳入，不依賴 MonoBehaviour 或 UI。</summary>
    private sealed class AwakeChangeModel
    {
        private readonly System.Random random = new System.Random();
        private double emergencyExpiresAt;
        private int revision;
        private bool applying;
        public bool IsEmergencyChecking { get; private set; }
        public int Revision => revision;
        public event Action<bool> OnEmergencyCheckChanged;

        public int Reset()
        {
            bool wasEmergencyChecking = IsEmergencyChecking;
            IsEmergencyChecking = false;
            emergencyExpiresAt = 0d;
            int resetRevision = unchecked(++revision);
            if (wasEmergencyChecking) OnEmergencyCheckChanged?.Invoke(false);
            return resetRevision;
        }

        public bool ExpireIfIdle(double now)
        {
            if (!IsEmergencyChecking || now < emergencyExpiresAt) return true;
            int resetRevision = Reset();
            // 結束事件若又重設、換角或停用，讓呼叫端取消舊操作。
            return revision == resetRevision;
        }

        public bool ApplyValueChange(HeroineStatusModel heroine, SleepStageSettings allSettings, double now, float idleSeconds)
        {
            // Awake 事件回呼若再次呼叫本方法，忽略巢狀操作，避免一輪重複抽選。
            if (applying) return true;
            applying = true;
            try
            {
                // 逾時通知也在重入保護內，避免結束事件造成巢狀抽選。
                if (!ExpireIfIdle(now)) return true;
                AwakeStageSettings settings = allSettings?.Get(heroine.CurrentSleepState);
                if (settings == null) return false;
                int currentRevision = revision;
                double duration = float.IsNaN(idleSeconds) || float.IsInfinity(idleSeconds)
                    ? 3d : Math.Max(0.01d, idleSeconds);
                if (IsEmergencyChecking)
                {
                    // 收到操作就延長倒數，不以機率通過或 Awake 實際增加為條件。
                    emergencyExpiresAt = now + duration;
                    if (Roll(settings.EmergencyAwakeIncreaseChance))
                        IncreaseAwake(heroine, settings.EmergencyAwakeIncrease);
                    return true;
                }

                if (Roll(settings.NormalAwakeIncreaseChance))
                    IncreaseAwake(heroine, settings.NormalAwakeIncrease);
                // Awake 回呼可能停用元件、換角或讀檔；取消這輪後續的緊急判定。
                if (revision != currentRevision) return true;
                if (Roll(settings.EmergencyCheckChance))
                {
                    IsEmergencyChecking = true;
                    emergencyExpiresAt = now + duration;
                    unchecked { revision++; }
                    OnEmergencyCheckChanged?.Invoke(true);
                }
                return true;
            }
            finally
            {
                applying = false;
            }
        }

        private bool Roll(float chance)
        {
            if (float.IsNaN(chance) || chance <= 0f) return false;
            return chance >= 1f || random.NextDouble() < chance;
        }

        private static void IncreaseAwake(HeroineStatusModel heroine, float amount)
        {
            if (!float.IsNaN(amount) && amount > 0f) heroine.AddAwake(amount);
        }
    }
}
