using UnityEngine;
using UnityEngine.UI;
//using TMPro; // 如果你用的是 TextMeshPro
using System.Collections.Generic;

/// <summary>
/// 處理解析度設定的 UI 互動邏輯 (左右切換版本)
/// </summary>
public class ResolutionSettingsUI : MonoBehaviour
{
    [Header("UI 元件")]
    [SerializeField] private Button nextResolutionButton;
    [SerializeField] private Button previousResolutionButton;
    [SerializeField] private Text currentResolutionText; // 將類型從 TextMeshProUGUI 改為 Text
    [SerializeField] private Toggle fullscreenToggle;
    // 確認按鈕將由更高層的 SettingsMenuController 管理，所以這裡不需要它的引用

    // 儲存從 Manager 獲取的解析度列表
    private List<CustomResolution> resolutionOptions; // 將類型從 Resolution 改為 CustomResolution

    // 玩家在UI上預選的設定，尚未確認 (Staged/Pending)
    private int stagedResolutionIndex;
    private bool stagedIsFullscreen;

    void Start()
    {
        if (ResolutionManager.Instance == null)
        {
            Debug.LogError("ResolutionSettingsUI: 場景中找不到 ResolutionManager!");
            return;
        }

        InitializeUI();

        // 綁定按鈕和 Toggle 的事件
        nextResolutionButton.onClick.AddListener(OnNextResolution);
        previousResolutionButton.onClick.AddListener(OnPreviousResolution);
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);
    }

    /// <summary>
    /// 初始化UI顯示
    /// </summary>
    private void InitializeUI()
    {
        // 1. 從 Manager 獲取資料
        resolutionOptions = ResolutionManager.Instance.AvailableResolutions;
        if (resolutionOptions == null || resolutionOptions.Count == 0) return;

        // 2. 獲取當前設定，並以此初始化 "預選設定"
        stagedResolutionIndex = ResolutionManager.Instance.CurrentResolutionIndex;
        stagedIsFullscreen = ResolutionManager.Instance.IsFullscreen;

        // 3. 更新UI的初始顯示狀態
        fullscreenToggle.isOn = stagedIsFullscreen;
        UpdateResolutionDisplay();
    }

    /// <summary>
    /// 更新顯示當前預選解析度的文字
    /// </summary>
    private void UpdateResolutionDisplay()
    {
        if (currentResolutionText != null && resolutionOptions.Count > 0)
        {
            // 直接使用我們在 CustomResolution 中定義的 ToString() 方法
            currentResolutionText.text = resolutionOptions[stagedResolutionIndex].ToString();
        }
    }

    /// <summary>
    /// 當點擊「下一個解析度」按鈕時
    /// </summary>
    public void OnNextResolution()
    {
        stagedResolutionIndex++;
        if (stagedResolutionIndex >= resolutionOptions.Count)
        {
            stagedResolutionIndex = 0; // 循環到開頭
        }
        UpdateResolutionDisplay();
    }

    /// <summary>
    /// 當點擊「上一個解析度」按鈕時
    /// </summary>
    public void OnPreviousResolution()
    {
        stagedResolutionIndex--;
        if (stagedResolutionIndex < 0)
        {
            stagedResolutionIndex = resolutionOptions.Count - 1; // 循環到結尾
        }
        UpdateResolutionDisplay();
    }

    /// <summary>
    /// 當全螢幕 Toggle 狀態改變時
    /// </summary>
    private void OnFullscreenToggleChanged(bool value)
    {
        stagedIsFullscreen = value;
    }



    /// <summary>
    /// 【重要】提供給外部呼叫的確認方法
    /// </summary>
    public void OnConfirmSettings()
    {
        // 呼叫 ResolutionManager 來執行真正的切換與儲存
        ResolutionManager.Instance.ApplySettings(stagedResolutionIndex, stagedIsFullscreen);
        Debug.Log("解析度設定已確認並套用！");
    }
}