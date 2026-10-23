using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 透過與 CursorArea 相同的 Pointer 事件，放大區域內的視覺物件。
/// UI 需要 GraphicRaycaster 與可接收射線的 Graphic；
/// 世界物件需要 Collider2D 與 Physics2DRaycaster。場景中皆需 EventSystem。
/// </summary>
[DisallowMultipleComponent]
public class HoverScaleArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("縮放設定")]
    [Tooltip("留空時縮放自己。可指定子物件，讓接收滑鼠的區域保持固定大小。啟用時套用此設定。")]
    [SerializeField] private Transform scaleTarget;

    [Tooltip("相對於原始 localScale 的倍率，保留原本各軸的比例。")]
    [Min(1f)]
    [SerializeField] private float hoverScale = 1.06f;

    [Tooltip("放大與還原所需秒數；0 代表立即切換。使用不受遊戲暫停影響的時間。")]
    [Min(0f)]
    [SerializeField] private float duration = 0.12f;

    [SerializeField] private Ease ease = Ease.OutQuad;

    private Transform _target;
    private Vector3 _originalScale;
    private Tween _scaleTween;
    private Selectable _selectable;
    private bool _pointerInside;
    private bool _isEnlarged;

    private void OnEnable()
    {
        _target = scaleTarget != null ? scaleTarget : transform;
        _originalScale = _target.localScale;
        _selectable = GetComponent<Selectable>();
        _pointerInside = false;
        _isEnlarged = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isActiveAndEnabled) return;
        _pointerInside = true;
        RefreshScale();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isActiveAndEnabled) return;
        _pointerInside = false;
        RefreshScale();
    }

    private void Update()
    {
        // 滑鼠停留期間，按鈕或父層 CanvasGroup 也可能切換可互動狀態。
        if (_pointerInside) RefreshScale();
    }

    private void RefreshScale()
    {
        bool enlarge = _pointerInside &&
            (_selectable == null || (_selectable.IsActive() && _selectable.IsInteractable()));
        if (_target == null || enlarge == _isEnlarged) return;

        _isEnlarged = enlarge;
        // 只停止自己建立的補間；快速進出時從目前大小接續，不累乘縮放。
        _scaleTween?.Kill();
        _scaleTween = null;
        Vector3 destination = enlarge ? _originalScale * Mathf.Max(1f, hoverScale) : _originalScale;
        if (duration <= 0f)
        {
            _target.localScale = destination;
            return;
        }

        _scaleTween = _target.DOScale(destination, duration).SetEase(ease).SetUpdate(true);
    }

    private void OnDisable()
    {
        _scaleTween?.Kill();
        _scaleTween = null;
        if (_target != null) _target.localScale = _originalScale;
        _pointerInside = false;
        _isEnlarged = false;
    }
}
