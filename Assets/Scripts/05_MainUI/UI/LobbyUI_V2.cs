using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using PixelCrushers;

// 職責：Lobby 場景的 Presenter/View (NHK 版)
// 主要顯示：星期、天數、Phase 名稱、金錢、主角核心數值。
// NHK 主角核心數值：Stress / LifePower / Sociality / Dependency。
// 注意：ProtagonistStatusModel.OnXXXChanged(int) 傳入的是「delta」，不是新值。
public class LobbyUI_V2 : MonoBehaviour
{
    public static LobbyUI_V2 Instance { get; private set; }

    // ==================================================
    // 主要顯示 - 左側面板 (Time)
    // ==================================================
    [Header("=== 主要顯示 - 左側面板 ===")]
    [SerializeField] private TextMeshProUGUI textDayOfWeek;
    [SerializeField] private TextMeshProUGUI textDayNumber;
    [SerializeField] private TextMeshProUGUI textPhaseName;

    // ==================================================
    // 主要顯示 - 右側面板 (Status)
    // ==================================================
    [Header("=== 主要顯示 - 右側面板 ===")]
    [SerializeField] private TextMeshProUGUI textMoney;

    [Header("=== 主角核心數值 ===")]
    [Tooltip("壓力數值。若未指定，會 fallback 使用舊 textSuspicion 欄位。")]
    [SerializeField] private TextMeshProUGUI textStress;
    [Tooltip("壓力標題。若未指定，會 fallback 使用舊 textSuspicionTitle 欄位。")]
    [SerializeField] private TextMeshProUGUI textStressTitle;
    [SerializeField] private TextMeshProUGUI textStressPreview;

    [Header("=== 壓力標題顯示規則 ===")]
    [Tooltip("OverStress 條件 Flag。此 Flag 成立時，壓力標題改顯示 OverStress（優先度最高）。\n" +
             "優先度：OverStress Flag > BadHealthy > 一般 Stress。")]
    [SerializeField] private ProgressFlagDefinition overStressFlag;

    [SerializeField] private TextMeshProUGUI textLifePower;
    [SerializeField] private TextMeshProUGUI textLifePowerPreview;

    [SerializeField] private TextMeshProUGUI textSociality;
    [SerializeField] private TextMeshProUGUI textSocialityPreview;

    [Tooltip("房間整潔度顯示（百分比）。由髒亂度反向換算。")]
    [SerializeField] private TextMeshProUGUI textRoomCleanLevel;
    [Tooltip("房間整潔度變動飄字（顯示 +N% / -N%）。")]
    [SerializeField] private TextMeshProUGUI textRoomCleanPreview;

    [Tooltip("身體整潔度顯示（百分比）。由身體髒污度反向換算。")]
    [SerializeField] private TextMeshProUGUI textBodyCleanLevel;
    [Tooltip("身體整潔度變動飄字（顯示 +N% / -N%）。")]
    [SerializeField] private TextMeshProUGUI textBodyCleanPreview;

    [SerializeField] private TextMeshProUGUI textDependency;
    [Tooltip("依賴度標題。平常顯示 Dependency；OverDependency Flag 成立時改顯示 OverDependency 並轉粉色。")]
    [SerializeField] private TextMeshProUGUI textDependencyTitle;
    [SerializeField] private TextMeshProUGUI textDependencyPreview;

    [Header("=== 依賴度標題顯示規則 ===")]
    [Tooltip("OverDependency 條件 Flag。此 Flag 成立時，依賴度標題改顯示 OverDependency 並轉粉色，依賴度數字也轉粉色。")]
    [SerializeField] private ProgressFlagDefinition overDependencyFlag;

    [Header("=== Preview 顯示設定 ===")]
    [Tooltip("正/負變動是否顯示飄字；播放間隔與淡入淡出時間改由 StatusPreviewSequencer 控制。")]
    [SerializeField] private bool showPositivePreview = true;
    [SerializeField] private bool showNegativePreview = true;
    [Tooltip("開啟後：主角『生活力』的變動不跳飄字，hover 預先預覽也不顯示（數字本體仍會更新）。")]
    [SerializeField] private bool hideLifePowerPreview = false;
    [Tooltip("開啟後：主角『社會性』的變動不跳飄字，hover 預先預覽也不顯示（數字本體仍會更新）。")]
    [SerializeField] private bool hideSocialityPreview = false;
    [Tooltip("開啟後：『房間整潔度』的變動不跳飄字，hover 預先預覽也不顯示（數字本體仍會更新）。")]
    [SerializeField] private bool hideRoomCleanPreview = false;
    [Tooltip("開啟後：『身體整潔度』的變動不跳飄字，hover 預先預覽也不顯示（數字本體仍會更新）。")]
    [SerializeField] private bool hideBodyCleanPreview = false;
    // 數字本體與飄字皆透過 StatusPreviewSequencer 依序播放（每隔固定秒數逐一跳）。

    // ==================================================
    // 舊欄位相容：如果 Inspector 還接在舊 Suspicion 欄位，會拿來顯示 Stress。
    // ==================================================
    [Space(10)]
    [Header("=== 舊欄位相容：Suspicion 位置改顯示 Stress ===")]
    [SerializeField] private TextMeshProUGUI textSuspicion;
    [SerializeField] private TextMeshProUGUI textSuspicionTitle;

    [Header("舊 Suspicion 倒數面板（NHK 版預設不用）")]
    [SerializeField] private GameObject suspicionDaysPanel;
    [SerializeField] private TextMeshProUGUI textSuspicionDays;

    // ==================================================
    // Debug 用欄位 (可隱藏)
    // ==================================================
    [Space(10)]
    [Header("=== Debug 用欄位 (可隱藏) ===")]
    [SerializeField] private TextMeshProUGUI textTimeOfDay;
    [SerializeField] private TextMeshProUGUI textDebugDayPhaseSlot;

    // ==================================================
    // Config
    // ==================================================
    [Space(10)]
    [Header("=== Config ===")]
    [SerializeField] private TimeMappingSO timeMapping;

    private ProtagonistStatusModel _protagonistModel;
    private TimeSystemModel _timeModel;
    private ProgressFlagModel _progressFlagModel;
    private int _lastPhaseIndex = -1;

    // 整潔度飄字用：事件傳來的是「髒亂度 delta」，這裡改用「整潔度百分比」的前後差來顯示 +N% / -N%。
    private int _cachedRoomClean;
    private int _cachedBodyClean;

    // 壓力標題的 Text Table Key（優先度由高到低：OverStress > BadHealthy > Stress）
    private const string TITLE_KEY_BAD_HEALTHY = "BadHealthy";
    private const string TITLE_KEY_OVER_STRESS = "OverStress";
    private const string TITLE_KEY_STRESS = "Stress";

    // 依賴度標題的 Text Table Key（OverDependency Flag 成立時改用 OverDependency）
    private const string TITLE_KEY_OVER_DEPENDENCY = "OverDependency";
    private const string TITLE_KEY_DEPENDENCY = "Dependency";

    // 警示變色用：記住未觸發警示時的原始顏色（只擷取一次），觸發時改用 UIColorPalette 的色票。
    private bool _alertColorsCaptured;
    private Color _stressDefaultColor = Color.white;
    private Color _stressTitleDefaultColor = Color.white;
    private Color _dependencyDefaultColor = Color.white;
    private Color _dependencyTitleDefaultColor = Color.white;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("重複的 LobbyUI_V2，已銷毀。");
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        TryBindModels();
        SubscribeEvents();
        InitializeAllUI();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        StatusPreviewSequencer.CancelAllIfExists();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void TryBindModels()
    {
        if (GameStatusService.Instance == null) return;
        _protagonistModel = GameStatusService.Instance.Protagonist;
        _timeModel = GameStatusService.Instance.Time;
        _progressFlagModel = GameStatusService.Instance.ProgressFlags;
    }

    private void SubscribeEvents()
    {
        if (_protagonistModel != null)
        {
            _protagonistModel.OnStressChanged += HandleStressChanged;
            _protagonistModel.OnLifePowerChanged += HandleLifePowerChanged;
            _protagonistModel.OnSocialityChanged += HandleSocialityChanged;
            _protagonistModel.OnDependencyChanged += HandleDependencyChanged;
            _protagonistModel.OnRoomMessLevelChanged += HandleRoomMessLevelChanged;
            _protagonistModel.OnBodyDirtyLevelChanged += HandleBodyDirtyLevelChanged;
            _protagonistModel.OnMoneyChanged += HandleMoneyChanged;
            _protagonistModel.OnBadHealthyChanged += HandleBadHealthyChanged;
        }

        if (_progressFlagModel != null)
            _progressFlagModel.OnFlagChanged += HandleProgressFlagChanged;

        if (_timeModel != null)
        {
            _timeModel.OnDayPassed += HandleDayPassed;
            _timeModel.OnPhaseChanged += HandlePhaseChange;
            _timeModel.OnTimeSlotAdvanced += HandleTimeSlotChanged;
        }

        if (GameStatusService.Instance != null)
            GameStatusService.Instance.OnGameStatusLoaded += HandleGameStatusLoaded;
    }

    private void UnsubscribeEvents()
    {
        if (_protagonistModel != null)
        {
            _protagonistModel.OnStressChanged -= HandleStressChanged;
            _protagonistModel.OnLifePowerChanged -= HandleLifePowerChanged;
            _protagonistModel.OnSocialityChanged -= HandleSocialityChanged;
            _protagonistModel.OnDependencyChanged -= HandleDependencyChanged;
            _protagonistModel.OnRoomMessLevelChanged -= HandleRoomMessLevelChanged;
            _protagonistModel.OnBodyDirtyLevelChanged -= HandleBodyDirtyLevelChanged;
            _protagonistModel.OnMoneyChanged -= HandleMoneyChanged;
            _protagonistModel.OnBadHealthyChanged -= HandleBadHealthyChanged;
        }

        if (_progressFlagModel != null)
            _progressFlagModel.OnFlagChanged -= HandleProgressFlagChanged;

        if (_timeModel != null)
        {
            _timeModel.OnDayPassed -= HandleDayPassed;
            _timeModel.OnPhaseChanged -= HandlePhaseChange;
            _timeModel.OnTimeSlotAdvanced -= HandleTimeSlotChanged;
        }

        if (GameStatusService.Instance != null)
            GameStatusService.Instance.OnGameStatusLoaded -= HandleGameStatusLoaded;
    }

    private void HandleGameStatusLoaded()
    {
        UnsubscribeEvents();
        TryBindModels();
        SubscribeEvents();
        InitializeAllUI();
    }

    private void InitializeAllUI()
    {
        _lastPhaseIndex = -1;

        // 初始化整潔度快取（避免第一次事件觸發時 delta 計算錯誤）
        if (_protagonistModel != null)
        {
            _cachedRoomClean = _protagonistModel.RoomCleanPercent;
            _cachedBodyClean = _protagonistModel.BodyCleanPercent;
        }

        // 兼容舊欄位：若新 stress 欄位沒接，就使用舊 suspicion 欄位。
        if (textStress == null) textStress = textSuspicion;
        if (textStressTitle == null) textStressTitle = textSuspicionTitle;

        // 先擷取原始顏色，再更新標題（UpdateXXXTitleUI 會套警示色，順序不能反）
        CaptureAlertDefaultColors();
        UpdateStressTitleUI();
        UpdateDependencyTitleUI();

        HideAllPreviewTexts();
        if (suspicionDaysPanel != null) suspicionDaysPanel.SetActive(false);

        UpdateMoneyUI();
        UpdateCoreStatusUI();
        UpdateTimeUI();
        UpdatePhaseNameUI();
    }

    // ==================================================
    // Protagonist event handlers
    // ProtagonistStatusModel 的 OnXXXChanged 傳進來的是 delta。
    // ==================================================

    private void HandleStressChanged(int delta)
    {
        int v = _protagonistModel != null ? _protagonistModel.Stress : 0;
        StatusPreviewSequencer.Instance.Enqueue(
            StatusPreviewSequencer.OrderStress,
            () => { SetText(textStress, v); ApplyStressColor(); },
            PreviewIfAllowed(textStressPreview, delta),
            delta);
    }

    private void HandleLifePowerChanged(int delta)
    {
        int v = _protagonistModel != null ? _protagonistModel.LifePower : 0;
        if (hideLifePowerPreview)
        {
            SetText(textLifePower, v); // 有勾：不進 Sequencer、不排隊也不等間隔，數字即時更新
            return;
        }
        StatusPreviewSequencer.Instance.Enqueue(
            StatusPreviewSequencer.OrderLifePower,
            () => SetText(textLifePower, v),
            PreviewIfAllowed(textLifePowerPreview, delta),
            delta);
    }

    private void HandleSocialityChanged(int delta)
    {
        int v = _protagonistModel != null ? _protagonistModel.Sociality : 0;
        if (hideSocialityPreview)
        {
            SetText(textSociality, v); // 有勾：不進 Sequencer、不排隊也不等間隔，數字即時更新
            return;
        }
        StatusPreviewSequencer.Instance.Enqueue(
            StatusPreviewSequencer.OrderSociality,
            () => SetText(textSociality, v),
            PreviewIfAllowed(textSocialityPreview, delta),
            delta);
    }

    private void HandleDependencyChanged(int delta)
    {
        int v = _protagonistModel != null ? _protagonistModel.Dependency : 0;
        StatusPreviewSequencer.Instance.Enqueue(
            StatusPreviewSequencer.OrderDependency,
            () => { SetText(textDependency, v); ApplyDependencyColor(); },
            PreviewIfAllowed(textDependencyPreview, delta),
            delta);
    }

    // 事件傳來的是「髒亂度 delta」；顯示改用「整潔度百分比」的前後差，飄字為 +N% / -N%。
    private void HandleRoomMessLevelChanged(int messDelta)
    {
        if (_protagonistModel == null) return;
        int newClean = _protagonistModel.RoomCleanPercent;
        int cleanDelta = newClean - _cachedRoomClean;
        _cachedRoomClean = newClean;
        if (hideRoomCleanPreview)
        {
            UpdateRoomCleanUI(); // 有勾：不進 Sequencer、不排隊也不等間隔，數字即時更新
            return;
        }
        EnqueueCleanPreview(StatusPreviewSequencer.OrderRoomClean, textRoomCleanPreview, cleanDelta, UpdateRoomCleanUI);
    }

    private void HandleBodyDirtyLevelChanged(int dirtyDelta)
    {
        if (_protagonistModel == null) return;
        int newClean = _protagonistModel.BodyCleanPercent;
        int cleanDelta = newClean - _cachedBodyClean;
        _cachedBodyClean = newClean;
        if (hideBodyCleanPreview)
        {
            UpdateBodyCleanUI(); // 有勾：不進 Sequencer、不排隊也不等間隔，數字即時更新
            return;
        }
        EnqueueCleanPreview(StatusPreviewSequencer.OrderBodyClean, textBodyCleanPreview, cleanDelta, UpdateBodyCleanUI);
    }

    // 整潔度飄字：格式為 +N% / -N%（單位是百分比，跟一般數值的飄字不同，所以走 EnqueueText 自訂文字）。
    // 被正/負顯示設定擋掉時仍會更新數字本體，只是不冒飄字。
    private void EnqueueCleanPreview(int order, TextMeshProUGUI preview, int cleanDelta, Action applyValue)
    {
        if (cleanDelta == 0)
        {
            applyValue?.Invoke();
            return;
        }
        TextMeshProUGUI target = PreviewIfAllowed(preview, cleanDelta);
        string text = target != null ? (cleanDelta > 0 ? $"+{cleanDelta}" : cleanDelta.ToString()) : null;
        StatusPreviewSequencer.Instance.EnqueueText(order, target, text, applyValue);
    }

    private void HandleMoneyChanged(int delta)
    {
        UpdateMoneyUI();
    }

    private void HandleBadHealthyChanged(bool _)
    {
        UpdateStressTitleUI();
    }

    // 監聽 OverStress / OverDependency 條件 Flag 的變化；含清桶（換日/換場景）時發出的 false 事件。
    // Flag 切換時：對應標題文字/顏色 + 數字顏色一起刷新。
    private void HandleProgressFlagChanged(string flagID, bool _)
    {
        if (overStressFlag != null && flagID == overStressFlag.FlagID)
        {
            UpdateStressTitleUI();
            ApplyStressColor();
        }

        if (overDependencyFlag != null && flagID == overDependencyFlag.FlagID)
        {
            UpdateDependencyTitleUI();
            ApplyDependencyColor();
        }
    }

    private void HandleTimeSlotChanged(int _) => UpdateTimeUI();

    private void HandlePhaseChange()
    {
        UpdateTimeUI();
        UpdatePhaseNameUI();
    }

    private void HandleDayPassed()
    {
        UpdateTimeUI();
        UpdatePhaseNameUI();
    }

    // ==================================================
    // UI updates
    // ==================================================

    private void UpdateMoneyUI()
    {
        if (textMoney != null && _protagonistModel != null)
            textMoney.text = $"{_protagonistModel.Money:N0}";
    }

    private void UpdateCoreStatusUI()
    {
        UpdateStressUI();
        UpdateLifePowerUI();
        UpdateSocialityUI();
        UpdateDependencyUI();
        UpdateRoomCleanUI();
        UpdateBodyCleanUI();
    }

    // 整潔度＝由髒亂度反向換算的百分比（Model.RoomCleanPercent），顯示為「NN%」。
    private void UpdateRoomCleanUI()
    {
        if (textRoomCleanLevel != null && _protagonistModel != null)
            textRoomCleanLevel.text = _protagonistModel.RoomCleanPercent.ToString();
    }

    // 身體整潔度＝由身體髒污度反向換算的百分比（Model.BodyCleanPercent），顯示為「NN%」。
    private void UpdateBodyCleanUI()
    {
        if (textBodyCleanLevel != null && _protagonistModel != null)
            textBodyCleanLevel.text = _protagonistModel.BodyCleanPercent.ToString();
    }

    /// <summary>
    /// 壓力標題三規則（優先度高→低）：
    /// 1. overStressFlag 成立     → "OverStress"（Red）
    /// 2. BadHealthy == true      → "BadHealthy"（DarkRed）
    /// 3. 平常                    → "Stress"（原始顏色）
    /// 每次都重新查表，語言重進場景後會跟著刷新。
    /// </summary>
    private void UpdateStressTitleUI()
    {
        if (textStressTitle == null) return;

        string key = TITLE_KEY_STRESS;
        Color color = _stressTitleDefaultColor;

        if (IsOverStressFlagActive())
        {
            key = TITLE_KEY_OVER_STRESS;
            color = UIColorPalette.Red;
        }
        else if (_protagonistModel != null && _protagonistModel.BadHealthy)
        {
            key = TITLE_KEY_BAD_HEALTHY;
            color = UIColorPalette.DarkRed;
        }

        textStressTitle.text = Localize(key);
        textStressTitle.color = color;
    }

    /// <summary>
    /// 依賴度標題兩規則：
    /// 1. overDependencyFlag 成立 → "OverDependency"（Pink）
    /// 2. 平常                    → "Dependency"（原始顏色）
    /// 每次都重新查表，語言重進場景後會跟著刷新。
    /// </summary>
    private void UpdateDependencyTitleUI()
    {
        if (textDependencyTitle == null) return;

        string key = TITLE_KEY_DEPENDENCY;
        Color color = _dependencyTitleDefaultColor;

        if (IsOverDependencyFlagActive())
        {
            key = TITLE_KEY_OVER_DEPENDENCY;
            color = UIColorPalette.Pink;
        }

        textDependencyTitle.text = Localize(key);
        textDependencyTitle.color = color;
    }

    // 標準程式端動態查表（見 CLAUDE.md「UI 多語系」）
    private string Localize(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        string text = PixelCrushers.DialogueSystem.DialogueManager.GetLocalizedText(key);
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning($"[LobbyUI_V2] Text Table 找不到 Key: {key}");
            return key;
        }
        return text;
    }

    private void UpdateStressUI()
    {
        if (_protagonistModel == null) return;
        if (textStress != null)
            textStress.text = _protagonistModel.Stress.ToString();
        ApplyStressColor();
    }

    private void UpdateLifePowerUI()
    {
        if (textLifePower != null && _protagonistModel != null)
            textLifePower.text = _protagonistModel.LifePower.ToString();
    }

    private void UpdateSocialityUI()
    {
        if (textSociality != null && _protagonistModel != null)
            textSociality.text = _protagonistModel.Sociality.ToString();
    }

    private void UpdateDependencyUI()
    {
        if (_protagonistModel == null) return;
        if (textDependency != null)
            textDependency.text = _protagonistModel.Dependency.ToString();
        ApplyDependencyColor();
    }

    // ==================================================
    // 警示變色：
    // - Stress 數字：OverStress Flag 成立時變 Red，解除時還原。
    // - Dependency 數字：OverDependency Flag 成立時變 Pink，解除時還原。
    // 顏色取自 UIColorPalette（靜態色票）；未觸發時還原成原始顏色。
    // ==================================================

    // 只擷取一次原始顏色，避免在已變色狀態下把警示色誤存成「預設色」。
    private void CaptureAlertDefaultColors()
    {
        if (_alertColorsCaptured) return;
        if (textStress != null) _stressDefaultColor = textStress.color;
        if (textStressTitle != null) _stressTitleDefaultColor = textStressTitle.color;
        if (textDependency != null) _dependencyDefaultColor = textDependency.color;
        if (textDependencyTitle != null) _dependencyTitleDefaultColor = textDependencyTitle.color;
        _alertColorsCaptured = true;
    }

    private void ApplyStressColor()
    {
        if (textStress == null) return;
        textStress.color = IsOverStressFlagActive()
            ? UIColorPalette.Red
            : _stressDefaultColor;
    }

    private bool IsOverStressFlagActive()
    {
        return overStressFlag != null && _progressFlagModel != null
            && _progressFlagModel.Contains(overStressFlag.FlagID);
    }

    private void ApplyDependencyColor()
    {
        if (textDependency == null) return;
        textDependency.color = IsOverDependencyFlagActive()
            ? UIColorPalette.Pink
            : _dependencyDefaultColor;
    }

    private bool IsOverDependencyFlagActive()
    {
        return overDependencyFlag != null && _progressFlagModel != null
            && _progressFlagModel.Contains(overDependencyFlag.FlagID);
    }

    private void HideAllPreviewTexts()
    {
        HidePreviewText(textStressPreview);
        HidePreviewText(textLifePowerPreview);
        HidePreviewText(textSocialityPreview);
        HidePreviewText(textDependencyPreview);
        HidePreviewText(textRoomCleanPreview);
        HidePreviewText(textBodyCleanPreview);
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

    // ==================================================
    // Time UI
    // ==================================================

    private void UpdateTimeUI()
    {
        if (_timeModel == null) return;

        int displayDay = _timeModel.DayIndex;

        if (timeMapping != null && timeMapping.ShouldShowNextDay(_timeModel.CurrentPhaseIndex, _timeModel.CurrentSlotInPhase))
        {
            displayDay += 1;
        }

        if (textDayNumber != null)
            textDayNumber.text = $"DAY {displayDay}";

        if (textDayOfWeek != null)
            textDayOfWeek.text = GetDayOfWeekAbbreviation(_timeModel.GetDayOfWeekForDay(displayDay));

        if (textDebugDayPhaseSlot != null)
            textDebugDayPhaseSlot.text = $"D: {_timeModel.DayIndex} / P: {_timeModel.CurrentPhaseIndex} / S: {_timeModel.CurrentSlotInPhase}";

        if (textTimeOfDay != null)
        {
            if (timeMapping != null)
                textTimeOfDay.text = timeMapping.GetTimeDisplay(_timeModel.CurrentPhaseIndex, _timeModel.CurrentSlotInPhase);
            else
                textTimeOfDay.text = "Missing SO";
        }
    }

    private void UpdatePhaseNameUI()
    {
        if (textPhaseName == null || timeMapping == null || _timeModel == null) return;

        int currentPhase = _timeModel.CurrentPhaseIndex;
        if (currentPhase == _lastPhaseIndex) return;
        _lastPhaseIndex = currentPhase;

        string localizationKey = timeMapping.GetPhaseLocalizationKey(currentPhase);
        if (string.IsNullOrEmpty(localizationKey))
        {
            textPhaseName.text = "???";
            return;
        }

        string localizedText = localizationKey;
        if (UILocalizationManager.instance != null)
        {
            string translated = UILocalizationManager.instance.GetLocalizedText(localizationKey);
            if (!string.IsNullOrEmpty(translated)) localizedText = translated;
        }

        textPhaseName.text = localizedText;
    }

    private string GetDayOfWeekAbbreviation(DayOfWeek day)
    {
        switch (day)
        {
            case DayOfWeek.Monday: return "MON";
            case DayOfWeek.Tuesday: return "TUE";
            case DayOfWeek.Wednesday: return "WED";
            case DayOfWeek.Thursday: return "THU";
            case DayOfWeek.Friday: return "FRI";
            case DayOfWeek.Saturday: return "SAT";
            case DayOfWeek.Sunday: return "SUN";
            default: return "???";
        }
    }

    // ==================================================
    // Hover 預覽（靜態）：滑鼠懸停時常駐顯示 +X / -X，移開清掉。
    // 不走 StatusPreviewSequencer（那是「數值真的變動」的飄字），
    // 這裡直接把字塞進 preview 欄位並拉滿 alpha（不透明）。
    // 由 StatPackagePreviewPresenter 呼叫，只處理主角四項。
    // ==================================================

    /// <summary>顯示主角數值的 hover 預覽（會先清掉上一輪）。</summary>
    public void ShowStatPreview(IReadOnlyList<StatPreviewItem> items)
    {
        ClearStatPreview();
        if (items == null) return;

        foreach (var it in items)
        {
            switch (it.kind)
            {
                case StatKind.Stress: SetStaticPreview(textStressPreview, it.delta); break;
                case StatKind.LifePower: if (!hideLifePowerPreview) SetStaticPreview(textLifePowerPreview, it.delta); break;
                case StatKind.Sociality: if (!hideSocialityPreview) SetStaticPreview(textSocialityPreview, it.delta); break;
                case StatKind.Dependency: SetStaticPreview(textDependencyPreview, it.delta); break;
                // Room / Body：套組給的是「髒污 delta」，換算成「整潔度 %」再顯示（+N% / -N%）。
                case StatKind.RoomMessLevel:
                    if (!hideRoomCleanPreview)
                        SetStaticPreview(textRoomCleanPreview,
                            MessToCleanDelta(it.delta, ProtagonistStatusModel.ROOM_MESS_LEVEL_MAX, ProtagonistStatusModel.ROOM_MESS_LEVEL_MIN));
                    break;
                case StatKind.BodyDirtyLevel:
                    if (!hideBodyCleanPreview)
                        SetStaticPreview(textBodyCleanPreview,
                            MessToCleanDelta(it.delta, ProtagonistStatusModel.BODY_DIRTY_LEVEL_MAX, ProtagonistStatusModel.BODY_DIRTY_LEVEL_MIN));
                    break;
                // Libido / Trust 屬女主角，由 HeroineUI 處理，這裡忽略。
            }
        }
    }

    /// <summary>清掉主角的 hover 預覽字。</summary>
    public void ClearStatPreview()
    {
        HideAllPreviewTexts();
    }

    // 靜態顯示：直接設 +X / -X（可帶後綴如 "%"）並把 alpha 拉滿（不透明），不做動畫。
    private static void SetStaticPreview(TextMeshProUGUI target, int delta, string suffix = "")
    {
        if (target == null) return;
        target.text = (delta > 0 ? $"+{delta}" : delta.ToString()) + suffix;
        var c = target.color;
        c.a = 1f;
        target.color = c;
        target.gameObject.SetActive(true);
    }

    // 把「髒污 delta」換算成「整潔度百分比 delta」：髒污越多整潔越少，故取負號。
    // 與飄字採同一線性換算（範圍 25 時每點 = 4%）。
    private static int MessToCleanDelta(int messDelta, int max, int min)
    {
        int range = max - min;
        if (range <= 0) return 0;
        return -messDelta * (100 / range);
    }

    // ==================================================
    // 舊 Stamina Preview 相容方法：NHK 版已停用。
    // 保留是為了避免舊 StaminaPreviewHoverTrigger 或 UnityEvent 編譯 / 綁定中斷。
    // ==================================================
    public bool IsStaminaAffordable(int cost) => true;
    public void ShowStaminaPreview(int delta) { }
    public void ShowStaminaDecreasePreview(int cost) { }
    public void ShowStaminaIncreasePreview(int amount) { }
    public void HideStaminaPreview() { }
}
