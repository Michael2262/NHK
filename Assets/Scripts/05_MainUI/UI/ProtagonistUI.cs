using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 主角技能點與貼紙總數面板，依自己的進度 Flag 控制顯示。
/// 數值與 Preview 沿用共用佇列的播放順序，演出取消只影響此面板。
/// </summary>
[DisallowMultipleComponent]
public class ProtagonistUI : MonoBehaviour
{
    [Header("依進度 Flag 自動顯示的主角面板")]
    [Tooltip("拖入原 Second Section 的 CanvasGroup，並保持其 GameObject 啟用。")]
    [SerializeField] private CanvasGroup canvasGroup;
    [Tooltip("沿用主角面板的顯示 Flag，例如 Flag_System_BoyPanelShow。")]
    [SerializeField] private ProgressFlagDefinition visibilityFlag;

    [Header("變動時顯示")]
    [Tooltip("顯示總開關成立時，此 Flag 成立才改為變動時顯示；留空則維持常駐。")]
    [SerializeField] private ProgressFlagDefinition showOnChangeFlag;
    [InspectorName("演完後停留秒數")]
    [Min(0f)] [SerializeField] private float holdDuration = 1f;
    [InspectorName("面板淡出秒數")]
    [Min(0f)] [SerializeField] private float fadeDuration = 0.25f;

    [Header("技能點")]
    [SerializeField] private TextMeshProUGUI textSkillPoints;
    [SerializeField] private TextMeshProUGUI textSkillPointsPreview;

    [Header("貼紙總數")]
    [SerializeField] private TextMeshProUGUI textStickerCount;
    [SerializeField] private TextMeshProUGUI textStickerCountPreview;

    private GameStatusService _service;
    private ProtagonistStatusModel _protagonistModel;
    private ProgressFlagModel _flags;
    private int _cachedSkillPoints;
    private int _cachedStickerCount;
    private readonly StatusPanelVisibilityState _visibility = new StatusPanelVisibilityState();

    private void OnEnable()
    {
        BindAndRefresh();
    }

    private void Update()
    {
        var service = GameStatusService.Instance;
        if (!ReferenceEquals(_service, service)
            || !ReferenceEquals(_protagonistModel, service?.Protagonist)
            || !ReferenceEquals(_flags, service?.ProgressFlags))
            BindAndRefresh();
        UpdatePanelAlpha();
    }

    private void OnDisable()
    {
        CancelPreviews();
        Unbind();
        _visibility.Configure(false, false);
        SetAlpha(0f);
    }

    private void BindAndRefresh()
    {
        CancelPreviews();
        Unbind();
        _service = GameStatusService.Instance;
        _protagonistModel = _service?.Protagonist;
        _flags = _service?.ProgressFlags;

        if (_service != null) _service.OnGameStatusLoaded += BindAndRefresh;
        if (_protagonistModel != null) _protagonistModel.OnChanged += HandleStatusChanged;
        if (_flags != null) _flags.OnFlagChanged += HandleVisibilityFlagChanged;

        // 初始化、讀檔及重新啟用只對齊目前值，不補播變動。
        RefreshVisibility(resetPresentation: true);
    }

    private void Unbind()
    {
        if (!ReferenceEquals(_service, null)) _service.OnGameStatusLoaded -= BindAndRefresh;
        if (_protagonistModel != null) _protagonistModel.OnChanged -= HandleStatusChanged;
        if (_flags != null) _flags.OnFlagChanged -= HandleVisibilityFlagChanged;
        _service = null;
        _protagonistModel = null;
        _flags = null;
    }

    private void HandleVisibilityFlagChanged(string key, bool _)
    {
        if (key != visibilityFlag?.FlagID && key != showOnChangeFlag?.FlagID) return;
        RefreshVisibility();
    }

    private void RefreshVisibility(bool resetPresentation = false)
    {
        bool allowed = canvasGroup != null && visibilityFlag != null
            && _flags != null && _flags.Contains(visibilityFlag.FlagID);
        bool onChange = showOnChangeFlag != null && _flags != null
            && _flags.Contains(showOnChangeFlag.FlagID);
        bool changed = _visibility.Configure(allowed, onChange);
        if (changed || resetPresentation)
        {
            // 模式切換立即清掉自己的舊演出，不補播；關閉變動模式則恢復常駐。
            CancelPreviews();
            UpdateStatusUI();
        }
        UpdatePanelAlpha();
    }

    private void UpdatePanelAlpha()
    {
        bool hasWork = _visibility.IsPresenting && StatusPreviewSequencer.HasOwnerWorkIfExists(this);
        SetAlpha(_visibility.GetAlpha(Time.unscaledTime, hasWork, holdDuration, fadeDuration));
    }

    private void SetAlpha(float alpha)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = alpha;
        canvasGroup.interactable = alpha > 0f;
        canvasGroup.blocksRaycasts = alpha > 0f;
    }

    private void UpdateStatusUI()
    {
        var model = _protagonistModel;
        SetText(textSkillPoints, model?.SkillPoints.ToString() ?? "—");
        SetText(textStickerCount, model?.StickerCount.ToString() ?? "—");
        _cachedSkillPoints = model?.SkillPoints ?? 0;
        _cachedStickerCount = model?.StickerCount ?? 0;
    }

    private void HandleStatusChanged()
    {
        var model = _protagonistModel;
        if (model == null) return;
        if (!_visibility.AllowsDisplay)
        {
            UpdateStatusUI();
            return;
        }

        // 捕捉本次數值；快取與畫面分開，避免連續事件遺失中間的變動量。
        int skillPoints = model.SkillPoints;
        int stickerCount = model.StickerCount;
        long skillPointsDelta = (long)skillPoints - _cachedSkillPoints;
        long stickerDelta = (long)stickerCount - _cachedStickerCount;
        _cachedSkillPoints = skillPoints;
        _cachedStickerCount = stickerCount;

        if (skillPointsDelta != 0 && (textSkillPoints != null || textSkillPointsPreview != null))
            QueuePreview(StatusPreviewSequencer.OrderSkillPoints, textSkillPointsPreview, skillPointsDelta,
                () => SetText(textSkillPoints, skillPoints.ToString()));
        if (stickerDelta != 0 && (textStickerCount != null || textStickerCountPreview != null))
            QueuePreview(StatusPreviewSequencer.OrderStickerCount, textStickerCountPreview, stickerDelta,
                () => SetText(textStickerCount, stickerCount.ToString()));
    }

    private void QueuePreview(int order, TextMeshProUGUI preview, long delta, Action applyValue)
    {
        StatusPreviewSequencer.Instance.EnqueueText(order, preview,
            StatusPreviewSequencer.GetNumericPreviewText(delta), () =>
            {
                _visibility.NotifyChangeStarted();
                SetAlpha(_visibility.AllowsDisplay ? 1f : 0f);
                applyValue?.Invoke();
            }, this);
    }

    private void CancelPreviews()
    {
        StatusPreviewSequencer.CancelOwnerIfExists(this);
        _visibility.ResetPresentation();
        HidePreview(textSkillPointsPreview);
        HidePreview(textStickerCountPreview);
    }

    private static void HidePreview(TextMeshProUGUI target)
    {
        if (target == null) return;
        target.text = "";
        var color = target.color;
        color.a = 0f;
        target.color = color;
    }

    private static void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null) target.text = value;
    }
}
