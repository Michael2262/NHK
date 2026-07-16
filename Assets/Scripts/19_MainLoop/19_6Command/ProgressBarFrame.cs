using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelCrushers.DialogueSystem;

/// <summary>
/// 共用時間條外框。
///
/// 把原本散在 CommandButtonGroup 上的「時間條 UI、結果文字、結果音效」設定
/// 集中到這一個腳本，獨立設置好之後，指定給場景中的多個 CommandButtonGroup 共用。
/// 這樣同場景多個群組（例如換頁產生的多組按鈕）只需要維護一份時間條設定。
///
/// 使用方式：
///   1. 場景中放一個 GameObject（例如 ProgressBarFrame）掛此腳本。
///   2. 指定 progressBarImage（填充式 Image）或 progressBarSlider（Slider）、
///      結果文字 TMP 與音效 Key。
///   3. 把此物件拖進各 CommandButtonGroup 的 progressBarFrame 欄位。
///
/// 注意：此物件請放在頁面容器（CommandButtonPage）外面，避免被換頁藏掉。
/// </summary>
public class ProgressBarFrame : MonoBehaviour
{
    [Header("時間條 UI")]
    [Tooltip("填充式 Image（fillAmount 0→1）。與 Slider 二選一。")]
    public Image progressBarImage;

    [Tooltip("Slider（value 0→1）。與 Image 二選一。")]
    public Slider progressBarSlider;

    [Tooltip("啟用後時間條從中間同時往左右展開（使用 RectTransform 寬度）。progressBarImage 的 Pivot 請設為 (0.5, 0.5)。")]
    public bool fillFromCenter = false;

    [Tooltip("時間條的父物件。跑條時自動顯示，結束後自動隱藏。若未指定則不控制顯隱。")]
    public GameObject progressBarRoot;

    [Header("結果文字")]
    [Tooltip("顯示成功或失敗文字的 TMP 元件。")]
    public TMP_Text resultText;

    [Tooltip("成功時的多語系 Key。")]
    public string successLocalizationKey = "System.Succese";

    [Tooltip("失敗時的多語系 Key。")]
    public string failLocalizationKey = "System.Failed";

    [Header("結果音效")]
    [Tooltip("成功時播放的音效 Key（對應 AudioManager 設定）。留空則不播放。")]
    public string successSoundKey = "action_success";

    [Tooltip("失敗時播放的音效 Key（對應 AudioManager 設定）。留空則不播放。")]
    public string failureSoundKey = "action_failure";

    private void Awake()
    {
        SetProgressValue(0f);
        SetVisible(false);
        HideResultText();
    }

    // ─────────────────────────────────────────────
    // 時間條
    // ─────────────────────────────────────────────

    /// <summary>
    /// 設定時間條進度（0~1）。
    /// </summary>
    public void SetProgressValue(float value)
    {
        if (progressBarImage != null)
        {
            if (fillFromCenter)
            {
                // 從中間往兩邊展開：X 軸縮放 0→1，Pivot (0.5, 0.5) 確保從中心擴張
                var scale = progressBarImage.rectTransform.localScale;
                scale.x = value;
                progressBarImage.rectTransform.localScale = scale;
            }
            else
            {
                progressBarImage.fillAmount = value;
            }
        }

        if (progressBarSlider != null)
            progressBarSlider.value = value;
    }

    /// <summary>
    /// 顯示 / 隱藏時間條。
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (progressBarRoot != null)
            progressBarRoot.SetActive(visible);
    }

    // ─────────────────────────────────────────────
    // 結果文字
    // ─────────────────────────────────────────────

    /// <summary>
    /// 依成功 / 失敗顯示對應的多語系結果文字。
    /// </summary>
    public void ShowResultText(bool success)
    {
        if (resultText == null) return;

        string key = success ? successLocalizationKey : failLocalizationKey;
        string localized = DialogueManager.GetLocalizedText(key);

        // 查不到就直接顯示 key
        if (string.IsNullOrEmpty(localized))
            localized = key;

        resultText.text = localized;
        resultText.gameObject.SetActive(true);
    }

    /// <summary>
    /// 隱藏結果文字。
    /// </summary>
    public void HideResultText()
    {
        if (resultText == null) return;
        resultText.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // 結果音效
    // ─────────────────────────────────────────────

    /// <summary>
    /// 依成功 / 失敗播放對應音效。透過 AudioManager 單例播放，Key 留空則不播放。
    /// </summary>
    public void PlayResultSound(bool success)
    {
        string key = success ? successSoundKey : failureSoundKey;
        if (string.IsNullOrEmpty(key)) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound(key);
        else
            Debug.LogWarning("[ProgressBarFrame] 找不到 AudioManager.Instance，無法播放結果音效。");
    }
}
