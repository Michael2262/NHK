using UnityEngine;

public enum BulletTimeOperation { Begin, AdvanceOneMinute, EndAtNearestMini, EndAtConfiguredTime }

/// <summary>每個使用 BulletTime 的小遊戲場景掛一份。FSM、Sequence 和 UnityEvent 共用此設定。</summary>
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class BulletTimeBridge : MonoBehaviour
{
    [SerializeField, Min(0), Tooltip("每幾個實際秒數推進一分鐘；0 表示手動推進。")]
    private float secondsPerMinute = 1f;
    [SerializeField, Tooltip("指定結束時間，必須是時間表上可到達的 HH:mm。")]
    private string targetTime = "07:00";
    [SerializeField, Tooltip("勾選為進場顯示日的隔天，未勾選為進場顯示日當天。")]
    private bool targetNextDay = true;

    private GameStatusService service;

    private void OnEnable()
    {
        service = GameStatusService.Instance;
        if (service == null)
        {
            Debug.LogWarning("[BulletTime] 請在 GameStatusService 初始化後啟用小遊戲控制元件。", this);
            return;
        }
        var previous = service.BulletTimeControls;
        if (previous != null && previous.gameObject.scene == gameObject.scene)
        {
            Debug.LogWarning("[BulletTime] 同一個小遊戲場景只能啟用一個 BulletTimeBridge。", this);
            return;
        }
        // 正規轉場會先載入新場景才卸載舊場景。
        if (previous != null) service.BulletTime.Cancel();
        service.BulletTimeControls = this;
    }

    private void OnDisable()
    {
        if (service == null || service.BulletTimeControls != this) return;
        service.BulletTimeControls = null;
        service.BulletTime.Cancel();
    }

    private void Update()
    {
        if (service != null && service.BulletTimeControls == this)
            service.BulletTime.Tick(Time.unscaledDeltaTime);
    }

    public void Begin() => TryExecute(BulletTimeOperation.Begin);
    public void AdvanceOneMinute() => TryExecute(BulletTimeOperation.AdvanceOneMinute);
    public void EndAtNearestMini() => TryExecute(BulletTimeOperation.EndAtNearestMini);
    public void EndAtConfiguredTime() => TryExecute(BulletTimeOperation.EndAtConfiguredTime);

    public static bool TryExecuteCurrent(BulletTimeOperation operation)
    {
        var bridge = GameStatusService.Instance?.BulletTimeControls;
        if (bridge != null) return bridge.TryExecute(operation);
        Debug.LogWarning("[BulletTime] 請先在小遊戲場景啟用 BulletTimeBridge。");
        return false;
    }

    private bool TryExecute(BulletTimeOperation operation)
    {
        if (service == null || service.BulletTimeControls != this) return false;
        var clock = service.BulletTime;
        bool success = operation switch
        {
            BulletTimeOperation.Begin => clock.Begin(LobbyUI_V2.Instance?.TimeMapping, secondsPerMinute),
            BulletTimeOperation.AdvanceOneMinute => clock.AdvanceOneMinute(),
            BulletTimeOperation.EndAtNearestMini => clock.EndAtNearestMini(),
            BulletTimeOperation.EndAtConfiguredTime => clock.EndAtTime(targetTime, targetNextDay),
            _ => false
        };
        if (!success)
            Debug.LogWarning($"[BulletTime] 無法執行 {operation}。請確認播放狀態、Lobby 時間表與指定落點（{targetTime}）。", this);
        return success;
    }
}
