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

    [SerializeField] private TextMeshProUGUI textLifePower;
    [SerializeField] private TextMeshProUGUI textLifePowerPreview;

    [SerializeField] private TextMeshProUGUI textSociality;
    [SerializeField] private TextMeshProUGUI textSocialityPreview;

    [SerializeField] private TextMeshProUGUI textDependency;
    [SerializeField] private TextMeshProUGUI textDependencyPreview;

    [Header("=== Preview 顯示設定 ===")]
    [Tooltip("正/負變動是否顯示飄字；播放間隔與淡入淡出時間改由 StatusPreviewSequencer 控制。")]
    [SerializeField] private bool showPositivePreview = true;
    [SerializeField] private bool showNegativePreview = true;
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
    private int _lastPhaseIndex = -1;

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
    }

    private void SubscribeEvents()
    {
        if (_protagonistModel != null)
        {
            _protagonistModel.OnStressChanged += HandleStressChanged;
            _protagonistModel.OnLifePowerChanged += HandleLifePowerChanged;
            _protagonistModel.OnSocialityChanged += HandleSocialityChanged;
            _protagonistModel.OnDependencyChanged += HandleDependencyChanged;
            _protagonistModel.OnMoneyChanged += HandleMoneyChanged;
        }

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
            _protagonistModel.OnMoneyChanged -= HandleMoneyChanged;
        }

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

        // 兼容舊欄位：若新 stress 欄位沒接，就使用舊 suspicion 欄位。
        if (textStress == null) textStress = textSuspicion;
        if (textStressTitle == null) textStressTitle = textSuspicionTitle;
        if (textStressTitle != null) textStressTitle.text = "壓力";

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
            () => SetText(textStress, v),
            PreviewIfAllowed(textStressPreview, delta),
            delta);
    }

    private void HandleLifePowerChanged(int delta)
    {
        int v = _protagonistModel != null ? _protagonistModel.LifePower : 0;
        StatusPreviewSequencer.Instance.Enqueue(
            StatusPreviewSequencer.OrderLifePower,
            () => SetText(textLifePower, v),
            PreviewIfAllowed(textLifePowerPreview, delta),
            delta);
    }

    private void HandleSocialityChanged(int delta)
    {
        int v = _protagonistModel != null ? _protagonistModel.Sociality : 0;
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
            () => SetText(textDependency, v),
            PreviewIfAllowed(textDependencyPreview, delta),
            delta);
    }

    private void HandleMoneyChanged(int delta)
    {
        UpdateMoneyUI();
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
    }

    private void UpdateStressUI()
    {
        if (textStress != null && _protagonistModel != null)
            textStress.text = _protagonistModel.Stress.ToString();
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
        if (textDependency != null && _protagonistModel != null)
            textDependency.text = _protagonistModel.Dependency.ToString();
    }

    private void HideAllPreviewTexts()
    {
        HidePreviewText(textStressPreview);
        HidePreviewText(textLifePowerPreview);
        HidePreviewText(textSocialityPreview);
        HidePreviewText(textDependencyPreview);
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
    // 舊 Stamina Preview 相容方法：NHK 版已停用。
    // 保留是為了避免舊 StaminaPreviewHoverTrigger 或 UnityEvent 編譯 / 綁定中斷。
    // ==================================================
    public bool IsStaminaAffordable(int cost) => true;
    public void ShowStaminaPreview(int delta) { }
    public void ShowStaminaDecreasePreview(int cost) { }
    public void ShowStaminaIncreasePreview(int amount) { }
    public void HideStaminaPreview() { }
}
