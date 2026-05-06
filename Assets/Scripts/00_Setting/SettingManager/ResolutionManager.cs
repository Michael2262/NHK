using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public struct CustomResolution
{
    public int width;
    public int height;

    public override string ToString() => $"{width} x {height}";
}

public class ResolutionManager : MonoBehaviour
{
    public static ResolutionManager Instance { get; private set; }

    [Header("手動設定的解析度選項")]
    [Tooltip("在這裡手動新增你想提供給玩家的解析度選項")]
    [SerializeField] private List<CustomResolution> selectableResolutions;

    [Header("安全機制")]
    [Tooltip("套用解析度後,在此秒數內若未確認,將自動還原")]
    [SerializeField] private float revertTimeoutSeconds = 15f;

    public List<CustomResolution> AvailableResolutions => selectableResolutions;
    public int CurrentResolutionIndex { get; private set; }
    public bool IsFullscreen { get; private set; }

    // 用於還原的暫存
    private int _previousResolutionIndex;
    private bool _previousIsFullscreen;
    private Coroutine _revertCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // ★ 修正:先讀存檔,再做合法性驗證,最後實際套用
            LoadSettings();
            ApplyToScreen(CurrentResolutionIndex, IsFullscreen);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 套用解析度設定 (含安全驗證)
    /// </summary>
    public void ApplySettings(int resolutionIndex, bool isFullscreen)
    {
        // ★ 防呆 1:索引合法性
        if (selectableResolutions == null || selectableResolutions.Count == 0)
        {
            Debug.LogError("[ResolutionManager] 解析度列表為空!");
            return;
        }
        if (resolutionIndex < 0 || resolutionIndex >= selectableResolutions.Count)
        {
            Debug.LogError($"[ResolutionManager] 無效的解析度索引: {resolutionIndex}");
            return;
        }

        CustomResolution resolution = selectableResolutions[resolutionIndex];

        // ★ 防呆 2:視窗模式下,不允許超過螢幕原生尺寸
        if (!isFullscreen)
        {
            int maxW = Display.main.systemWidth;
            int maxH = Display.main.systemHeight;
            if (resolution.width > maxW || resolution.height > maxH)
            {
                Debug.LogWarning(
                    $"[ResolutionManager] 視窗模式下,解析度 {resolution} 超過螢幕原生 {maxW}x{maxH},將自動改為全螢幕");
                isFullscreen = true;
            }
        }

        // 暫存舊設定,以便在失敗時還原
        _previousResolutionIndex = CurrentResolutionIndex;
        _previousIsFullscreen = IsFullscreen;

        ApplyToScreen(resolutionIndex, isFullscreen);
        SaveSettings();

        Debug.Log($"[ResolutionManager] 解析度已套用: {resolution}, 全螢幕: {isFullscreen}");
    }

    /// <summary>
    /// 實際呼叫 Screen API 並更新內部狀態
    /// </summary>
    private void ApplyToScreen(int resolutionIndex, bool isFullscreen)
    {
        CustomResolution resolution = selectableResolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, isFullscreen);
        CurrentResolutionIndex = resolutionIndex;
        IsFullscreen = isFullscreen;
    }

    /// <summary>
    /// 啟動「未確認就還原」的計時(可選功能)
    /// 在套用新設定後呼叫,若玩家在時限內呼叫 ConfirmSettings() 就保留
    /// </summary>
    public void StartRevertTimer()
    {
        if (_revertCoroutine != null) StopCoroutine(_revertCoroutine);
        _revertCoroutine = StartCoroutine(RevertCountdown());
    }

    /// <summary>玩家確認保留新設定</summary>
    public void ConfirmSettings()
    {
        if (_revertCoroutine != null)
        {
            StopCoroutine(_revertCoroutine);
            _revertCoroutine = null;
        }
    }

    private IEnumerator RevertCountdown()
    {
        yield return new WaitForSecondsRealtime(revertTimeoutSeconds);
        Debug.LogWarning("[ResolutionManager] 玩家未確認,還原為先前的解析度設定");
        ApplyToScreen(_previousResolutionIndex, _previousIsFullscreen);
        SaveSettings();
        _revertCoroutine = null;
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt("ResolutionPreference", CurrentResolutionIndex);
        PlayerPrefs.SetInt("FullscreenPreference", IsFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        // 預設值:全螢幕 + 找一個和當前螢幕最接近的索引
        int defaultIndex = FindIndexOfCurrentResolution();
        if (defaultIndex < 0) defaultIndex = 0;

        int loadedIndex = PlayerPrefs.GetInt("ResolutionPreference", defaultIndex);

        // ★ 防呆:驗證讀回來的 index 是否仍在合法範圍
        if (loadedIndex < 0 || loadedIndex >= selectableResolutions.Count)
        {
            Debug.LogWarning($"[ResolutionManager] 存檔的解析度索引 {loadedIndex} 已超出範圍,改用預設值 {defaultIndex}");
            loadedIndex = defaultIndex;
        }

        CurrentResolutionIndex = loadedIndex;
        IsFullscreen = PlayerPrefs.GetInt("FullscreenPreference", Screen.fullScreen ? 1 : 0) == 1;
    }

    private int FindIndexOfCurrentResolution()
    {
        if (selectableResolutions == null || selectableResolutions.Count == 0) return -1;

        for (int i = 0; i < selectableResolutions.Count; i++)
        {
            if (selectableResolutions[i].width == Screen.width &&
                selectableResolutions[i].height == Screen.height)
            {
                return i;
            }
        }
        return -1;
    }
}