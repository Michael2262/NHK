using UnityEngine;
using UnityEngine.UI;
using PixelCrushers.DialogueSystem;
using TMPro;
using DG.Tweening;

/// <summary>
/// 角色背景圖設定（用於 Inspector）
/// </summary>
[System.Serializable]
public class HeroineBackground
{
    [Tooltip("對應的 HeroineID")]
    public string heroineID;
    [Tooltip("該角色的背景圖")]
    public Sprite backgroundSprite;
}

/// <summary>
/// 女主角 Status 介面的主控腳本（單例）。
/// 
/// 版面配置（左→右）：
///   左側：H詳細資料（平時隱藏，arrow 展開）
///   中間：角色資訊 + Tab（與你的關係 / 基本資料 / H經驗）
///   右側：立繪區
/// </summary>
public class HeroineStatusUI : MonoBehaviour
{
    // ══════════════════════════════════════════
    //  Singleton
    // ══════════════════════════════════════════
    public static HeroineStatusUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
    }

    // ══════════════════════════════════════════
    //  Inspector 參考
    // ══════════════════════════════════════════

    [Header("根面板")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("順位列表 (依序填入 HeroineID)")]
    [SerializeField]
    private System.Collections.Generic.List<string> heroineOrder
        = new System.Collections.Generic.List<string>();

    // ── 右側：立繪區 ──
    [Header("── 右側：立繪區 ──")]
    [SerializeField] private GameObject inHeatIcon;      // 發情圖示
    [SerializeField] private GameObject enragedIcon;      // 盛怒圖示
    [SerializeField] private Image backgroundImage;       // 角色背景圖

    [Header("角色背景圖設定")]
    [Tooltip("每個角色對應的背景 Sprite。heroineID 必須與 heroineOrder 一致。")]
    [SerializeField]
    private System.Collections.Generic.List<HeroineBackground> heroineBackgrounds
        = new System.Collections.Generic.List<HeroineBackground>();

    // ── 中間上方：角色資訊 ──
    [Header("── 中間上方：角色資訊 ──")]
    [SerializeField] private TMP_Text nameText;                // 角色名稱
    [SerializeField] private TMP_Text lewdnessLevelText;       // "開發度 Lv.0"
    [SerializeField] private TMP_Text lewdnessExpText;         // "(125/500)"
    [SerializeField] private Slider lewdnessBar;               // 開發度經驗條
    [SerializeField] private TMP_Text excitementLevelText;     // "興奮度 Lv.0"
    [SerializeField] private TMP_Text excitementExpText;       // "(50/150)"
    [SerializeField] private Slider excitementBar;             // 興奮度條
    [SerializeField] private TMP_Text affinityLevelText;       // "好感度 Lv.0"
    [SerializeField] private TMP_Text affinityExpText;         // "(80/200)"
    [SerializeField] private Slider affinityBar;               // 好感度條
    [SerializeField] private TMP_Text suspicionText;           // 個人可疑度（超過 100 變紅）
    [SerializeField] private Color suspicionNormalColor = Color.white;
    [SerializeField] private Color suspicionDangerColor = Color.red;
    [SerializeField] private int suspicionDangerThreshold = 100;
    [SerializeField] private Button switchCharacterButton;     // 切換角色按鈕

    // ── 中間下方：Tab 切換 ──
    [Header("── 中間下方：Tab 切換 ──")]
    [SerializeField] private Button tabRelationship;           // Tab 1：與你的關係
    [SerializeField] private Button tabBasicInfo;              // Tab 2：基本資料
    [SerializeField] private Button tabHExperience;            // Tab 3：H經驗
    [SerializeField] private GameObject panelRelationship;     // Panel 1
    [SerializeField] private GameObject panelBasicInfo;        // Panel 2
    [SerializeField] private GameObject panelHExperience;      // Panel 3

    [Header("Tab 顏色設定")]
    [SerializeField] private Color tabActiveColor = new Color(0.816f, 0.816f, 0.816f, 1f);   // #D0D0D0
    [SerializeField] private Color tabInactiveColor = Color.white;                             // #FFFFFF

    // ── Tab 1：與你的關係 ──
    [Header("── Tab 1：與你的關係 ──")]
    [SerializeField] private TMP_Text relationSelfDesc;        // 兩人的關係（自述）
    [SerializeField] private TMP_Text relationHerView;         // 她對你的看法

    // ── Tab 2：基本資料 ──
    [Header("── Tab 2：基本資料 ──")]
    [SerializeField] private TMP_Text basicAge;
    [SerializeField] private TMP_Text basicRelationship;
    [SerializeField] private TMP_Text basicSexHistory;
    [SerializeField] private TMP_Text basicFirstPartner;
    [SerializeField] private TMP_Text basicHarassCount;
    [SerializeField] private TMP_Text basicBathCount;
    [SerializeField] private TMP_Text basicHarassToilet;
    [SerializeField] private TMP_Text basicHarassSofa;
    [SerializeField] private TMP_Text basicHarassBedroom;

    // ── Tab 3：H經驗 ──
    [Header("── Tab 3：H經驗 ──")]
    [SerializeField] private GameObject hexpContent;           // H經驗的實際內容容器
    [SerializeField] private GameObject hexpEmptyMessage;      // "尚無相關資料..." 的 GameObject
    [SerializeField] private TMP_Text hexpMostFreqLocation;
    [SerializeField] private TMP_Text hexpNightCrawl;
    [SerializeField] private TMP_Text hexpInitiatedByHer;
    [SerializeField] private TMP_Text hexpFamilyNearby;
    [SerializeField] private TMP_Text hexpTotalSex;
    [SerializeField] private TMP_Text hexpSexBedroom;
    [SerializeField] private TMP_Text hexpSexLivingRoom;
    [SerializeField] private TMP_Text hexpSexBathroom;
    [SerializeField] private TMP_Text hexpSexToilet;

    // ── 左側：H詳細資料（展開式） ──
    [Header("── 左側：H詳細資料（展開式） ──")]
    [SerializeField] private RectTransform leftAreaRect;       // 整個左側區塊的 RectTransform（DOTween 平移用）
    [SerializeField] private Button arrowButton;               // 展開/收合箭頭按鈕
    [SerializeField] private RectTransform arrowRect;          // 箭頭本身的 RectTransform（翻轉用）
    [SerializeField] private float slideDistance = 400f;       // 向左平移的距離
    [SerializeField] private float slideDuration = 0.3f;       // 動畫時長

    [Header("H詳細 — 圖片")]
    [SerializeField] private Image detailImage;                // 上方圖片（換 Sprite）
    [SerializeField] private Sprite[] detailPageSprites;       // 三頁對應的 Sprite（長度 3）
    [SerializeField] private Sprite detailVirginSprite;        // 處女狀態時顯示的圖片

    [Header("H詳細 — 第1頁：行為次數")]
    [SerializeField] private TMP_Text detailFacial;
    [SerializeField] private TMP_Text detailKiss;
    [SerializeField] private TMP_Text detailOralCreampie;
    [SerializeField] private TMP_Text detailExternalEjac;
    [SerializeField] private TMP_Text detailCreampie;

    [Header("H詳細 — 第2頁：液體量")]
    [SerializeField] private TMP_Text detailSwallowMl;
    [SerializeField] private TMP_Text detailExternalMl;
    [SerializeField] private TMP_Text detailCreampieMl;

    [Header("H詳細 — 第3頁：高潮/射精紀錄")]
    [SerializeField] private TMP_Text detailTotalOrgasm;
    [SerializeField] private TMP_Text detailMaxOrgasm;
    [SerializeField] private TMP_Text detailMaxConsecEjac;

    [Header("H詳細 — 頁面容器")]
    [SerializeField] private GameObject detailPage1;           // 行為次數容器
    [SerializeField] private GameObject detailPage2;           // 液體量容器
    [SerializeField] private GameObject detailPage3;           // 高潮/射精紀錄容器
    [SerializeField] private Button detailNextPageButton;      // 下一頁按鈕

    // ── 設定檔 ──
    [Header("── 設定檔參考 ──")]
    [SerializeField] private HeroineEvaluationConfig evaluationConfig;

    [Header("── 關閉按鈕 ──")]
    [SerializeField] private Button backButton;

    // ══════════════════════════════════════════
    //  內部狀態
    // ══════════════════════════════════════════
    private int _currentOrderIndex = 0;
    private string _previousHeroineID = null;
    private bool _isDetailExpanded = false;
    private int _detailPageIndex = 0;
    private Vector2 _leftAreaOriginalPos;
    private Tween _slideTween;

    // ══════════════════════════════════════════
    //  初始化
    // ══════════════════════════════════════════
    private void Start()
    {
        // Tab 按鈕事件
        tabRelationship.onClick.AddListener(() => SwitchTab(0));
        tabBasicInfo.onClick.AddListener(() => SwitchTab(1));
        tabHExperience.onClick.AddListener(() => SwitchTab(2));

        // 切換角色
        switchCharacterButton.onClick.AddListener(SwitchToNextHeroine);

        // H詳細資料展開/收合
        arrowButton.onClick.AddListener(ToggleDetailPanel);
        detailNextPageButton.onClick.AddListener(NextDetailPage);

        // 關閉
        backButton.onClick.AddListener(Close);

        // 記錄左側區塊原始位置
        _leftAreaOriginalPos = leftAreaRect.anchoredPosition;

        // 預設關閉
        SetCanvasGroupActive(false);
    }

    // ══════════════════════════════════════════
    //  公開 API：開啟 / 關閉
    // ══════════════════════════════════════════

    public void ShowByOrder(int orderIndex)
    {
        if (heroineOrder == null || heroineOrder.Count == 0)
        {
            Debug.LogWarning("[HeroineStatusUI] heroineOrder 為空，無法顯示。");
            return;
        }

        _currentOrderIndex = Mathf.Clamp(orderIndex, 0, heroineOrder.Count - 1);
        OpenInternal();
    }

    public void Show(string heroineID)
    {
        if (string.IsNullOrEmpty(heroineID))
        {
            Debug.LogWarning("[HeroineStatusUI] heroineID 為空。");
            return;
        }

        int idx = heroineOrder.IndexOf(heroineID);
        if (idx >= 0)
            _currentOrderIndex = idx;
        else
            Debug.LogWarning($"[HeroineStatusUI] heroineID '{heroineID}' 不在 heroineOrder 列表中。");

        OpenInternal();
    }

    private void OpenInternal()
    {
        // 先在不可見狀態下把 Tab / Detail 切好，避免打開瞬間閃一下
        SwitchTab(0);
        ResetDetailPanel();

        SetCanvasGroupActive(true);
        UpdateSwitchButtonVisibility();
        ShowTachie(CurrentHeroineID);
        RefreshAll();
    }

    public void Close()
    {
        _slideTween?.Kill();
        HideAllTachie();
        SetCanvasGroupActive(false);
    }

    public bool IsOpen => canvasGroup.alpha > 0f;

    private void SetCanvasGroupActive(bool active)
    {
        canvasGroup.alpha = active ? 1f : 0f;
        canvasGroup.interactable = active;
        canvasGroup.blocksRaycasts = active;
    }

    // ══════════════════════════════════════════
    //  角色切換
    // ══════════════════════════════════════════
    private void SwitchToNextHeroine()
    {
        if (heroineOrder == null || heroineOrder.Count <= 1) return;
        _currentOrderIndex = (_currentOrderIndex + 1) % heroineOrder.Count;
        ShowTachie(CurrentHeroineID);
        ResetDetailPanel();
        RefreshAll();
    }

    private void UpdateSwitchButtonVisibility()
    {
        if (switchCharacterButton != null)
            switchCharacterButton.gameObject.SetActive(heroineOrder.Count > 1);
    }

    private string CurrentHeroineID =>
        (heroineOrder != null && _currentOrderIndex < heroineOrder.Count)
            ? heroineOrder[_currentOrderIndex]
            : null;

    // ══════════════════════════════════════════
    //  Tab 切換
    // ══════════════════════════════════════════
    private void SwitchTab(int tabIndex)
    {
        panelRelationship.SetActive(tabIndex == 0);
        panelBasicInfo.SetActive(tabIndex == 1);
        panelHExperience.SetActive(tabIndex == 2);

        // 更新 Tab 按鈕顏色
        SetTabColor(tabRelationship, tabIndex == 0);
        SetTabColor(tabBasicInfo, tabIndex == 1);
        SetTabColor(tabHExperience, tabIndex == 2);
    }

    /// <summary>
    /// 設定 Tab 按鈕的背景顏色（活躍 = D0D0D0，非活躍 = 白色）。
    /// </summary>
    private void SetTabColor(Button tab, bool isActive)
    {
        var img = tab.GetComponent<Image>();
        if (img != null)
            img.color = isActive ? tabActiveColor : tabInactiveColor;
    }

    // ══════════════════════════════════════════
    //  H詳細資料：展開 / 收合 / 分頁
    // ══════════════════════════════════════════

    /// <summary>
    /// 重置 H詳細資料面板狀態（收合 + 回第1頁）。
    /// 切換角色或開啟面板時呼叫。
    /// </summary>
    private void ResetDetailPanel()
    {
        _slideTween?.Kill();
        _isDetailExpanded = false;
        _detailPageIndex = 0;
        leftAreaRect.anchoredPosition = _leftAreaOriginalPos;
        arrowRect.localScale = new Vector3(1f, 1f, 1f);

        // Arrow 顯示條件：H次數 >= 1
        UpdateArrowVisibility();

        // H次數為0時將圖片設為預設圖
        if (detailImage != null && detailVirginSprite != null)
        {
            if (GameStatusService.Instance.Heroines.TryGetValue(CurrentHeroineID, out var heroine)
                && heroine.HCount < 1)
            {
                detailImage.sprite = detailVirginSprite;
            }
        }
    }

    /// <summary>
    /// Arrow 只在角色非處女時顯示。
    /// </summary>
    private void UpdateArrowVisibility()
    {
        if (!GameStatusService.Instance.Heroines.TryGetValue(CurrentHeroineID, out var heroine))
        {
            arrowButton.gameObject.SetActive(false);
            return;
        }
        arrowButton.gameObject.SetActive(heroine.HCount >= 1);
    }

    /// <summary>
    /// 點擊箭頭：展開或收合 H詳細資料。
    /// </summary>
    private void ToggleDetailPanel()
    {
        _slideTween?.Kill();
        _isDetailExpanded = !_isDetailExpanded;

        Vector2 targetPos = _isDetailExpanded
            ? _leftAreaOriginalPos + new Vector2(-slideDistance, 0f)
            : _leftAreaOriginalPos;

        _slideTween = leftAreaRect.DOAnchorPos(targetPos, slideDuration).SetEase(Ease.OutCubic);

        // 翻轉箭頭
        float scaleX = _isDetailExpanded ? -1f : 1f;
        arrowRect.localScale = new Vector3(scaleX, 1f, 1f);

        // 展開時刷新詳細資料
        if (_isDetailExpanded)
        {
            SwitchDetailPage(_detailPageIndex);
            RefreshHDetail();
        }
    }

    /// <summary>
    /// 下一頁按鈕：到底回第1頁。
    /// </summary>
    private void NextDetailPage()
    {
        _detailPageIndex = (_detailPageIndex + 1) % 3;
        SwitchDetailPage(_detailPageIndex);
    }

    /// <summary>
    /// 切換 H詳細資料的分頁（0/1/2）並更新圖片。
    /// </summary>
    private void SwitchDetailPage(int pageIndex)
    {
        detailPage1.SetActive(pageIndex == 0);
        detailPage2.SetActive(pageIndex == 1);
        detailPage3.SetActive(pageIndex == 2);

        // 更新上方圖片
        if (detailImage != null && detailPageSprites != null && pageIndex < detailPageSprites.Length)
            detailImage.sprite = detailPageSprites[pageIndex];
    }

    // ══════════════════════════════════════════
    //  資料刷新（核心）
    // ══════════════════════════════════════════
    private void RefreshAll()
    {
        if (!GameStatusService.Instance.Heroines.TryGetValue(CurrentHeroineID, out var heroine)) return;
        var stats = heroine.Statistics;
        var cfg = GameStatusService.Instance.HeroineConfig;
        var stat = cfg.GetStat(CurrentHeroineID);

        // ── 右側：狀態圖示 ──
        inHeatIcon.SetActive(heroine.IsInHeat);
        enragedIcon.SetActive(heroine.IsEnraged);

        // ── 中間上方：角色資訊 ──
        nameText.text = Localize(heroine.NameTextKey);

        // 開發度
        int lewdLv = heroine.LewdnessLevel;
        int lewdExp = heroine.LewdnessExp;
        int lewdExpMax = GetLewdnessExpMax(cfg, lewdLv);
        lewdnessLevelText.text = LocalizeFormat("Status.LewdnessLevel", lewdLv);
        lewdnessExpText.text = $"({lewdExp}/{lewdExpMax})";
        lewdnessBar.maxValue = lewdExpMax;
        lewdnessBar.value = lewdExp;

        // 興奮度
        int excLv = heroine.TotalExcitementLevel;
        int excExp = heroine.BaseExcitementExp;
        int excExpMax = heroine.GetCurrentExcitementThreshold(heroine.BaseExcitementLevel);
        excitementLevelText.text = LocalizeFormat("Status.ExcitementLevel", excLv);
        excitementExpText.text = $"({excExp}/{excExpMax})";
        excitementBar.maxValue = excExpMax;
        excitementBar.value = excExp;

        // 好感度
        int affLv = heroine.BaseAffinityLevel;
        int affExp = heroine.BaseAffinityExp;
        int affExpMax = GetAffinityExpMax(cfg, affLv);
        affinityLevelText.text = LocalizeFormat("Status.AffinityLevel", affLv);
        affinityExpText.text = $"({affExp}/{affExpMax})";
        affinityBar.maxValue = affExpMax;
        affinityBar.value = affExp;

        // 個人可疑度（超過閾值變紅字）
        if (suspicionText != null)
        {
            int suspicion = heroine.PersonalSuspicion;
            suspicionText.text = suspicion.ToString();
            suspicionText.color = (suspicion > suspicionDangerThreshold)
                ? suspicionDangerColor
                : suspicionNormalColor;
        }

        // ── Tab 1：與你的關係 ──
        RefreshRelationship(heroine);

        // ── Tab 2：基本資料 ──
        RefreshBasicInfo(heroine, stats, stat);

        // ── Tab 3：H經驗 ──
        RefreshHExperience(heroine, stats);

        // ── 左側：H詳細資料（若已展開則刷新） ──
        if (_isDetailExpanded)
            RefreshHDetail();

        // ── Arrow 可見性 ──
        UpdateArrowVisibility();
    }

    // ──────────────────────────────────────────
    //  Tab 1：與你的關係
    // ──────────────────────────────────────────
    private void RefreshRelationship(HeroineStatusModel heroine)
    {
        // 兩人的關係（自述）— 依親密度查表
        string affinityKey = evaluationConfig?.GetAffinityEvaluation(CurrentHeroineID, heroine.BaseAffinityLevel);
        relationSelfDesc.text = !string.IsNullOrEmpty(affinityKey) ? Localize(affinityKey) : "---";

        // 她對你的看法 — 依開發度查表
        string lewdKey = evaluationConfig?.GetLewdnessEvaluation(CurrentHeroineID, heroine.LewdnessLevel);
        relationHerView.text = !string.IsNullOrEmpty(lewdKey) ? Localize(lewdKey) : "---";
    }

    // ──────────────────────────────────────────
    //  Tab 2：基本資料
    // ──────────────────────────────────────────
    private void RefreshBasicInfo(HeroineStatusModel heroine, HeroineStatisticsModel stats, HeroineStat stat)
    {
        basicAge.text = stat.Age.ToString();

        string relationKey = evaluationConfig?.GetRelationshipLabel(CurrentHeroineID, heroine.BaseAffinityLevel);
        basicRelationship.text = !string.IsNullOrEmpty(relationKey) ? Localize(relationKey) : "---";

        basicSexHistory.text = GetVirginityDisplayText(heroine.Virginity);

        if (heroine.Virginity == VirginityState.Virgin)
            basicFirstPartner.text = Localize("Status.None");
        else
            basicFirstPartner.text = !string.IsNullOrEmpty(stat.FirstPartnerNameKey) ? Localize(stat.FirstPartnerNameKey) : "---";

        basicHarassCount.text = FormatCount(stats.GetInt(HeroineStatisticType.SexualHarassmentCount));
        basicBathCount.text = FormatCount(stats.GetInt(HeroineStatisticType.BathTogetherCount));
        basicHarassToilet.text = FormatCount(stats.GetInt(HeroineStatisticType.HarassInToilet));
        basicHarassSofa.text = FormatCount(stats.GetInt(HeroineStatisticType.HarassOnSofa));
        basicHarassBedroom.text = FormatCount(stats.GetInt(HeroineStatisticType.HarassInBedroom));
    }

    // ──────────────────────────────────────────
    //  Tab 3：H經驗
    // ──────────────────────────────────────────
    private void RefreshHExperience(HeroineStatusModel heroine, HeroineStatisticsModel stats)
    {
        // H次數為0 → 顯示「尚無相關資料」，隱藏實際內容
        bool hasNoHExp = (heroine.HCount < 1);
        hexpContent.SetActive(!hasNoHExp);
        hexpEmptyMessage.SetActive(hasNoHExp);

        if (hasNoHExp) return; // 不需要填值

        var mostFreq = stats.GetMostFrequentSexLocation();
        hexpMostFreqLocation.text = mostFreq.HasValue
            ? Localize(GetLocationTextKey(mostFreq.Value))
            : Localize("Status.None");

        hexpNightCrawl.text = FormatCount(stats.GetInt(HeroineStatisticType.NightCrawlCount));
        hexpInitiatedByHer.text = FormatCount(stats.GetInt(HeroineStatisticType.InitiatedByHerCount));
        hexpFamilyNearby.text = FormatCount(stats.GetInt(HeroineStatisticType.SexWithFamilyNearby));
        hexpTotalSex.text = FormatCount(heroine.HCount);
        hexpSexBedroom.text = FormatCount(stats.GetInt(HeroineStatisticType.SexInBedroom));
        hexpSexLivingRoom.text = FormatCount(stats.GetInt(HeroineStatisticType.SexInLivingRoom));
        hexpSexBathroom.text = FormatCount(stats.GetInt(HeroineStatisticType.SexInBathroom));
        hexpSexToilet.text = FormatCount(stats.GetInt(HeroineStatisticType.SexInToilet));
    }

    // ──────────────────────────────────────────
    //  左側：H詳細資料（3 頁）
    // ──────────────────────────────────────────
    private void RefreshHDetail()
    {
        if (!GameStatusService.Instance.Heroines.TryGetValue(CurrentHeroineID, out var heroine)) return;
        var stats = heroine.Statistics;

        // 第1頁：行為次數
        detailFacial.text = LocalizeFormat("Status.CountTimes", stats.GetInt(HeroineStatisticType.FacialCount));
        detailKiss.text = LocalizeFormat("Status.CountTimes", stats.GetInt(HeroineStatisticType.KissCount));
        detailOralCreampie.text = LocalizeFormat("Status.CountTimes", stats.GetInt(HeroineStatisticType.OralCreampieCount));
        detailExternalEjac.text = LocalizeFormat("Status.CountTimes", stats.GetInt(HeroineStatisticType.ExternalEjaculationCount));
        detailCreampie.text = LocalizeFormat("Status.CountTimes", stats.GetInt(HeroineStatisticType.CreampieCount));

        // 第2頁：液體量
        detailSwallowMl.text = $"{stats.Get(HeroineStatisticType.SwallowedMl)}ml";
        detailExternalMl.text = $"{stats.Get(HeroineStatisticType.ExternalEjaculationMl)}ml";
        detailCreampieMl.text = $"{stats.Get(HeroineStatisticType.CreampieMl)}ml";

        // 第3頁：高潮/射精紀錄
        detailTotalOrgasm.text = FormatCount(stats.GetInt(HeroineStatisticType.TotalOrgasmCount));
        detailMaxOrgasm.text = FormatCount(stats.GetInt(HeroineStatisticType.MaxOrgasmInOneSession));
        detailMaxConsecEjac.text = FormatCount(stats.GetInt(HeroineStatisticType.MaxConsecutiveEjaculation));
    }

    // ══════════════════════════════════════════
    //  本地化工具
    // ══════════════════════════════════════════

    private string Localize(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        string result = DialogueManager.GetLocalizedText(key);
        return !string.IsNullOrEmpty(result) ? result : key;
    }

    private string LocalizeFormat(string key, object arg0)
    {
        string template = Localize(key);
        try { return string.Format(template, arg0); }
        catch { return template; }
    }

    private string FormatCount(int count)
    {
        return LocalizeFormat("Status.Count", count);
    }

    private string GetVirginityDisplayText(VirginityState state)
    {
        switch (state)
        {
            case VirginityState.Virgin: return Localize("Status.None");
            case VirginityState.NonVirginUnaware: return Localize("Status.SexHistoryUnaware");
            case VirginityState.NonVirgin: return Localize("Status.SexHistoryYes");
            default: return "---";
        }
    }

    private string GetLocationTextKey(HeroineStatisticType locationType)
    {
        switch (locationType)
        {
            case HeroineStatisticType.SexInBedroom: return "Location.Bedroom";
            case HeroineStatisticType.SexInLivingRoom: return "Location.LivingRoom";
            case HeroineStatisticType.SexInBathroom: return "Location.Bathroom";
            case HeroineStatisticType.SexInToilet: return "Location.Toilet";
            default: return "Status.None";
        }
    }

    private int GetLewdnessExpMax(HeroineStatusConfig cfg, int level)
    {
        if (cfg.lewdnessExpTable != null && level < cfg.lewdnessExpTable.Count)
            return cfg.lewdnessExpTable[level];
        return cfg.excitementExpPerLevel;
    }

    private int GetAffinityExpMax(HeroineStatusConfig cfg, int level)
    {
        // 親密度是從 Lv.1 起算，index = level - 1
        int idx = level - 1;
        if (cfg.affinityExpTable != null && idx >= 0 && idx < cfg.affinityExpTable.Count)
            return cfg.affinityExpTable[idx];
        return 100; // fallback
    }

    // ══════════════════════════════════════════
    //  立繪控制（透過 TachieController）
    // ══════════════════════════════════════════

    private const float TachiePositionX = 635f;

    /// <summary>
    /// 顯示指定角色的立繪，並隱藏前一個角色。
    /// 隱藏時：先 fade out → 隱藏狀態下回到中間。
    /// </summary>
    private void ShowTachie(string heroineID)
    {
        var tc = TachieController.Instance;
        if (tc == null || string.IsNullOrEmpty(heroineID)) return;

        // 隱藏前一個角色（若不同）
        if (!string.IsNullOrEmpty(_previousHeroineID)
            && !_previousHeroineID.Equals(heroineID, System.StringComparison.OrdinalIgnoreCase))
        {
            HideSingleTachie(_previousHeroineID);
        }

        // 先移到定位（隱藏狀態下），再 fade in
        tc.MoveToX(heroineID, TachiePositionX, 0f);
        tc.SetVisibility(heroineID, true);

        // 更新背景圖
        UpdateBackground(heroineID);

        _previousHeroineID = heroineID;
    }

    /// <summary>
    /// 隱藏單一角色：瞬間隱藏後回到中間位置。
    /// </summary>
    private void HideSingleTachie(string heroineID)
    {
        var tc = TachieController.Instance;
        if (tc == null || string.IsNullOrEmpty(heroineID)) return;

        tc.SetVisibility(heroineID, false, 0f); // duration=0 瞬間隱藏
        tc.MoveToX(heroineID, 0f, 0f);          // 隱藏後歸位
    }

    /// <summary>
    /// 隱藏所有立繪（關閉面板時呼叫）。
    /// </summary>
    private void HideAllTachie()
    {
        var tc = TachieController.Instance;
        if (tc == null) return;

        // 逐一瞬間隱藏+歸位，避免 ClearAll 的 fade 動畫讓移動被看到
        foreach (var id in heroineOrder)
        {
            tc.SetVisibility(id, false, 0f);
            tc.MoveToX(id, 0f, 0f);
        }
        _previousHeroineID = null;
    }

    // ══════════════════════════════════════════
    //  角色背景圖
    // ══════════════════════════════════════════

    /// <summary>
    /// 根據角色 ID 更新背景圖。
    /// 找不到設定時使用列表第一張作為預設。
    /// </summary>
    private void UpdateBackground(string heroineID)
    {
        if (backgroundImage == null || heroineBackgrounds == null || heroineBackgrounds.Count == 0) return;

        // 查找對應角色的背景
        Sprite targetSprite = null;
        for (int i = 0; i < heroineBackgrounds.Count; i++)
        {
            if (string.Equals(heroineBackgrounds[i].heroineID, heroineID, System.StringComparison.OrdinalIgnoreCase))
            {
                targetSprite = heroineBackgrounds[i].backgroundSprite;
                break;
            }
        }

        // 找不到則用第一張作為預設
        if (targetSprite == null)
            targetSprite = heroineBackgrounds[0].backgroundSprite;

        backgroundImage.sprite = targetSprite;
    }
}