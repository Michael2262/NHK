using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// TeaseGame 輸入接收器（基礎版）。
///
/// 職責：把玩家的指標輸入轉成「指令 ID」發給 SensorLogicTrigger，本身不含任何遊戲邏輯。
///   - 按下      → 發送 "PRESS"
///   - 放開      → 發送 "RELEASE"
///   - 按住超過門檻秒數 → 發送一次 "HOLD"（每次按壓只發一次）
///
/// 變體寫法：繼承本類，覆寫 OnPressed / OnReleased / OnHoldTriggered，
/// 在裡面用 Emit() 發送額外的指令 ID（慣例用 "TRIGGER"、"TRIGGER2"、"TRIGGER3"）。
/// 指標偵測與按壓計時由基類處理，變體不要重新實作。
/// </summary>
public class InputSensor : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    // ─────────────────────────────────────────────
    // 預設指令 ID
    // ─────────────────────────────────────────────
    public const string CMD_PRESS = "PRESS";
    public const string CMD_RELEASE = "RELEASE";
    public const string CMD_HOLD = "HOLD";
    public const string CMD_TRIGGER = "TRIGGER";
    public const string CMD_TRIGGER2 = "TRIGGER2";
    public const string CMD_TRIGGER3 = "TRIGGER3";

    [Header("目標接收器")]
    [Tooltip("要接收指令的 SensorLogicTrigger 列表。留空則自動抓取同物件上的所有 SensorLogicTrigger。")]
    [SerializeField] private SensorLogicTrigger[] targets;

    [Header("HOLD 設定")]
    [Tooltip("按住超過此秒數時，發送一次 HOLD 指令。")]
    [SerializeField] private float holdThreshold = 0.5f;

    // ─────────────────────────────────────────────
    // 內部狀態
    // ─────────────────────────────────────────────
    private bool _isPressed;
    private bool _holdFired;
    private float _pressStartTime;

    /// <summary>目前是否按壓中（供變體讀取）。</summary>
    protected bool IsPressed => _isPressed;

    /// <summary>目前這次按壓已持續的秒數；未按壓時為 0（供變體讀取）。</summary>
    protected float HeldDuration => _isPressed ? Time.time - _pressStartTime : 0f;

    protected virtual void Awake()
    {
        if (targets == null || targets.Length == 0)
            targets = GetComponents<SensorLogicTrigger>();
    }

    protected virtual void OnDisable()
    {
        // 物件在按壓中被停用時清掉狀態，避免重新啟用後卡在按壓狀態
        _isPressed = false;
        _holdFired = false;
    }

    protected virtual void Update()
    {
        if (_isPressed && !_holdFired && Time.time - _pressStartTime >= holdThreshold)
        {
            _holdFired = true;
            Emit(CMD_HOLD);
            OnHoldTriggered();
        }
    }

    // ─────────────────────────────────────────────
    // Event System 介面
    // ─────────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_isPressed) return; // 多點觸控時只認第一根手指

        _isPressed = true;
        _holdFired = false;
        _pressStartTime = Time.time;

        Emit(CMD_PRESS);
        OnPressed(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isPressed) return;

        float heldDuration = Time.time - _pressStartTime;
        _isPressed = false;

        Emit(CMD_RELEASE);
        OnReleased(heldDuration, eventData);
    }

    // ─────────────────────────────────────────────
    // 公開 API
    // ─────────────────────────────────────────────

    /// <summary>
    /// 發送指令 ID 給所有目標接收器。
    /// 公開給變體與外部（UnityEvent / FSM）使用，可注入任意指令。
    /// </summary>
    public void Emit(string commandId)
    {
        if (string.IsNullOrEmpty(commandId) || targets == null) return;

        foreach (var target in targets)
        {
            if (target != null && target.isActiveAndEnabled)
                target.ReceiveCommand(commandId);
        }
    }

    /// <summary>
    /// 取消這次按壓尚未觸發的 HOLD（例如突發中斷時呼叫）。
    /// 不影響 PRESS / RELEASE 的發送。
    /// </summary>
    public void CancelHold()
    {
        _holdFired = true;
    }

    // ─────────────────────────────────────────────
    // 變體覆寫掛點（發送順序：基礎指令先發，再呼叫掛點）
    // ─────────────────────────────────────────────

    /// <summary>按下時呼叫，在 PRESS 發送之後。</summary>
    protected virtual void OnPressed(PointerEventData eventData) { }

    /// <summary>放開時呼叫，在 RELEASE 發送之後。heldDuration = 這次按壓總秒數。</summary>
    protected virtual void OnReleased(float heldDuration, PointerEventData eventData) { }

    /// <summary>按住達到門檻時呼叫，在 HOLD 發送之後。</summary>
    protected virtual void OnHoldTriggered() { }
}
