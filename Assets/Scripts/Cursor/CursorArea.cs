using UnityEngine;
using UnityEngine.EventSystems;

// 實作 EventSystem 的 Pointer 介面。
// 這兩個介面 UI 與世界物件通用：
//   - 世界物件：需要 Collider2D + 攝影機上的 Physics2DRaycaster
//   - UI 物件 ：需要 Image(等 Graphic) 勾 Raycast Target + Canvas 上的 GraphicRaycaster
// 因此這裡不再強制 Collider2D，才能同時掛在 UI Image 上。
//
// 巢狀重疊處理：Pointer 事件會沿父鏈往上冒泡，父物件的 CursorArea 也會收到子物件的
// 進出事件。為避免「父層後手覆蓋子層」，這裡採「指到誰、離它最近的 CursorArea 就贏，
// 父層自動讓位」——以滑鼠實際命中的最上層目標(pointerCurrentRaycast)往上找最近的
// CursorArea 當贏家。前提：想贏的物件要是 raycast 命中的最上層(sorting 要夠高)。
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
    [Tooltip("留空 = 監聽自己身上的 Pointer 事件（含上述巢狀讓位規則）。\n" +
             "拖入一個 UI 物件(Image / Button…) = 改監聽那個物件的 hover，\n" +
             "此腳本本身就不需要 Collider / Raycast Target。")]
    public GameObject hoverTarget;

    // 目前正在「管」游標的 CursorArea（全域唯一）。用來在冒泡/交棒時判斷誰該生效。
    private static CursorArea _activeArea;

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

        // 若「還被 hover 著」就被關掉（例如按鈕按下立刻觸發對話並關閉自己），
        // Unity 不會發 OnPointerExit，這裡補做一次收回，避免游標卡在此區域圖案。
        if (_activeArea == this)
        {
            _activeArea = null;
            if (GlobalCursorManager.Instance != null)
            {
                GlobalCursorManager.Instance.ResetToDefaultCursor();
                GlobalCursorManager.Instance.ClearHoverScale(hoverDuration);
            }
        }
    }

    // --- 自己身上的 Pointer 事件（hoverTarget 留空時走這條，含巢狀讓位）---

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverTarget != null) return;               // 已改由 hoverTarget 監聽
        if (ResolveWinner(eventData) != this) return;  // 只有離命中目標最近的 CursorArea 生效
        Activate();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverTarget != null) return;
        if (_activeArea != this) return;               // 不是我在管，無視這次冒泡

        // 交棒：看滑鼠新的落點，換成新贏家；沒有就回預設
        CursorArea next = ResolveWinner(eventData);
        if (next != null && next != this)
            next.Activate();
        else if (next == null)
            Deactivate();
        // next == this：理論上已離開不會發生，保險起見不動作
    }

    /// <summary>以滑鼠實際命中的最上層目標，往上找最近的 CursorArea 當贏家。</summary>
    private static CursorArea ResolveWinner(PointerEventData eventData)
    {
        if (eventData == null) return null;
        GameObject target = eventData.pointerCurrentRaycast.gameObject;
        if (target == null) return null;
        return target.GetComponentInParent<CursorArea>();
    }

    // --- 實際套用 / 收回（自己 or hoverTarget 都走這裡）---

    private void Activate()
    {
        if (GlobalCursorManager.Instance == null) return;
        _activeArea = this;

        // 只換圖案；hotspot 沿用預設游標的設定
        GlobalCursorManager.Instance.SetCursorArea(normalTexture, clickTexture);

        // 進入時放大並維持；沒開放大則確保清掉前一個區域殘留的放大
        if (enableHoverScale)
            GlobalCursorManager.Instance.ApplyHoverScale(hoverScale, hoverDuration);
        else
            GlobalCursorManager.Instance.ClearHoverScale(hoverDuration);
    }

    private void Deactivate()
    {
        if (GlobalCursorManager.Instance == null) return;
        if (_activeArea == this) _activeArea = null;

        // 恢復為「預設」圖案並縮回原大小
        GlobalCursorManager.Instance.ResetToDefaultCursor();
        GlobalCursorManager.Instance.ClearHoverScale(hoverDuration);
    }

    // --- hoverTarget（監聽別的物件）---

    /// <summary>若有指定 hoverTarget，於其上掛 EventTrigger 監聽進出。</summary>
    private void BindHoverTarget()
    {
        if (hoverTarget == null) return;

        _boundTrigger = hoverTarget.GetComponent<EventTrigger>();
        if (_boundTrigger == null)
            _boundTrigger = hoverTarget.AddComponent<EventTrigger>();

        _enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        _enterEntry.callback.AddListener(_ => Activate());
        _boundTrigger.triggers.Add(_enterEntry);

        _exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        _exitEntry.callback.AddListener(_ => Deactivate());
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
}
