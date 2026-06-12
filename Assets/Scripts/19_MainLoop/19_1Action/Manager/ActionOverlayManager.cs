using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using PixelCrushers.Wrappers;

/// <summary>
/// 行動遮罩演出管理器。
/// NHK 用途：
/// 1. 白天 1 生活行動：跑條結束後直接 onComplete。
/// 2. 白天 2 復歸行動：跑條結束後顯示成功 / 失敗，等待數秒後回傳不同結果。
/// </summary>
public class ActionOverlayManager : MonoBehaviour
{
    public static ActionOverlayManager Instance;

    [Header("UI Components")]
    public GameObject overlayRoot;
    public Image actionImage;
    public TextMeshProUGUI actionText;
    public LocalizeUI localizeUI;
    public Slider progressSlider;

    [Header("Position Settings")]
    [Tooltip("實際要移動的面板（通常是 overlayRoot 底下的 Panel）。未指定時不做位置移動。")]
    public RectTransform overlayPanel;

    [Tooltip("各出現位置的標記。新增位置時在此加一筆即可。")]
    public List<OverlayPositionEntry> positionMarkers = new List<OverlayPositionEntry>();

    [System.Serializable]
    public class OverlayPositionEntry
    {
        public ActionOverlayPosition position;
        [Tooltip("空的 RectTransform 標記，面板顯示時會對齊它的 anchor / pivot / anchoredPosition。")]
        public RectTransform marker;
    }

    [Header("Result Display")]
    [Tooltip("是否在結果顯示階段隱藏進度條。")]
    public bool hideSliderWhenShowingResult = true;

    [Tooltip("若指定，結果文字會顯示到此欄位；未指定時會沿用 actionText。")]
    public TextMeshProUGUI resultText;

    [Tooltip("若 resultText 有自己的 LocalizeUI，填這裡；未填時會沿用 localizeUI。")]
    public LocalizeUI resultLocalizeUI;

    private Coroutine _runningCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }
    }

    /// <summary>
    /// 舊版相容：只跑行動條，結束後執行 onComplete。
    /// </summary>
    public void TriggerAction(float duration, Sprite sprite, string textKey, UnityAction onComplete)
    {
        TriggerAction(duration, sprite, textKey, onComplete, null);
    }

    /// <summary>
    /// 只跑行動條，結束後執行 onComplete。
    /// </summary>
    public void TriggerAction(float duration, Sprite sprite, string textKey, UnityAction onComplete, UnityAction onStarted,
        ActionOverlayPosition position = ActionOverlayPosition.Right)
    {
        StopCurrent();
        ApplyOverlayPosition(position);
        _runningCoroutine = StartCoroutine(ProcessActionOnly(duration, sprite, textKey, onStarted, onComplete));
    }

    /// <summary>
    /// 跑行動條，結束後顯示成功/失敗文字，等待 resultHoldSeconds 後回傳結果。
    /// 抽選/判定結果在跑條開始前就可以決定；演出結束後才呼叫 callback。
    /// </summary>
    public void TriggerActionWithResult(
        float duration,
        Sprite sprite,
        string textKey,
        bool isSuccess,
        string successTextKey,
        string failureTextKey,
        float resultHoldSeconds,
        UnityAction<bool> onResult,
        UnityAction onStarted = null,
        string successSoundKey = "action_success",
        string failureSoundKey = "action_failure",
        ActionOverlayPosition position = ActionOverlayPosition.Right)
    {
        StopCurrent();
        ApplyOverlayPosition(position);
        _runningCoroutine = StartCoroutine(ProcessActionWithResult(
            duration,
            sprite,
            textKey,
            isSuccess,
            successTextKey,
            failureTextKey,
            resultHoldSeconds,
            onStarted,
            onResult,
            successSoundKey,
            failureSoundKey));
    }

    public void StopCurrent()
    {
        if (_runningCoroutine != null)
        {
            StopCoroutine(_runningCoroutine);
            _runningCoroutine = null;
        }
    }

    private IEnumerator ProcessActionOnly(float duration, Sprite sprite, string textKey, UnityAction onStarted, UnityAction onComplete)
    {
        SetupActionView(sprite, textKey);
        onStarted?.Invoke();

        yield return RunProgress(duration);

        HideOverlay();
        onComplete?.Invoke();
        _runningCoroutine = null;
    }

    private IEnumerator ProcessActionWithResult(
        float duration,
        Sprite sprite,
        string textKey,
        bool isSuccess,
        string successTextKey,
        string failureTextKey,
        float resultHoldSeconds,
        UnityAction onStarted,
        UnityAction<bool> onResult,
        string successSoundKey,
        string failureSoundKey)
    {
        SetupActionView(sprite, textKey);
        onStarted?.Invoke();

        yield return RunProgress(duration);

        string resultKey = isSuccess ? successTextKey : failureTextKey;
        ShowResultText(resultKey);
        PlayResultSound(isSuccess ? successSoundKey : failureSoundKey);

        if (resultHoldSeconds > 0f)
        {
            yield return new WaitForSeconds(resultHoldSeconds);
        }

        HideOverlay();
        onResult?.Invoke(isSuccess);
        _runningCoroutine = null;
    }

    /// <summary>
    /// 把面板對齊指定位置的 marker。任一參照沒填則不移動（維持場景中原本的位置）。
    /// </summary>
    private void ApplyOverlayPosition(ActionOverlayPosition position)
    {
        if (overlayPanel == null) return;

        OverlayPositionEntry entry = positionMarkers.Find(e => e != null && e.position == position);
        if (entry == null || entry.marker == null)
        {
            Debug.LogWarning($"ActionOverlayManager：positionMarkers 中找不到位置 {position} 的標記，面板位置不變。", this);
            return;
        }

        RectTransform marker = entry.marker;
        overlayPanel.anchorMin = marker.anchorMin;
        overlayPanel.anchorMax = marker.anchorMax;
        overlayPanel.pivot = marker.pivot;
        overlayPanel.anchoredPosition = marker.anchoredPosition;
    }

    private void SetupActionView(Sprite sprite, string textKey)
    {
        if (overlayRoot != null)
        {
            overlayRoot.SetActive(true);
        }

        if (actionImage != null)
        {
            actionImage.sprite = sprite;
            actionImage.enabled = sprite != null;
        }

        SetLocalizedText(actionText, localizeUI, textKey);

        if (resultText != null && resultText != actionText)
        {
            resultText.gameObject.SetActive(false);
        }

        if (progressSlider != null)
        {
            progressSlider.gameObject.SetActive(true);
            progressSlider.value = 0f;
        }
    }

    private IEnumerator RunProgress(float duration)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            if (progressSlider != null)
            {
                progressSlider.value = Mathf.Clamp01(elapsed / safeDuration);
            }
            yield return null;
        }

        if (progressSlider != null)
        {
            progressSlider.value = 1f;
        }
    }

    private void ShowResultText(string resultKey)
    {
        if (hideSliderWhenShowingResult && progressSlider != null)
        {
            progressSlider.gameObject.SetActive(false);
        }

        TextMeshProUGUI targetText = resultText != null ? resultText : actionText;
        LocalizeUI targetLocalize = resultText != null && resultLocalizeUI != null ? resultLocalizeUI : localizeUI;

        if (targetText != null)
        {
            targetText.gameObject.SetActive(true);
        }

        SetLocalizedText(targetText, targetLocalize, resultKey);
    }

    private void SetLocalizedText(TextMeshProUGUI textComponent, LocalizeUI localization, string keyOrText)
    {
        if (textComponent == null) return;

        textComponent.text = keyOrText;

        if (localization != null)
        {
            // LocalizeUI 會鎖定第一次抓到的 fieldName，之後 UpdateText() 都查同一個 key，
            // 必須先改 fieldName 再刷新，否則永遠顯示原始 key 字串。
            localization.fieldName = keyOrText;
            localization.UpdateText();
        }
    }

    /// <summary>
    /// 在結果顯示時播放對應音效（成功/失敗）。透過 AudioManager 單例播放。
    /// </summary>
    private void PlayResultSound(string soundKey)
    {
        if (string.IsNullOrEmpty(soundKey)) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(soundKey);
        }
        else
        {
            Debug.LogWarning("AudioManager.Instance 未找到，無法播放行動結果音效！");
        }
    }

    private void HideOverlay()
    {
        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }
    }
}
