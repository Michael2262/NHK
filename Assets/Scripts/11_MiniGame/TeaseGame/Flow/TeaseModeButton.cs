using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 模式按鈕（每顆模式按鈕一個，例如手／舌／脫衣）。
///
/// 一個元件包辦按鈕的互動：
///   - 點擊 → 切換到本按鈕的模式（TeaseModeController.SetMode）。
///   - 懸浮 或 按住（尚未放開）→ 預覽該模式的提示（各 TeaseZone 亮自己的愛心）。
///     滑鼠離開且放開後才取消預覽。
///
/// mode 字串要與 TeaseZone 的 mode、TeaseModeController 的 initialMode 一致（大小寫敏感）。
/// </summary>
[DisallowMultipleComponent]
public class TeaseModeButton : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Tooltip("這顆按鈕對應的操作模式。")]
    [SerializeField] private TeaseMode mode = TeaseMode.Hand;

    private bool _over;      // 滑鼠是否懸浮在按鈕上
    private bool _pressed;   // 是否按住中（尚未放開）
    private TeaseModeController _previewController; // 本次預覽實際使用的控制器

    public void OnPointerClick(PointerEventData eventData)
    {
        var mc = TeaseModeController.GetForScene(gameObject.scene);
        if (mc != null) mc.SetMode(mode);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _over = true;
        UpdatePreview();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _over = false;
        UpdatePreview();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pressed = true;
        UpdatePreview();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pressed = false;
        UpdatePreview();
    }

    /// <summary>懸浮或按住時預覽本模式提示；兩者都解除才取消。</summary>
    private void UpdatePreview()
    {
        var mc = TeaseModeController.GetForScene(gameObject.scene);
        if (_previewController != mc) ClearPreview();

        if (mc != null && (_over || _pressed))
        {
            _previewController = mc;
            mc.SetHoveredMode(mode);
        }
        else
        {
            ClearPreview();
        }
    }

    private void OnDisable()
    {
        _over = false;
        _pressed = false;

        ClearPreview();
    }

    private void ClearPreview()
    {
        // 不重新查詢單例，避免舊按鈕卸載時清掉新場景的預覽。
        var mc = _previewController;
        _previewController = null;
        if (mc != null && mc.IsHovered(mode)) mc.ClearHoveredMode();
    }
}
