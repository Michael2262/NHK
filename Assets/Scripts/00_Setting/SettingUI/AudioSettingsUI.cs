using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 專門處理音量設定相關的 UI 互動邏輯
/// </summary>
public class AudioSettingsUI : MonoBehaviour
{
    [Header("UI 元件")]
    [Tooltip("將場景中的音量控制 Slider 元件拖曳到這裡")]
    [SerializeField] private Slider volumeSlider;

    void Start()
    {
        // 檢查 volumeSlider 是否已在 Inspector 中設定，避免錯誤
        if (volumeSlider == null)
        {
            Debug.LogError("AudioSettingsUI: Volume Slider尚未設定！");
            return;
        }

        // 1. 讀取儲存的音量值，若無則預設為 1.0f
        float savedVolume = PlayerPrefs.GetFloat("SfxVolume", 0.4f);

        // 2. 將讀取到的值設定給 Slider，讓拉桿顯示在正確的位置
        volumeSlider.value = savedVolume;

        // 3. 為 Slider 添加監聽事件，當拉桿數值改變時，呼叫 OnVolumeSliderChanged 方法
        volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
    }

    /// <summary>
    /// 當音量拉桿的數值被改變時呼叫此方法
    /// </summary>
    /// <param name="value">Slider 自動傳入的目前數值 (0.0 ~ 1.0)</param>
    private void OnVolumeSliderChanged(float value)
    {
        // 1. 呼叫 AudioManager 來即時更新遊戲音量
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVolume(value);
        }

        // 2. 將新的數值儲存到 PlayerPrefs
        PlayerPrefs.SetFloat("SfxVolume", value);
    }
}