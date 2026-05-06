using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuController : MonoBehaviour
{
    [Header("統一的按鈕")]
    [SerializeField] private Button confirmAllButton; // 這是那個共用的確認按鈕

    [Header("管理的 UI 腳本")]
    [SerializeField] private LanguageSettingsUI languageSettingsUI;
    [SerializeField] private ResolutionSettingsUI resolutionSettingsUI;
    // 如果未來有音量設定UI，也可以加進來
    // [SerializeField] private AudioSettingsUI audioSettingsUI; 

    void Start()
    {
        if (confirmAllButton == null)
        {
            Debug.LogError("尚未設定共用的確認按鈕！");
            return;
        }

        // 為共用按鈕綁定一個事件，當它被點擊時，呼叫 ConfirmAllSettings 方法
        confirmAllButton.onClick.AddListener(ConfirmAllSettings);
    }

    /// <summary>
    /// 呼叫所有子設定 UI 的確認方法
    /// </summary>
    public void ConfirmAllSettings()
    {
        // 檢查確保腳本都已設定
        if (languageSettingsUI != null)
        {
            // 注意：我們需要將 LanguageSettingsUI 中的 OnConfirmLanguage 方法改成 public
            languageSettingsUI.OnConfirmLanguage();
        }

        if (resolutionSettingsUI != null)
        {
            resolutionSettingsUI.OnConfirmSettings();
        }

        // if (audioSettingsUI != null) { ... }

        Debug.Log("所有設定已儲存！");
        // 在這裡可以加上關閉設定視窗或播放音效等通用邏輯
    }
}