using System.Collections.Generic;
using TMPro;
using UnityEngine;

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
/// 顯示三項資料：
///   1. Trust（信任）— 目前值 + Preview 變動量
///   2. Libido（性慾）— 目前值 + Preview 變動量
///   3. Affinity（好感度）— 目前值 + Preview 變動量
///
/// Affinity 特殊過渡：affinityRoot 這個 GameObject 一開始隱藏，
///   當某女主角的好感度第一次從 0 變成 >0 後才首次顯示，之後永久維持顯示。
///   解鎖狀態以 per-heroine 的 Persistent Flag 記錄（存檔後讀檔可還原）。
///
/// 注意：HeroineStatusModel 的 OnLibidoChanged / OnTrustChanged / OnAffinityChanged
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

    [Header("=== 好感度數值 ===")]
    [Tooltip("好感度整組的容器 GameObject。一開始隱藏；該女主角好感度第一次從 0 變成 >0 後首次顯示，之後永久維持（狀態存檔）。")]
    [SerializeField] private GameObject affinityRoot;
    [SerializeField] private TextMeshProUGUI textAffinity;
    [SerializeField] private TextMeshProUGUI textAffinityPreview;

    [Header("=== Preview 顯示設定 ===")]
    [Tooltip("正/負變動是否顯示飄字；播放間隔與淡入淡出時間改由 StatusPreviewSequencer 控制。")]
    [SerializeField] private bool showPositivePreview = true;
    [SerializeField] private bool showNegativePreview = true;

    [Header("=== 顯示設定（暫時性） ===")]
    [Tooltip("開啟後：女主角不顯示好感度（affinityRoot），" +
             "也不做好感度的數值/preview 更新、hover 預覽與首次解鎖顯示。")]
    [SerializeField] private bool hideAffinityDisplay = false;

    [Header("=== 按鈕綁定 ===")]
    [SerializeField] private UnityEngine.UI.Button nextButton;
    [SerializeField] private UnityEngine.UI.Button closeButton;

    // ==========================================================
    //  ProgressFlag 常數
    // ==========================================================

    // 0 = 隱藏；N > 0 = 顯示 heroineOrder[N-1]
    // 單一 Variable 同時記錄顯示狀態與角色順位，存檔後讀檔可自動還原
    private const string VAR_VISIBLE = "Value_System_HeroineUIVisible";

    // Affinity 顯示解鎖旗標（per-heroine）。key = 前綴 + HeroineID。
    // 女主角好感度第一次 0→>0 時寫入，之後 affinityRoot 永久顯示；存檔後讀檔可還原。
    private const string FLAG_AFFINITY_SHOWN_PREFIX = "Flag_System_AffinityShown_";

    // ==========================================================
    //  內部狀態
    // ==========================================================
    private int _currentOrderIndex = 0;
    private HeroineStatusModel _currentModel;

    // 用於計算 Preview delta（OnLibidoChanged / OnTrustChanged / OnAffinityChanged 傳入的是 newValue）
    private int _cachedTrust;
    private int _cachedLibido;
    private int _cachedAffinity;

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
        _cachedAffinity = _currentModel.Affinity;

        // 好感度顯示：hideAffinityDisplay 時一律隱藏；否則「已解鎖（曾 0→>0）或目前已有數值」才顯示。
        // 目前已有數值卻還沒寫解鎖旗標時，補寫一次（相容舊檔 / 非顯示中才增值的情況）。
        if (hideAffinityDisplay)
        {
            SetAffinityRootVisible(false);
        }
        else
        {
            bool affinityUnlocked = IsAffinityUnlocked(_currentModel.HeroineID) || _currentModel.Affinity > 0;
            if (_currentModel.Affinity > 0) MarkAffinityUnlocked(_currentModel.HeroineID);
            SetAffinityRootVisible(affinityUnlocked);
        }

        SubscribeToModel();
        RefreshAllUI();
    }

    // ==========================================================
    //  Affinity 顯示解鎖（per-heroine，Persistent Flag）
    // ==========================================================

    private static string AffinityShownFlagKey(string heroineID) => FLAG_AFFINITY_SHOWN_PREFIX + heroineID;

    private bool IsAffinityUnlocked(string heroineID)
    {
        var flags = GameStatusService.Instance?.ProgressFlags;
        return flags != null
            && !string.IsNullOrEmpty(heroineID)
            && flags.Contains(AffinityShownFlagKey(heroineID));
    }

    private void MarkAffinityUnlocked(string heroineID)
    {
        var flags = GameStatusService.Instance?.ProgressFlags;
        if (flags == null || string.IsNullOrEmpty(heroineID)) return;
        flags.AddPersistentFlag(AffinityShownFlagKey(heroineID));
    }

    private void SetAffinityRootVisible(bool visible)
    {
        if (affinityRoot != null && affinityRoot.activeSelf != visible)
            affinityRoot.SetActive(visible);
    }

    // ==========================================================
    //  事件訂閱 / 退訂
    // ==========================================================

    private void SubscribeToModel()
    {
        if (_currentModel == null) return;
        _currentModel.OnTrustChanged += HandleTrustChanged;
        _currentModel.OnLibidoChanged += HandleLibidoChanged;
        _currentModel.OnAffinityChanged += HandleAffinityChanged;
    }

    private void UnsubscribeFromModel()
    {
        if (_currentModel == null) return;
        _currentModel.OnTrustChanged -= HandleTrustChanged;
        _currentModel.OnLibidoChanged -= HandleLibidoChanged;
        _currentModel.OnAffinityChanged -= HandleAffinityChanged;
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

    private void HandleAffinityChanged(int newValue)
    {
        // 暫時性：關閉好感度顯示時，不更新數值/preview、也不觸發首次解鎖顯示。
        // 仍同步快取，讓之後若重新開啟顯示時 delta 計算正確。
        if (hideAffinityDisplay) { _cachedAffinity = newValue; return; }

        int delta = newValue - _cachedAffinity;

        // 第一次從 0 變成 >0：解鎖顯示（永久，存檔），並讓 affinityRoot 首次現身。
        if (_cachedAffinity == 0 && newValue > 0)
        {
            MarkAffinityUnlocked(_currentModel != null ? _currentModel.HeroineID : null);
            SetAffinityRootVisible(true);
        }

        _cachedAffinity = newValue;
        StatusPreviewSequencer.Instance.Enqueue(
            StatusPreviewSequencer.OrderHeroineAffinity,
            () => SetAffinityText(newValue),   // 主數值顯示 Lv.X；Preview 飄字仍為 +X/-X
            PreviewIfAllowed(textAffinityPreview, delta),
            delta);
    }

    // ==========================================================
    //  UI 刷新
    // ==========================================================

    private void RefreshAllUI()
    {
        UpdateTrustUI();
        UpdateLibidoUI();
        UpdateAffinityUI();
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

    private void UpdateAffinityUI()
    {
        if (_currentModel == null) return;
        SetAffinityText(_currentModel.Affinity);
    }

    // 好感度主數值固定加 "Lv." 前綴（Preview 飄字不加，維持 +X/-X）。
    private void SetAffinityText(int value)
    {
        if (textAffinity != null) textAffinity.text = $"Lv.{value}";
    }

    // ==========================================================
    //  Preview 顯示（參考 LobbyUI_V2 寫法）
    // ==========================================================

    // ==========================================================
    //  Hover 預覽（靜態）：滑鼠懸停時常駐顯示 +X / -X，移開清掉。
    //  只處理女主角三項（Trust / Libido / Affinity），對象為目前顯示中的女主角。
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
                case StatKind.Affinity:
                    if (!hideAffinityDisplay) SetStaticPreview(textAffinityPreview, it.delta);
                    break;
                // 其餘（主角數值）由 LobbyUI_V2 處理，這裡忽略。
            }
        }
    }

    /// <summary>清掉女主角的 hover 預覽字。</summary>
    public void ClearStatPreview()
    {
        HidePreviewText(textTrustPreview);
        HidePreviewText(textLibidoPreview);
        HidePreviewText(textAffinityPreview);
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
        HidePreviewText(textAffinityPreview);
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