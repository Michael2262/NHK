using UnityEngine;
using System.Collections;

public class WarningHintController : MonoBehaviour
{
    public static WarningHintController Instance;

    public enum Position { Left, Center, Right }
    public enum Intensity { Normal, Medium, Fast }

    [Header("UI Components")]
    public RectTransform warningHint;
    public CanvasGroup canvasGroup;

    [Header("Position Settings")]
    public float sideXValue = 400f;

    [Header("Duration Settings (seconds)")]
    public float durationNormal = 2f;
    public float durationMedium = 4f;
    public float durationFast = 6f;

    [Header("Blink Interval Settings (seconds)")]
    public float blinkNormal = 0.5f;
    public float blinkMedium = 0.25f;
    public float blinkFast = 0.1f;

    private Coroutine _activeCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        canvasGroup.alpha = 0f;
    }

    public void ShowWarning(Position position, Intensity durationLevel, Intensity blinkLevel)
    {
        if (_activeCoroutine != null)
            StopCoroutine(_activeCoroutine);

        // Set position
        Vector2 pos = warningHint.anchoredPosition;
        pos.x = position switch
        {
            Position.Left => -sideXValue,
            Position.Right => sideXValue,
            _ => 0f
        };
        warningHint.anchoredPosition = pos;

        float duration = durationLevel switch
        {
            Intensity.Normal => durationNormal,
            Intensity.Medium => durationMedium,
            _ => durationFast
        };

        float blinkInterval = blinkLevel switch
        {
            Intensity.Normal => blinkNormal,
            Intensity.Medium => blinkMedium,
            _ => blinkFast
        };

        _activeCoroutine = StartCoroutine(BlinkRoutine(duration, blinkInterval));
    }

    public void HideWarning()
    {
        if (_activeCoroutine != null)
        {
            StopCoroutine(_activeCoroutine);
            _activeCoroutine = null;
        }
        canvasGroup.alpha = 0f;
    }

    private IEnumerator BlinkRoutine(float duration, float blinkInterval)
    {
        float elapsed = 0f;
        bool visible = true;

        while (elapsed < duration)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            visible = !visible;

            float wait = Mathf.Min(blinkInterval, duration - elapsed);
            yield return new WaitForSeconds(wait);
            elapsed += blinkInterval;
        }

        canvasGroup.alpha = 0f;
        _activeCoroutine = null;
    }
}