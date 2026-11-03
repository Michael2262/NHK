using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>依正式女主角 Awake 推算的睡眠階段，更新 UI 與圓圈動畫。</summary>
public class SleepTypeUIController : MonoBehaviour
{
    [Header("連動角色")]
    [SerializeField] private string heroineID = "sister";

    [Header("UI 顯示 — 依 Awake 六階段順序放入")]
    [Tooltip("必須為六格，依序：DeepSleep、SoundSleep、Sleep、HalfAwake、Dazed、Wake。請勿直接沿用舊五階段排列。")]
    public GameObject[] sleepTypeObjects = new GameObject[6];

    [Header("跳動圓圈")]
    public RectTransform pulseCircle;

    [Header("跳動設定")]
    [Tooltip("六格依序：DeepSleep、SoundSleep、Sleep、HalfAwake、Dazed、Wake；單位為每秒跳動次數。")]
    public float[] pulseSpeeds = new float[] { 0.5f, 0.75f, 1f, 2f, 3.5f, 6f };

    [Tooltip("跳動的縮放幅度")]
    [Range(0.01f, 0.3f)]
    public float pulseIntensity = 0.08f;

    [Header("變色設定")]
    [Tooltip("達到此階段（含）時切換為高亮色，低於則回到原色。預設 Dazed：Awake 80 以上。")]
    public HeroineSleepState highlightFromState = HeroineSleepState.Dazed;

    [Tooltip("圓圈原始顏色")]
    public Color normalColor = Color.black;

    [Tooltip("達到門檻後的顏色")]
    public Color highlightColor = Color.red;

    private const float COLOR_FADE_DURATION = 0.3f;
    private const int STAGE_COUNT = 6;
    private GameStatusService service;
    private HeroineStatusModel model;
    private int lastIndex = -1;
    private float currentSpeed;
    private Vector3 baseScale;
    private Graphic pulseGraphic;
    private Tween colorTween;
    private bool isHighlighted;

    private void Awake()
    {
        if (pulseCircle == null) return;
        baseScale = pulseCircle.localScale;
        pulseGraphic = pulseCircle.GetComponent<Graphic>();
    }

    private void OnEnable()
    {
        if (sleepTypeObjects == null || sleepTypeObjects.Length != STAGE_COUNT)
            Debug.LogWarning("[SleepTypeUIController] 請將 Sleep Type Objects 設為六格，依序放入 DeepSleep、SoundSleep、Sleep、HalfAwake、Dazed、Wake；設定完成前隱藏階段 UI。", this);
        if (pulseSpeeds == null || pulseSpeeds.Length != STAGE_COUNT)
            Debug.LogWarning("[SleepTypeUIController] 請將 Pulse Speeds 設為六格並依新的睡眠階段排列；設定完成前暫停跳動。", this);
        RefreshBinding();
        CheckAndUpdate(true);
    }

    private void Update()
    {
        // 支援服務延後初始化、角色替換及 Inspector 修改角色 ID。
        RefreshBinding();
        AnimatePulse();
    }

    private void OnDisable()
    {
        DetachModel();
        if (service != null) service.OnGameStatusLoaded -= HandleGameStatusLoaded;
        service = null;
        if (colorTween != null && colorTween.IsActive()) colorTween.Kill();
        colorTween = null;
        if (pulseCircle != null) pulseCircle.localScale = baseScale;
    }

    public void SetHeroineID(string id)
    {
        heroineID = id;
        if (isActiveAndEnabled) RefreshBinding();
    }

    private void RefreshBinding()
    {
        var nextService = GameStatusService.Instance;
        if (service != nextService)
        {
            if (service != null) service.OnGameStatusLoaded -= HandleGameStatusLoaded;
            service = nextService;
            if (service != null) service.OnGameStatusLoaded += HandleGameStatusLoaded;
        }

        HeroineStatusModel nextModel = null;
        if (service != null && service.Heroines != null && !string.IsNullOrEmpty(heroineID))
            service.Heroines.TryGetValue(heroineID, out nextModel);
        if (ReferenceEquals(model, nextModel)) return;

        DetachModel();
        model = nextModel;
        if (model != null) model.OnAwakeChanged += HandleAwakeChanged;
        CheckAndUpdate(true);
    }

    private void DetachModel()
    {
        if (model != null) model.OnAwakeChanged -= HandleAwakeChanged;
        model = null;
    }

    private void HandleAwakeChanged(float unused)
    {
        CheckAndUpdate();
    }

    private void HandleGameStatusLoaded()
    {
        RefreshBinding();
        CheckAndUpdate(true);
    }

    private void CheckAndUpdate(bool immediate = false)
    {
        // 門檻統一由 Model 定義；找不到角色時不顯示任何階段。
        int currentIndex = model != null ? (int)model.CurrentSleepState : -1;
        if (!immediate && currentIndex == lastIndex) return;
        lastIndex = currentIndex;
        ApplyStage(currentIndex, immediate);
    }

    private void ApplyStage(int index, bool immediate)
    {
        // 舊五格設定不可直接解讀為新六階段，避免睡眠狀態錯位。
        if (sleepTypeObjects != null)
        {
            for (int i = 0; i < sleepTypeObjects.Length; i++)
            {
                var stageObject = sleepTypeObjects[i];
                if (stageObject == null) continue;
                // 不可關閉控制器本身或其父物件，否則後續階段無法刷新。
                if (transform.IsChildOf(stageObject.transform)) continue;
                stageObject.SetActive(sleepTypeObjects.Length == STAGE_COUNT && i == index);
                if (!isActiveAndEnabled) return;
            }
        }

        currentSpeed = index >= 0 && index < STAGE_COUNT && pulseSpeeds != null && pulseSpeeds.Length == STAGE_COUNT
            ? Mathf.Max(0f, pulseSpeeds[index]) : 0f;
        UpdatePulseColor(index, immediate);
    }

    private void UpdatePulseColor(int index, bool immediate)
    {
        if (pulseGraphic == null) return;
        bool shouldHighlight = index >= 0 && index >= (int)highlightFromState;
        if (!immediate && shouldHighlight == isHighlighted) return;
        isHighlighted = shouldHighlight;

        if (colorTween != null && colorTween.IsActive()) colorTween.Kill();
        colorTween = null;
        Color targetColor = shouldHighlight ? highlightColor : normalColor;
        if (immediate)
            pulseGraphic.color = targetColor;
        else
            colorTween = pulseGraphic.DOColor(targetColor, COLOR_FADE_DURATION).SetEase(Ease.OutQuad);
    }

    private void AnimatePulse()
    {
        if (pulseCircle == null) return;
        float pulse = 1f + Mathf.Sin(Time.time * currentSpeed * Mathf.PI * 2f) * pulseIntensity;
        pulseCircle.localScale = baseScale * pulse;
    }
}
