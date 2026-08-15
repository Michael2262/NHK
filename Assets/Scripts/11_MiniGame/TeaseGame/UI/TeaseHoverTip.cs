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

    private TeaseHoverTipDisplay Display
        => display != null ? display : TeaseHoverTipDisplay.Instance;

    public void OnPointerEnter(PointerEventData eventData)
    {
        var d = Display;
        if (d != null) d.Show(textKey);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        var d = Display;
        if (d != null) d.Hide();
    }

    private void OnDisable()
    {
        // 物件在 hover 中被停用時，收掉提示避免卡住
        var d = Display;
        if (d != null) d.Hide();
    }
}
