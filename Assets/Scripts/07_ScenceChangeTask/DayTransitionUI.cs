using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System;
using System.Collections;
using PixelCrushers.DialogueSystem;
using PixelCrushers.Wrappers;

/// <summary>
/// 隔天轉場 UI 控制器（v2）。
/// 
/// 【視覺流程】
/// 白天圖淡入 → 停留顯示Tips → 日期推進 + 自動存檔 + 播放音效 → 淡出
/// 
/// 【UI 結構】
/// DayTransitionUI (掛載此腳本 + CanvasGroup)
///   ├── TransitionImage (Image — 顯示白天圖)
///   └── TipsText (TextMeshProUGUI + LocalizeUI)
/// </summary>
public class DayTransitionUI : MonoBehaviour
{
    private static DayTransitionUI _instance;

    public static void PerformTransition(Action onMidTransition = null, Action onComplete = null)
    {
        if (_instance != null)
        {
            _instance.StartCoroutine(_instance.TransitionRoutine(onMidTransition, onComplete));
        }
        else
        {
            Debug.LogWarning("[DayTransitionUI] 找不到實例！直接執行回呼。");
            onMidTransition?.Invoke();
            onComplete?.Invoke();
        }
    }

    public static void SetTipsKey(string key)
    {
        if (_instance != null) _instance._pendingTipsKey = key;
    }

    public static bool IsAvailable => _instance != null;

    // ============================================================
    // Inspector 設定
    // ============================================================

    [Header("轉場圖片")]
    [Tooltip("壓幕/白天圖共用的 Image")]
    [SerializeField] private Image transitionImage;

    [Tooltip("壓幕顏色")]
    [SerializeField] private Color curtainColor = Color.black;

    [Tooltip("白天的背景圖片")]
    [SerializeField] private Sprite morningSprite;

    [Header("UI 元件引用")]
    [SerializeField] private TextMeshProUGUI tipsText;
    [SerializeField] private LocalizeUI tipsLocalizeUI;

    [Header("時間設定")]
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float holdDuration = 2.5f;
    [SerializeField] private float morningHoldDuration = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.4f;

    [Header("Tips 設定（Text Table Keys）")]
    [SerializeField]
    private string[] tipsKeys = new string[]
    {
        "Tips/Sleep_01",
        "Tips/Sleep_02",
        "Tips/Sleep_03",
        "Tips/Sleep_04",
    };

    [Header("點擊設定")]
    [SerializeField] private bool allowClickToChangeTips = true;

    [Header("換日附加動作")]
    [Tooltip("換日時播放的音效 key（對應 AudioManager 的 key）。留空 = 不播放。")]
    [SerializeField] private string dayTransitionSoundKey = "";

    [Tooltip("淡入完成後，延遲幾秒播放音效（0 = 淡入完成立刻播）")]
    [SerializeField] private float soundDelay = 0.5f;

    [Tooltip("換日時是否自動存檔")]
    [SerializeField] private bool autoSaveOnDayTransition = true;

    [Header("換日音樂控制")]
    [Tooltip("換日時是否停止場景音樂")]
    [SerializeField] private bool stopSceneMusicOnTransition = true;

    [Tooltip("停止場景音樂的淡出時長(秒)")]
    [SerializeField] private float sceneMusicFadeOutDuration = 1.0f;

    // ============================================================
    // 內部狀態
    // ============================================================

    private CanvasGroup _canvasGroup;
    private string _pendingTipsKey = null;
    private bool _isTransitioning = false;
    private bool _isShowingTips = false;
    private int _currentTipsIndex = -1;

    // ============================================================
    // Unity 生命週期
    // ============================================================

    private void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) _instance = this;

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        SetCanvasGroupState(0f, false);
        Debug.Log("[DayTransitionUI] 初始化完成");
    }

    private void OnEnable()
    {
        if (_instance == null || _instance != this) _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void Update()
    {
        if (_isShowingTips && allowClickToChangeTips)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                ChangeTips();
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                ChangeTips();
        }
    }

    // ============================================================
    // CanvasGroup 狀態控制
    // ============================================================

    private void SetCanvasGroupState(float alpha, bool visible)
    {
        _canvasGroup.alpha = alpha;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    // ============================================================
    // 核心轉場流程
    // ============================================================

    private IEnumerator TransitionRoutine(Action onMidTransition, Action onComplete)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning("[DayTransitionUI] 正在轉場中，忽略重複請求");
            yield break;
        }

        _isTransitioning = true;
        Debug.Log("[DayTransitionUI] 開始隔天轉場演出");

        // --- 準備：設定白天圖 ---
        SetImageAsMorning();
        PrepareUI();
        SetCanvasGroupState(0f, true);

        // --- 1. 淡入 ---
        yield return FadeCanvasGroup(0f, 1f, fadeInDuration);

        // --- 1.5 播放換日音效（淡入完成後延遲播放）---
        if (!string.IsNullOrEmpty(dayTransitionSoundKey) && AudioManager.Instance != null)
        {
            if (soundDelay > 0f)
                yield return new WaitForSecondsRealtime(soundDelay);

            AudioManager.Instance.PlaySound(dayTransitionSoundKey);
            Debug.Log($"[DayTransitionUI] 播放換日音效: {dayTransitionSoundKey}");
        }

        // --- 2. 顯示 Tips ---
        _isShowingTips = true;

        // --- 3. 停留 ---
        yield return new WaitForSecondsRealtime(holdDuration);

        // --- 4. 日期推進 ---
        Debug.Log("[DayTransitionUI] 執行日期推進回呼");
        onMidTransition?.Invoke();

        // --- 4.5 換日附加動作（日期推進後、淡出前）---
        PerformDayTransitionActions();

        // --- 5. 停止換句 ---
        _isShowingTips = false;

        // --- 6. 淡出 ---
        yield return FadeCanvasGroup(1f, 0f, fadeOutDuration);

        // --- 結束 ---
        SetCanvasGroupState(0f, false);
        _isTransitioning = false;

        Debug.Log("[DayTransitionUI] 隔天轉場演出完成");
        onComplete?.Invoke();
    }

    // ============================================================
    // Image 狀態切換
    // ============================================================

    private void SetImageAsCurtain()
    {
        if (transitionImage == null) return;
        transitionImage.sprite = null;
        transitionImage.color = curtainColor;
    }

    private void SetImageAsMorning()
    {
        if (transitionImage == null || morningSprite == null) return;
        transitionImage.sprite = morningSprite;
        transitionImage.color = Color.white;
    }

    // ============================================================
    // 換日附加動作
    // ============================================================

    /// <summary>
    /// 在日期推進完成後執行的附加動作（停止場景音樂、自動存檔）
    /// </summary>
    private void PerformDayTransitionActions()
    {
        // 停止場景音樂
        if (stopSceneMusicOnTransition && SceneMusicController.Instance != null)
        {
            SceneMusicController.Instance.StopOverride(false, sceneMusicFadeOutDuration);
            Debug.Log("[DayTransitionUI] 換日停止場景音樂");
        }

        if (autoSaveOnDayTransition && GameStatusService.Instance != null)
        {
            GameStatusService.Instance.AutoSave();
            Debug.Log("[DayTransitionUI] 換日自動存檔完成");
        }
    }

    // ============================================================
    // Tips 處理
    // ============================================================

    private void PrepareUI()
    {
        if (tipsKeys == null || tipsKeys.Length == 0) return;

        string keyToShow;

        if (!string.IsNullOrEmpty(_pendingTipsKey))
        {
            keyToShow = _pendingTipsKey;
            _pendingTipsKey = null;
            _currentTipsIndex = -1;
        }
        else
        {
            int newIndex;
            if (tipsKeys.Length == 1)
            {
                newIndex = 0;
            }
            else
            {
                do { newIndex = UnityEngine.Random.Range(0, tipsKeys.Length); }
                while (newIndex == _currentTipsIndex);
            }
            _currentTipsIndex = newIndex;
            keyToShow = tipsKeys[_currentTipsIndex];
        }

        ApplyTipsKey(keyToShow);
    }

    private void ChangeTips()
    {
        if (tipsKeys == null || tipsKeys.Length <= 1) return;

        int newIndex;
        do { newIndex = UnityEngine.Random.Range(0, tipsKeys.Length); }
        while (newIndex == _currentTipsIndex);

        _currentTipsIndex = newIndex;
        ApplyTipsKey(tipsKeys[_currentTipsIndex]);
    }

    private void ApplyTipsKey(string key)
    {
        if (tipsLocalizeUI != null)
        {
            tipsLocalizeUI.fieldName = key;
            tipsLocalizeUI.UpdateText();
        }
        else if (tipsText != null)
        {
            string localizedText = DialogueManager.GetLocalizedText(key);
            tipsText.text = !string.IsNullOrEmpty(localizedText) ? localizedText : key;
        }
    }

    // ============================================================
    // 整體淡入淡出
    // ============================================================

    private IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        float timer = 0f;
        _canvasGroup.alpha = from;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            _canvasGroup.alpha = Mathf.Lerp(from, to, progress);
            yield return null;
        }

        _canvasGroup.alpha = to;
    }

    // ============================================================
    // 編輯器輔助
    // ============================================================

    [ContextMenu("Preview - Show Curtain")]
    private void PreviewShowCurtain()
    {
        SetImageAsCurtain();
        var cg = GetComponent<CanvasGroup>();
        if (cg != null) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
    }

    [ContextMenu("Preview - Show Morning")]
    private void PreviewShowMorning()
    {
        SetImageAsMorning();
        var cg = GetComponent<CanvasGroup>();
        if (cg != null) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
    }

    [ContextMenu("Preview - Hide")]
    private void PreviewHide()
    {
        var cg = GetComponent<CanvasGroup>();
        if (cg != null) { cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false; }
    }
}