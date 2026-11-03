using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 指定女主角的 Awake 填色 UI；顯示範圍可設定，超出範圍時維持空格或滿格。
/// 掛在獨立的填色 Image 上；僅限制顯示範圍，不改變 Model 的 Awake 上限。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class AwakeSliderVisualizer : MonoBehaviour
{
    [Header("連動角色")]
    [SerializeField] private string heroineID = "sister";

    [Header("顯示範圍")]
    [InspectorName("顯示最小值")]
    [SerializeField] private float displayMinimum = 0f;
    [InspectorName("顯示最大值")]
    [SerializeField] private float displayMaximum = 100f;

    [Header("刷新方式")]
    [InspectorName("非即時刷新")]
    [Tooltip("開啟後每隔指定秒數才取最新 Awake，直接更新填色，不使用平滑過渡。啟用、換角或讀檔仍立即同步。")]
    [SerializeField] private bool nonRealtimeRefresh = false;
    [InspectorName("刷新間隔（秒）")]
    [Min(0.01f)] [SerializeField] private float refreshInterval = 1.5f;

    [Header("填色與平滑過渡")]
    [SerializeField] private Color fillColor = Color.white;
    [Tooltip("每次目標改變後，從目前填色位置過渡到新值所需秒數。")]
    [Min(0.01f)] [SerializeField] private float transitionDuration = 0.45f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("右端滿格通知")]
    [Tooltip("Awake 從低於顯示最大值升到最大值以上，且填色抵達右端時觸發一次。非即時模式會等到刷新時才通知。維持滿值不重複觸發；降到最大值以下後可再次觸發。啟用、換角、讀檔或修改顯示範圍時不補發。")]
    [SerializeField] private UnityEvent onReachedMax = new UnityEvent();

    public UnityEvent OnReachedMax => onReachedMax;
    public float DisplayedValue => Mathf.Lerp(displayMinimum, displayMaximum, displayedFill);

    private Image fillImage;
    private GameStatusService service;
    private HeroineStatusModel model;
    private float displayedFill;
    private float targetFill;
    private float transitionStart;
    private float transitionElapsed;
    private bool fullArmed;
    private float refreshElapsed;
    private float appliedMinimum;
    private float appliedMaximum;
    private bool appliedNonRealtimeRefresh;

    private void Awake()
    {
        ValidateSettings();
        fillImage = GetComponent<Image>();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.raycastTarget = false;
    }

    private void OnValidate() => ValidateSettings();

    private void ValidateSettings()
    {
        if (float.IsNaN(displayMinimum) || float.IsInfinity(displayMinimum)) displayMinimum = 0f;
        if (float.IsNaN(displayMaximum) || float.IsInfinity(displayMaximum)) displayMaximum = 100f;
        if (displayMaximum <= displayMinimum)
        {
            displayMaximum = displayMinimum + 1f;
            if (displayMaximum <= displayMinimum)
            {
                displayMinimum = 0f;
                displayMaximum = 100f;
            }
        }
        if (float.IsNaN(refreshInterval) || float.IsInfinity(refreshInterval)) refreshInterval = 1.5f;
        refreshInterval = Mathf.Max(0.01f, refreshInterval);
    }

    private void OnEnable()
    {
        RefreshBinding();
        SynchronizeImmediately();
    }

    private void OnDisable()
    {
        DetachModel();
        if (service != null) service.OnGameStatusLoaded -= HandleGameStatusLoaded;
        service = null;
        fullArmed = false;
    }

    /// <summary>切換角色時立即同步，不補發滿格通知。</summary>
    public void SetHeroineID(string id)
    {
        heroineID = id;
        if (isActiveAndEnabled) RefreshBinding();
    }

    /// <summary>轉交正式 Model 設值；保留小數，數值上限由 Model 處理。</summary>
    public void SetAwake(float value)
    {
        if (TryGetModel()) model.SetAwake(value);
    }

    public void AddAwake(float amount)
    {
        if (TryGetModel()) model.AddAwake(amount);
    }

    private bool TryGetModel()
    {
        if (!isActiveAndEnabled) return false;
        RefreshBinding();
        if (model != null) return true;
        Debug.LogWarning($"[AwakeSliderVisualizer] 尚未找到女主角：{heroineID}", this);
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
        if (ReferenceEquals(model, next)) return;

        DetachModel();
        model = next;
        if (model != null) model.OnAwakeChanged += HandleAwakeChanged;
        SynchronizeImmediately();
    }

    private void DetachModel()
    {
        if (model != null) model.OnAwakeChanged -= HandleAwakeChanged;
        model = null;
    }

    private void HandleGameStatusLoaded()
    {
        RefreshBinding();
        SynchronizeImmediately();
    }

    private void SynchronizeImmediately()
    {
        ValidateSettings();
        appliedMinimum = displayMinimum;
        appliedMaximum = displayMaximum;
        appliedNonRealtimeRefresh = nonRealtimeRefresh;
        targetFill = GetCurrentFill();
        displayedFill = transitionStart = targetFill;
        transitionElapsed = 0f;
        refreshElapsed = 0f;
        fullArmed = model != null && model.Awake < displayMaximum;
        RenderVisual();
    }

    private float GetCurrentFill()
        => model != null ? Mathf.InverseLerp(displayMinimum, displayMaximum, model.Awake) : 0f;

    private void HandleAwakeChanged(float unused)
    {
        if (model == null) return;
        float nextFill = GetCurrentFill();
        // 即使同一幀先降低再回到最大值，也允許下一次滿格通知。
        if (model.Awake < displayMaximum) fullArmed = true;
        // 超過顯示最大值都是同一個目標，不重啟尚未完成的填色動畫。
        if (targetFill == nextFill) return;
        targetFill = nextFill;
        transitionStart = displayedFill;
        transitionElapsed = 0f;
    }

    private void Update()
    {
        RefreshBinding();
        if (model == null) return;

        if (appliedMinimum != displayMinimum || appliedMaximum != displayMaximum)
        {
            SynchronizeImmediately();
            return;
        }
        if (appliedNonRealtimeRefresh != nonRealtimeRefresh)
        {
            appliedNonRealtimeRefresh = nonRealtimeRefresh;
            refreshElapsed = 0f;
            transitionStart = displayedFill;
            transitionElapsed = 0f;
        }

        float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (nonRealtimeRefresh)
        {
            // Awake 變化不重設刷新計時，避免持續加值時一直無法刷新。
            refreshElapsed += delta;
            if (refreshElapsed < refreshInterval) return;
            refreshElapsed %= refreshInterval;
            targetFill = GetCurrentFill();
            displayedFill = transitionStart = targetFill;
            transitionElapsed = 0f;
            RenderVisual();
            NotifyIfReachedMax();
            return;
        }

        transitionElapsed += delta;
        float t = Mathf.Clamp01(transitionElapsed / Mathf.Max(0.01f, transitionDuration));
        displayedFill = t >= 1f ? targetFill : Mathf.Lerp(transitionStart, targetFill, t * t * (3f - 2f * t));
        RenderVisual();
        if (t >= 1f) NotifyIfReachedMax();
    }

    private void NotifyIfReachedMax()
    {
        if (fullArmed && model != null && model.Awake >= displayMaximum && displayedFill >= 1f)
        {
            // 先關閉本輪通知；回呼可以改值、換角、停用 UI 或讀檔。
            fullArmed = false;
            onReachedMax.Invoke();
        }
    }

    private void RenderVisual()
    {
        if (fillImage == null) return;
        fillImage.color = fillColor;
        fillImage.fillAmount = displayedFill;
    }
}
