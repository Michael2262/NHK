using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>滑動方向（TeaseGame 自用）。</summary>
public enum TeaseSwipeDir
{
    None,
    Up,
    Down,
    Left,
    Right,
}

/// <summary>
/// 一個「觸碰點」＝女主角身上一個可操作的部位。整個小遊戲畫面由多個 TeaseZone 拼成。
///
/// 這一個元件就包含一個觸碰點的完整定義：
///   - 手勢：點一下（Tap）或往某方向滑（Swipe）
///   - 出現條件：屬於哪個模式（mode）＋需要哪個進度旗標（requiredFlag，拖 SO）
///   - 跑條：這次觸碰要跑多久（duration）
///   - 提示：這一點的愛心（hint，懸浮對應模式按鈕時亮）
///   - 成功回呼：onTouch（觸碰當下）、onComplete（跑條結束、女主角反應）
///
/// 執行時做三件事：
///   1. 依「當前模式＋flag」決定自己能不能被碰（開關 Collider2D / Graphic raycast）。
///   2. 依「懸浮的模式＋flag」顯示/隱藏自己的提示愛心。
///   3. 玩家做對手勢 → 交給 TeaseActionGate 跑跑條（忙碌中會被丟棄）。
///
/// 掛法：世界 Sprite 物件上放 Collider2D ＋ 本元件即可（攝影機需有 Physics2DRaycaster）。
/// UI 版本則放帶 Graphic（Raycast Target）的物件，兩種都支援。手勢偵測本元件自己做，
/// 不需要 InputSensor / SensorLogicTrigger。
/// </summary>
[DisallowMultipleComponent]
public class TeaseZone : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum GestureType { Tap, Swipe }

    [Header("手勢")]
    [Tooltip("Tap = 點一下；Swipe = 往指定方向滑。")]
    [SerializeField] private GestureType gesture = GestureType.Tap;

    [Tooltip("Swipe 用：允許的滑動方向（符合任一即算成功）。")]
    [SerializeField] private TeaseSwipeDir[] allowedDirections;

    [Tooltip("Swipe 用：位移超過此像素才算一次滑動。")]
    [SerializeField] private float swipeThreshold = 50f;

    [Tooltip("Tap 用：按住超過此秒數就不算點擊。")]
    [SerializeField] private float tapMaxDuration = 0.4f;

    [Tooltip("Tap 用：按下到放開位移超過此像素就不算點擊。")]
    [SerializeField] private float tapMoveTolerance = 20f;

    [Header("出現條件")]
    [Tooltip("此觸碰點屬於哪個操作模式。")]
    [SerializeField] private TeaseMode mode = TeaseMode.Hand;

    [Tooltip("需要此進度旗標才出現；留空 = 不看旗標。拖入 Flag Definition SO。")]
    [SerializeField] private ProgressFlagDefinition requiredFlag;

    [Tooltip("反向：勾選後改成「沒有此旗標時才出現」。requiredFlag 留空則此項無效。")]
    [SerializeField] private bool invertFlag = false;

    [Header("跑條")]
    [Tooltip("這一點的跑條時長（秒）。留 0 = 使用 TeaseActionGate 的預設時長。")]
    [SerializeField] private float duration = 0f;

    [Header("提示")]
    [Tooltip("這一點的提示愛心（懸浮對應模式按鈕、且已解鎖時顯示）。可多個。")]
    [SerializeField] private GameObject[] hints;

    [Header("成功時的回呼")]
    [Tooltip("觸碰成功、跑條「開始」當下觸發（播觸碰表演）。")]
    public UnityEvent onTouch;

    [Tooltip("跑條「結束」時觸發（女主角做出反應）。")]
    public UnityEvent onComplete;

    [Header("除錯")]
    [Tooltip("在 Console 印出按下/放開/手勢判定/觸發的過程，用來定位問題。")]
    [SerializeField] private bool logDebug = false;

    private Collider2D _collider;
    private Graphic _graphic;
    private float _nextDurationOverride = -1f;
    private bool _isActive;

    private TeaseModeController _mc;   // 目前訂閱中的 controller
    private bool _subscribed;

    // 手勢偵測暫存
    private bool _pressed;
    private float _pressTime;
    private Vector2 _pressPos;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _graphic = GetComponent<Graphic>();

        if (_collider == null && _graphic == null)
            Debug.LogWarning($"[TeaseZone] {name} 沒有 Collider2D 也沒有 Graphic，玩家碰不到。", this);
    }

    private void OnEnable()
    {
        TrySubscribe();
        Refresh();
    }

    private void Start()
    {
        // Start 在所有 Awake 之後，此時 TeaseModeController.Instance 一定已建立。
        // 若 OnEnable 當下 controller 還沒 Awake（場景載入順序），這裡補上訂閱與初始化。
        TrySubscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void TrySubscribe()
    {
        if (_subscribed) return;

        var mc = TeaseModeController.Instance;
        if (mc == null) return;

        mc.OnModeChanged += HandleModeChanged;
        mc.OnHintPreviewChanged += HandleHintPreview;
        _mc = mc;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;

        if (_mc != null)
        {
            _mc.OnModeChanged -= HandleModeChanged;
            _mc.OnHintPreviewChanged -= HandleHintPreview;
        }
        _mc = null;
        _subscribed = false;
    }

    private void HandleModeChanged(TeaseMode _) => Refresh();
    private void HandleHintPreview(TeaseMode? _) => RefreshHint();

    /// <summary>重算啟用狀態與提示（模式或 flag 改變時）。</summary>
    public void Refresh()
    {
        RefreshActivation();
        RefreshHint();
    }

    // ───── 啟用條件 ─────

    private bool FlagOk
    {
        get
        {
            if (requiredFlag == null) return true;
            bool has = HasFlag(requiredFlag.FlagID);
            return invertFlag ? !has : has;
        }
    }

    private bool ModeOk
    {
        get
        {
            var mc = TeaseModeController.Instance;
            return mc != null && mc.IsMode(mode);
        }
    }

    private void RefreshActivation()
    {
        SetActive(ModeOk && FlagOk);
    }

    private void SetActive(bool active)
    {
        _isActive = active;
        if (_collider != null) _collider.enabled = active;
        if (_graphic != null) _graphic.raycastTarget = active;
    }

    private void RefreshHint()
    {
        if (hints == null || hints.Length == 0) return;

        var mc = TeaseModeController.Instance;
        bool visible = mc != null && mc.IsHovered(mode) && FlagOk;

        foreach (var h in hints)
            if (h != null) h.SetActive(visible);
    }

    // ───── 手勢偵測 ─────

    public void OnPointerDown(PointerEventData eventData)
    {
        if (logDebug) Debug.Log($"[TeaseZone] {name} PointerDown（active={_isActive}）", this);
        if (!_isActive) return;

        _pressed = true;
        _pressTime = Time.time;
        _pressPos = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_pressed) return;
        _pressed = false;
        if (!_isActive) return;

        float held = Time.time - _pressTime;
        Vector2 delta = eventData.position - _pressPos;

        bool success = gesture == GestureType.Tap ? IsTap(held, delta) : IsSwipe(delta);
        if (logDebug)
            Debug.Log($"[TeaseZone] {name} PointerUp held={held:F2}s delta={delta} gesture={gesture} success={success}", this);

        if (success) Perform();
    }

    private bool IsTap(float held, Vector2 delta)
        => held <= tapMaxDuration && delta.magnitude <= tapMoveTolerance;

    private bool IsSwipe(Vector2 delta)
    {
        if (allowedDirections == null || allowedDirections.Length == 0) return false;
        if (delta.magnitude < swipeThreshold) return false;
        return System.Array.IndexOf(allowedDirections, Resolve(delta)) >= 0;
    }

    private static TeaseSwipeDir Resolve(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return delta.x > 0 ? TeaseSwipeDir.Right : TeaseSwipeDir.Left;
        return delta.y > 0 ? TeaseSwipeDir.Up : TeaseSwipeDir.Down;
    }

    // ───── 跑條觸發 ─────

    /// <summary>覆寫「下一次」動作的跑條時長（給 FSM / 效果動態調整用）。只作用一次。</summary>
    public void SetNextDuration(float seconds) => _nextDurationOverride = seconds;

    private void Perform()
    {
        var gate = TeaseActionGate.Instance;
        if (gate == null)
        {
            Debug.LogWarning($"[TeaseZone] {name} 找不到 TeaseActionGate，無法觸發。", this);
            return;
        }

        float d = _nextDurationOverride > 0f ? _nextDurationOverride : duration;
        _nextDurationOverride = -1f; // 用完即棄

        bool began = gate.TryBegin(d, onTouch, onComplete);
        if (logDebug)
            Debug.Log($"[TeaseZone] {name} Perform → gate.TryBegin(duration={d}) = {began}（false=跑條忙碌中）", this);
    }

    private static bool HasFlag(string flag)
    {
        var gss = GameStatusService.Instance;
        if (gss == null || gss.ProgressFlags == null) return false;
        return gss.ProgressFlags.Contains(flag);
    }
}
