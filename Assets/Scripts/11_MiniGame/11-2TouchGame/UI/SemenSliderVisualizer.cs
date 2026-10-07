using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 主角 Semen 填色 UI；滿格後等待玩家操作，由外部取消等待或開始分段衰退。
/// 按鈕的滿值閃爍由 ShootButtonGroup 自行控制。
/// 掛在獨立的填色 Image 上，外框與背景由其他 UI 物件提供。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class SemenSliderVisualizer : MonoBehaviour
{
    [Header("填色與平滑過渡")]
    [SerializeField] private Color fillColor = new Color(156f / 255f, 253f / 255f, 1f, 1f);
    [Tooltip("每次目標改變後，從目前顯示位置過渡到新值所需秒數。")]
    [Min(0.01f)] [SerializeField] private float transitionDuration = 0.45f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("滿格事件與等待")]
    [Tooltip("填色抵達滿格後，等待多久才觸發 On Flash Wait。保留原欄位名稱以相容既有設定。")]
    [Min(0f)] [SerializeField] private float flashWaitDuration = 2f;
    [Tooltip("填色抵達滿格時觸發一次。")]
    [SerializeField] private UnityEvent onReachedMax = new UnityEvent();
    [Tooltip("滿格等待時間到達時觸發一次，不會自動歸零或衰退。呼叫 StopFlashing 可取消等待，不影響按鈕閃爍。")]
    [SerializeField] private UnityEvent onFlashWait = new UnityEvent();

    [Header("高潮後分段衰退（由外部呼叫開始）")]
    [Tooltip("呼叫 StartDecay 時將 Model 歸零，顯示值分段下降。期間實際值仍可累積。")]
    [Min(0.01f)] [SerializeField] private float decayDuration = 5f;
    [Tooltip("例如 5 段：100 → 80 → 60 → 40 → 20 → 0。")]
    [Range(1, 20)] [SerializeField] private int decaySteps = 5;
    [Tooltip("每段前段用來下降的時間比例，剩餘時間停留。")]
    [Range(0.1f, 1f)] [SerializeField] private float decayMoveRatio = 0.5f;

    public UnityEvent OnReachedMax => onReachedMax;
    public UnityEvent OnFlashWait => onFlashWait;
    public float DisplayedValue => displayedValue;

    private enum VisualState { Tracking, Waiting, Holding, Decaying }

    private Image fillImage;
    private GameStatusService service;
    private ProtagonistStatusModel model;
    private VisualState state;
    private float maximum = 100f;
    private float displayedValue;
    private float targetValue;
    private float transitionStart;
    private float transitionElapsed;
    private float flashElapsed;
    private float decayElapsed;
    private bool flashWaitInvoked;
    private bool fullArmed;

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
        state = VisualState.Tracking;
        RenderVisual();
    }

    /// <summary>轉交 Model 設值；UI 平滑追上。</summary>
    public void SetSemen(int value)
    {
        if (TryGetModel()) model.SetSemen(value);
    }

    public void AddSemen(int amount)
    {
        if (TryGetModel()) model.AddSemen(amount);
    }

    /// <summary>保留舊 UnityEvent 入口：取消滿格等待並保持滿格，不歸零、不衰退，也不控制按鈕閃爍。</summary>
    public void StopFlashing()
    {
        if (!isActiveAndEnabled) return;
        RefreshBinding();
        if (state != VisualState.Waiting) return;
        state = VisualState.Holding;
        RenderVisual();
    }

    /// <summary>供 UnityEvent 呼叫：結束滿格等待、將 Model 歸零並開始分段衰退。</summary>
    public void StartDecay()
    {
        if (!TryGetModel()) return;
        if (state != VisualState.Waiting && state != VisualState.Holding) return;
        // 先切入衰退，避免歸零事件把顯示值直接拉走；重複呼叫也不會再次歸零。
        state = VisualState.Decaying;
        decayElapsed = 0f;
        model.SetSemen(0);
        // 訂閱者若關閉 UI 或讀檔，沿用其同步後的狀態，不再覆寫。
        RenderVisual();
    }

    private bool TryGetModel()
    {
        if (!isActiveAndEnabled) return false;
        RefreshBinding();
        if (model != null) return true;
        Debug.LogWarning("[SemenSliderVisualizer] 尚未找到主角 Model。", this);
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

        var next = service != null ? service.Protagonist : null;
        if (ReferenceEquals(model, next)) return;
        DetachModel();
        model = next;
        if (model != null) model.OnSemenChanged += HandleSemenChanged;
        SynchronizeImmediately();
    }

    private void DetachModel()
    {
        if (model != null) model.OnSemenChanged -= HandleSemenChanged;
        model = null;
    }

    private void HandleGameStatusLoaded()
    {
        RefreshBinding();
        SynchronizeImmediately();
    }

    private void SynchronizeImmediately()
    {
        maximum = model != null ? Mathf.Max(1, model.SemenMax) : 100f;
        displayedValue = targetValue = model != null ? model.Semen : 0f;
        transitionStart = displayedValue;
        transitionElapsed = flashElapsed = decayElapsed = 0f;
        state = VisualState.Tracking;
        flashWaitInvoked = false;
        // 與範例一致：啟用或讀檔時同步，不補播滿格事件。
        fullArmed = displayedValue < maximum;
        RenderVisual();
    }

    private void HandleSemenChanged(int value)
    {
        targetValue = Mathf.Clamp(value, 0f, maximum);
        // 玩家操作與演出期間只記錄最新值，不改變滿格等待或衰退流程。
        if (state != VisualState.Tracking) return;
        transitionStart = displayedValue;
        transitionElapsed = 0f;
    }

    private void Update()
    {
        RefreshBinding();
        if (model == null) return;
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        if (state == VisualState.Waiting)
        {
            flashElapsed += dt;
            if (!flashWaitInvoked && flashElapsed >= Mathf.Max(0f, flashWaitDuration))
            {
                flashWaitInvoked = true;
                RenderVisual();
                // 先完成內部更新；事件可取消等待、開始衰退、停用 UI 或讀檔。
                onFlashWait.Invoke();
                return;
            }
        }
        else if (state == VisualState.Decaying)
        {
            UpdateDecay(dt);
        }
        else if (state == VisualState.Tracking)
        {
            transitionElapsed += dt;
            float t = Mathf.Clamp01(transitionElapsed / Mathf.Max(0.01f, transitionDuration));
            displayedValue = Mathf.Lerp(transitionStart, targetValue, t * t * (3f - 2f * t));
            if (targetValue < maximum) fullArmed = true;
            if (fullArmed && targetValue >= maximum && t >= 1f)
            {
                displayedValue = maximum;
                fullArmed = false;
                state = VisualState.Waiting;
                flashElapsed = 0f;
                flashWaitInvoked = false;
                RenderVisual();
                onReachedMax.Invoke();
                return;
            }
        }
        RenderVisual();
    }

    private void UpdateDecay(float dt)
    {
        decayElapsed += dt;
        float duration = Mathf.Max(0.01f, decayDuration);
        int steps = Mathf.Clamp(decaySteps, 1, 20);
        float progress = Mathf.Clamp01(decayElapsed / duration) * steps;
        int step = Mathf.Min(Mathf.FloorToInt(progress), steps - 1);
        float t = Mathf.Clamp01((progress - step) / Mathf.Clamp(decayMoveRatio, 0.1f, 1f));
        // 沿用範例：每段下降後稍微回彈，再停留。
        float u = t - 1f;
        float eased = 1f + 2.70158f * u * u * u + 1.70158f * u * u;
        float from = maximum * (1f - (float)step / steps);
        float to = maximum * (1f - (float)(step + 1) / steps);
        displayedValue = Mathf.Clamp(Mathf.LerpUnclamped(from, to, eased), 0f, maximum);

        if (decayElapsed < duration) return;
        displayedValue = 0f;
        state = VisualState.Tracking;
        targetValue = Mathf.Clamp(model.Semen, 0f, maximum);
        transitionStart = displayedValue;
        transitionElapsed = 0f;
        fullArmed = true;
    }

    private void RenderVisual()
    {
        if (fillImage == null) return;
        fillImage.color = fillColor;
        fillImage.fillAmount = Mathf.Clamp01(displayedValue / maximum);
    }
}
