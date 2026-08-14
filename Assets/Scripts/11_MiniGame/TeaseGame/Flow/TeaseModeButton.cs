using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 模式按鈕（每顆模式按鈕一個，例如手／舌／脫衣）。
///
/// 一個元件包辦按鈕的兩種互動：
///   - 點擊 → 切換到本按鈕的模式（TeaseModeController.SetMode）。
///   - 懸浮進入/離開 → 預覽/取消該模式的提示（各 TeaseZone 亮/滅自己的愛心）。
///
/// 取代原本「手動綁 onClick + 另掛 TeaseHintOnHover」的兩步做法。
/// mode 字串要與 TeaseZone 的 mode、TeaseModeController 的 initialMode 一致（大小寫敏感）。
/// </summary>
[DisallowMultipleComponent]
public class TeaseModeButton : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Tooltip("這顆按鈕對應的操作模式字串。")]
    [SerializeField] private string mode = "";

    public void OnPointerClick(PointerEventData eventData)
    {
        if (TeaseModeController.Instance != null)
            TeaseModeController.Instance.SetMode(mode);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TeaseModeController.Instance != null)
            TeaseModeController.Instance.SetHoveredMode(mode);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TeaseModeController.Instance != null)
            TeaseModeController.Instance.ClearHoveredMode();
    }
}
