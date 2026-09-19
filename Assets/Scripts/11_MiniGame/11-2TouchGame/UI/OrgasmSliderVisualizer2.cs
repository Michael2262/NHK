using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 女主角 Orgasm 專用液體 UI；只處理顯示，實際數值仍由 Model 管理。
/// 掛在填色用的獨立 UI 物件上，外框、背景與數字由其他 UI 物件提供。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(OrgasmWaveGraphic))]
public class OrgasmSliderVisualizer2 : MonoBehaviour
{
    [Header("連動角色與數字")]
    [SerializeField] private string heroineID = "sister";
    [Tooltip("放在填色上方的右側 TMP 數字；顯示值隨填色動畫變動。")]
    [SerializeField] private TMP_Text valueText;

    [Header("填色與平滑過渡")]
    [SerializeField] private Color fillColor = new Color(1f, 0.61f, 0.72f, 1f);
    [Tooltip("每次目標改變後，從目前顯示位置過渡到新值所需秒數。")]
    [Min(0.01f)] [SerializeField] private float transitionDuration = 0.45f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("波浪")]
    [Range(8, 128)] [SerializeField] private int waveSegments = 32;
    [Tooltip("最大波幅，以 RectTransform 的本地 UI 單位表示。")]
    [Min(0f)] [SerializeField] private float waveAmplitude = 9f;
    [Tooltip("右側邊界從下到上的主要波浪週期數。")]
    [Range(0.25f, 4f)] [SerializeField] private float waveCycles = 1.2f;
    [Tooltip("每秒流動的波浪週期數。")]
    [Min(0f)] [SerializeField] private float waveSpeed = 1.5f;
    [Tooltip("數值變化多少點會累積到最大波幅。")]
    [Min(1f)] [SerializeField] private float changeForFullWave = 25f;
    [Tooltip("波浪衰減速度，越大越快平靜。")]
    [Min(0.01f)] [SerializeField] private float waveDamping = 2.5f;

    [Header("滿格閃白")]
    [Min(0.01f)] [SerializeField] private float flashDuration = 2f;
    [Min(0.1f)] [SerializeField] private float flashFrequency = 4f;
    [Tooltip("填色實際抵達右端時觸發一次。可在此修改 Model，UI 會在閃白後追上新值。")]
    [SerializeField] private UnityEvent onReachedMax = new UnityEvent();
    [Tooltip("閃白自然完成時觸發。關閉 UI、切換角色或讀檔會取消動畫，不觸發此事件。")]
    [SerializeField] private UnityEvent onFlashCompleted = new UnityEvent();

    public UnityEvent OnReachedMax => onReachedMax;
    public UnityEvent OnFlashCompleted => onFlashCompleted;
    public float DisplayedValue => displayedValue;

    private OrgasmWaveGraphic waveGraphic;
    private GameStatusService service;
    private HeroineStatusModel model;
    private float maximum = 100f;
    private float displayedValue;
    private float targetValue;
    private float transitionStart;
    private float transitionElapsed;
    private float waveEnergy;
    private float wavePhase;
    private float flashElapsed;
    private bool flashing;
    private bool fullArmed;
    private int lastNumber = -1;

    private void Awake()
    {
        waveGraphic = GetComponent<OrgasmWaveGraphic>();
        waveGraphic.raycastTarget = false;
    }

    private void OnEnable()
    {
        if (waveGraphic == null) waveGraphic = GetComponent<OrgasmWaveGraphic>();
        RefreshBinding();
        SynchronizeImmediately();
    }

    private void OnDisable()
    {
        DetachModel();
        if (service != null) service.OnGameStatusLoaded -= HandleGameStatusLoaded;
        service = null;
        flashing = false;
        waveEnergy = 0f;
        RenderVisual();
    }

    /// <summary>可供 UnityEvent 指定角色；切換時直接同步，不播放滿格事件。</summary>
    public void SetHeroineID(string id)
    {
        heroineID = id;
        if (isActiveAndEnabled) RefreshBinding();
    }

    /// <summary>轉交 Model；與其他 Bridge 的 SetOrgasm 一樣，UI 會平滑追上。</summary>
    public void SetOrgasm(int value)
    {
        if (TryGetModel()) model.SetOrgasm(value);
    }

    public void AddOrgasm(int amount)
    {
        if (TryGetModel()) model.AddOrgasm(amount);
    }

    private bool TryGetModel()
    {
        if (!isActiveAndEnabled) return false;
        RefreshBinding();
        if (model != null) return true;
        Debug.LogWarning($"[OrgasmSliderVisualizer2] 尚未找到女主角：{heroineID}", this);
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
        if (model != null) model.OnOrgasmChanged += HandleOrgasmChanged;
        SynchronizeImmediately();
    }

    private void DetachModel()
    {
        if (model != null) model.OnOrgasmChanged -= HandleOrgasmChanged;
        model = null;
    }

    private void HandleGameStatusLoaded()
    {
        // 讀檔可能重建角色，也可能只改動同一個 Model；兩者都要同步。
        RefreshBinding();
        SynchronizeImmediately();
    }

    private void SynchronizeImmediately()
    {
        maximum = model != null ? Mathf.Max(1, model.OrgasmMax) : 100f;
        displayedValue = targetValue = model != null ? model.Orgasm : 0f;
        transitionStart = displayedValue;
        transitionElapsed = 0f;
        waveEnergy = wavePhase = flashElapsed = 0f;
        flashing = false;
        fullArmed = displayedValue < maximum;
        lastNumber = -1;
        RenderVisual();
    }

    private void HandleOrgasmChanged(int value)
    {
        float next = Mathf.Clamp(value, 0f, maximum);
        waveEnergy = Mathf.Clamp01(waveEnergy + Mathf.Abs(next - targetValue) / Mathf.Max(1f, changeForFullWave));
        targetValue = next;
        transitionStart = displayedValue;
        transitionElapsed = 0f;
    }

    private void Update()
    {
        // 支援 UI 比服務更早啟用、角色重建及 Inspector 切換角色。
        RefreshBinding();
        if (model == null) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        wavePhase = Mathf.Repeat(wavePhase + dt * waveSpeed * Mathf.PI * 2f, Mathf.PI * 200f);
        waveEnergy *= Mathf.Exp(-Mathf.Max(0.01f, waveDamping) * dt);
        if (waveEnergy < 0.001f) waveEnergy = 0f;

        if (flashing)
        {
            flashElapsed += dt;
            if (flashElapsed >= Mathf.Max(0.01f, flashDuration))
            {
                flashing = false;
                transitionStart = displayedValue;
                transitionElapsed = 0f;
                // 閃白期間先暫存目標，真正開始退回時再推動波浪。
                waveEnergy = Mathf.Clamp01(waveEnergy + Mathf.Abs(targetValue - displayedValue)
                    / Mathf.Max(1f, changeForFullWave));
                RenderVisual();
                // 先完成內部狀態，再呼叫外部；事件內可改值、停用或切換角色。
                onFlashCompleted.Invoke();
                return;
            }
        }
        else
        {
            transitionElapsed += dt;
            float t = Mathf.Clamp01(transitionElapsed / Mathf.Max(0.01f, transitionDuration));
            displayedValue = Mathf.Lerp(transitionStart, targetValue, t * t * (3f - 2f * t));
            if (displayedValue < maximum) fullArmed = true;
            if (fullArmed && targetValue >= maximum && t >= 1f)
            {
                displayedValue = maximum;
                fullArmed = false;
                flashing = true;
                flashElapsed = 0f;
                RenderVisual();
                onReachedMax.Invoke();
                return;
            }
        }

        RenderVisual();
    }

    private void RenderVisual()
    {
        Color tint = fillColor;
        if (flashing)
        {
            float pulse = 0.5f + 0.5f * Mathf.Cos(flashElapsed * flashFrequency * Mathf.PI * 2f);
            tint = Color.Lerp(fillColor, new Color(1f, 1f, 1f, fillColor.a), pulse);
        }
        if (waveGraphic != null)
            waveGraphic.SetVisual(displayedValue / maximum, waveAmplitude * waveEnergy,
                wavePhase, waveCycles, waveSegments, tint);

        // 尚未真正滿格時不提早顯示 100。
        int number = displayedValue >= maximum ? Mathf.RoundToInt(maximum)
            : Mathf.Min(Mathf.RoundToInt(displayedValue), Mathf.CeilToInt(maximum) - 1);
        if (valueText != null && number != lastNumber)
        {
            valueText.SetText("{0:0}", number);
            lastNumber = number;
        }
    }
}
