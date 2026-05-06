using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
// using TMPro; // 如果使用 TextMeshPro

/// <summary>
/// 專門處理語系切換相關的 UI 互動邏輯
/// </summary>
public class LanguageSettingsUI : MonoBehaviour
{
    [Header("UI 元件")]
    [SerializeField] private Button nextLanguageButton;
    [SerializeField] private Button previousLanguageButton;
    [SerializeField] private Text currentLanguageText; // 或者使用 TextMeshProUGUI
    [SerializeField] private Button confirmLanguageButton;

    // 從 LanguageManager 獲取的語言列表
    private List<LanguageOption> languageOptions;
    // 玩家在UI上預選的語言索引，尚未確認
    private int stagedLanguageIndex;

    void Start()
    {
        // 確保 LanguageManager 存在
        if (LanguageManager.Instance == null)
        {
            Debug.LogError("LanguageSettingsUI: 場景中找不到 LanguageManager!");
            return;
        }

        //InitializeUI();// 我們不再於 Start 中立即初始化，而是等待 LanguageManager 通知

        // 綁定按鈕的點擊事件
        nextLanguageButton.onClick.AddListener(OnNextLanguage);
        previousLanguageButton.onClick.AddListener(OnPreviousLanguage);
        confirmLanguageButton.onClick.AddListener(OnConfirmLanguage);
    }

    /// <summary>
    /// 初始化UI顯示，將此方法設為 public，供 LanguageManager 在初始化完成後呼叫
    /// </summary>
    public void InitializeUI()
    {
        // 1. 從 LanguageManager 獲取支援的語言列表
        languageOptions = LanguageManager.Instance.SupportedLanguages;
        if (languageOptions == null || languageOptions.Count == 0) return;

        // 2. 找到目前遊戲正在使用的語言，並設定為UI的初始顯示
        string currentLangCode = LanguageManager.Instance.CurrentLanguage;
        stagedLanguageIndex = languageOptions.FindIndex(lang => lang.languageCode == currentLangCode);

        // 如果找不到 (例如 PlayerPrefs 存了舊的語言)，就預設為第一個
        if (stagedLanguageIndex == -1)
        {
            stagedLanguageIndex = 0;
        }

        // 3. 更新UI上的文字
        UpdateLanguageDisplay();
    }

    /// <summary>
    /// 更新顯示當前預選語言的文字
    /// </summary>
    private void UpdateLanguageDisplay()
    {
        if (currentLanguageText != null && languageOptions.Count > 0)
        {
            currentLanguageText.text = languageOptions[stagedLanguageIndex].displayName;
        }
    }

    /// <summary>
    /// 當點擊「下一個語系」按鈕時
    /// </summary>
    public void OnNextLanguage()
    {
        stagedLanguageIndex++;
        if (stagedLanguageIndex >= languageOptions.Count)
        {
            stagedLanguageIndex = 0; // 循環到開頭
        }
        UpdateLanguageDisplay();
    }

    /// <summary>
    /// 當點擊「上一個語系」按鈕時
    /// </summary>
    public void OnPreviousLanguage()
    {
        stagedLanguageIndex--;
        if (stagedLanguageIndex < 0)
        {
            stagedLanguageIndex = languageOptions.Count - 1; // 循環到結尾
        }
        UpdateLanguageDisplay();
    }

    /// <summary>
    /// 當點擊「確認」按鈕時，正式套用設定
    /// </summary>
    public void OnConfirmLanguage()
    {
        // 獲取預選的語言代碼
        string selectedLanguageCode = languageOptions[stagedLanguageIndex].languageCode;

        // 呼叫 LanguageManager 來執行真正的切換與儲存
        LanguageManager.Instance.SetLanguage(selectedLanguageCode);

        Debug.Log($"語系設定已確認: {selectedLanguageCode}");
        // 在這裡可以加上一些UI回饋，例如顯示 "設定已儲存" 或關閉設定視窗
    }
}