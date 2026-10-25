using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PixelCrushers;

/// <summary>主角現役數值與時間顯示。退役的四維與清潔百分比欄位請在 Editor 移除。</summary>
public class LobbyUI_V2 : MonoBehaviour
{
    // 體力條顯示設定：Max 25 對應 80；Max 100 對應 176，之後不再伸長。
    private const int StaminaZoneBaseMax = 25;
    private const float StaminaZoneBaseWidth = 80f;
    private const float StaminaZoneMaxWidth = 176f;
    private const int StaminaZoneWidthCap = 100;
    private const float StaminaBarTransitionSeconds = 0.25f;

    public static LobbyUI_V2 Instance { get; private set; }

    [Header("時間")]
    [SerializeField] private TextMeshProUGUI textDayOfWeek;
    [SerializeField] private TextMeshProUGUI textDayNumber;
    [UnityEngine.Serialization.FormerlySerializedAs("textPhaseName")]
    [SerializeField] private TextMeshProUGUI textMiniName;
    [SerializeField] private TextMeshProUGUI textTimeOfDay;
    [SerializeField] private TextMeshProUGUI textDebugDayPhaseSlot;
    [SerializeField] private TimeMappingSO timeMapping;

    [Header("主角數值（只顯示值；欄位可留空）")]
    [SerializeField] private TextMeshProUGUI textMoney;
    [SerializeField] private TextMeshProUGUI textStamina;
    [SerializeField] private TextMeshProUGUI textShootTimes;
    [SerializeField] private TextMeshProUGUI textLust;
    [SerializeField] private TextMeshProUGUI textStickerCount;
    [SerializeField] private TextMeshProUGUI textPendingStickerCount;
    [SerializeField] private TextMeshProUGUI textSkillPoints;
    [SerializeField] private TextMeshProUGUI textSemen;
    [UnityEngine.Serialization.FormerlySerializedAs("textRoomCleanLevel")]
    [SerializeField] private TextMeshProUGUI textRoomMessLevel;

    [Header("體力條（欄位可留空）")]
    [Tooltip("體力條外框。請將水平錨點固定於左側、Pivot X 設為 0，避免 Layout Group 控制寬度。")]
    [SerializeField] private RectTransform staminaZone;
    [Tooltip("外框內的填滿圖片。請水平延展至外框內緣；程式會設為由左向右的 Filled 模式。")]
    [SerializeField] private Image staminaBarFill;

    private GameStatusService _service;
    private ProtagonistStatusModel _protagonistModel;
    private TimeSystemModel _timeModel;
    private bool _staminaBarInitialized;
    private float _staminaStartWidth;
    private float _staminaTargetWidth;
    private float _staminaStartFill;
    private float _staminaTargetFill;
    private float _staminaTransitionElapsed = StaminaBarTransitionSeconds;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    private void OnEnable()
    {
        UILocalizationManager.languageChanged += HandleLanguageChanged;
        BindAndRefresh();
    }
    private void Update()
    {
        var service = GameStatusService.Instance;
        if (!ReferenceEquals(_service, service)
            || !ReferenceEquals(_protagonistModel, service?.Protagonist)
            || !ReferenceEquals(_timeModel, service?.Time)) BindAndRefresh();
        AnimateStaminaBar();
    }
    private void OnDisable()
    {
        UILocalizationManager.languageChanged -= HandleLanguageChanged;
        _staminaBarInitialized = false;
        _staminaTransitionElapsed = StaminaBarTransitionSeconds;
        Unbind();
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void BindAndRefresh()
    {
        // 初始化、重開 UI 與讀檔直接對齊；一般遊玩中的數值變化才播放過渡。
        _staminaBarInitialized = false;
        Unbind();
        _service = GameStatusService.Instance;
        _protagonistModel = _service?.Protagonist;
        _timeModel = _service?.Time;
        if (_service != null) _service.OnGameStatusLoaded += BindAndRefresh;
        if (_protagonistModel != null)
        {
            _protagonistModel.OnChanged += UpdateStatusUI;
        }
        if (_timeModel != null)
        {
            _timeModel.OnDayPassed += RefreshTime;
            _timeModel.OnPhaseChanged += RefreshTime;
            _timeModel.OnTimeSlotAdvanced += HandleTimeChanged;
            _timeModel.OnMiniAdvanced += HandleTimeChanged;
        }
        UpdateStatusUI();
        RefreshTime();
    }
    private void Unbind()
    {
        if (!ReferenceEquals(_service, null)) _service.OnGameStatusLoaded -= BindAndRefresh;
        if (_protagonistModel != null)
        {
            _protagonistModel.OnChanged -= UpdateStatusUI;
        }
        if (_timeModel != null)
        {
            _timeModel.OnDayPassed -= RefreshTime;
            _timeModel.OnPhaseChanged -= RefreshTime;
            _timeModel.OnTimeSlotAdvanced -= HandleTimeChanged;
            _timeModel.OnMiniAdvanced -= HandleTimeChanged;
        }
        _service = null;
        _protagonistModel = null;
        _timeModel = null;
    }
    private void UpdateStatusUI()
    {
        var p = _protagonistModel;
        SetText(textMoney, p != null ? $"{p.Money}G" : "—");
        SetResourceText(textStamina, p?.Stamina, p?.StaminaMax);
        SetResourceText(textShootTimes, p?.ShootTimes, p?.ShootTimesMax);
        SetText(textLust, p != null ? $"{p.Lust}%" : "—");
        SetText(textStickerCount, p != null ? p.StickerCount.ToString() : "—");
        SetText(textPendingStickerCount, p != null ? p.PendingStickerCount.ToString() : "—");
        SetText(textSkillPoints, p != null ? p.SkillPoints.ToString() : "—");
        SetText(textSemen, p != null ? p.Semen.ToString() : "—");
        SetText(textRoomMessLevel, p != null ? p.RoomMessLevel.ToString() : "—");
        UpdateStaminaBar();
    }

    private void UpdateStaminaBar()
    {
        int maximum = _protagonistModel?.StaminaMax ?? 0;
        int current = _protagonistModel?.Stamina ?? 0;
        // 低於 25 時由 0～80 線性縮放；25～100 改以 80～176 線性增長。
        float width = maximum <= StaminaZoneBaseMax
            ? StaminaZoneBaseWidth * Mathf.Clamp01((float)maximum / StaminaZoneBaseMax)
            : Mathf.Lerp(StaminaZoneBaseWidth, StaminaZoneMaxWidth,
                Mathf.InverseLerp(StaminaZoneBaseMax, StaminaZoneWidthCap, maximum));
        // 只限制條的顯示比例；文字仍呈現負值或超出上限的實際體力。
        float fill = maximum > 0 ? Mathf.Clamp01((float)current / maximum) : 0f;

        if (staminaBarFill != null)
        {
            staminaBarFill.type = Image.Type.Filled;
            staminaBarFill.fillMethod = Image.FillMethod.Horizontal;
            staminaBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        if (!_staminaBarInitialized)
        {
            _staminaTargetWidth = width;
            _staminaTargetFill = fill;
            _staminaTransitionElapsed = StaminaBarTransitionSeconds;
            ApplyStaminaBarVisuals(width, fill);
            _staminaBarInitialized = true;
            return;
        }

        // 金錢等其他數值刷新時，不重新啟動正在播放的體力條動畫。
        if (Mathf.Approximately(width, _staminaTargetWidth)
            && Mathf.Approximately(fill, _staminaTargetFill)) return;

        // 連續變更時從目前畫面接續，直接轉向最新目標，不堆疊動畫。
        _staminaStartWidth = staminaZone != null ? staminaZone.rect.width : width;
        _staminaStartFill = staminaBarFill != null ? staminaBarFill.fillAmount : fill;
        _staminaTargetWidth = width;
        _staminaTargetFill = fill;
        _staminaTransitionElapsed = 0f;
    }

    private void AnimateStaminaBar()
    {
        if (!_staminaBarInitialized || _staminaTransitionElapsed >= StaminaBarTransitionSeconds) return;
        _staminaTransitionElapsed = Mathf.Min(StaminaBarTransitionSeconds,
            _staminaTransitionElapsed + UnityEngine.Time.unscaledDeltaTime);
        float progress = Mathf.SmoothStep(0f, 1f, _staminaTransitionElapsed / StaminaBarTransitionSeconds);
        ApplyStaminaBarVisuals(Mathf.Lerp(_staminaStartWidth, _staminaTargetWidth, progress),
            Mathf.Lerp(_staminaStartFill, _staminaTargetFill, progress));
    }

    private void ApplyStaminaBarVisuals(float width, float fill)
    {
        if (staminaZone != null)
            staminaZone.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        if (staminaBarFill != null)
            staminaBarFill.fillAmount = fill;
    }

    private static void SetText(TextMeshProUGUI text, string value) { if (text != null) text.text = value; }
    private static void SetResourceText(TextMeshProUGUI text, int? current, int? maximum)
    {
        if (text == null) return;
        if (!current.HasValue || !maximum.HasValue)
        {
            text.text = "—";
            return;
        }

        text.richText = true;
        text.overrideColorTags = false;
        // 色標只包住當前負值（含負號）；斜線和最大值沿用文字元件原色。
        string currentText = current.Value < 0
            ? $"<color=#{ColorUtility.ToHtmlStringRGB(UIColorPalette.DarkRed)}>{current.Value}</color>"
            : current.Value.ToString();
        text.text = $"{currentText}/{maximum.Value}";
    }

    private void HandleTimeChanged(int _) => RefreshTime();
    private void RefreshTime() => UpdateTimeUI();
    private void HandleLanguageChanged(string language) => RefreshTime();
    public bool IsStaminaAffordable(int cost) => _protagonistModel != null && _protagonistModel.Stamina >= Math.Max(0, cost);
    // 舊介面原為壓力預覽，已停用；不可把舊壓力變化自動解讀為體力變化。
    public void ShowStaminaPreview(int delta) { }
    public void ShowStaminaDecreasePreview(int cost) { }
    public void ShowStaminaIncreasePreview(int amount) { }
    public void HideStaminaPreview() { }

    private void UpdateTimeUI()
    {
        if (_timeModel == null) return;

        int displayDay = _timeModel.DayIndex;

        if (timeMapping != null && timeMapping.ShouldShowNextDay(_timeModel.CurrentPhaseIndex, _timeModel.CurrentSlotInPhase))
        {
            displayDay += 1;
        }

        if (textDayNumber != null)
            textDayNumber.text = $"{displayDay}Day";

        if (textDayOfWeek != null)
            textDayOfWeek.text = GetLocalizedWeekday(_timeModel.GetDayOfWeekForDay(displayDay));

        if (textDebugDayPhaseSlot != null)
            textDebugDayPhaseSlot.text = $"D: {_timeModel.DayIndex} / P: {_timeModel.CurrentPhaseIndex} / S: {_timeModel.CurrentSlotInPhase} / M: {_timeModel.CurrentMiniInSlot}/{_timeModel.MinisInCurrentSlot}";

        string miniTime = timeMapping != null
            ? timeMapping.GetTimeDisplay(_timeModel.CurrentPhaseIndex, _timeModel.CurrentSlotInPhase, _timeModel.CurrentMiniInSlot)
            : "--:--";
        SetText(textMiniName, miniTime);
        SetText(textTimeOfDay, miniTime);
    }

    private string GetLocalizedWeekday(DayOfWeek day)
    {
        string key = timeMapping != null ? timeMapping.GetWeekdayLocalizationKey(day) : "";
        if (string.IsNullOrWhiteSpace(key)) return "";

        string translated = PixelCrushers.DialogueSystem.DialogueManager.GetLocalizedText(key);
        if (string.IsNullOrEmpty(translated) || translated == key)
        {
            Debug.LogWarning($"[LobbyUI_V2] Text Table 找不到星期 Key：{key}", this);
            return key;
        }
        return translated;
    }
}
