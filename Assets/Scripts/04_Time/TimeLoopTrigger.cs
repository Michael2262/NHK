using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 時間循環觸發器
/// 一般推進沿用 Slot 通知。Play 演出期間忽略所有中途通知，正常結束後只檢查最後位置。
/// 不記錄或補觸發中途經過的條件；取消演出不做結束判斷。
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

    private TimeSystemModel subscribedTime;
    private TimePlaybackModel subscribedPlayback;

    private void OnEnable() => Bind();
    private void Update() => Bind();

    private void Bind()
    {
        var service = GameStatusService.Instance;
        var time = service?.Time;
        var playback = service?.TimePlayback;
        if (time == subscribedTime && playback == subscribedPlayback) return;
        Unbind();
        subscribedTime = time;
        subscribedPlayback = playback;
        if (subscribedTime != null) subscribedTime.OnTimeSlotAdvanced += OnSlotAdvanced;
        if (subscribedPlayback != null) subscribedPlayback.OnTimeFlowCompleted += CheckFinalPosition;
    }

    private void OnDisable() => Unbind();

    private void Unbind()
    {
        if (subscribedTime != null) subscribedTime.OnTimeSlotAdvanced -= OnSlotAdvanced;
        if (subscribedPlayback != null) subscribedPlayback.OnTimeFlowCompleted -= CheckFinalPosition;
        subscribedTime = null;
        subscribedPlayback = null;
    }

    private void OnSlotAdvanced(int slotsAdvanced) => CheckFinalPosition();

    private void CheckFinalPosition()
    {
        var time = subscribedTime;
        if (time == null || (subscribedPlayback != null && subscribedPlayback.IsTimeFlowing)) return;

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
