using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

/// <summary>
/// 轉場淡出入控制器。
/// 支援：
/// 1. 動態顏色 - 根據當前時段 (Phase) 決定 Fade 顏色
/// 2. 隔天演出模式 - 較長的淡出入時間，搭配壓幕 UI
/// </summary>
[DefaultExecutionOrder(-900)]
public class TransitionFader : MonoBehaviour
{
    public static TransitionFader Instance { get; private set; }

    [Header("基本設定")]
    [Tooltip("一般轉場的淡出入持續時間 (秒)")]
    [SerializeField] private float fadeDuration = 0.15f;

    [Tooltip("預設的 Fade 顏色 (當無法取得 TimeConfig 時使用)")]
    [SerializeField] private Color defaultFadeColor = Color.white;

    [Header("依賴引用 (可選)")]
    [Tooltip("時間配置檔，用於獲取各階段的 Fade 顏色。若未指定，會嘗試從 GameStatusService 取得。")]
    [SerializeField] private TimeConfig timeConfig;

    // 內部元件
    private Image _fadeImage;
    private Canvas _canvas;

    // 當前使用的顏色 (保持 Alpha 分離管理)
    private Color _currentBaseColor;

    // ============================================================
    // 自動導引與單例初始化
    // ============================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoBootstrap()
    {
        if (Instance == null)
        {
            var go = new GameObject("TransitionFader_Auto");
            go.AddComponent<TransitionFader>();
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 以程式碼自動創建及設定所有需要的 UI 元件
    /// </summary>
    private void Initialize()
    {
        // 1. 創建及設定 Canvas
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 999;

        // 2. 創建掛載 Image 的子物件
        var imageObject = new GameObject("FadeImage");
        imageObject.transform.SetParent(transform, false);
        _fadeImage = imageObject.AddComponent<Image>();
        _fadeImage.raycastTarget = false;

        // 3. 讓 Image 充滿覆蓋滿全螢幕
        var rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;

        // 4. 初始狀態：透明
        _currentBaseColor = defaultFadeColor;
        SetAlpha(0);
    }

    // ============================================================
    // 【核心 API】由 SceneController 呼叫
    // ============================================================

    /// <summary>
    /// 一般轉場：淡出至指定顏色 (根據當前 Phase 決定顏色)
    /// </summary>
    public IEnumerator FadeToColor()
    {
        UpdateFadeColorFromCurrentPhase();
        yield return Fade(0f, 1f, fadeDuration);
    }

    /// <summary>
    /// 【新增】一般轉場：淡出至指定 Phase 的顏色
    /// </summary>
    /// <param name="phaseIndex">Phase 索引 (0=白天, 1=黃昏, 2=晚上, 3=深夜)，傳入 -1 則使用當前 Phase</param>
    public IEnumerator FadeToColor(int phaseIndex)
    {
        if (phaseIndex < 0)
        {
            // -1 或負數：使用當前 Phase
            UpdateFadeColorFromCurrentPhase();
        }
        else
        {
            // 指定 Phase
            UpdateFadeColorFromPhase(phaseIndex);
        }
        yield return Fade(0f, 1f, fadeDuration);
    }

    /// <summary>
    /// 一般轉場：從指定顏色淡入至透明
    /// </summary>
    public IEnumerator FadeToClear()
    {
        yield return Fade(1f, 0f, fadeDuration);
    }

    /// <summary>
    /// 【新增】指定 Phase 顏色 + 自訂持續時間的淡出
    /// </summary>
    /// <param name="phaseIndex">Phase 索引，傳入 -1 則使用當前 Phase</param>
    /// <param name="duration">淡出持續時間 (秒)</param>
    public IEnumerator FadeToColor(int phaseIndex, float duration)
    {
        if (phaseIndex < 0)
            UpdateFadeColorFromCurrentPhase();
        else
            UpdateFadeColorFromPhase(phaseIndex);

        yield return Fade(0f, 1f, duration);
    }

    /// <summary>
    /// 手動指定顏色的淡出
    /// </summary>
    public IEnumerator FadeToColor(Color color, float duration)
    {
        SetBaseColor(color);
        yield return Fade(0f, 1f, duration);
    }

    /// <summary>
    /// 手動指定時間的淡入
    /// </summary>
    public IEnumerator FadeToClear(float duration)
    {
        yield return Fade(1f, 0f, duration);
    }

    // ============================================================
    // 內部方法
    // ============================================================

    /// <summary>
    /// 根據當前遊戲時段更新 Fade 顏色
    /// </summary>
    private void UpdateFadeColorFromCurrentPhase()
    {
        TimeConfig cfg = GetTimeConfig();

        if (cfg != null && GameStatusService.Instance != null)
        {
            int currentPhase = GameStatusService.Instance.Time.CurrentPhaseIndex;
            Color phaseColor = cfg.GetPhaseFadeColor(currentPhase);
            SetBaseColor(phaseColor);

            Debug.Log($"[TransitionFader] Phase {currentPhase} → Fade 顏色: {phaseColor}");
        }
        else
        {
            SetBaseColor(defaultFadeColor);
        }
    }

    /// <summary>
    /// 【新增】根據指定的 Phase 索引更新 Fade 顏色
    /// </summary>
    /// <param name="phaseIndex">Phase 索引 (0=白天, 1=黃昏, 2=晚上, 3=深夜)</param>
    private void UpdateFadeColorFromPhase(int phaseIndex)
    {
        TimeConfig cfg = GetTimeConfig();

        if (cfg != null)
        {
            Color phaseColor = cfg.GetPhaseFadeColor(phaseIndex);
            SetBaseColor(phaseColor);

            Debug.Log($"[TransitionFader] 指定 Phase {phaseIndex} → Fade 顏色: {phaseColor}");
        }
        else
        {
            SetBaseColor(defaultFadeColor);
        }
    }

    /// <summary>
    /// 嘗試獲取 TimeConfig
    /// </summary>
    private TimeConfig GetTimeConfig()
    {
        // 優先使用 Inspector 指定的
        if (timeConfig != null) return timeConfig;

        // 從 GameStatusService 取得
        return GameStatusService.Instance?.TimeConfig;
    }

    /// <summary>
    /// 設定基礎顏色 (不影響 Alpha)
    /// </summary>
    private void SetBaseColor(Color color)
    {
        _currentBaseColor = color;
        // 保持當前 Alpha，只更新 RGB
        var currentColor = _fadeImage.color;
        _fadeImage.color = new Color(color.r, color.g, color.b, currentColor.a);
    }

    /// <summary>
    /// 執行淡入/淡出動畫
    /// </summary>
    private IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            SetAlpha(Mathf.Lerp(fromAlpha, toAlpha, progress));
            yield return null;
        }
        SetAlpha(toAlpha);
    }

    /// <summary>
    /// 設定 Alpha 值
    /// </summary>
    private void SetAlpha(float alpha)
    {
        _fadeImage.color = new Color(_currentBaseColor.r, _currentBaseColor.g, _currentBaseColor.b, alpha);
    }
}