using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 時間循環觸發器
/// 每當遊戲時間到達指定的 Phase / Slot 時，觸發一次 UnityEvent。
/// 每天都會觸發（只要時間經過該時刻）。
/// 
/// 使用方式：掛在任何 GameObject 上，在 Inspector 設定目標 Phase/Slot 和事件即可。
/// </summary>
public class TimeLoopTrigger : MonoBehaviour
{
    [Header("觸發時刻")]
    [Tooltip("目標 Phase（從 0 開始：0=白天, 1=黃昏, 2=晚上, 3=深夜）")]
    [SerializeField] private int targetPhaseIndex = 0;

    [Tooltip("目標 Slot（從 1 開始）")]
    [SerializeField] private int targetSlotInPhase = 1;

    [Header("日期條件")]
    [Tooltip("何時觸發：Every = 每天, WeekdayOnly = 僅平日, WeekendOnly = 僅假日")]
    [SerializeField] private DayFilter dayFilter = DayFilter.Every;

    public enum DayFilter
    {
        Every,          // 每天都觸發
        WeekdayOnly,    // 僅平日
        WeekendOnly     // 僅假日
    }

    [Header("觸發事件")]
    [SerializeField] private UnityEvent onTimeReached;

    private void OnEnable()
    {
        var time = GameStatusService.Instance?.Time;
        if (time == null) return;

        time.OnTimeSlotAdvanced += OnSlotAdvanced;
    }

    private void OnDisable()
    {
        var time = GameStatusService.Instance?.Time;
        if (time == null) return;

        time.OnTimeSlotAdvanced -= OnSlotAdvanced;
    }

    private void OnSlotAdvanced(int slotsAdvanced)
    {
        var time = GameStatusService.Instance?.Time;
        if (time == null) return;

        if (time.CurrentPhaseIndex == targetPhaseIndex &&
            time.CurrentSlotInPhase == targetSlotInPhase)
        {
            // 檢查平日/假日條件
            if (dayFilter == DayFilter.WeekdayOnly && time.IsWeekend) return;
            if (dayFilter == DayFilter.WeekendOnly && !time.IsWeekend) return;

            Debug.Log($"[TimeLoopTrigger] 到達 P{targetPhaseIndex}S{targetSlotInPhase}，觸發事件。({gameObject.name})");
            onTimeReached?.Invoke();
        }
    }
}