using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using PixelCrushers.DialogueSystem;

// 常駐顯示設計說明：
//   HeroineUI 用單一 int Variable（VAR_VISIBLE）同時記錄「要不要顯示」與「顯示哪位」。
//   值為 0 → 隱藏；值為 N（N > 0）→ 顯示 heroineOrder[N-1]（順位從 1 起算）。
//   - Show()  → SetValue(順位索引 + 1)
//   - Hide()  → SetValue(0)
//   - 讀檔後（OnGameStatusLoaded）→ 取值，0 不動，> 0 則 ShowByOrder(value - 1)
//   開場 FSM 只需照常呼叫 Show()，讀檔路徑由 HeroineUI 自行處理，兩條路徑共用同一邏輯。

/// <summary>
/// 女主角狀態面板 UI（NHK 重構版）。
/// 透過 CanvasGroup 控制顯示/隱藏，初始不可見。
///
/// 顯示 Trust、Interest、Libido 的目前值，以及依 Mood 區間查表的心情文字。
/// 四項共用數值 preview 排序；Mood 主文字維持五段心情文字。
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
    [SerializeField] private UnityEngine.UI.Button closeButton;

    // ==========================================================
    //  ProgressFlag 常數
    // ==========================================================

    // 0 = 隱藏；N > 0 = 顯示 heroineOrder[N-1]
    // 單一 Variable 同時記錄顯示狀態與角色順位，存檔後讀檔可自動還原
    private const string VAR_VISIBLE = "Value_System_HeroineUIVisible";

    // 性興味接在性欲之後播放，沿用本面板原本第三個數值欄位的順位。
    private const int InterestPreviewOrder = StatusPreviewSequencer.OrderHeroineLibido + 1;
    private const int MoodPreviewOrder = InterestPreviewOrder + 1;

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

        SetCanvasGroupVisible(false);
        HideAllPreviewTexts();

        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);

    }

    private void Start()
    {
        // 訂閱讀檔完成事件，讓 HeroineUI 在讀檔後自行還原顯示狀態
        // 放在 Start 確保 GameStatusService.Instance 已在 Awake（-900）完成初始化
        if (GameStatusService.Instance != null)
            GameStatusService.Instance.OnGameStatusLoaded += OnGameStatusLoaded;
    }

    private void OnDestroy()
    {
        if (GameStatusService.Instance != null)
            GameStatusService.Instance.OnGameStatusLoaded -= OnGameStatusLoaded;
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
        Show(heroineOrder[_currentOrderIndex]);
    }

    /// <summary>
    /// 以 HeroineID 開啟面板，並設置 Persistent Flag 讓讀檔後能自動還原。
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

        // 存入順位索引 + 1（0 保留給「隱藏」狀態，讀檔時以此還原）
        GameStatusService.Instance?.ProgressFlags?.SetValue(VAR_VISIBLE, _currentOrderIndex + 1);
    }

    /// <summary>關閉面板，並清除 Persistent Flag。</summary>
    public void Hide()
    {
        SetCanvasGroupVisible(false);
        UnsubscribeFromModel();
        StatusPreviewSequencer.CancelAllIfExists();
        HideAllPreviewTexts();
        _currentModel = null;

        // 歸零表示「隱藏」，讀檔後不會還原顯示
        GameStatusService.Instance?.ProgressFlags?.SetValue(VAR_VISIBLE, 0);
    }

    /// <summary>
    /// 讀檔完成後由 GameStatusService.OnGameStatusLoaded 觸發。
    /// 此時 ProgressFlags 已還原，若 FLAG_VISIBLE 存在則自動重新顯示 UI。
    /// </summary>
    private void OnGameStatusLoaded()
    {
        var flags = GameStatusService.Instance?.ProgressFlags;
        if (flags == null) return;

        // 0 = 隱藏，不還原；N > 0 = 顯示 heroineOrder[N-1]
        int saved = flags.GetValue(VAR_VISIBLE);
        if (saved > 0) ShowByOrder(saved - 1);
        else Hide();
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
        _cachedInterest = _currentModel.Interest;
        _cachedMood = _currentModel.Mood;
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
        StatusPreviewSequencer.Instance.Enqueue(
            InterestPreviewOrder,
            () => SetText(textInterest, newValue),
            PreviewIfAllowed(textInterestPreview, delta),
            delta);
    }

    private void HandleMoodChanged(int newValue)
    {
        int delta = newValue - _cachedMood;
        _cachedMood = newValue;
        StatusPreviewSequencer.Instance.Enqueue(
            MoodPreviewOrder,
            () => SetMoodText(newValue),
            PreviewIfAllowed(textMoodPreview, delta),
            delta);
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
    //  Hover 預覽（靜態）：滑鼠懸停時常駐顯示 +X / -X，移開清掉。
    //  顯示 Trust / Interest / Libido / Mood 的數值預覽，不改變目前主顯示。
    //  由 StatPackagePreviewPresenter 呼叫。
    // ==========================================================

    /// <summary>目前面板顯示中的女主角 ID；面板未開啟時回 null。</summary>
    public string CurrentHeroineID
    {
        get
        {
            if (_currentModel == null) return null; // Hide() 會把 _currentModel 設回 null
            if (heroineOrder == null || _currentOrderIndex < 0 || _currentOrderIndex >= heroineOrder.Count)
                return null;
            return heroineOrder[_currentOrderIndex];
        }
    }

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
                case StatKind.Mood: SetStaticPreview(textMoodPreview, it.delta); break;
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

    // 靜態顯示：直接設 +X / -X 並把 alpha 拉滿（不透明），不做動畫。
    private static void SetStaticPreview(TextMeshProUGUI target, int delta)
    {
        if (target == null) return;
        target.text = delta > 0 ? $"+{delta}" : delta.ToString();
        var c = target.color;
        c.a = 1f;
        target.color = c;
        target.gameObject.SetActive(true);
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

    private void SetCanvasGroupVisible(bool visible)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
