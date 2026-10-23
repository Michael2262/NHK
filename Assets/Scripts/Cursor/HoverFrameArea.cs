using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 掛在按鈕根物件，透過 Pointer 事件控制子物件的懸停框線。
/// 僅切換 Graphic 的顯示，不停用物件，避免影響排版。
/// </summary>
[DisallowMultipleComponent]
public class HoverFrameArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("拖入按鈕底下 Frame 的 Image。Frame 物件須保持啟用，且不要作為 Button 的 Target Graphic。")]
    [SerializeField] private Graphic hoverFrame;

    private Selectable _selectable;
    private bool _pointerInside;

    private void OnEnable()
    {
        _selectable = GetComponent<Selectable>();
        _pointerInside = false;
        if (hoverFrame != null)
        {
            // 框線只負責顯示，滑鼠射線由按鈕原本的 Graphic 接收。
            hoverFrame.raycastTarget = false;
        }
        SetFrameVisible(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isActiveAndEnabled) return;
        _pointerInside = true;
        RefreshFrame();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isActiveAndEnabled) return;
        _pointerInside = false;
        SetFrameVisible(false);
    }

    private void Update()
    {
        // 同步停留期間 Button 或父層 CanvasGroup 的可互動狀態。
        if (_pointerInside) RefreshFrame();
    }

    private void RefreshFrame()
    {
        bool interactable = _selectable == null ||
            (_selectable.IsActive() && _selectable.IsInteractable());
        SetFrameVisible(_pointerInside && interactable);
    }

    private void SetFrameVisible(bool visible)
    {
        if (hoverFrame != null && hoverFrame.enabled != visible)
            hoverFrame.enabled = visible;
    }

    private void OnDisable()
    {
        _pointerInside = false;
        SetFrameVisible(false);
    }
}
