using UnityEngine;
using UnityEngine.EventSystems;

// 實作 EventSystem 的 Pointer 介面。
// 這兩個介面 UI 與世界物件通用：
//   - 世界物件：需要 Collider2D + 攝影機上的 Physics2DRaycaster
//   - UI 物件 ：需要 Image(等 Graphic) 勾 Raycast Target + Canvas 上的 GraphicRaycaster
// 因此這裡不再強制 Collider2D，才能同時掛在 UI Image 上。
public class CursorArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("此區域的鼠標 (Area-Specific)")]
    [Tooltip("只換圖案；hotspot（對位點/縮放軸心）沿用預設游標的設定。")]
    public Texture2D normalTexture;
    public Texture2D clickTexture;

    [Header("進入時放大 (可選)")]
    [Tooltip("勾選後，滑鼠進入此區域時游標會放大並「維持」，離開才縮回。")]
    public bool enableHoverScale = false;
    [Tooltip("進入時放大到的倍率（1 = 原大小）")]
    public float hoverScale = 1.2f;
    [Tooltip("放大 / 縮回的補間時間（秒）")]
    public float hoverDuration = 0.12f;

    [Header("監聽目標 (可選)")]
    [Tooltip("留空 = 監聽自己身上的 Pointer 事件。\n" +
             "拖入一個 UI 物件(Image / Button…) = 改監聽那個物件的 hover，\n" +
             "此腳本本身就不需要 Collider / Raycast Target。")]
    public GameObject hoverTarget;

    // 掛到 hoverTarget 上的 EventTrigger 與我們加入的兩個項目（用於離開時移除）
    private EventTrigger _boundTrigger;
    private EventTrigger.Entry _enterEntry;
    private EventTrigger.Entry _exitEntry;

    private void OnEnable()
    {
        BindHoverTarget();
    }

    private void OnDisable()
    {
        UnbindHoverTarget();
    }

    /// <summary>若有指定 hoverTarget，於其上掛 EventTrigger 監聽進出。</summary>
    private void BindHoverTarget()
    {
        if (hoverTarget == null) return;

        _boundTrigger = hoverTarget.GetComponent<EventTrigger>();
        if (_boundTrigger == null)
            _boundTrigger = hoverTarget.AddComponent<EventTrigger>();

        _enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        _enterEntry.callback.AddListener(_ => EnterArea());
        _boundTrigger.triggers.Add(_enterEntry);

        _exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        _exitEntry.callback.AddListener(_ => ExitArea());
        _boundTrigger.triggers.Add(_exitEntry);
    }

    /// <summary>移除先前掛上的監聽（不刪除別人可能也在用的 EventTrigger 元件）。</summary>
    private void UnbindHoverTarget()
    {
        if (_boundTrigger == null) return;

        if (_enterEntry != null) _boundTrigger.triggers.Remove(_enterEntry);
        if (_exitEntry != null) _boundTrigger.triggers.Remove(_exitEntry);

        _boundTrigger = null;
        _enterEntry = null;
        _exitEntry = null;
    }

    // --- 自己身上的 Pointer 事件（hoverTarget 留空時走這條）---

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverTarget != null) return; // 已改由 hoverTarget 監聽，避免重複
        EnterArea();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverTarget != null) return;
        ExitArea();
    }

    // --- 進 / 出區域的實際行為（自己 or hoverTarget 都走這裡）---

    private void EnterArea()
    {
        if (GlobalCursorManager.Instance == null) return;

        // 只換圖案；hotspot 沿用預設游標的設定
        GlobalCursorManager.Instance.SetCursorArea(normalTexture, clickTexture);

        // 可選：進入時放大並維持
        if (enableHoverScale)
            GlobalCursorManager.Instance.ApplyHoverScale(hoverScale, hoverDuration);
    }

    private void ExitArea()
    {
        if (GlobalCursorManager.Instance == null) return;

        // 恢復為「預設」圖案
        GlobalCursorManager.Instance.ResetToDefaultCursor();

        // 可選：離開時縮回原大小
        if (enableHoverScale)
            GlobalCursorManager.Instance.ClearHoverScale(hoverDuration);
    }
}
