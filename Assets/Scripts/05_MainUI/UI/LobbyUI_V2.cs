using System;
using TMPro;
using UnityEngine;
using PixelCrushers;

/// <summary>主角現役數值與時間顯示。退役的四維與清潔百分比欄位請在 Editor 移除。</summary>
public class LobbyUI_V2 : MonoBehaviour
{
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

    private GameStatusService _service;
    private ProtagonistStatusModel _protagonistModel;
    private TimeSystemModel _timeModel;

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
    }
    private void OnDisable()
    {
        UILocalizationManager.languageChanged -= HandleLanguageChanged;
        Unbind();
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void BindAndRefresh()
    {
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
        SetText(textStamina, p != null ? $"{p.Stamina}/{p.StaminaMax}" : "—");
        SetText(textShootTimes, p != null ? $"{p.ShootTimes}/{p.ShootTimesMax}" : "—");
        SetText(textLust, p != null ? $"{p.Lust}%" : "—");
        SetText(textStickerCount, p != null ? p.StickerCount.ToString() : "—");
        SetText(textPendingStickerCount, p != null ? p.PendingStickerCount.ToString() : "—");
        SetText(textSkillPoints, p != null ? p.SkillPoints.ToString() : "—");
        SetText(textSemen, p != null ? p.Semen.ToString() : "—");
        SetText(textRoomMessLevel, p != null ? p.RoomMessLevel.ToString() : "—");
    }
    private static void SetText(TextMeshProUGUI text, string value) { if (text != null) text.text = value; }
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
