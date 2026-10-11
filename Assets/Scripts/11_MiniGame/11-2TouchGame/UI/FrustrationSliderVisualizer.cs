using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 指定女主角的不滿值填色 UI；平滑追蹤數值，顯示抵達各比例門檻時通知。
/// 掛在獨立的填色 Image 上，外框與背景由其他 UI 物件提供。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class FrustrationSliderVisualizer : MonoBehaviour
{
    [Header("連動角色")]
    [SerializeField] private string heroineID = "sister";

    [Header("填色與平滑過渡")]
    [SerializeField] private Color fillColor = new Color(1f, 0.4f, 0.35f, 1f);
    [Tooltip("每次目標改變後，從目前顯示位置過渡到新值所需秒數。")]
    [Min(0.01f)] [SerializeField] private float transitionDuration = 0.45f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("比例門檻通知")]
    [Tooltip("填色向上抵達或跨過 25% 時觸發一次；數值降到門檻以下後可再次觸發。啟用、換角或讀檔時不補發。")]
    [SerializeField] private UnityEvent onReachedQuarter = new UnityEvent();
    [Tooltip("填色向上抵達或跨過 50% 時觸發一次；數值降到門檻以下後可再次觸發。啟用、換角或讀檔時不補發。")]
    [SerializeField] private UnityEvent onReachedHalf = new UnityEvent();
    [Tooltip("填色向上抵達或跨過 75% 時觸發一次；數值降到門檻以下後可再次觸發。啟用、換角或讀檔時不補發。")]
    [SerializeField] private UnityEvent onReachedThreeQuarters = new UnityEvent();

    [Header("滿格通知")]
    [Tooltip("填色抵達滿格時觸發一次；維持滿格不重複觸發，不自動歸零。啟用、換角或讀檔時不補發。")]
    [SerializeField] private UnityEvent onReachedMax = new UnityEvent();

    public UnityEvent OnReachedQuarter => onReachedQuarter;
    public UnityEvent OnReachedHalf => onReachedHalf;
    public UnityEvent OnReachedThreeQuarters => onReachedThreeQuarters;
    public UnityEvent OnReachedMax => onReachedMax;
    public float DisplayedValue => displayedFill * maximum;

    private Image fillImage;
    private GameStatusService service;
    private HeroineStatusModel model;
    private int maximum = HeroineStatusModel.DefaultFrustrationMax;
    // 使用比例插值，上限變更時可從目前填色位置平滑追上。
    private float displayedFill;
    private float targetFill;
    private float transitionStart;
    private float transitionElapsed;
    private int targetValue;
    private bool fullArmed;
    private bool quarterArmed;
    private bool halfArmed;
    private bool threeQuartersArmed;
    // 事件回呼若改值、換角、停用或讀檔，停止派送本次更新的後續通知。
    private int notificationVersion;

    private void Awake()
    {
        fillImage = GetComponent<Image>();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.raycastTarget = false;
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
        quarterArmed = halfArmed = threeQuartersArmed = false;
        notificationVersion++;
    }

    /// <summary>切換角色時立即同步，不補發門檻通知。</summary>
    public void SetHeroineID(string id)
    {
        heroineID = id;
        if (isActiveAndEnabled) RefreshBinding();
    }

    /// <summary>轉交 Model 設值；UI 平滑追上。</summary>
    public void SetFrustration(int value)
    {
        if (TryGetModel()) model.SetFrustration(value);
    }

    public void AddFrustration(int amount)
    {
        if (TryGetModel()) model.AddFrustration(amount);
    }

    public void SetFrustrationMax(int value)
    {
        if (TryGetModel()) model.SetFrustrationMax(value);
    }

    private bool TryGetModel()
    {
        if (!isActiveAndEnabled) return false;
        RefreshBinding();
        if (model != null) return true;
        Debug.LogWarning($"[FrustrationSliderVisualizer] 尚未找到女主角：{heroineID}", this);
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
        if (model != null)
        {
            model.OnFrustrationChanged += HandleFrustrationChanged;
            model.OnFrustrationMaxChanged += HandleFrustrationChanged;
        }
        SynchronizeImmediately();
    }

    private void DetachModel()
    {
        if (model != null)
        {
            model.OnFrustrationChanged -= HandleFrustrationChanged;
            model.OnFrustrationMaxChanged -= HandleFrustrationChanged;
        }
        model = null;
    }

    private void HandleGameStatusLoaded()
    {
        RefreshBinding();
        SynchronizeImmediately();
    }

    private void SynchronizeImmediately()
    {
        notificationVersion++;
        maximum = model != null ? Mathf.Max(1, model.FrustrationMax) : HeroineStatusModel.DefaultFrustrationMax;
        targetValue = model != null ? model.Frustration : 0;
        displayedFill = targetFill = Mathf.Clamp01((float)targetValue / maximum);
        transitionStart = displayedFill;
        transitionElapsed = 0f;
        fullArmed = targetValue < maximum;
        quarterArmed = targetFill < 0.25f;
        halfArmed = targetFill < 0.5f;
        threeQuartersArmed = targetFill < 0.75f;
        RenderVisual();
    }

    private void HandleFrustrationChanged(int unused)
    {
        if (model == null) return;
        int nextMaximum = Mathf.Max(1, model.FrustrationMax);
        int nextValue = model.Frustration;
        // 調整上限可能連續發出上限與數值事件，避免重啟同一次動畫。
        if (maximum == nextMaximum && targetValue == nextValue) return;
        notificationVersion++;
        maximum = nextMaximum;
        targetValue = nextValue;
        targetFill = Mathf.Clamp01((float)targetValue / maximum);
        transitionStart = displayedFill;
        transitionElapsed = 0f;
        // 即使同一幀先降低再集滿，也能開始下一輪通知。
        if (targetValue < maximum) fullArmed = true;
        if (targetFill < 0.25f) quarterArmed = true;
        if (targetFill < 0.5f) halfArmed = true;
        if (targetFill < 0.75f) threeQuartersArmed = true;
    }

    private void Update()
    {
        RefreshBinding();
        if (model == null) return;

        transitionElapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float t = Mathf.Clamp01(transitionElapsed / Mathf.Max(0.01f, transitionDuration));
        displayedFill = Mathf.Lerp(transitionStart, targetFill, t * t * (3f - 2f * t));
        RenderVisual();
        int version = notificationVersion;
        NotifyThreshold(0.25f, ref quarterArmed, onReachedQuarter);
        if (version != notificationVersion) return;
        NotifyThreshold(0.5f, ref halfArmed, onReachedHalf);
        if (version != notificationVersion) return;
        NotifyThreshold(0.75f, ref threeQuartersArmed, onReachedThreeQuarters);
        if (version != notificationVersion) return;
        if (fullArmed && targetValue >= maximum && t >= 1f)
        {
            fullArmed = false;
            // 先完成內部更新；外部事件可改值、換角、停用 UI 或讀檔。
            onReachedMax.Invoke();
        }
    }

    private void NotifyThreshold(float threshold, ref bool armed, UnityEvent notification)
    {
        if (!armed || targetFill < threshold || displayedFill < threshold) return;
        armed = false;
        notification.Invoke();
    }

    private void RenderVisual()
    {
        if (fillImage == null) return;
        fillImage.color = fillColor;
        fillImage.fillAmount = displayedFill;
    }
}
