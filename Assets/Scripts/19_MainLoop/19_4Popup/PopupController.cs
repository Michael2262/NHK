using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelCrushers.DialogueSystem;
using System;

/// <summary>
/// Popup 控制器（單例）
/// 放在不卸載的場景上，接收外部呼叫後顯示 Popup 面板。
///
/// 使用方式：
///   PopupController.Instance.Show(new PopupData { ... });
///
/// Prefab 結構需求：
///   PopupPanel (GameObject)
///   ├─ MessageText   (TextMeshProUGUI)
///   ├─ ConfirmButton (Button + TextMeshProUGUI 子物件)
///   └─ CancelButton  (Button + TextMeshProUGUI 子物件)
/// </summary>
public class PopupController : MonoBehaviour
{
    // ══════════════════════════════════════════
    //  Singleton
    // ══════════════════════════════════════════

    public static PopupController Instance { get; private set; }

    // ══════════════════════════════════════════
    //  請求用資料結構
    // ══════════════════════════════════════════

    /// <summary>
    /// 按鈕數量模式
    /// </summary>
    public enum ButtonMode
    {
        ConfirmOnly,        // 只有 Confirm（單按鈕）
        ConfirmAndCancel    // Confirm + Cancel（雙按鈕）
    }

    /// <summary>
    /// 外部傳入的 Popup 設定資料
    /// </summary>
    [Serializable]
    public class PopupData
    {
        [Header("告示文字（多語系 Key）")]
        [Tooltip("Dialogue System Text Table 的 Field Name，留空則直接顯示 rawMessage")]
        public string messageLocalizationKey;

        [Tooltip("若不使用多語系，可直接填入文字")]
        public string rawMessage;

        [Header("按鈕模式")]
        public ButtonMode buttonMode = ButtonMode.ConfirmOnly;

        [Header("Confirm 按鈕文字（多語系 Key，留空用預設）")]
        public string confirmLocalizationKey;

        [Header("Cancel 按鈕文字（多語系 Key，留空用預設）")]
        public string cancelLocalizationKey;

        [Header("回調")]
        public Action onConfirm;
        public Action onCancel;
    }

    // ══════════════════════════════════════════
    //  Inspector 配置
    // ══════════════════════════════════════════

    [Header("Popup 面板")]
    [SerializeField] private GameObject popupPanel;

    [Header("UI 元素")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TextMeshProUGUI confirmButtonText;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI cancelButtonText;

    [Header("預設按鈕文字（多語系 Key）")]
    [Tooltip("Confirm 按鈕的預設多語系 Key")]
    [SerializeField] private string defaultConfirmKey = "UI_Confirm";
    [Tooltip("Cancel 按鈕的預設多語系 Key")]
    [SerializeField] private string defaultCancelKey = "UI_Cancel";

    // ══════════════════════════════════════════
    //  內部狀態
    // ══════════════════════════════════════════

    private PopupData _currentData;

    // ══════════════════════════════════════════
    //  生命週期
    // ══════════════════════════════════════════

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[PopupController] 發現重複的 PopupController，已銷毀。");
            Destroy(gameObject);
            return;
        }

        if (popupPanel != null) popupPanel.SetActive(false);

        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);
    }

    private void OnDestroy()
    {
        if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirmClicked);
        if (cancelButton != null) cancelButton.onClick.RemoveListener(OnCancelClicked);
        if (Instance == this) Instance = null;
    }

    // ══════════════════════════════════════════
    //  公開 API
    // ══════════════════════════════════════════

    /// <summary>
    /// 顯示 Popup
    /// </summary>
    public void Show(PopupData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[PopupController] PopupData 為 null，忽略。");
            return;
        }

        _currentData = data;

        // ── 設定告示文字 ──
        if (messageText != null)
        {
            messageText.text = ResolveLocalizedText(data.messageLocalizationKey, data.rawMessage);
        }

        // ── 設定 Confirm 按鈕文字 ──
        if (confirmButtonText != null)
        {
            string confirmKey = string.IsNullOrEmpty(data.confirmLocalizationKey)
                ? defaultConfirmKey
                : data.confirmLocalizationKey;
            confirmButtonText.text = ResolveLocalizedText(confirmKey, confirmKey);
        }

        // ── 設定按鈕模式 ──
        if (cancelButton != null)
        {
            bool showCancel = data.buttonMode == ButtonMode.ConfirmAndCancel;
            cancelButton.gameObject.SetActive(showCancel);

            if (showCancel && cancelButtonText != null)
            {
                string cancelKey = string.IsNullOrEmpty(data.cancelLocalizationKey)
                    ? defaultCancelKey
                    : data.cancelLocalizationKey;
                cancelButtonText.text = ResolveLocalizedText(cancelKey, cancelKey);
            }
        }

        // ── 顯示面板 ──
        if (popupPanel != null) popupPanel.SetActive(true);
    }

    /// <summary>
    /// 簡易呼叫：只顯示訊息 + 單按鈕確認
    /// </summary>
    public void ShowMessage(string messageKey, Action onConfirm = null)
    {
        Show(new PopupData
        {
            messageLocalizationKey = messageKey,
            buttonMode = ButtonMode.ConfirmOnly,
            onConfirm = onConfirm
        });
    }

    /// <summary>
    /// 簡易呼叫：顯示訊息 + 確認/取消雙按鈕
    /// </summary>
    public void ShowConfirmCancel(string messageKey, Action onConfirm = null, Action onCancel = null)
    {
        Show(new PopupData
        {
            messageLocalizationKey = messageKey,
            buttonMode = ButtonMode.ConfirmAndCancel,
            onConfirm = onConfirm,
            onCancel = onCancel
        });
    }

    /// <summary>
    /// 關閉 Popup（不觸發任何回調）
    /// </summary>
    public void ForceClose()
    {
        CloseInternal();
    }

    public bool IsOpen => popupPanel != null && popupPanel.activeSelf;

    // ══════════════════════════════════════════
    //  按鈕事件
    // ══════════════════════════════════════════

    private void OnConfirmClicked()
    {
        var callback = _currentData?.onConfirm;
        CloseInternal();
        callback?.Invoke();
    }

    private void OnCancelClicked()
    {
        var callback = _currentData?.onCancel;
        CloseInternal();
        callback?.Invoke();
    }

    // ══════════════════════════════════════════
    //  工具方法
    // ══════════════════════════════════════════

    private void CloseInternal()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
        _currentData = null;
    }

    /// <summary>
    /// 嘗試用多語系 Key 取得文字，失敗則回傳 fallback
    /// </summary>
    private string ResolveLocalizedText(string localizationKey, string fallback)
    {
        if (string.IsNullOrEmpty(localizationKey))
            return fallback ?? string.Empty;

        string localized = DialogueManager.GetLocalizedText(localizationKey);
        if (!string.IsNullOrEmpty(localized))
            return localized;

        // Key 查不到時回傳 fallback
        return fallback ?? localizationKey;
    }
}
