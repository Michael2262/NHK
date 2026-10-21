using UnityEngine;

/// <summary>
/// 時間系統的 Button／UnityEvent 入口。
/// 沿用共用時間 Model 的規則與事件，跨日結算由 GameStatusService 處理。
/// </summary>
[AddComponentMenu("Game/API/Time Bridge")]
public class TimeBridge : MonoBehaviour
{
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
