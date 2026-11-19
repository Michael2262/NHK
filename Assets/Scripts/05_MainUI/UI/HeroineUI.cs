using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using PixelCrushers.DialogueSystem;

/// <summary>
/// 女主角狀態面板 UI（NHK 重構版）。
/// 女主角面板依進度 Flag 控制 CanvasGroup，主角面板由 ProtagonistUI 獨立管理。
///
/// 顯示 Trust、Interest、Libido 的目前值，以及依 Mood 區間查表的心情文字。
/// 四項共用數值 preview 排序；Mood 主文字維持五段心情文字。
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

    [System.Serializable]
    private class PanelSection
    {
        [Tooltip("拖入女主角面板的 CanvasGroup，並保持其 GameObject 啟用。")]
        public CanvasGroup canvasGroup;
        public ProgressFlagDefinition flag;
    }

    [Header("依進度 Flag 自動顯示的女主角面板")]
    [FormerlySerializedAs("firstSection")]
    [SerializeField] private PanelSection panel = new PanelSection();

    [Header("變動時顯示")]
    [Tooltip("顯示總開關成立時，此 Flag 成立才改為變動時顯示；留空則維持常駐。")]
    [SerializeField] private ProgressFlagDefinition showOnChangeFlag;
    [InspectorName("演完後停留秒數")]
    [Min(0f)] [SerializeField] private float holdDuration = 1f;
    [InspectorName("面板淡出秒數")]
    [Min(0f)] [SerializeField] private float fadeDuration = 0.25f;

    [Header("=== 信賴數值 ===")]
    [SerializeField] private TextMeshProUGUI textTrust;
    [SerializeField] private TextMeshProUGUI textTrustPreview;

    [Header("=== 性興味數值 ===")]
    [FormerlySerializedAs("textAffinity")]
    [SerializeField] private TextMeshProUGUI textInterest;
    [FormerlySerializedAs("textAffinityPreview")]
    [SerializeField] private TextMeshProUGUI textInterestPreview;

    [Header("=== 性欲數值 ===")]
    [SerializeField] private TextMeshProUGUI textLibido;
    [SerializeField] private TextMeshProUGUI textLibidoPreview;

    [Header("=== 心情文字 ===")]
    [SerializeField] private TextMeshProUGUI textMood;
    [SerializeField] private TextMeshProUGUI textMoodPreview;
    [Tooltip("Mood 0～1 對應的 Text Table Key。")]
    [SerializeField] private string mood1TextKey = "Mood1";
    [Tooltip("Mood 2～3 對應的 Text Table Key。")]
    [SerializeField] private string mood2TextKey = "Mood2";
    [Tooltip("Mood 4～6 對應的 Text Table Key。")]
    [SerializeField] private string mood3TextKey = "Mood3";
    [Tooltip("Mood 7～8 對應的 Text Table Key。")]
    [SerializeField] private string mood4TextKey = "Mood4";
    [Tooltip("Mood 9～10 對應的 Text Table Key。")]
    [SerializeField] private string mood5TextKey = "Mood5";

    [Header("=== Preview 顯示設定 ===")]
    [Tooltip("正/負變動是否顯示飄字；播放間隔與淡入淡出時間改由 StatusPreviewSequencer 控制。")]
    [SerializeField] private bool showPositivePreview = true;
    [SerializeField] private bool showNegativePreview = true;

    [Header("=== 按鈕綁定 ===")]
    [SerializeField] private UnityEngine.UI.Button nextButton;

    // ==========================================================
    //  ProgressFlag 常數
    // ==========================================================

    // 沿用存檔鍵，順位從 1 起算，0 表示尚無有效角色編號。
    private const string HeroineNumberKey = "Value_System_HeroineUIVisible";

    // 同一幀內，女主角數值接在 ProtagonistUI 的貼紙總數之後播放。
    private const int MoodPreviewOrder = StatusPreviewSequencer.OrderStickerCount + 1;
    private const int LibidoPreviewOrder = MoodPreviewOrder + 1;
    private const int InterestPreviewOrder = LibidoPreviewOrder + 1;
    private const int TrustPreviewOrder = InterestPreviewOrder + 1;

    // ==========================================================
    //  內部狀態
    // ==========================================================
    private int _currentOrderIndex = 0;
    private HeroineStatusModel _currentModel;
    private GameStatusService _service;
    private ProgressFlagModel _flags;
    private readonly StatusPanelVisibilityState _visibility = new StatusPanelVisibilityState();

    // 用於計算 Preview delta（數值事件傳入的是 newValue）
    private int _cachedTrust;
    private int _cachedLibido;
    private int _cachedInterest;
    private int _cachedMood;

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

        HideAllPreviewTexts();

        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
    }

    private void OnEnable()
    {
        if (Instance != this) return;
        BindAndRefresh();
    }

    private void Update()
    {
        if (Instance != this) return;
        var service = GameStatusService.Instance;
        if (!ReferenceEquals(_service, service) || !ReferenceEquals(_flags, service?.ProgressFlags))
            BindAndRefresh();
        UpdatePanelAlpha();
    }

    private void BindAndRefresh()
    {
        UnbindService();
        _service = GameStatusService.Instance;
        _flags = _service?.ProgressFlags;
        if (_service != null) _service.OnGameStatusLoaded += OnGameStatusLoaded;
        if (_flags != null) _flags.OnFlagChanged += OnVisibilityFlagChanged;
        RefreshAutomaticDisplay(restoreHeroine: true, resetPresentation: true);
    }

    private void UnbindService()
    {
        if (!ReferenceEquals(_service, null)) _service.OnGameStatusLoaded -= OnGameStatusLoaded;
        if (_flags != null) _flags.OnFlagChanged -= OnVisibilityFlagChanged;
        _service = null;
        _flags = null;
    }

    private void OnDestroy()
    {
        UnbindService();
        ClearHeroineBinding();
        if (Instance == this) Instance = null;
    }

    private void OnDisable()
    {
        if (Instance != this) return;
        UnbindService();
        ClearHeroineBinding();
        _visibility.Configure(false, false);
        SetCanvasGroupAlpha(panel.canvasGroup, 0f);
    }

    private void ClearHeroineBinding()
    {
        UnsubscribeFromModel();
        CancelPreviews();
        _currentModel = null;
    }

    private void CancelPreviews()
    {
        StatusPreviewSequencer.CancelOwnerIfExists(this);
        _visibility.ResetPresentation();
        HideAllPreviewTexts();
    }

    /// <summary>
    /// 讀檔完成後由 GameStatusService.OnGameStatusLoaded 觸發。
    /// 依存檔中的 Flag 顯示女主角面板，再綁定該存檔的女主角。
    /// </summary>
    private void OnGameStatusLoaded()
    {
        BindAndRefresh();
    }

    private void OnVisibilityFlagChanged(string key, bool _)
    {
        if (key == panel.flag?.FlagID || key == showOnChangeFlag?.FlagID)
            RefreshAutomaticDisplay();
    }

    private void RefreshAutomaticDisplay(bool restoreHeroine = false, bool resetPresentation = false)
    {
        bool allowed = panel.canvasGroup != null && panel.flag != null
            && _flags != null && _flags.Contains(panel.flag.FlagID);
        bool onChange = showOnChangeFlag != null && _flags != null
            && _flags.Contains(showOnChangeFlag.FlagID);
        bool changed = _visibility.Configure(allowed, onChange);
        if (changed || resetPresentation) CancelPreviews();

        // 變動模式雖然暫時透明，仍須綁定角色，才能收到下一次真正的數值變動。
        if (!allowed || heroineOrder.Count == 0)
        {
            ClearHeroineBinding();
            UpdatePanelAlpha();
            return;
        }
        if (restoreHeroine || _currentModel == null)
        {
            int number = _flags.GetValue(HeroineNumberKey);
            _currentOrderIndex = number > 0 && number <= heroineOrder.Count ? number - 1 : 0;
            if (!ApplyHeroineData(heroineOrder[_currentOrderIndex]))
                ClearHeroineBinding();
        }
        else if (changed || resetPresentation)
        {
            CacheCurrentValues();
            RefreshAllUI();
        }
        UpdatePanelAlpha();
    }

    // ==========================================================
    //  按鈕回調
    // ==========================================================

    private void OnNextClicked()
    {
        if (_currentModel == null || heroineOrder.Count == 0) return;
        int nextIndex = (_currentOrderIndex + 1) % heroineOrder.Count;
        if (!ApplyHeroineData(heroineOrder[nextIndex])) return;
        _currentOrderIndex = nextIndex;
        GameStatusService.Instance.ProgressFlags.SetValue(HeroineNumberKey, nextIndex + 1);
    }

    // ==========================================================
    //  資料綁定
    // ==========================================================

    private bool ApplyHeroineData(string heroineID)
    {
        var service = GameStatusService.Instance;
        if (service == null || service.Heroines == null)
        {
            Debug.LogWarning("[HeroineUI] GameStatusService 尚未就緒。");
            return false;
        }

        if (string.IsNullOrEmpty(heroineID) || !service.Heroines.TryGetValue(heroineID, out var model))
        {
            Debug.LogWarning($"[HeroineUI] 找不到 HeroineID: {heroineID}");
            return false;
        }

        UnsubscribeFromModel();
        CancelPreviews();
        _currentModel = model;

        // 初始化快取值（避免第一次事件觸發時 delta 計算錯誤）
        CacheCurrentValues();
        SubscribeToModel();
        RefreshAllUI();
        UpdatePanelAlpha();
        return true;
    }

    private void CacheCurrentValues()
    {
        if (_currentModel == null) return;
        _cachedTrust = _currentModel.Trust;
        _cachedLibido = _currentModel.Libido;
        _cachedInterest = _currentModel.Interest;
        _cachedMood = _currentModel.Mood;
    }

    // ==========================================================
    //  事件訂閱 / 退訂
    // ==========================================================

    private void SubscribeToModel()
    {
        if (_currentModel == null) return;
        _currentModel.OnTrustChanged += HandleTrustChanged;
        _currentModel.OnLibidoChanged += HandleLibidoChanged;
        _currentModel.OnInterestChanged += HandleInterestChanged;
        _currentModel.OnMoodChanged += HandleMoodChanged;
    }

    private void UnsubscribeFromModel()
    {
        if (_currentModel == null) return;
        _currentModel.OnTrustChanged -= HandleTrustChanged;
        _currentModel.OnLibidoChanged -= HandleLibidoChanged;
        _currentModel.OnInterestChanged -= HandleInterestChanged;
        _currentModel.OnMoodChanged -= HandleMoodChanged;
    }

    // ==========================================================
    //  事件處理
    //  OnTrustChanged / OnLibidoChanged 傳入的是 newValue，不是 delta。
    // ==========================================================

    private void HandleInterestChanged(int newValue)
    {
        int delta = newValue - _cachedInterest;
        _cachedInterest = newValue;
        EnqueueArrowPreview(
            InterestPreviewOrder,
            () => SetText(textInterest, newValue),
            PreviewIfAllowed(textInterestPreview, delta),
            delta);
    }

    private void HandleMoodChanged(int newValue)
    {
        int delta = newValue - _cachedMood;
        _cachedMood = newValue;
        EnqueueArrowPreview(
            MoodPreviewOrder,
            () => SetMoodText(newValue),
            PreviewIfAllowed(textMoodPreview, delta),
            delta, isMood: true);
    }

    private void SetMoodText(int value)
    {
        if (textMood == null) return;
        string key = value <= 1 ? mood1TextKey
            : value <= 3 ? mood2TextKey
            : value <= 6 ? mood3TextKey
            : value <= 8 ? mood4TextKey
            : mood5TextKey;
        textMood.text = Localize(key);
    }

    private string Localize(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        string text = DialogueManager.GetLocalizedText(key);
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning($"[HeroineUI] Text Table 找不到 Key: {key}", this);
            return key;
        }
        return text;
    }

    private void HandleTrustChanged(int newValue)
    {
        int delta = newValue - _cachedTrust;
        _cachedTrust = newValue;
        EnqueueArrowPreview(
            TrustPreviewOrder,
            () => SetText(textTrust, newValue),
            PreviewIfAllowed(textTrustPreview, delta),
            delta);
    }

    private void HandleLibidoChanged(int newValue)
    {
        int delta = newValue - _cachedLibido;
        _cachedLibido = newValue;
        EnqueueArrowPreview(
            LibidoPreviewOrder,
            () => SetText(textLibido, newValue),
            PreviewIfAllowed(textLibidoPreview, delta),
            delta);
    }

    // ==========================================================
    //  UI 刷新
    // ==========================================================

    private void RefreshAllUI()
    {
        UpdateTrustUI();
        UpdateLibidoUI();
        if (_currentModel != null) SetText(textInterest, _currentModel.Interest);
        if (_currentModel != null) SetMoodText(_currentModel.Mood);
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

    // ==========================================================
    //  Preview 顯示（參考 LobbyUI_V2 寫法）
    // ==========================================================

    // ==========================================================
    //  Hover 預覽（靜態）：滑鼠懸停時常駐顯示增減箭頭，移開清掉。
    //  顯示 Trust / Interest / Libido / Mood 的數值預覽，不改變目前主顯示。
    //  由 StatPackagePreviewPresenter 呼叫。
    // ==========================================================

    /// <summary>目前綁定的女主角 ID；變動模式暫時隱藏時仍保留，總開關關閉時回 null。</summary>
    public string CurrentHeroineID => _currentModel?.HeroineID;

    /// <summary>顯示女主角數值的 hover 預覽（會先清掉上一輪）。</summary>
    public void ShowStatPreview(IReadOnlyList<StatPreviewItem> items)
    {
        ClearStatPreview();
        if (items == null || !_visibility.AllowsDisplay) return;

        foreach (var it in items)
        {
            switch (it.kind)
            {
                case StatKind.Trust: SetStaticPreview(textTrustPreview, it.delta); break;
                case StatKind.Libido: SetStaticPreview(textLibidoPreview, it.delta); break;
                case StatKind.Interest: SetStaticPreview(textInterestPreview, it.delta); break;
                case StatKind.Mood: SetStaticPreview(textMoodPreview, it.delta, isMood: true); break;
                // 其餘項目本階段不在此面板顯示預覽。
            }
        }
    }

    /// <summary>清掉女主角的 hover 預覽字。</summary>
    public void ClearStatPreview()
    {
        HidePreviewText(textTrustPreview);
        HidePreviewText(textLibidoPreview);
        HidePreviewText(textInterestPreview);
        HidePreviewText(textMoodPreview);
    }

    private static void SetStaticPreview(TextMeshProUGUI target, int delta, bool isMood = false)
    {
        if (target == null) return;
        target.text = GetPreviewText(delta, isMood);
        var c = target.color;
        c.a = delta == 0 ? 0f : 1f;
        target.color = c;
        if (delta != 0) target.gameObject.SetActive(true);
    }

    private static string GetPreviewText(int delta, bool isMood)
    {
        return isMood ? StatusPreviewSequencer.GetPreviewArrowText(delta)
            : StatusPreviewSequencer.GetNumericPreviewText(delta);
    }

    private void EnqueueArrowPreview(int order, System.Action applyValue,
        TextMeshProUGUI preview, int delta, bool isMood = false)
    {
        if (delta == 0) return;
        if (!isActiveAndEnabled || !_visibility.AllowsDisplay)
        {
            applyValue?.Invoke();
            return;
        }
        StatusPreviewSequencer.Instance.EnqueueText(
            order, preview, GetPreviewText(delta, isMood), () =>
            {
                _visibility.NotifyChangeStarted();
                SetCanvasGroupAlpha(panel.canvasGroup, _visibility.AllowsDisplay ? 1f : 0f);
                applyValue?.Invoke();
            }, this);
    }

    private void HideAllPreviewTexts()
    {
        HidePreviewText(textTrustPreview);
        HidePreviewText(textLibidoPreview);
        HidePreviewText(textInterestPreview);
        HidePreviewText(textMoodPreview);
    }

    private void HidePreviewText(TextMeshProUGUI target)
    {
        if (target == null) return;
        target.text = "";
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

    private void UpdatePanelAlpha()
    {
        bool hasWork = _visibility.IsPresenting && StatusPreviewSequencer.HasOwnerWorkIfExists(this);
        float alpha = _visibility.GetAlpha(Time.unscaledTime, hasWork, holdDuration, fadeDuration);
        SetCanvasGroupAlpha(panel.canvasGroup, alpha);
    }

    private static void SetCanvasGroupAlpha(CanvasGroup canvasGroup, float alpha)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = alpha;
        canvasGroup.interactable = alpha > 0f;
        canvasGroup.blocksRaycasts = alpha > 0f;
    }
}
