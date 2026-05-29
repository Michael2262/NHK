using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;
using PixelCrushers;

/// <summary>
/// 女主角狀態面板 UI（NHK 重構版）。
/// 透過 CanvasGroup 控制顯示/隱藏，初始不可見。
///
/// 顯示三項資料：
///   1. Trust（信任）— 目前值 + Preview 變動量
///   2. Libido（性慾）— 目前值 + Preview 變動量
///   3. CurrentEmotion（主導情緒）— 查 EmotionCardCatalog 取 TextTable Key，再本地化顯示
///
/// 注意：HeroineStatusModel 的 OnLibidoChanged / OnTrustChanged
///       傳入的是「新值（newValue）」，不是 delta。
///       Preview delta 由 handler 自行計算（newValue − 舊快取值）。
///
/// 使用方式：
///   HeroineUI.Instance.Show("Sister");     // 指定 ID 開啟
///   HeroineUI.Instance.ShowByOrder(0);     // 指定順位開啟
///   HeroineUI.Instance.Hide();             // 關閉面板
/// </summary>
public class HeroineUI : MonoBehaviour
{
    // ==========================================================
    //  Singleton
    // ==========================================================
    public static HeroineUI Instance { get; private set; }

    // ==========================================================
    //  Inspector 設定
    // ==========================================================

    [Header("=== 順位列表 (依序填入 HeroineID) ===")]
    [Tooltip("在 Inspector 中按照你想要的 Next 切換順序填入 HeroineID")]
    [SerializeField] private List<string> heroineOrder = new List<string>();

    [Header("=== UI 元件綁定 ===")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("=== 信任數值 ===")]
    [SerializeField] private TextMeshProUGUI textTrust;
    [SerializeField] private TextMeshProUGUI textTrustPreview;

    [Header("=== 性慾數值 ===")]
    [SerializeField] private TextMeshProUGUI textLibido;
    [SerializeField] private TextMeshProUGUI textLibidoPreview;

    [Header("=== 主導情緒 ===")]
    [SerializeField] private TextMeshProUGUI textCurrentEmotion;

    [Header("=== 情緒查表 ===")]
    [Tooltip("EmotionCardCatalog ScriptableObject，用於查詢情緒對應的 TextTable Key。")]
    [SerializeField] private EmotionCardCatalog emotionCatalog;

    [Header("=== Preview 顯示設定 ===")]
    [SerializeField] private float previewHoldSeconds = 1.0f;
    [SerializeField] private float previewFadeSeconds = 0.35f;
    [SerializeField] private bool showPositivePreview = true;
    [SerializeField] private bool showNegativePreview = true;

    [Header("=== 按鈕綁定 ===")]
    [SerializeField] private UnityEngine.UI.Button nextButton;
    [SerializeField] private UnityEngine.UI.Button closeButton;

    // ==========================================================
    //  內部狀態
    // ==========================================================
    private int _currentOrderIndex = 0;
    private HeroineStatusModel _currentModel;

    // 用於計算 Preview delta（OnLibidoChanged / OnTrustChanged 傳入的是 newValue）
    private int _cachedTrust;
    private int _cachedLibido;

    private readonly Dictionary<TextMeshProUGUI, Tween> _previewTweens = new Dictionary<TextMeshProUGUI, Tween>();

    // ==========================================================
    //  生命週期
    // ==========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetCanvasGroupVisible(false);

        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    private void OnDestroy()
    {
        UnsubscribeFromModel();
        KillAllTweens();
        if (Instance == this) Instance = null;
    }

    // ==========================================================
    //  公開方法：顯示 / 隱藏
    // ==========================================================

    /// <summary>依「順位索引」開啟面板 (對應 heroineOrder 列表)。</summary>
    public void ShowByOrder(int orderIndex)
    {
        if (heroineOrder == null || heroineOrder.Count == 0)
        {
            Debug.LogWarning("[HeroineUI] heroineOrder 為空，無法顯示。");
            return;
        }

        _currentOrderIndex = Mathf.Clamp(orderIndex, 0, heroineOrder.Count - 1);
        ApplyHeroineData(heroineOrder[_currentOrderIndex]);
        SetCanvasGroupVisible(true);
    }

    /// <summary>
    /// 以 HeroineID 開啟面板。
    /// 若該 ID 存在於順位列表中，會同步更新 currentOrderIndex。
    /// </summary>
    public void Show(string heroineID)
    {
        if (string.IsNullOrEmpty(heroineID))
        {
            Debug.LogWarning("[HeroineUI] heroineID 為空。");
            return;
        }

        int idx = heroineOrder.IndexOf(heroineID);
        if (idx >= 0) _currentOrderIndex = idx;

        ApplyHeroineData(heroineID);
        SetCanvasGroupVisible(true);
    }

    /// <summary>關閉面板。</summary>
    public void Hide()
    {
        SetCanvasGroupVisible(false);
        UnsubscribeFromModel();
        KillAllTweens();
        HideAllPreviewTexts();
        _currentModel = null;
    }

    // ==========================================================
    //  按鈕回調
    // ==========================================================

    private void OnNextClicked()
    {
        if (heroineOrder == null || heroineOrder.Count == 0) return;
        _currentOrderIndex = (_currentOrderIndex + 1) % heroineOrder.Count;
        ApplyHeroineData(heroineOrder[_currentOrderIndex]);
    }

    // ==========================================================
    //  資料綁定
    // ==========================================================

    private void ApplyHeroineData(string heroineID)
    {
        UnsubscribeFromModel();
        KillAllTweens();
        HideAllPreviewTexts();

        var service = GameStatusService.Instance;
        if (service == null || service.Heroines == null)
        {
            Debug.LogWarning("[HeroineUI] GameStatusService 尚未就緒。");
            return;
        }

        if (!service.Heroines.TryGetValue(heroineID, out var model))
        {
            Debug.LogWarning($"[HeroineUI] 找不到 HeroineID: {heroineID}");
            return;
        }

        _currentModel = model;

        // 初始化快取值（避免第一次事件觸發時 delta 計算錯誤）
        _cachedTrust = _currentModel.Trust;
        _cachedLibido = _currentModel.Libido;

        SubscribeToModel();
        RefreshAllUI();
    }

    // ==========================================================
    //  事件訂閱 / 退訂
    // ==========================================================

    private void SubscribeToModel()
    {
        if (_currentModel == null) return;
        _currentModel.OnTrustChanged += HandleTrustChanged;
        _currentModel.OnLibidoChanged += HandleLibidoChanged;
        _currentModel.OnCurrentEmotionChanged += HandleCurrentEmotionChanged;
    }

    private void UnsubscribeFromModel()
    {
        if (_currentModel == null) return;
        _currentModel.OnTrustChanged -= HandleTrustChanged;
        _currentModel.OnLibidoChanged -= HandleLibidoChanged;
        _currentModel.OnCurrentEmotionChanged -= HandleCurrentEmotionChanged;
    }

    // ==========================================================
    //  事件處理
    //  OnTrustChanged / OnLibidoChanged 傳入的是 newValue，不是 delta。
    // ==========================================================

    private void HandleTrustChanged(int newValue)
    {
        int delta = newValue - _cachedTrust;
        _cachedTrust = newValue;
        UpdateTrustUI();
        ShowDeltaPreview(textTrustPreview, delta);
    }

    private void HandleLibidoChanged(int newValue)
    {
        int delta = newValue - _cachedLibido;
        _cachedLibido = newValue;
        UpdateLibidoUI();
        ShowDeltaPreview(textLibidoPreview, delta);
    }

    private void HandleCurrentEmotionChanged(HeroineEmotionCardType emotion)
    {
        UpdateCurrentEmotionUI();
    }

    // ==========================================================
    //  UI 刷新
    // ==========================================================

    private void RefreshAllUI()
    {
        UpdateTrustUI();
        UpdateLibidoUI();
        UpdateCurrentEmotionUI();
    }

    private void UpdateTrustUI()
    {
        if (textTrust == null || _currentModel == null) return;
        textTrust.text = _currentModel.Trust.ToString();
    }

    private void UpdateLibidoUI()
    {
        if (textLibido == null || _currentModel == null) return;
        textLibido.text = _currentModel.Libido.ToString();
    }

    private void UpdateCurrentEmotionUI()
    {
        if (textCurrentEmotion == null || _currentModel == null) return;

        string displayText = ResolveEmotionDisplayText(_currentModel.CurrentEmotion);
        textCurrentEmotion.text = displayText;
    }

    /// <summary>
    /// 透過 EmotionCardCatalog 查詢情緒的 TextTable Key，
    /// 再透過 UILocalizationManager 取得本地化文字。
    /// 若 catalog 未設定，回退至 enum 名稱。
    /// </summary>
    private string ResolveEmotionDisplayText(HeroineEmotionCardType emotion)
    {
        if (emotionCatalog == null)
        {
            Debug.LogWarning("[HeroineUI] emotionCatalog 未設定，回退使用 enum 名稱。");
            return emotion.ToString();
        }

        string textKey = emotionCatalog.GetEmotionNameTextKey(emotion);

        if (string.IsNullOrEmpty(textKey))
            return emotion.ToString();

        if (UILocalizationManager.instance != null)
        {
            string localized = UILocalizationManager.instance.GetLocalizedText(textKey);
            if (!string.IsNullOrEmpty(localized))
                return localized;
        }

        // Fallback：回傳 key 本身（方便 debug）
        return textKey;
    }

    // ==========================================================
    //  Preview 顯示（參考 LobbyUI_V2 寫法）
    // ==========================================================

    private void HideAllPreviewTexts()
    {
        HidePreviewText(textTrustPreview);
        HidePreviewText(textLibidoPreview);
    }

    private void HidePreviewText(TextMeshProUGUI target)
    {
        if (target == null) return;
        target.text = "";
        var c = target.color;
        c.a = 0f;
        target.color = c;
    }

    private void ShowDeltaPreview(TextMeshProUGUI target, int delta)
    {
        if (target == null || delta == 0) return;
        if (delta > 0 && !showPositivePreview) return;
        if (delta < 0 && !showNegativePreview) return;

        if (_previewTweens.TryGetValue(target, out var oldTween) && oldTween != null)
            oldTween.Kill();

        target.text = delta > 0 ? $"+{delta}" : delta.ToString();
        var c = target.color;
        c.a = 1f;
        target.color = c;
        target.gameObject.SetActive(true);

        Tween tween = DOTween.Sequence()
            .AppendInterval(previewHoldSeconds)
            .Append(target.DOFade(0f, previewFadeSeconds))
            .OnComplete(() =>
            {
                if (target != null) target.text = "";
            });

        _previewTweens[target] = tween;
    }

    private void KillAllTweens()
    {
        foreach (var kv in _previewTweens)
            kv.Value?.Kill();
        _previewTweens.Clear();
    }

    // ==========================================================
    //  CanvasGroup 開關
    // ==========================================================

    private void SetCanvasGroupVisible(bool visible)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}