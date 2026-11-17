using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>女主角數值條共用的角色綁定、顯示範圍、填色與右端滿格通知。</summary>
[DisallowMultipleComponent]
public abstract class HeroineValueSliderVisualizer : MonoBehaviour, ISerializationCallbackReceiver
{
    [Tooltip("選填：指定這條數值條根物件上的顯示行為。")]
    [UnityEngine.Serialization.FormerlySerializedAs("visibilityController")]
    [SerializeField] private SliderVisibilityBehaviour visibilityBehaviour;

    [Header("連動角色")]
    [SerializeField] private string heroineID = "sister";

    [Header("顯示範圍")]
    [InspectorName("自訂最小值")]
    [Tooltip("未勾選時沿用角色數值的下限。")]
    [SerializeField] private bool overrideMinimum;
    [InspectorName("顯示最小值")]
    [SerializeField] private float displayMinimum;
    [InspectorName("自訂最大值")]
    [Tooltip("未勾選時沿用角色數值的上限。")]
    [SerializeField] private bool overrideMaximum;
    [InspectorName("顯示最大值")]
    [SerializeField] private float displayMaximum = 100f;
    [SerializeField, HideInInspector] private bool rangeOptionsInitialized;

    [Header("刷新方式")]
    [InspectorName("非即時刷新")]
    [Tooltip("依刷新間隔更新填色。搭配「變動時顯示」時，收到數值變化就顯示整條。啟用、換角與讀檔時立即同步。")]
    [SerializeField] private bool nonRealtimeRefresh;
    [InspectorName("刷新間隔（秒）")]
    [Min(0.01f)] [SerializeField] private float refreshInterval = 1.5f;
    [InspectorName("出現時立即更新填色")]
    [Tooltip("適用於「非即時刷新」搭配「變動時顯示」。每次從隱藏重新出現時，勾選會立即填入最新值，取消勾選會等到下一個刷新點才更新填色。條可見期間的後續變動都依刷新間隔更新填色。")]
    [UnityEngine.Serialization.FormerlySerializedAs("showImmediatelyOnChange")]
    [SerializeField, HideInInspector] private bool refreshFillOnShow = false;

    [Header("填色與平滑過渡")]
    [Tooltip("指定填色圖片，留空時使用本物件上的 Image。")]
    [SerializeField] private Image fillImage;
    [Min(0.01f)] [SerializeField] private float transitionDuration = 0.45f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("右端滿格通知")]
    [Tooltip("數值從下方跨入有效上限，等填色抵達右端時觸發一次。進場、換角、讀檔或改範圍時只同步。")]
    [SerializeField] private UnityEvent onReachedMax = new UnityEvent();

    public UnityEvent OnReachedMax => onReachedMax;
    public float DisplayedValue => Mathf.Lerp(AppliedMinimum, AppliedMaximum, displayedFill);

    protected HeroineStatusModel Model { get; private set; }
    protected float AppliedMinimum { get; private set; }
    protected float AppliedMaximum { get; private set; }
    protected abstract float ModelMinimum { get; }
    protected abstract float ModelMaximum { get; }
    protected abstract float CurrentValue { get; }
    protected abstract Color FillColor { get; }
    protected abstract void Subscribe(HeroineStatusModel model);
    protected abstract void Unsubscribe(HeroineStatusModel model);

    private GameStatusService service;
    private float displayedFill, targetFill, transitionStart;
    private float transitionElapsed, refreshElapsed;
    private bool appliedNonRealtimeRefresh, fullArmed;
    private bool showAfterRender;
    private int notificationVersion;

    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize()
    {
        if (rangeOptionsInitialized) return;
        // 舊版 Awake、Orgasm 的 Inspector 預設是 0～100；沿用已儲存的非預設界限。
        overrideMinimum |= displayMinimum != 0f;
        overrideMaximum |= displayMaximum != 100f;
        rangeOptionsInitialized = true;
    }

    protected void Reset() => rangeOptionsInitialized = true;

    protected void OnEnable()
    {
        if (fillImage == null && !TryGetComponent(out fillImage))
        {
            Debug.LogWarning($"[{GetType().Name}] 請在 Fill Image 指定填色圖片，再啟用此元件。", this);
            enabled = false;
            return;
        }
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.raycastTarget = false;
        RefreshBinding();
        SynchronizeImmediately();
    }

    protected void OnDisable()
    {
        notificationVersion++;
        if (visibilityBehaviour != null) visibilityBehaviour.ResetPresentation();
        if (Model != null) Unsubscribe(Model);
        Model = null;
        if (service != null) service.OnGameStatusLoaded -= HandleGameStatusLoaded;
        service = null;
    }

    public void SetHeroineID(string id)
    {
        heroineID = id;
        if (isActiveAndEnabled) RefreshBinding();
    }

    protected bool TryGetModel()
    {
        if (!isActiveAndEnabled) return false;
        RefreshBinding();
        if (Model != null) return true;
        Debug.LogWarning($"[{GetType().Name}] 尚未找到女主角：{heroineID}", this);
        return false;
    }

    private void RefreshBinding()
    {
        var currentService = GameStatusService.Instance;
        if (service != currentService)
        {
            if (service != null) service.OnGameStatusLoaded -= HandleGameStatusLoaded;
            service = currentService;
            if (service != null) service.OnGameStatusLoaded += HandleGameStatusLoaded;
        }

        HeroineStatusModel next = null;
        if (service != null && service.Heroines != null && !string.IsNullOrEmpty(heroineID))
            service.Heroines.TryGetValue(heroineID, out next);
        if (ReferenceEquals(Model, next)) return;

        if (Model != null) Unsubscribe(Model);
        Model = next;
        if (Model != null) Subscribe(Model);
        SynchronizeImmediately();
    }

    private void HandleGameStatusLoaded()
    {
        RefreshBinding();
        SynchronizeImmediately();
    }

    private void ResolveRange(out float minimum, out float maximum)
    {
        minimum = overrideMinimum && IsFinite(displayMinimum) ? displayMinimum : ModelMinimum;
        maximum = overrideMaximum && IsFinite(displayMaximum) ? displayMaximum : ModelMaximum;
        // 自訂範圍無法形成有效區間時，兩端暫時沿用 Model。
        if (maximum > minimum) return;
        minimum = ModelMinimum;
        maximum = ModelMaximum;
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private void SynchronizeImmediately()
    {
        notificationVersion++;
        showAfterRender = false;
        if (visibilityBehaviour != null) visibilityBehaviour.ResetPresentation();
        ResolveRange(out float minimum, out float maximum);
        AppliedMinimum = minimum;
        AppliedMaximum = maximum;
        appliedNonRealtimeRefresh = nonRealtimeRefresh;
        float value = Model != null ? CurrentValue : minimum;
        displayedFill = targetFill = transitionStart = Mathf.InverseLerp(minimum, maximum, value);
        refreshElapsed = transitionElapsed = 0f;
        fullArmed = value < maximum;
        UpdateAdditionalThreshold(value, true);
        RenderVisual();
    }

    protected void HandleValueChanged(float value)
    {
        if (Model == null || CurrentValue != value) return;
        notificationVersion++;
        if (value < AppliedMaximum) fullArmed = true;
        UpdateAdditionalThreshold(value, false);
        float nextFill = Mathf.InverseLerp(AppliedMinimum, AppliedMaximum, value);
        if (nonRealtimeRefresh)
        {
            targetFill = nextFill;
            showAfterRender = true;
            if (visibilityBehaviour != null && visibilityBehaviour.isActiveAndEnabled && visibilityBehaviour.ShowsOnChange)
            {
                if (refreshFillOnShow && visibilityBehaviour.TryGetComponent(out CanvasGroup group) && group.alpha <= 0f)
                {
                    displayedFill = targetFill;
                    RenderAndNotify(settled: true);
                }
                else
                {
                    showAfterRender = false;
                    visibilityBehaviour.NotifyChangeStarted();
                }
            }
            return;
        }

        showAfterRender = false;
        if (visibilityBehaviour != null) visibilityBehaviour.NotifyChangeStarted();
        if (targetFill == nextFill)
        {
            if (displayedFill == nextFill && visibilityBehaviour != null)
                visibilityBehaviour.NotifyChangeFinished();
            return;
        }
        targetFill = nextFill;
        transitionStart = displayedFill;
        transitionElapsed = 0f;
    }

    protected void Update()
    {
        RefreshBinding();
        if (Model == null) return;
        ResolveRange(out float minimum, out float maximum);
        if (minimum != AppliedMinimum || maximum != AppliedMaximum)
        {
            SynchronizeImmediately();
            return;
        }
        if (appliedNonRealtimeRefresh != nonRealtimeRefresh)
        {
            appliedNonRealtimeRefresh = nonRealtimeRefresh;
            refreshElapsed = transitionElapsed = 0f;
            transitionStart = displayedFill;
        }

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        bool settled;
        if (nonRealtimeRefresh)
        {
            float interval = IsFinite(refreshInterval) ? Mathf.Max(0.01f, refreshInterval) : 1.5f;
            refreshElapsed += dt;
            if (refreshElapsed < interval) return;
            refreshElapsed %= interval;
            displayedFill = targetFill;
            settled = true;
        }
        else
        {
            transitionElapsed += dt;
            float t = Mathf.Clamp01(transitionElapsed / Mathf.Max(0.01f, transitionDuration));
            settled = t >= 1f;
            displayedFill = settled ? targetFill : Mathf.Lerp(transitionStart, targetFill, t * t * (3f - 2f * t));
        }
        RenderAndNotify(settled);
    }

    private void RenderAndNotify(bool settled)
    {
        RenderVisual();
        if (showAfterRender)
        {
            showAfterRender = false;
            if (visibilityBehaviour != null) visibilityBehaviour.NotifyChangeStarted();
        }
        if (settled && visibilityBehaviour != null) visibilityBehaviour.NotifyChangeFinished();

        int version = notificationVersion;
        NotifyAdditionalThreshold(CurrentValue);
        // 前一個事件可能改值、讀檔、換角或停用元件。
        if (version != notificationVersion) return;
        if (settled && fullArmed && CurrentValue >= AppliedMaximum && displayedFill >= 1f)
        {
            fullArmed = false;
            onReachedMax.Invoke();
        }
    }

    protected virtual void UpdateAdditionalThreshold(float value, bool synchronize) { }
    protected virtual void NotifyAdditionalThreshold(float value) { }

    private void RenderVisual()
    {
        if (fillImage == null) return;
        fillImage.color = FillColor;
        fillImage.fillAmount = displayedFill;
    }
}
