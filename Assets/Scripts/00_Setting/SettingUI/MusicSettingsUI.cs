using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 專門處理背景音樂(BGM)設定相關的 UI 互動邏輯
/// </summary>
public class MusicSettingsUI : MonoBehaviour
{
    [Header("UI 元件")]
    [Tooltip("將場景中的 BGM 音量控制 Slider 元件拖曳到這裡")]
    [SerializeField] private Slider musicVolumeSlider;

    void Start()
    {
        // 檢查 musicVolumeSlider 是否已在 Inspector 中設定，避免錯誤
        if (musicVolumeSlider == null)
        {
            Debug.LogError("MusicSettingsUI: Music Volume Slider尚未設定！");
            return;
        }

        // 1. 讀取儲存的 BGM 音量值，若無則預設為 0.4f
        //    *** 注意：這裡使用了一個新的 Key "BgmVolume" ***
        float savedVolume = PlayerPrefs.GetFloat("BgmVolume", 0.4f);

        // 2. 將讀取到的值設定給 Slider，讓拉桿顯示在正確的位置
        musicVolumeSlider.value = savedVolume;

        // 3. 為 Slider 添加監聽事件，當拉桿數值改變時，呼叫 OnMusicVolumeSliderChanged 方法
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeSliderChanged);
    }

    /// <summary>
    /// 當 BGM 音量拉桿的數值被改變時呼叫此方法
    /// </summary>
    /// <param name="value">Slider 自動傳入的目前數值 (0.0 ~ 1.0)</param>
    private void OnMusicVolumeSliderChanged(float value)
    {
        // 1. 呼叫 MusicManager 來即時更新遊戲 BGM 音量
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetVolume(value);
        }

        // 2. 將新的數值儲存到 PlayerPrefs
        //    *** 注意：儲存到獨立的 Key "BgmVolume" ***
        PlayerPrefs.SetFloat("BgmVolume", value);
    }
}