using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 指令按鈕頁面標記。
///
/// 掛在一個「頁面容器」GameObject 上，底下放這一頁的所有按鈕。
/// 由 PagedCommandButtonGroup 統一控制顯示/隱藏，不要自行 SetActive。
///
/// 使用方式：
///   1. 在指令面板下建立空物件（例如 Page_Main、Page_Undress），掛此腳本。
///   2. 把這一頁的按鈕放進該物件底下。
///   3. 設定 pageId，並把此物件加進 PagedCommandButtonGroup 的 pages 列表。
/// </summary>
public class CommandButtonPage : MonoBehaviour
{
    [Header("頁面設定")]
    [Tooltip("頁面 ID。給 PagedCommandButtonGroup.ShowPage(pageId) 指名切換用。")]
    public string pageId;

    [Tooltip("是否參與「上一頁 / 下一頁」的循環。" +
             "主指令頁請勾選；子選單頁（脫衣服、觸摸等）請取消勾選，" +
             "這樣 NextPage / PreviousPage 不會誤入子選單。")]
    public bool includeInPaging = true;

    [Tooltip("此頁被隱藏時，重置頁面底下所有 CommandButton 的時間條點擊計數。" +
             "不勾選則切回此頁時，計數從上次離開處繼續累計。")]
    public bool resetClickCountsOnHide = false;

    [Header("頁面事件（可選）")]
    [Tooltip("此頁被顯示時觸發。可接音效、動畫等。")]
    public UnityEvent onPageShown;

    [Tooltip("此頁被隱藏時觸發。")]
    public UnityEvent onPageHidden;

    /// <summary>
    /// 由 PagedCommandButtonGroup 呼叫，切換此頁的顯示狀態。
    /// 狀態沒有變化時不重複觸發事件。
    /// </summary>
    internal void SetShown(bool shown)
    {
        if (gameObject.activeSelf == shown) return;

        gameObject.SetActive(shown);

        if (shown)
        {
            onPageShown?.Invoke();
        }
        else
        {
            if (resetClickCountsOnHide)
            {
                foreach (var button in GetComponentsInChildren<CommandButton>(true))
                    button.ResetClickCount();
            }

            onPageHidden?.Invoke();
        }
    }
}
