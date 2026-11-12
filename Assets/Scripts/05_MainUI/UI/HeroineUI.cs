using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using PixelCrushers.DialogueSystem;

/// <summary>
/// 女主角狀態面板 UI（NHK 重構版）。
/// 兩個部位各依自己的進度 Flag 控制 CanvasGroup，外層沿用場景設定。
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
        [Tooltip("拖入此部位的 CanvasGroup，並保持其 GameObject 啟用。")]
        public CanvasGroup canvasGroup;
        public ProgressFlagDefinition flag;

        public bool Refresh(ProgressFlagModel flags)
        {
            if (canvasGroup == null) return false;
            bool visible = flag != null && flags != null && flags.Contains(flag.FlagID);
            SetCanvasGroupVisible(canvasGroup, visible);
            return visible;
        }
    }

    [Header("依進度 Flag 自動顯示的部位")]
    [SerializeField] private PanelSection firstSection = new PanelSection();
    [SerializeField] private PanelSection secondSection = new PanelSection();

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

    // 同一幀內，女主角數值接在大廳的貼紙總數之後播放。
    private const int MoodPreviewOrder = StatusPreviewSequencer.OrderStickerCount + 1;
    private const int LibidoPreviewOrder = MoodPreviewOrder + 1;
    private const int InterestPreviewOrder = LibidoPreviewOrder + 1;
    private const int TrustPreviewOrder = InterestPreviewOrder + 1;

    // ==========================================================
    //  內部狀態
    // ==========================================================
    private int _currentOrderIndex = 0;
    private HeroineStatusModel _currentModel;

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

    private void Start()
    {
        // 訂閱讀檔完成事件，讓 HeroineUI 在讀檔後自行還原顯示狀態
        // 放在 Start 確保 GameStatusService.Instance 已在 Awake（-900）完成初始化
        var service = GameStatusService.Instance;
        if (service == null) return;
        service.OnGameStatusLoaded += OnGameStatusLoaded;
        service.ProgressFlags.OnFlagChanged += OnSectionFlagChanged;
        RefreshAutomaticDisplay();
    }

    private void OnDestroy()
    {
        if (GameStatusService.Instance != null)
        {
            GameStatusService.Instance.OnGameStatusLoaded -= OnGameStatusLoaded;
            GameStatusService.Instance.ProgressFlags.OnFlagChanged -= OnSectionFlagChanged;
        }
        UnsubscribeFromModel();
        StatusPreviewSequencer.CancelOwnerIfExists(this);
        if (Instance == this) Instance = null;
    }

    private void OnDisable()
    {
        StatusPreviewSequencer.CancelOwnerIfExists(this);
        HideAllPreviewTexts();
        RefreshAllUI();
    }

    private void ClearHeroineBinding()
    {
        UnsubscribeFromModel();
        StatusPreviewSequencer.CancelOwnerIfExists(this);
        HideAllPreviewTexts();
        _currentModel = null;
    }

    /// <summary>
    /// 讀檔完成後由 GameStatusService.OnGameStatusLoaded 觸發。
    /// 依存檔中的 Flag 顯示各部位，再綁定該存檔的女主角。
    /// </summary>
    private void OnGameStatusLoaded()
    {
        RefreshAutomaticDisplay(restoreHeroine: true);
    }

    private void OnSectionFlagChanged(string key, bool _)
    {
        if (key == firstSection.flag?.FlagID || key == secondSection.flag?.FlagID)
            RefreshAutomaticDisplay();
    }

    private bool RefreshSections(ProgressFlagModel flags)
    {
        bool firstVisible = firstSection.Refresh(flags);
        bool secondVisible = secondSection.Refresh(flags);
        return firstVisible || secondVisible;
    }

    private void RefreshAutomaticDisplay(bool restoreHeroine = false)
    {
        var flags = GameStatusService.Instance.ProgressFlags;
        if (!RefreshSections(flags) || heroineOrder.Count == 0)
        {
            ClearHeroineBinding();
            return;
        }
        if (restoreHeroine || _currentModel == null)
        {
            int number = flags.GetValue(HeroineNumberKey);
            _currentOrderIndex = number > 0 && number <= heroineOrder.Count ? number - 1 : 0;
            if (!ApplyHeroineData(heroineOrder[_currentOrderIndex]))
                ClearHeroineBinding();
        }
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
        StatusPreviewSequencer.CancelOwnerIfExists(this);
        HideAllPreviewTexts();
        _currentModel = model;

        // 初始化快取值（避免第一次事件觸發時 delta 計算錯誤）
        _cachedTrust = _currentModel.Trust;
        _cachedLibido = _currentModel.Libido;
        _cachedInterest = _currentModel.Interest;
        _cachedMood = _currentModel.Mood;
        SubscribeToModel();
        RefreshAllUI();
        return true;
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

    /// <summary>目前面板顯示中的女主角 ID；面板未開啟時回 null。</summary>
    public string CurrentHeroineID => _currentModel?.HeroineID;

    /// <summary>顯示女主角數值的 hover 預覽（會先清掉上一輪）。</summary>
    public void ShowStatPreview(IReadOnlyList<StatPreviewItem> items)
    {
        ClearStatPreview();
        if (items == null) return;

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
        if (!isActiveAndEnabled)
        {
            applyValue?.Invoke();
            return;
        }
        StatusPreviewSequencer.Instance.EnqueueText(
            order, preview, GetPreviewText(delta, isMood), applyValue, this);
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

    private static void SetCanvasGroupVisible(CanvasGroup canvasGroup, bool visible)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
