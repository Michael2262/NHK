using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
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
    public void TriggerAction(float duration, Sprite sprite, string textKey, UnityAction onComplete, UnityAction onStarted)
    {
        StopCurrent();
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
        UnityAction onStarted = null)
    {
        StopCurrent();
        _runningCoroutine = StartCoroutine(ProcessActionWithResult(
            duration,
            sprite,
            textKey,
            isSuccess,
            successTextKey,
            failureTextKey,
            resultHoldSeconds,
            onStarted,
            onResult));
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
        UnityAction<bool> onResult)
    {
        SetupActionView(sprite, textKey);
        onStarted?.Invoke();

        yield return RunProgress(duration);

        string resultKey = isSuccess ? successTextKey : failureTextKey;
        ShowResultText(resultKey);

        if (resultHoldSeconds > 0f)
        {
            yield return new WaitForSeconds(resultHoldSeconds);
        }

        HideOverlay();
        onResult?.Invoke(isSuccess);
        _runningCoroutine = null;
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
            localization.UpdateText();
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
