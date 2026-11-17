using UnityEngine;

/// <summary>掛在單條數值條的視覺根物件，由該條的 Visualizer 回報演出進度。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class SliderVisibilityBehaviour : MonoBehaviour
{
    public enum VisibilityMode
    {
        [InspectorName("常駐")] AlwaysVisible,
        [InspectorName("關閉")] Hidden,
        [InspectorName("變動時顯示")] OnChange
    }

    [InspectorName("顯示模式")]
    [SerializeField] private VisibilityMode mode = VisibilityMode.AlwaysVisible;
    [InspectorName("演完後停留秒數")]
    [Min(0f)] [SerializeField] private float holdDuration = 1f;
    [InspectorName("淡出秒數")]
    [Min(0f)] [SerializeField] private float fadeDuration = 0.25f;

    public bool ShowsOnChange => mode == VisibilityMode.OnChange;

    private enum Phase { Idle, Playing, Finished }
    private Phase phase;
    private float finishedAt;
    private VisibilityMode appliedMode;
    private CanvasGroup group;
    private float originalAlpha;
    private bool originalInteractable;
    private bool originalBlocksRaycasts;

    private void OnEnable()
    {
        group = GetComponent<CanvasGroup>();
        originalAlpha = group.alpha;
        originalInteractable = group.interactable;
        originalBlocksRaycasts = group.blocksRaycasts;
        RefreshVisibility();
    }

    private void OnDisable()
    {
        group.alpha = originalAlpha;
        group.interactable = originalInteractable;
        group.blocksRaycasts = originalBlocksRaycasts;
        group = null;
    }

    public void SetMode(VisibilityMode value)
    {
        mode = value;
        RefreshVisibility();
    }

    public void NotifyChangeStarted()
    {
        phase = Phase.Playing;
        RefreshVisibility();
    }

    public void NotifyChangeFinished()
    {
        if (phase != Phase.Playing) return;
        phase = Phase.Finished;
        finishedAt = Time.unscaledTime;
        RefreshVisibility();
    }

    public void ResetPresentation()
    {
        phase = Phase.Idle;
        RefreshVisibility();
    }

    private void Update()
    {
        if (appliedMode != mode || (mode == VisibilityMode.OnChange && phase == Phase.Finished))
            RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        if (group == null) return;
        appliedMode = mode;
        float alpha = 0f;
        if (mode == VisibilityMode.AlwaysVisible || (mode == VisibilityMode.OnChange && phase == Phase.Playing))
            alpha = 1f;
        else if (mode == VisibilityMode.OnChange && phase == Phase.Finished)
        {
            float fadeElapsed = Time.unscaledTime - finishedAt - holdDuration;
            if (fadeElapsed < 0f) alpha = 1f;
            else if (fadeDuration > 0f) alpha = 1f - Mathf.Clamp01(fadeElapsed / fadeDuration);
            if (alpha <= 0f) phase = Phase.Idle;
        }

        group.alpha = alpha;
        group.interactable = alpha > 0f && originalInteractable;
        group.blocksRaycasts = alpha > 0f && originalBlocksRaycasts;
    }
}
