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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (TeaseModeController.Instance != null)
            TeaseModeController.Instance.SetMode(mode);
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
        var mc = TeaseModeController.Instance;
        if (mc == null) return;

        if (_over || _pressed)
        {
            mc.SetHoveredMode(mode);
        }
        else if (mc.IsHovered(mode))
        {
            // 只有目前預覽的是自己時才取消，避免蓋掉別顆按鈕的預覽
            mc.ClearHoveredMode();
        }
    }

    private void OnDisable()
    {
        _over = false;
        _pressed = false;

        var mc = TeaseModeController.Instance;
        if (mc != null && mc.IsHovered(mode)) mc.ClearHoveredMode();
    }
}
