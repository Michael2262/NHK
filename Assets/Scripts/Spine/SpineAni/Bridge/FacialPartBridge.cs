using UnityEngine;

/// <summary>
/// 表情部位控制器的 UnityEvent 入口，以 ID 尋找目前啟用的目標。
/// 表情模式、計數與播放規則交由 FacialPartController 處理，不快取場景元件。
/// </summary>
[AddComponentMenu("Spine/Bridge/Facial Part Bridge")]
public class FacialPartBridge : MonoBehaviour
{
    [Header("目標設定")]
    [SerializeField, Tooltip("填入 FacialPartController 的 Controller Id，區分大小寫；不是 SpineAnimationController 的 ID。")]
    private string controllerId;

    [Header("表情模式設定")]
    [SerializeField, Tooltip("ApplyConfiguredFacialMode 呼叫時套用的表情模式。只變更表情模式，不立即播放或重置倒數。")]
    [UnityEngine.Serialization.FormerlySerializedAs("emotion")]
    private HeroineEmotionType facialMode = HeroineEmotionType.Idle;

    [Header("定時 Remote（X／Y／Z 互斥）")]
    [SerializeField, Min(0.01f), Tooltip("X 模式的間隔秒數。使用遊戲時間，Time.timeScale 為 0 時暫停；修改後重新啟動模式才套用。")]
    private float remoteIntervalX = 1f;

    [SerializeField, Min(0.01f), Tooltip("Y 模式的間隔秒數；修改後重新啟動模式才套用。")]
    private float remoteIntervalY = 2f;

    [SerializeField, Min(0.01f), Tooltip("Z 模式的間隔秒數；修改後重新啟動模式才套用。")]
    private float remoteIntervalZ = 3f;

    // 一個 Bridge 只持有一份計時狀態，因此三種模式不會同時執行。
    private bool timedRemoteActive;
    private float activeInterval;
    private float remainingSeconds;

    /// <summary>切換此 Bridge 後續操作的目標 ID，不修改 Controller 本身的 ID。</summary>
    public void SetTargetId(string id)
    {
        controllerId = (id ?? string.Empty).Trim();
    }

    /// <summary>讓 Inspector 指定目標的所有部位計數各減一；需在目標 Start 完成後呼叫。</summary>
    public void NotifyRemote()
    {
        FacialPartController.NotifyRemote(controllerId);
    }

    /// <summary>每次獨立抽選，有二分之一機率通知目前目標；未命中不推進計數，不影響定時模式。</summary>
    public void NotifyRemoteHalfChance()
    {
        if (Random.Range(0, 2) == 0) NotifyRemote();
    }

    /// <summary>每次獨立抽選，有三分之一機率通知目前目標；未命中不推進計數，不影響定時模式。</summary>
    public void NotifyRemoteOneThirdChance()
    {
        if (Random.Range(0, 3) == 0) NotifyRemote();
    }

    /// <summary>切換為每隔 X 秒通知目前目標；從完整間隔重新計時。</summary>
    public void StartRemoteX() => StartTimedRemote(remoteIntervalX);

    /// <summary>切換為每隔 Y 秒通知目前目標；取消 X／Z，從完整間隔重新計時。</summary>
    public void StartRemoteY() => StartTimedRemote(remoteIntervalY);

    /// <summary>切換為每隔 Z 秒通知目前目標；取消 X／Y，從完整間隔重新計時。</summary>
    public void StartRemoteZ() => StartTimedRemote(remoteIntervalZ);

    /// <summary>停止本 Bridge 的定時發送，不改變 Controller 接收 Remote 的開關。</summary>
    public void StopTimedRemote()
    {
        timedRemoteActive = false;
        remainingSeconds = 0f;
    }

    private void StartTimedRemote(float interval)
    {
        StopTimedRemote();
        if (!isActiveAndEnabled) return;
        if (interval <= 0f || float.IsNaN(interval) || float.IsInfinity(interval))
        {
            Debug.LogWarning("[FacialPartBridge] 定時 Remote 的間隔必須是大於 0 的有限秒數，已停止定時發送。", this);
            return;
        }
        activeInterval = interval;
        remainingSeconds = interval;
        timedRemoteActive = true;
    }

    private void Update()
    {
        if (!timedRemoteActive) return;
        remainingSeconds -= Time.deltaTime;
        if (remainingSeconds > 0f) return;

        // 先重設再通知，讓播放回呼可以安全切換／停止計時。低幀率不補發多次。
        remainingSeconds = activeInterval;
        NotifyRemote();
    }

    // 停用後需明確呼叫 StartRemoteX／Y／Z 才重新啟動。
    private void OnDisable() => StopTimedRemote();

    /// <summary>只對當次傳入的 ID 推進計數，不變更 Bridge 的目標設定。</summary>
    public void NotifyRemoteById(string id)
    {
        FacialPartController.NotifyRemote(id);
    }

    /// <summary>廣播給所有已註冊且就緒的目標；不使用此 Bridge 的目標 ID。</summary>
    public void NotifyRemoteAll()
    {
        FacialPartController.NotifyRemoteAll();
    }

    /// <summary>暫停目前目標接收 Remote，保留倒數與播放進度。</summary>
    public void StopRemote() => FacialPartController.StopRemoteById(controllerId);

    /// <summary>恢復目前目標接收 Remote。</summary>
    public void ResumeRemote() => FacialPartController.ResumeRemoteById(controllerId);

    /// <summary>暫停當次指定 ID，不變更 Bridge 的目標設定。</summary>
    public void StopRemoteById(string id) => FacialPartController.StopRemoteById(id);

    /// <summary>恢復當次指定 ID，不變更 Bridge 的目標設定。</summary>
    public void ResumeRemoteById(string id) => FacialPartController.ResumeRemoteById(id);

    /// <summary>暫停當下所有已註冊的 Controller，不使用 Bridge 的目標 ID。</summary>
    public void StopRemoteAll() => FacialPartController.StopRemoteAll();

    /// <summary>恢復當下所有已註冊的 Controller，不使用 Bridge 的目標 ID。</summary>
    public void ResumeRemoteAll() => FacialPartController.ResumeRemoteAll();

    /// <summary>套用 Inspector 選定的表情模式至目前目標。</summary>
    public void ApplyConfiguredFacialMode()
    {
        SetFacialMode(facialMode);
    }

    /// <summary>供程式或帶有表情模式參數的動態 UnityEvent 呼叫。</summary>
    public void SetFacialMode(HeroineEmotionType value)
    {
        FacialPartController.SetFacialModeById(controllerId, value);
    }

    // 無參數入口，可直接從一般 UnityEvent 的選單選取。
    public void SetFacialModeIdle() => SetFacialMode(HeroineEmotionType.Idle);
    public void SetFacialModeSad() => SetFacialMode(HeroineEmotionType.Sad);
    public void SetFacialModeAngry() => SetFacialMode(HeroineEmotionType.Angry);
    public void SetFacialModeShy() => SetFacialMode(HeroineEmotionType.Shy);
    public void SetFacialModeSexy() => SetFacialMode(HeroineEmotionType.Sexy);
    public void SetFacialModeOrgasm() => SetFacialMode(HeroineEmotionType.Orgasm);
}
