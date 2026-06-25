using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    [Tooltip("情緒卡池新增情緒時，冒出『情緒名稱 ↑』的飄字。")]
    [SerializeField] private TextMeshProUGUI textEmotionAddedPreview;
    [Tooltip("CurrentEmotion 變化時閃一下的提示。文字固定（在此元件上自行填好），只做開關、不改內容。")]
    [SerializeField] private TextMeshProUGUI textCurrentEmotionChangedPreview;

    [Header("=== 情緒查表 ===")]
    [Tooltip("EmotionCardCatalog ScriptableObject，用於查詢情緒對應的 TextTable Key。")]
    [SerializeField] private EmotionCardCatalog emotionCatalog;

    [Header("=== Preview 顯示設定 ===")]
    [Tooltip("正/負變動是否顯示飄字；播放間隔與淡入淡出時間改由 StatusPreviewSequencer 控制。")]
    [SerializeField] private bool showPositivePreview = true;
    [SerializeField] private bool showNegativePreview = true;

    [Header("=== 顯示設定（暫時性） ===")]
    [Tooltip("開啟後：女主角不顯示主導情緒（textCurrentEmotion），" +
             "也不顯示情緒相關的兩種 preview（新增情緒飄字、情緒轉變提示）。")]
    [SerializeField] private bool hideEmotionDisplay = false;

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
        StatusPreviewSequencer.CancelAllIfExists();
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
        StatusPreviewSequencer.CancelAllIfExists();
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
        StatusPreviewSequencer.CancelAllIfExists();
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
        _currentModel.OnEmotionCardAdded += HandleEmotionCardAdded;
    }

    private void UnsubscribeFromModel()
    {
        if (_currentModel == null) return;
        _currentModel.OnTrustChanged -= HandleTrustChanged;
        _currentModel.OnLibidoChanged -= HandleLibidoChanged;
        _currentModel.OnCurrentEmotionChanged -= HandleCurrentEmotionChanged;
        _currentModel.OnEmotionCardAdded -= HandleEmotionCardAdded;
    }

    // ==========================================================
    //  事件處理
    //  OnTrustChanged / OnLibidoChanged 傳入的是 newValue，不是 delta。
    // ==========================================================

    private void HandleTrustChanged(int newValue)
    {
        int delta = newValue - _cachedTrust;
        _cachedTrust = newValue;
        StatusPreviewSequencer.Instance.Enqueue(
            StatusPreviewSequencer.OrderHeroineTrust,
            () => SetText(textTrust, newValue),
            PreviewIfAllowed(textTrustPreview, delta),
            delta);
    }

    private void HandleLibidoChanged(int newValue)
    {
        int delta = newValue - _cachedLibido;
        _cachedLibido = newValue;
        StatusPreviewSequencer.Instance.Enqueue(
            StatusPreviewSequencer.OrderHeroineLibido,
            () => SetText(textLibido, newValue),
            PreviewIfAllowed(textLibidoPreview, delta),
            delta);
    }

    private void HandleCurrentEmotionChanged(HeroineEmotionCardType emotion)
    {
        // 暫時性：關閉情緒顯示時，不更新標籤、也不閃「情緒轉變」提示。
        if (hideEmotionDisplay) return;

        // 標籤本體在輪到它的 step 才更新；同時閃一下「情緒轉變」提示（文字固定、只開關）。
        StatusPreviewSequencer.Instance.EnqueueToggle(
            StatusPreviewSequencer.OrderHeroineCurrentEmotion,
            textCurrentEmotionChangedPreview,
            applyValue: UpdateCurrentEmotionUI);
    }

    private void HandleEmotionCardAdded(HeroineEmotionCardType emotion)
    {
        // 暫時性：關閉情緒顯示時，不冒出「情緒名稱 ↑」飄字。
        if (hideEmotionDisplay) return;

        // 新增情緒卡：冒出「情緒名稱 ↑」飄字（名稱同樣走 EmotionCardCatalog 查表本地化）。
        string label = ResolveEmotionDisplayText(emotion);
        StatusPreviewSequencer.Instance.EnqueueText(
            StatusPreviewSequencer.OrderHeroineEmotionAdded,
            textEmotionAddedPreview,
            label + "↑ ");
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

        // 暫時性：關閉情緒顯示時，主導情緒標籤隱藏。
        if (hideEmotionDisplay)
        {
            if (textCurrentEmotion.gameObject.activeSelf)
                textCurrentEmotion.gameObject.SetActive(false);
            return;
        }

        if (!textCurrentEmotion.gameObject.activeSelf)
            textCurrentEmotion.gameObject.SetActive(true);

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
        HidePreviewText(textEmotionAddedPreview);
        // 情緒轉變提示文字是固定的，只歸零 alpha、不清字。
        HidePreviewKeepText(textCurrentEmotionChangedPreview);
    }

    private void HidePreviewText(TextMeshProUGUI target)
    {
        if (target == null) return;
        target.text = "";
        var c = target.color;
        c.a = 0f;
        target.color = c;
    }

    // 只把 alpha 歸零、保留原本文字（給「純開關」型 preview 用）。
    private void HidePreviewKeepText(TextMeshProUGUI target)
    {
        if (target == null) return;
        var c = target.color;
        c.a = 0f;
        target.color = c;
    }

    private static void SetText(TextMeshProUGUI target, int value)
    {
        if (target != null) target.text = value.ToString();
    }

    // 依正/負顯示設定決定是否要冒飄字；不允許時回傳 null（數字仍會跳）。
    private TextMeshProUGUI PreviewIfAllowed(TextMeshProUGUI target, int delta)
    {
        if (delta > 0 && !showPositivePreview) return null;
        if (delta < 0 && !showNegativePreview) return null;
        return target;
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