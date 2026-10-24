using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 時間系統的 Button／UnityEvent 入口。
/// 沿用共用時間 Model 的規則與事件，跨日結算由 GameStatusService 處理。
/// </summary>
[AddComponentMenu("Game/API/Time Bridge")]
public class TimeBridge : MonoBehaviour
{
    [Header("Mini 事件")]
    [Tooltip("每推進一個 Mini 後呼叫，可接上條件檢查。直接跳 Slot 或讀檔不觸發。")]
    [SerializeField] private UnityEvent onMiniAdvanced = new UnityEvent();
    [Tooltip("呼叫不跨日推進 Mini，但當日剩餘數量不足或參數為負數時呼叫。")]
    [SerializeField] private UnityEvent onMiniAdvanceRejected = new UnityEvent();

    [Header("時間流逝演出")]
    [Tooltip("開始播放時觸發一次，可開啟操作阻擋。")]
    [SerializeField] private UnityEvent onTimeFlowStarted = new UnityEvent();
    [Tooltip("播放完成或取消時觸發一次，可解除操作阻擋；正常完成後 TimeLoopTrigger 才檢查。")]
    [SerializeField] private UnityEvent onTimeFlowEnded = new UnityEvent();
    [Tooltip("已有播放、參數無效或今日剩餘量不足時觸發。")]
    [SerializeField] private UnityEvent onTimeFlowRejected = new UnityEvent();

    private TimePlaybackModel subscribedPlayback;
    public bool IsTimeFlowing => GameStatusService.Instance?.TimePlayback?.IsTimeFlowing ?? false;

    private TimeSystemModel subscribedModel;

    private void OnEnable() => SubscribeToMini();
    private void Start() => SubscribeToMini();

    private void SubscribeToMini()
    {
        var model = GameStatusService.Instance?.Time;
        var playback = GameStatusService.Instance?.TimePlayback;
        if (model == subscribedModel && playback == subscribedPlayback) return;
        UnsubscribeFromMini();
        subscribedModel = model;
        if (subscribedModel != null) subscribedModel.OnMiniAdvanced += HandleMiniAdvanced;
        subscribedPlayback = playback;
        if (subscribedPlayback != null)
        {
            subscribedPlayback.OnTimeFlowStarted += HandleFlowStarted;
            subscribedPlayback.OnTimeFlowEnded += HandleFlowEnded;
            if (subscribedPlayback.IsTimeFlowing) HandleFlowStarted();
        }
    }

    private void OnDisable() => UnsubscribeFromMini();

    private void UnsubscribeFromMini()
    {
        if (subscribedModel != null) subscribedModel.OnMiniAdvanced -= HandleMiniAdvanced;
        subscribedModel = null;
        if (subscribedPlayback != null)
        {
            subscribedPlayback.OnTimeFlowStarted -= HandleFlowStarted;
            subscribedPlayback.OnTimeFlowEnded -= HandleFlowEnded;
        }
        subscribedPlayback = null;
    }

    private void HandleMiniAdvanced(int delta) => onMiniAdvanced?.Invoke();
    private void HandleFlowStarted() => onTimeFlowStarted?.Invoke();
    private void HandleFlowEnded(bool completed) => onTimeFlowEnded?.Invoke();

    public void PlayAdvanceMinis(int minis)
    {
        if (!(GameStatusService.Instance?.TimePlayback?.PlayAdvanceMinis(minis) ?? false))
            onTimeFlowRejected?.Invoke();
    }

    public void PlayAdvanceShortTime()
    {
        if (!(GameStatusService.Instance?.TimePlayback?.PlayAdvanceShortTime() ?? false))
            onTimeFlowRejected?.Invoke();
    }

    public void PlayAdvanceMediumTime()
    {
        if (!(GameStatusService.Instance?.TimePlayback?.PlayAdvanceMediumTime() ?? false))
            onTimeFlowRejected?.Invoke();
    }

    public void PlayAdvanceLongTime()
    {
        if (!(GameStatusService.Instance?.TimePlayback?.PlayAdvanceLongTime() ?? false))
            onTimeFlowRejected?.Invoke();
    }

    public void PlayAdvanceMinisWithinDay(int minis)
    {
        if (!(GameStatusService.Instance?.TimePlayback?.PlayAdvanceMinis(minis, withinDay: true) ?? false))
            onTimeFlowRejected?.Invoke();
    }

    public void CancelTimeFlow() => GameStatusService.Instance?.TimePlayback?.Cancel();

    private TimeSystemModel Model
    {
        get
        {
            var service = GameStatusService.Instance;
            var model = service != null ? service.Time : null;
            if (model == null)
                Debug.LogWarning("[TimeBridge] 時間系統尚未初始化，請確認場景中有 GameStatusService。", this);
            return model;
        }
    }

    /// <summary>推進指定格數；沿用 Model 行為，跨日後會停在隔天第一格。</summary>
    public void AdvanceTime(int slots)
    {
        if (slots <= 0) return;
        Model?.AdvanceTime(slots);
    }

    /// <summary>推進一格，供無參數 UnityEvent 使用。</summary>
    public void AdvanceOneSlot() => AdvanceTime(1);

    /// <summary>推進指定 Mini 數量；跨日後停在隔天起點。</summary>
    public void AdvanceMinis(int minis)
    {
        if (minis <= 0) return;
        Model?.AdvanceMinis(minis);
    }

    public void AdvanceOneMini() => AdvanceMinis(1);

    /// <summary>推進一小段時間；Mini 數量統一由 TimeConfig 設定，預設為 6。</summary>
    public void AdvanceShortTime() => Model?.AdvanceShortTime();

    /// <summary>推進中段時間；Mini 數量統一由 TimeConfig 設定，預設為 12。</summary>
    public void AdvanceMediumTime() => Model?.AdvanceMediumTime();

    /// <summary>推進長段時間；Mini 數量統一由 TimeConfig 設定，預設為 24。</summary>
    public void AdvanceLongTime() => Model?.AdvanceLongTime();

    /// <summary>Button／UnityEvent 使用：當日不足時不推進，並發送拒絕事件。</summary>
    public void AdvanceMinisWithinDay(int minis)
    {
        var model = Model;
        if (model != null && !model.TryAdvanceMinis(minis))
            onMiniAdvanceRejected?.Invoke();
    }

    public void AdvanceOneMiniWithinDay() => AdvanceMinisWithinDay(1);

    public bool CanAdvanceMinisWithinDay(int minis) => Model?.CanAdvanceMinisWithinDay(minis) ?? false;
    public int GetCurrentMiniInSlot() => Model?.CurrentMiniInSlot ?? 0;
    public int GetMinisInCurrentSlot() => Model?.MinisInCurrentSlot ?? 0;
    public int GetRemainingMinisInDay() => Model?.GetRemainingMinisInDay() ?? 0;

    [ContextMenu("除錯／顯示目前 Mini 進度")]
    public void LogMiniProgress()
    {
        var model = Model;
        if (model == null) return;
        Debug.Log($"[TimeBridge] 第 {model.DayIndex} 天／Phase {model.CurrentPhaseIndex}／Slot {model.CurrentSlotInPhase}／Mini {model.CurrentMiniInSlot}/{model.MinisInCurrentSlot}，今日剩餘 {model.GetRemainingMinisInDay()} Mini（不跨日）。", this);
    }

    /// <summary>進入下一個階段的第一格；最後階段會進入隔天。</summary>
    public void AdvanceOnePhase() => Model?.AdvancePhases(1);

    /// <summary>直接推進指定天數至當日第一階段第一格，逐日結算，不播放睡覺演出。</summary>
    public void AdvanceDays(int days)
    {
        if (days <= 0) return;
        Model?.AdvanceDays(days);
    }

    /// <summary>直接推進到隔天開頭並執行換日結算。</summary>
    public void AdvanceOneDay() => AdvanceDays(1);

    /// <summary>請求既有睡覺轉場流程；演出完成後才換日。</summary>
    public void SkipToNextDay() => Model?.SkipToNextDay();
}
