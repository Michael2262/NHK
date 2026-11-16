using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>以進度旗標控制觸碰區，點擊或滑動成功時直接觸發自己的事件。</summary>
[DisallowMultipleComponent]
public class TeaseZone2 : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [System.Serializable]
    private class FlagCondition
    {
        public ProgressFlagDefinition flag;
        [Tooltip("勾選時，旗標必須不存在。")]
        public bool invert;
    }

    [Header("手勢")]
    [SerializeField] private TeaseZone.GestureType gesture = TeaseZone.GestureType.Tap;
    [Tooltip("滑動方向符合其中一項即可。")]
    [SerializeField] private TeaseSwipeDir[] allowedDirections;
    [Tooltip("滑動所需的最短位移（像素）。")]
    [SerializeField] private float swipeThreshold = 50f;
    [Tooltip("點擊允許的最長按住時間（秒）。")]
    [SerializeField] private float tapMaxDuration = 0.4f;
    [Tooltip("點擊允許的最大位移（像素）。")]
    [SerializeField] private float tapMoveTolerance = 20f;

    [Header("出現條件")]
    [SerializeField] private TeaseZone.FlagConditionMode flagConditionMode = TeaseZone.FlagConditionMode.AND;
    [Tooltip("忽略未指定旗標的項目。全部留空時可自由觸碰。")]
    [SerializeField] private FlagCondition[] flagConditions;

    [Header("成功時的回呼")]
    public UnityEvent onTouch = new UnityEvent();

    private Collider2D _collider;
    private Graphic _graphic;
    private GameStatusService _service;
    private bool _canTouch;
    private int? _pressedPointer;
    private float _pressTime;
    private Vector2 _pressPosition;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _graphic = GetComponent<Graphic>();
        if (_collider == null && _graphic == null)
            Debug.LogWarning($"[TeaseZone2] {name} 缺少用來接收觸碰的 Collider2D 或 Graphic。", this);
    }

    private void OnEnable() => BindAndRefresh();

    private void Start()
    {
        if (_service == null) BindAndRefresh();
    }

    private void BindAndRefresh()
    {
        _service = GameStatusService.Instance;
        if (_service != null)
        {
            _service.ProgressFlags.OnFlagChanged += HandleFlagChanged;
            _service.OnGameStatusLoaded += Refresh;
        }
        Refresh();
    }

    private void OnDisable()
    {
        if (_service != null)
        {
            _service.ProgressFlags.OnFlagChanged -= HandleFlagChanged;
            _service.OnGameStatusLoaded -= Refresh;
        }
        _service = null;
        SetTouchable(false);
    }

    private void HandleFlagChanged(string flag, bool present) => Refresh();

    public void Refresh() => SetTouchable(isActiveAndEnabled && MeetsFlagConditions());

    private void SetTouchable(bool active)
    {
        _canTouch = active;
        if (!active) _pressedPointer = null;
        if (_collider != null) _collider.enabled = active;
        if (_graphic != null) _graphic.raycastTarget = active;
    }

    private bool MeetsFlagConditions()
    {
        if (flagConditions == null) return true;
        bool useOr = flagConditionMode == TeaseZone.FlagConditionMode.OR;
        bool hasCondition = false;
        foreach (var condition in flagConditions)
        {
            if (condition == null || condition.flag == null) continue;
            hasCondition = true;
            bool present = _service != null && _service.ProgressFlags.Contains(condition.flag.FlagID);
            bool satisfied = condition.invert ? !present : present;
            if (useOr && satisfied) return true;
            if (!useOr && !satisfied) return false;
        }
        return !hasCondition || !useOr;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_canTouch || _pressedPointer.HasValue) return;
        _pressedPointer = eventData.pointerId;
        _pressTime = Time.time;
        _pressPosition = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_pressedPointer != eventData.pointerId) return;
        _pressedPointer = null;

        Vector2 delta = eventData.position - _pressPosition;
        bool success = gesture == TeaseZone.GestureType.Tap
            ? Time.time - _pressTime <= tapMaxDuration && delta.magnitude <= tapMoveTolerance
            : IsAllowedSwipe(delta);
        if (success) onTouch?.Invoke();
    }

    private bool IsAllowedSwipe(Vector2 delta)
    {
        if (allowedDirections == null || delta.magnitude < swipeThreshold) return false;
        TeaseSwipeDir direction;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            direction = delta.x > 0 ? TeaseSwipeDir.Right : TeaseSwipeDir.Left;
        else
            direction = delta.y > 0 ? TeaseSwipeDir.Up : TeaseSwipeDir.Down;
        return System.Array.IndexOf(allowedDirections, direction) >= 0;
    }
}
