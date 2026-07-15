using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 指令按鈕換頁控制器。搭配 CommandButtonGroup 使用。
///
/// 概念：
///   - 每一「頁」是一個掛著 CommandButtonPage 的容器物件，底下放該頁的按鈕。
///   - 換頁 = 關掉目前頁的容器、打開目標頁的容器。
///     CommandButton 會在 OnEnable/OnDisable 時自動向 group 註冊/取消註冊，
///     所以 CommandButtonGroup 的按鈕名單會自動跟著更新。
///   - 常駐按鈕（脫衣服、觸摸等入口）放在所有頁面容器「外面」，永遠顯示。
///
/// 換頁按鈕掛法（用普通 Unity Button，不要掛 CommandButton）：
///   - 「脫衣服」入口 → Button.OnClick 掛 ShowPage(pageId)
///   - 「下一頁 / 上一頁」 → 掛 NextPage() / PreviousPage()
///   - 子選單的「返回」 → 掛 Back()
///
/// 與跑條的關係：
///   - 跑條期間「允許」換頁；但機率按鈕仍被群組鎖定，無法觸發下一次跑條。
///   - 跑條中換頁進來的新按鈕，會由 CommandButtonGroup.Register() 同步鎖定狀態。
///
/// 注意：共用 UI（時間條、結果文字、思考氣泡）請放在頁面容器外，避免被換頁藏掉。
/// </summary>
public class PagedCommandButtonGroup : MonoBehaviour
{
    [Header("頁面")]
    [Tooltip("所有頁面。每頁是一個掛著 CommandButtonPage 的容器物件。")]
    public List<CommandButtonPage> pages = new List<CommandButtonPage>();

    [Tooltip("預設頁。未指定則使用 pages 列表的第一個。")]
    public CommandButtonPage defaultPage;

    [Header("連動群組（可選）")]
    [Tooltip("換頁後會呼叫其 UpdateButtonConditions()，讓新頁面上的 Flag 條件按鈕立即生效。")]
    public CommandButtonGroup linkedGroup;

    [Header("行為")]
    [Tooltip("OnEnable 時回到預設頁並清空返回歷史。面板重開時建議勾選。")]
    public bool resetToDefaultOnEnable = true;

    [Tooltip("NextPage 超過最後一頁時循環回第一頁（PreviousPage 同理）。不勾則停在頭尾。")]
    public bool loopPaging = true;

    [Header("Debug")]
    [SerializeField] private string currentPageId;

    private CommandButtonPage _current;
    private readonly Stack<CommandButtonPage> _history = new Stack<CommandButtonPage>();

    private void OnEnable()
    {
        if (resetToDefaultOnEnable || _current == null)
            ResetToDefault();
    }

    // ─────────────────────────────────────────────
    // 公開 API（可掛 Button.OnClick / UnityEvent / FSM）
    // ─────────────────────────────────────────────

    /// <summary>
    /// 目前顯示中的頁面。
    /// </summary>
    public CommandButtonPage CurrentPage => _current;

    /// <summary>
    /// 切換到指定 ID 的頁面。找不到則警告並不動作。
    /// </summary>
    public void ShowPage(string pageId)
    {
        var page = FindPage(pageId);
        if (page == null)
        {
            Debug.LogWarning($"[PagedCommandButtonGroup] 找不到 pageId「{pageId}」的頁面。", this);
            return;
        }

        SwitchTo(page, pushHistory: true);
    }

    /// <summary>
    /// 切到下一個參與循環（includeInPaging）的頁面。
    /// </summary>
    public void NextPage()
    {
        StepPaging(+1);
    }

    /// <summary>
    /// 切到上一個參與循環（includeInPaging）的頁面。
    /// </summary>
    public void PreviousPage()
    {
        StepPaging(-1);
    }

    /// <summary>
    /// 回到上一次待的頁面（依切換歷史）。沒有歷史則不動作。
    /// 適合子選單的「返回」按鈕。
    /// </summary>
    public void Back()
    {
        if (_history.Count == 0)
        {
            Debug.Log("[PagedCommandButtonGroup] 沒有可返回的頁面歷史。");
            return;
        }

        SwitchTo(_history.Pop(), pushHistory: false);
    }

    /// <summary>
    /// 回到預設頁並清空返回歷史。
    /// </summary>
    public void ResetToDefault()
    {
        _history.Clear();

        var target = defaultPage != null
            ? defaultPage
            : (pages.Count > 0 ? pages[0] : null);

        if (target == null)
        {
            Debug.LogWarning("[PagedCommandButtonGroup] pages 列表為空，沒有可顯示的頁面。", this);
            return;
        }

        SwitchTo(target, pushHistory: false);
    }

    // ─────────────────────────────────────────────
    // 內部實作
    // ─────────────────────────────────────────────

    private CommandButtonPage FindPage(string pageId)
    {
        foreach (var page in pages)
        {
            if (page != null && page.pageId == pageId)
                return page;
        }
        return null;
    }

    /// <summary>
    /// 在參與循環的頁面之間前進 / 後退一頁。
    /// 目前頁若不在循環名單內（例如正在子選單），則跳到循環的第一頁。
    /// </summary>
    private void StepPaging(int step)
    {
        var pagingPages = new List<CommandButtonPage>();
        foreach (var page in pages)
        {
            if (page != null && page.includeInPaging)
                pagingPages.Add(page);
        }

        if (pagingPages.Count == 0)
        {
            Debug.LogWarning("[PagedCommandButtonGroup] 沒有任何頁面勾選 includeInPaging，無法翻頁。", this);
            return;
        }

        int currentIndex = pagingPages.IndexOf(_current);
        int newIndex;

        if (currentIndex < 0)
        {
            // 目前在子選單等非循環頁 → 回到循環的第一頁
            newIndex = 0;
        }
        else if (loopPaging)
        {
            newIndex = (currentIndex + step + pagingPages.Count) % pagingPages.Count;
        }
        else
        {
            newIndex = Mathf.Clamp(currentIndex + step, 0, pagingPages.Count - 1);
        }

        SwitchTo(pagingPages[newIndex], pushHistory: true);
    }

    private void SwitchTo(CommandButtonPage target, bool pushHistory)
    {
        if (target == null || target == _current) return;

        if (pushHistory && _current != null)
            _history.Push(_current);

        // 先關掉所有非目標頁（含編輯器裡忘記關的頁），再開目標頁
        foreach (var page in pages)
        {
            if (page == null || page == target) continue;
            page.SetShown(false);
        }

        target.SetShown(true);

        _current = target;
        currentPageId = target.pageId;

        // 讓新頁面上的 Flag 條件按鈕立即套用顯示條件
        if (linkedGroup != null)
            linkedGroup.UpdateButtonConditions();
    }
}
