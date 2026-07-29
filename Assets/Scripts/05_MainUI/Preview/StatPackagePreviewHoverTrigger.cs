using UnityEngine;
using UnityEngine.EventSystems;

// ============================================================
// StatPackagePreviewHoverTrigger.cs
// ============================================================
// 掛在按鈕（或任何 UI）上的 hover 預覽觸發器。
//
// 只存一個 packageID：滑鼠移上去 → 依該 ID 顯示數值預覽（+X / -X），
// 移開 / 點擊 / 被停用 → 清掉預覽。
//
// 「新版輸入法」相容說明：
//   UI 的 hover 走 EventSystem 上的 InputSystemUIInputModule，
//   IPointerEnter / IPointerExit 在新版 Input System 下照常觸發，
//   這正是新版偵測 UI 懸停的標準做法（不需自己 poll 滑鼠座標）。
//
// 實際掛到哪個按鈕、填哪個 packageID，由你在 Unity Editor 設定。
// ============================================================

public class StatPackagePreviewHoverTrigger : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("預覽設定")]
    [Tooltip("要預覽的 StatChangePackage ID（對應 StatChangePackageDatabase 內的套組 id）。")]
    [SerializeField] private string packageID = "";

    private bool _isHovering;

    /// <summary>供動態生成按鈕時設定 packageID（例如 IxMenuService）。</summary>
    public void SetPackageID(string id)
    {
        packageID = id;
    }

    private void OnDisable()
    {
        // 物件被關掉時，若還在懸停狀態要記得清掉預覽，避免殘留。
        if (_isHovering)
        {
            StatPackagePreviewPresenter.Hide();
            _isHovering = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(packageID)) return;
        _isHovering = true;
        StatPackagePreviewPresenter.Show(packageID);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        StatPackagePreviewPresenter.Hide();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 點下去通常會觸發真實變化（換成飄字），先把 hover 預覽收掉。
        _isHovering = false;
        StatPackagePreviewPresenter.Hide();
    }
}
