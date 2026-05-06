using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 休息按鈕的體力回復預覽觸發器
/// 當滑鼠懸停在休息按鈕上時，自動計算並顯示預計回復的體力量。
/// 
/// 支援三種休息模式：
///   - RestOneSlot     : 休息 1 個 Slot
///   - RestToNextPhase : 休息到下一個 Phase
///   - RestToNextDay   : 休息到隔天
/// 
/// 使用方式：
///   靜態掛載：將此腳本掛載在休息按鈕上，在 Inspector 中選擇對應的 restMode。
///   動態掛載：由 ResponseListService 在生成按鈕時透過 AddComponent + SetRestMode() 設定。
/// </summary>
public class RestPreviewHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    // ==================================================
    // 設定
    // ==================================================
    public enum RestMode
    {
        None,              // 不啟用預覽（用於 ButtonData 的預設值）
        RestOneSlot,       // 休息 1 個 Slot
        RestToNextPhase,   // 休息到下一個 Phase
        RestToNextDay      // 休息到隔天
    }

    [Header("休息模式")]
    [Tooltip("選擇此按鈕對應的休息類型")]
    [SerializeField] private RestMode restMode = RestMode.RestOneSlot;

    // ==================================================
    // 私有變數
    // ==================================================
    private bool _isHovering = false;

    // ==================================================
    // 生命週期
    // ==================================================

    private void OnDisable()
    {
        // 當腳本或物件被停用時，確保隱藏預覽
        if (_isHovering)
        {
            HidePreview();
            _isHovering = false;
        }
    }

    // ==================================================
    // IPointerEnterHandler / IPointerExitHandler / IPointerClickHandler
    // ==================================================

    /// <summary>
    /// 滑鼠進入時觸發
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        ShowPreview();
    }

    /// <summary>
    /// 滑鼠離開時觸發
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        HidePreview();
    }

    /// <summary>
    /// 滑鼠點擊時觸發，結束預覽狀態
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        _isHovering = false;
        HidePreview();
    }

    // ==================================================
    // 公開方法（供動態掛載時設定）
    // ==================================================

    /// <summary>
    /// 設定休息模式（供 ResponseListService 動態掛載時呼叫）
    /// </summary>
    public void SetRestMode(RestMode mode)
    {
        restMode = mode;
    }

    // ==================================================
    // 預覽邏輯
    // ==================================================

    /// <summary>
    /// 根據 restMode 從 RestButtonController 取得預覽數值
    /// </summary>
    private int GetPreviewDelta()
    {
        if (restMode == RestMode.None) return 0;

        var controller = RestButtonController.Instance;
        if (controller == null) return 0;

        switch (restMode)
        {
            case RestMode.RestOneSlot:
                return controller.CalculateRestOneSlotPreview();

            case RestMode.RestToNextPhase:
                return controller.CalculateRestToNextPhasePreview();

            case RestMode.RestToNextDay:
                return controller.CalculateRestToNextDayPreview();

            default:
                return 0;
        }
    }

    /// <summary>
    /// 顯示預覽（正數 = 增加預覽）
    /// </summary>
    private void ShowPreview()
    {
        int delta = GetPreviewDelta();
        if (delta <= 0) return;

        LobbyUI_V2.Instance?.ShowStaminaPreview(delta);
    }

    /// <summary>
    /// 隱藏預覽
    /// </summary>
    private void HidePreview()
    {
        LobbyUI_V2.Instance?.HideStaminaPreview();
    }
}