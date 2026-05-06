using UnityEngine;
using PixelCrushers.DialogueSystem; // 引用 Dialogue System
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Collections;

// 建立一個專門的類別來存放語言資訊，方便在 Inspector 中設定
[System.Serializable]
public class LanguageOption
{
    [Tooltip("顯示在UI上的名稱, e.g., '繁體中文', 'English'")]
    public string displayName;
    [Tooltip("對應 Dialogue System 的語言代碼, e.g., 'zh-TW', 'en'")]
    public string languageCode;
}

/// <summary>
/// 遊戲的全域語言管理器 (Singleton)
/// 現在同時負責指揮 Dialogue System 和 Unity Localization
/// </summary>
public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance { get; private set; }

    [Header("支援的語言列表")]
    [Tooltip("在這裡設定所有遊戲支援的語言")]
    [SerializeField] private List<LanguageOption> supportedLanguages;

    [Header("預設語言")]
    [Tooltip("如果玩家從未設定過，則使用此語言代碼")]
    [SerializeField] private string defaultLanguageCode = "ja"; // 預設日文

    [Header("UI 連動 (可選)")]
    [Tooltip("將場景中的語系設定UI拖到這裡，以便在初始化後通知它更新")]
    [SerializeField] private LanguageSettingsUI languageSettingsUI;

    // 公開屬性，讓外部 (例如UI) 可以取得語言列表
    public List<LanguageOption> SupportedLanguages => supportedLanguages;

    // 目前使用的語言代碼
    public string CurrentLanguage { get; private set; }

    private bool isLocalizationInitialized = false; // 追蹤系統初始化狀態

    void Awake()
    {
        // Singleton 初始化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            //InitializeLanguage(); // 我們不在 Awake 中直接初始化，而是等待 Unity Localization 準備好
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 【新增】使用 Start Coroutine 來安全地等待初始化
    private IEnumerator Start()
    {
        // 等待 Unity Localization 系統完成它的非同步初始化
        yield return LocalizationSettings.InitializationOperation;
        isLocalizationInitialized = true; // 標記為已完成
        InitializeLanguage(); // 在此處執行真正的語言初始化
    }

    /// <summary>
    /// 初始化語言設定
    /// </summary>
    private void InitializeLanguage()
    {
        // 從 PlayerPrefs 讀取已儲存的語言，如果沒有則使用預設值
        string savedLanguage = PlayerPrefs.GetString("SelectedLanguage", defaultLanguageCode);
        SetLanguage(savedLanguage);
        // 【新增】在所有東西都準備好後，通知 UI 進行初始化
        if (languageSettingsUI != null)
        {
            languageSettingsUI.InitializeUI();
        }
    }

    /// <summary>
    /// 【已升級】供外部呼叫的核心功能：設定並儲存語言
    /// </summary>
    /// <param name="languageCode">要切換到的語言代碼 (e.g., "ja", "en", "zh-TW")</param>
    public void SetLanguage(string languageCode)
    {
        if (!isLocalizationInitialized) return;

        if (supportedLanguages.Any(lang => lang.languageCode == languageCode))
        {
            CurrentLanguage = languageCode;

            // --- 指揮鏈 ---
            // 1. 指揮「內容部」(Dialogue System) — 對話、Text Table 查表
            DialogueManager.SetLanguage(languageCode);

            // 2. 指揮「字體與資源部」(Unity Localization)
            var localeIdentifier = new LocaleIdentifier(languageCode);
            var locale = LocalizationSettings.AvailableLocales.GetLocale(localeIdentifier);
            if (locale != null)
            {
                LocalizationSettings.SelectedLocale = locale;
            }

            // 3. ★ 指揮「UI 顯示部」(Pixel Crushers UILocalizationManager)
            //    讓場景中所有 LocalizeUI 元件即時重新查表
            // 3. ★ 指揮「UI 顯示部」(Pixel Crushers UILocalizationManager)
            if (PixelCrushers.UILocalizationManager.instance != null)
            {
                PixelCrushers.UILocalizationManager.instance.currentLanguage = languageCode;
                PixelCrushers.UILocalizationManager.instance.UpdateUIs(languageCode);
            }
            // --- 指揮結束 ---

            PlayerPrefs.SetString("SelectedLanguage", languageCode);
            PlayerPrefs.Save();

            Debug.Log($"[LanguageManager] 語言已統一設定為: {languageCode}");
        }
        else
        {
            Debug.LogWarning($"LanguageManager: 嘗試切換到一個不支援的語言代碼 '{languageCode}'");
        }
        //Debug.Log($"[檢查] PlayerPrefs Language = {PlayerPrefs.GetString("Language", "(未設定)")}");
       // Debug.Log($"[檢查] UILocalizationManager.currentLanguage = {PixelCrushers.UILocalizationManager.instance.currentLanguage}");
    }

    /// <summary>
    /// 重新套用當前語言到所有 UI。
    /// 用於場景切換或特殊時機後,確保所有 LocalizeUI 顯示正確語言。
    /// </summary>
    public void ReapplyCurrentLanguage()
    {
        if (!isLocalizationInitialized || string.IsNullOrEmpty(CurrentLanguage))
        {
            Debug.LogWarning("[LanguageManager] 尚未初始化或無當前語言,跳過重新套用。");
            return;
        }

        Debug.Log($"[LanguageManager] 重新套用當前語言: {CurrentLanguage}");

        if (PixelCrushers.UILocalizationManager.instance != null)
        {
            PixelCrushers.UILocalizationManager.instance.currentLanguage = CurrentLanguage;
            PixelCrushers.UILocalizationManager.instance.UpdateUIs(CurrentLanguage);
        }
    }
}