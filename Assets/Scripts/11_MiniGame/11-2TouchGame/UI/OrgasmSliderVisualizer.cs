using UnityEngine;
using UnityEngine.UI;
using HutongGames.PlayMaker;
using DG.Tweening;

// 解決 Tooltip 命名空間衝突
using Tooltip = UnityEngine.TooltipAttribute;

[RequireComponent(typeof(Slider))]
public class OrgasmSliderVisualizer : MonoBehaviour
{
    private enum State { Normal, Maintaining, PerformingDecay }

    [Header("1. 連動對象 (外部數據來源)")]
    public PlayMakerFSM eroMinigameFSM;

    [Header("2. 事件發送對象 (本機)")]
    [Tooltip("接收 A, B, C 事件的 FSM。若不手動拖拽，啟動時會自動抓取同物件上的第一個 PlayMakerFSM")]
    public PlayMakerFSM localEventFSM;

    [Header("3. 基礎追趕設定 (Normal)")]
    public float tweenDuration = 0.2f;
    public Ease normalEase = Ease.OutQuad;
    public AnimationCurve displayCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("4. 滿格維持與震動 (Maintenance)")]
    public float maintenanceDuration = 1.0f;
    public float shakeIntensity = 5.0f;

    [Header("5. 閃爍特效設定 (Flash)")]
    public Image flashImage;
    public Color flashColor = Color.white;
    public float flashDuration = 0.1f;

    [Header("6. 表演扣除設定 (Decay)")]
    public float totalPerformanceDuration = 5.0f;
    public float decayInterval = 1.0f;
    public float decayAmountX = 15f;
    public float decayTweenDuration = 0.5f;
    public Ease decayEase = Ease.OutBack;

    [Header("7. 結束回歸設定 (Return)")]
    public Ease returnEase = Ease.InOutSine;

    [Header("8. FSM 事件自定義")]
    [UnityEngine.Tooltip("警戒狀態的開始，當數值從低位往上爬，第一次碰到或超過 80% 的瞬間，發送事件")]
    [Range(0, 1)] public float thresholdA = 0.8f;      // 觸發門檻
    [UnityEngine.Tooltip("回到安全狀態，只有當數值掉到 60% 以下時，這道門才會重新開啟")]
    [Range(0, 1)] public float resetThresholdA = 0.6f; // 重置門檻（需小於觸發門檻）
    public string eventA_Warning = "ORG_THRESHOLD_REACHED";
    [Tooltip("事件 B：進入 Maintaining (滿格) 的瞬間傳送")]
    public string eventB_ClimaxStart = "ORG_CLIMAX_START";
    [Tooltip("事件 C：PerformingDecay 每次執行扣除循環時傳送")]
    public string eventC_DecayPulse = "ORG_DECAY_PULSE";

    // --- 內部控制變數 ---
    private Slider slider;
    private FsmFloat fsmLocalOrgasm;
    private FsmFloat fsmOrgasmMax;
    private State currentState = State.Normal;

    private float lastFsmValue;
    private float performanceTimer;
    private float decayLoopTimer;
    private float visualPerformanceValue;
    private bool isInitialized = false;
    private bool hasTriggeredA = false; // 用於確保 A 事件在單次上升中只傳一次

    private Tweener currentTweener;
    private Tweener shakeTweener;
    private Tweener flashTweener;

    private Vector3 originalLocalPosition;
    private Color originalColor;

    void Awake()
    {
        slider = GetComponent<Slider>();
        originalLocalPosition = transform.localPosition;

        if (flashImage == null && slider.fillRect != null)
        {
            flashImage = slider.fillRect.GetComponent<Image>();
        }

        if (flashImage != null)
        {
            originalColor = flashImage.color;
        }

        // 自動嘗試抓取本機物件上的 FSM
        if (localEventFSM == null)
        {
            localEventFSM = GetComponent<PlayMakerFSM>();
        }
    }

    void Start()
    {
        if (eroMinigameFSM == null) return;
        fsmLocalOrgasm = eroMinigameFSM.FsmVariables.FindFsmFloat("fsm_LocalOrgasm");
        fsmOrgasmMax = eroMinigameFSM.FsmVariables.FindFsmFloat("fsm_OrgasmMax");
    }

    void Update()
    {
        if (!CheckInitialization()) return;

        switch (currentState)
        {
            case State.Normal:
                HandleNormalTracking();
                break;
            case State.Maintaining:
                HandleMaintenance();
                break;
            case State.PerformingDecay:
                HandlePerformanceDecay();
                break;
        }
    }

    void HandleNormalTracking()
    {
        float actual = fsmLocalOrgasm.Value;
        float max = fsmOrgasmMax.Value;

        if (max <= 0) return;

        float ratio = actual / max;

        // --- [核心修正] 優先檢查是否達到 100% ---
        // 使用 >= 並配合一個極小的誤差值 (Epsilon) 確保觸發
        if (actual >= max - 0.001f)
        {
            // 如果直接衝到 100% 卻還沒傳過 A 事件，這裡強制補傳或標記已觸發
            if (!hasTriggeredA)
            {
                SendLocalEvent(eventA_Warning);
                hasTriggeredA = true;
            }

            EnterPerformance(); // 這裡會傳送事件 B (ON_CLIMAX_START)
            return; // 進入表演模式後，下方邏輯不再執行
        }

        // --- [事件 A 邏輯] 只有在未達到 100% 時才處理 80% 的邏輯 ---
        if (!hasTriggeredA && ratio >= thresholdA)
        {
            SendLocalEvent(eventA_Warning);
            hasTriggeredA = true;
        }
        else if (hasTriggeredA && ratio < resetThresholdA)
        {
            // 掉回重置門檻（例如 0.6）以下，才允許下次再次觸發 A
            hasTriggeredA = false;
        }

        // 更新 Slider 視覺追趕
        if (!Mathf.Approximately(actual, lastFsmValue))
        {
            lastFsmValue = actual;
            float target = CalculateCurveValue(actual);
            UpdateSlider(target, tweenDuration, normalEase);
        }
    }

    void HandleMaintenance()
    {
        performanceTimer += Time.deltaTime;
        slider.value = slider.maxValue;

        if (performanceTimer >= maintenanceDuration)
        {
            StopPerformanceVisuals();
            currentState = State.PerformingDecay;
            visualPerformanceValue = slider.maxValue;
            decayLoopTimer = 0;
        }
    }

    void HandlePerformanceDecay()
    {
        performanceTimer += Time.deltaTime;
        decayLoopTimer += Time.deltaTime;

        float currentActual = fsmLocalOrgasm.Value;

        if (currentActual >= visualPerformanceValue)
        {
            ExitPerformance();
            return;
        }

        float fsmDelta = currentActual - lastFsmValue;
        if (fsmDelta > 0) visualPerformanceValue += fsmDelta;
        lastFsmValue = currentActual;

        // --- 處理 C 事件: 每次扣除脈衝時觸發 ---
        if (decayLoopTimer >= decayInterval)
        {
            visualPerformanceValue -= decayAmountX;
            visualPerformanceValue = Mathf.Clamp(visualPerformanceValue, 0, slider.maxValue);
            decayLoopTimer = 0;
            UpdateSlider(visualPerformanceValue, decayTweenDuration, decayEase);

            SendLocalEvent(eventC_DecayPulse);
        }

        if (performanceTimer >= totalPerformanceDuration)
        {
            ExitPerformance();
        }
    }

    void EnterPerformance()
    {
        Debug.Log("<color=red>EnterPerformance 被呼叫了！</color>");
        currentState = State.Maintaining;
        performanceTimer = 0;
        lastFsmValue = fsmLocalOrgasm.Value;

        // --- 處理 B 事件: Maintaining 開始瞬間 ---
        SendLocalEvent(eventB_ClimaxStart);

        originalLocalPosition = transform.localPosition;
        if (shakeIntensity > 0)
        {
            shakeTweener = transform.DOShakePosition(maintenanceDuration, shakeIntensity, 15, 90, false, false)
                            .SetLoops(-1);
        }

        if (flashImage != null)
        {
            flashTweener = flashImage.DOColor(flashColor, flashDuration)
                            .SetLoops(-1, LoopType.Yoyo)
                            .SetEase(Ease.InOutSine);
        }
    }

    void ExitPerformance()
    {
        StopPerformanceVisuals();
        currentState = State.Normal;
        hasTriggeredA = false; // 離開表演模式重置 A 事件

        float target = CalculateCurveValue(fsmLocalOrgasm.Value);
        UpdateSlider(target, tweenDuration * 2, returnEase);
    }

    // --- 工具 ---

    private void SendLocalEvent(string eventName)
    {
        if (localEventFSM != null && !string.IsNullOrEmpty(eventName))
        {
            localEventFSM.SendEvent(eventName);
        }
    }

    void UpdateSlider(float target, float duration, Ease ease)
    {
        if (currentTweener != null) currentTweener.Kill();
        currentTweener = slider.DOValue(target, duration).SetEase(ease);
    }

    void StopPerformanceVisuals()
    {
        if (shakeTweener != null)
        {
            shakeTweener.Kill();
            transform.localPosition = originalLocalPosition;
            shakeTweener = null;
        }

        if (flashTweener != null)
        {
            flashTweener.Kill();
            if (flashImage != null) flashImage.color = originalColor;
            flashTweener = null;
        }
    }

    float CalculateCurveValue(float actual)
    {
        if (fsmOrgasmMax.Value <= 0) return 0;
        float normalized = Mathf.Clamp01(actual / fsmOrgasmMax.Value);
        return displayCurve.Evaluate(normalized) * fsmOrgasmMax.Value;
    }

    bool CheckInitialization()
    {
        if (isInitialized) return true;
        if (fsmOrgasmMax != null && fsmOrgasmMax.Value > 0)
        {
            slider.maxValue = fsmOrgasmMax.Value;
            isInitialized = true;
            return true;
        }
        return false;
    }
}