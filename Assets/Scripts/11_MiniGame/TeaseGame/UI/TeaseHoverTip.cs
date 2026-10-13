using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// hover 顯示提示字（掛在觸碰區上）。
///
/// 滑鼠移到此物件上 → 把指定的 Text Table key 交給 TeaseHoverTipDisplay，
/// 在固定位置顯示對應提示；離開 → 隱藏。
///
/// 觸碰區判定來源不限：只要物件可被射線打到（Collider2D + 攝影機 Physics2DRaycaster，
/// 或 Canvas Image + GraphicRaycaster），IPointerEnter/Exit 都會照常觸發，兩種都支援。
/// </summary>
[DisallowMultipleComponent]
public class TeaseHoverTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("要顯示的 Text Table key。")]
    [SerializeField] private string textKey;

    [Tooltip("指定顯示器；留空則用 TeaseHoverTipDisplay.Instance。")]
    [SerializeField] private TeaseHoverTipDisplay display;

    // 記住這次 hover 使用的顯示器，離開／卸載時不可改抓新場景的單例。
    private TeaseHoverTipDisplay _hoverDisplay;

    private TeaseHoverTipDisplay Display
        => display != null ? display : TeaseHoverTipDisplay.Instance;

    public void OnPointerEnter(PointerEventData eventData)
    {
        HideHoverTip();
        var d = Display;
        // 自動尋找時只使用本場景的顯示器，避免轉場期間舊觸碰區操作新場景。
        if (d == null || (display == null && d.gameObject.scene != gameObject.scene)) return;
        _hoverDisplay = d;
        d.Show(textKey);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideHoverTip();
    }

    private void OnDisable()
    {
        // 只收掉這次 hover 使用的顯示器；未 hover 的物件停用不影響提示。
        HideHoverTip();
    }

    private void HideHoverTip()
    {
        var d = _hoverDisplay;
        _hoverDisplay = null;
        if (d != null) d.Hide();
    }
}
