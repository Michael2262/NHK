using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;

/// <summary>
/// Popup 呼叫器（掛在按鈕或任意物件上）
///
/// 點擊 targetButton 或手動呼叫 Invoke() 即可顯示 Popup。
/// 在 Inspector 中配置告示內容、按鈕文字、回調事件。
/// </summary>
public class PopupCaller : MonoBehaviour
{
    // ══════════════════════════════════════════
    //  Inspector 配置
    // ══════════════════════════════════════════

    [Header("觸發按鈕（留空自動抓同物件的 Button）")]
    [SerializeField] private Button targetButton;

    [Header("告示文字")]
    [Tooltip("多語系 Key（優先使用）")]
    [SerializeField] private string messageLocalizationKey;
    [Tooltip("若不使用多語系，直接填入文字")]
    [SerializeField] private string rawMessage;

    [Header("按鈕模式")]
    [SerializeField] private PopupController.ButtonMode buttonMode = PopupController.ButtonMode.ConfirmOnly;

    [Header("按鈕文字（多語系 Key，留空用 PopupController 預設）")]
    [SerializeField] private string confirmLocalizationKey;
    [SerializeField] private string cancelLocalizationKey;

    [Header("Confirm 點擊事件")]
    [SerializeField] private UnityEvent onConfirm;

    [Header("Cancel 點擊事件")]
    [SerializeField] private UnityEvent onCancel;

    // ══════════════════════════════════════════
    //  生命週期
    // ══════════════════════════════════════════

    private void Start()
    {
        if (targetButton == null)
            targetButton = GetComponent<Button>();

        if (targetButton != null)
            targetButton.onClick.AddListener(Invoke);
    }

    private void OnDestroy()
    {
        if (targetButton != null)
            targetButton.onClick.RemoveListener(Invoke);
    }

    // ══════════════════════════════════════════
    //  公開方法
    // ══════════════════════════════════════════

    /// <summary>
    /// 顯示 Popup（可由按鈕、程式碼、或 PlayMaker SendMessage 呼叫）
    /// </summary>
    public void Invoke()
    {
        if (PopupController.Instance == null)
        {
            Debug.LogWarning("[PopupCaller] PopupController 單例不存在，請確認不卸載場景已載入。");
            return;
        }

        var data = new PopupController.PopupData
        {
            messageLocalizationKey = messageLocalizationKey,
            rawMessage = rawMessage,
            buttonMode = buttonMode,
            confirmLocalizationKey = confirmLocalizationKey,
            cancelLocalizationKey = cancelLocalizationKey,
            onConfirm = onConfirm != null ? (Action)(() => onConfirm.Invoke()) : null,
            onCancel = onCancel != null ? (Action)(() => onCancel.Invoke()) : null
        };

        PopupController.Instance.Show(data);
    }
}
