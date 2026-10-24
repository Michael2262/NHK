using UnityEngine;

/// <summary>
/// NHK 版休息按鈕控制器。
/// 舊版用途：回復 Stamina。
/// 用途：推進休息時間；跨日由正式主角 Model 恢復資源。
/// </summary>
public class RestButtonController : MonoBehaviour
{
    public static RestButtonController Instance { get; private set; }

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
                _time.TryAdvanceTime(slotsRested);
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
                _time.TryAdvanceTime(slotsToNextPhase);
            }
        );
    }

    public void RestToNextDay()
    {
        if (!CanRestToNextDay()) return;

        int remainingSlots = GetSlotsToNextDay();
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
        return 0; // 不再預覽舊壓力回復；跨日恢復由正式主角 Model 負責。
    }

    public int CalculateRestToNextPhasePreview()
    {
        return 0; // 不再預覽舊壓力回復；跨日恢復由正式主角 Model 負責。
    }

    public int CalculateRestToNextDayPreview()
    {
        return 0; // 不再預覽舊壓力回復；跨日恢復由正式主角 Model 負責。
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
