using UnityEngine;

/// <summary>
/// NHK 版休息按鈕控制器。
/// 舊版用途：回復 Stamina。
/// NHK 用途：透過休息降低 Stress，並推進時間。
/// </summary>
public class RestButtonController : MonoBehaviour
{
    public static RestButtonController Instance { get; private set; }

    [Header("NHK 壓力回復設定")]
    [Tooltip("每休息 1 個 Slot 可降低的壓力值。")]
    [SerializeField] private int stressRecoveryPerSlot = 10;

    [Tooltip("睡到隔天時額外降低的壓力值。")]
    [SerializeField] private int nightlyStressRecovery = 20;

    private GameStatusService _service;
    private ProtagonistStatusModel _protagonist;
    private TimeSystemModel _time;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void OnEnable()
    {
        RefreshReferences();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void RefreshReferences()
    {
        _service = GameStatusService.Instance;
        if (_service == null) return;

        _protagonist = _service.Protagonist;
        _time = _service.Time;
    }

    public void RestOneSlot()
    {
        if (!CanRestOneSlot()) return;

        int slotsRested = 1;
        SceneController.PerformSlotTransition(
            onMidTransition: () =>
            {
                int recovered = RecoverStressBySlots(slotsRested);
                _time.TryAdvanceTime(slotsRested);
                Debug.Log($"[RestButton/NHK] 休息 1 Slot，壓力 -{recovered}。");
            }
        );
    }

    public void RestToNextPhase()
    {
        if (!CanRestToNextPhase()) return;

        int slotsToNextPhase = GetSlotsToNextPhase();
        SceneController.PerformSlotTransition(
            onMidTransition: () =>
            {
                int recovered = RecoverStressBySlots(slotsToNextPhase);
                _time.TryAdvanceTime(slotsToNextPhase);
                Debug.Log($"[RestButton/NHK] 休息到下一 Phase，經過 {slotsToNextPhase} Slots，壓力 -{recovered}。");
            }
        );
    }

    public void RestToNextDay()
    {
        if (!CanRestToNextDay()) return;

        int remainingSlots = GetSlotsToNextDay();
        int recovered = RecoverStressBySlots(remainingSlots) + RecoverNightlyStress();
        Debug.Log($"[RestButton/NHK] 休息到隔天，經過 {remainingSlots} Slots，總壓力 -{recovered}。");
        _time.SkipToNextDay();
    }

    public bool CanRestOneSlot()
    {
        if (!IsReady()) return false;
        return _time.CanAdvanceWithinDay(1);
    }

    public bool CanRestToNextPhase()
    {
        if (!IsReady()) return false;
        int slots = GetSlotsToNextPhase();
        return slots > 0 && _time.CanAdvanceWithinDay(slots);
    }

    public bool CanRestToNextDay()
    {
        return IsReady();
    }

    public bool IsRestAvailable(RestPreviewHoverTrigger.RestMode mode)
    {
        switch (mode)
        {
            case RestPreviewHoverTrigger.RestMode.RestOneSlot:
                return CanRestOneSlot();
            case RestPreviewHoverTrigger.RestMode.RestToNextPhase:
                return CanRestToNextPhase();
            case RestPreviewHoverTrigger.RestMode.RestToNextDay:
                return CanRestToNextDay();
            default:
                return false;
        }
    }

    public int CalculateRestOneSlotPreview()
    {
        if (!CanRestOneSlot()) return 0;
        return CalculateActualStressRecovery(1, false);
    }

    public int CalculateRestToNextPhasePreview()
    {
        if (!CanRestToNextPhase()) return 0;
        return CalculateActualStressRecovery(GetSlotsToNextPhase(), false);
    }

    public int CalculateRestToNextDayPreview()
    {
        if (!IsReady()) return 0;
        return CalculateActualStressRecovery(GetSlotsToNextDay(), true);
    }

    private int RecoverStressBySlots(int slots)
    {
        int recovered = CalculateActualStressRecovery(slots, false);
        _protagonist.ReduceStress(recovered);
        return recovered;
    }

    private int RecoverNightlyStress()
    {
        int recovered = Mathf.Min(nightlyStressRecovery, _protagonist.Stress);
        _protagonist.ReduceStress(recovered);
        return recovered;
    }

    private int CalculateActualStressRecovery(int slots, bool includeNightly)
    {
        if (_protagonist == null) return 0;
        int raw = Mathf.Max(0, slots) * stressRecoveryPerSlot;
        if (includeNightly) raw += nightlyStressRecovery;
        return Mathf.Clamp(raw, 0, _protagonist.Stress);
    }

    private int GetSlotsToNextPhase()
    {
        if (_time == null || _service == null) return 0;

        var config = _service.TimeConfig;
        int currentPhase = _time.CurrentPhaseIndex;
        int currentSlot = _time.CurrentSlotInPhase;
        int totalSlotsInCurrentPhase = config.GetTotalSlots(currentPhase);
        int remainingInCurrentPhase = totalSlotsInCurrentPhase - currentSlot;

        if (currentPhase >= config.PhaseNames.Count - 1)
            return 0;

        return remainingInCurrentPhase + 1;
    }

    private int GetSlotsToNextDay()
    {
        if (_time == null) return 1;
        int remaining = _time.GetRemainingSlotsInDay();
        return Mathf.Max(1, remaining);
    }

    private bool IsReady()
    {
        if (_service == null || _protagonist == null || _time == null)
            RefreshReferences();

        if (_service == null || _protagonist == null || _time == null)
        {
            Debug.LogWarning("[RestButton/NHK] 系統尚未初始化。");
            return false;
        }
        return true;
    }
}
