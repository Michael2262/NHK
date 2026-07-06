using UnityEngine;
using UnityEngine.UI;
using HutongGames.PlayMaker;
using DG.Tweening;

// 解決 Tooltip 命名空間衝突 (UnityEngine 與 HutongGames.PlayMaker 都有 Tooltip)
using Tooltip = UnityEngine.TooltipAttribute;

/// <summary>
/// 精簡版 Slider FSM 數值顯示器。
/// 指定一個來源 FSM，分別填入「最大值」與「當前值」兩個變數名稱 (支援 Int / Float)，
/// Slider 會用 DOTween 即時平滑追趕 FSM 數值。
/// </summary>
[RequireComponent(typeof(Slider))]
public class BasicSliderVisualizer : MonoBehaviour
{
    [Header("1. 數據來源 FSM")]
    [Tooltip("提供數值的 PlayMakerFSM (資料來源)")]
    public PlayMakerFSM sourceFSM;

    [Header("2. 變數名稱設定")]
    [Tooltip("作為 Slider 最大值的 FSM 變數名稱 (自動支援 Int / Float)")]
    public string maxValueVariableName = "fsm_OrgasmMax";

    [Tooltip("作為 Slider 當前值的 FSM 變數名稱 (自動支援 Int / Float)")]
    public string currentValueVariableName = "fsm_LocalOrgasm";

    [Header("3. 漸變動畫設定 (DOTween)")]
    [Tooltip("Slider 追趕數值的漸變時間")]
    public float tweenDuration = 0.2f;

    [Tooltip("漸變的緩動曲線")]
    public Ease tweenEase = Ease.OutQuad;

    // --- 內部控制變數 ---
    private Slider slider;
    private FsmFloatOrInt maxValueRef;
    private FsmFloatOrInt currentValueRef;
    private Tweener currentTweener;
    private float lastValue = float.NaN;
    private bool isInitialized = false;

    void Awake()
    {
        slider = GetComponent<Slider>();
    }

    void Start()
    {
        if (sourceFSM == null)
        {
            Debug.LogError($"[{nameof(BasicSliderVisualizer)}] 尚未指定 sourceFSM，已停用此元件。", this);
            enabled = false;
            return;
        }

        // 包裝兩個變數，內部會自動判斷是 Float 還是 Int
        maxValueRef = new FsmFloatOrInt(sourceFSM, maxValueVariableName);
        currentValueRef = new FsmFloatOrInt(sourceFSM, currentValueVariableName);

        if (!maxValueRef.IsValid)
            Debug.LogWarning($"[{nameof(BasicSliderVisualizer)}] 在 FSM 中找不到最大值變數: {maxValueVariableName}", this);

        if (!currentValueRef.IsValid)
            Debug.LogWarning($"[{nameof(BasicSliderVisualizer)}] 在 FSM 中找不到當前值變數: {currentValueVariableName}", this);
    }

    void Update()
    {
        if (!CheckInitialization()) return;

        // --- 即時更新最大值 (萬一 max 會中途改變) ---
        float max = maxValueRef.Value;
        if (max > 0 && !Mathf.Approximately(slider.maxValue, max))
        {
            slider.maxValue = max;
        }

        // --- 讀取當前值，只有真的變動時才重新發 tween ---
        float actual = currentValueRef.Value;
        if (!Mathf.Approximately(actual, lastValue))
        {
            lastValue = actual;
            float target = Mathf.Clamp(actual, slider.minValue, slider.maxValue);
            UpdateSlider(target);
        }
    }

    /// <summary>
    /// 【外部可呼叫】重新抓取一次 FSM 變數參照，並依當前數值立即刷新 Slider（瞬間定位，不漸變）。
    /// 用於解決進場時序問題：Start() 執行時 FSM 變數尚未就緒、或 sourceFSM 是之後才指定的情況。
    /// 可由 Button.onClick / UnityEvent / PlayMaker (Call Method / Send Message "ForceRefresh") 呼叫。
    /// </summary>
    public void ForceRefresh()
    {
        if (slider == null) slider = GetComponent<Slider>();

        if (sourceFSM == null)
        {
            Debug.LogWarning($"[{nameof(BasicSliderVisualizer)}] ForceRefresh 失敗：sourceFSM 為空。", this);
            return;
        }

        // 若先前因 sourceFSM 為空被停用，這裡重新啟用讓 Update 恢復運作
        enabled = true;

        // 重新抓一次變數參照（涵蓋 FSM 較晚初始化 / 變數執行期才建立的情況）
        maxValueRef = new FsmFloatOrInt(sourceFSM, maxValueVariableName);
        currentValueRef = new FsmFloatOrInt(sourceFSM, currentValueVariableName);

        if (!maxValueRef.IsValid)
            Debug.LogWarning($"[{nameof(BasicSliderVisualizer)}] ForceRefresh：在 FSM 中找不到最大值變數: {maxValueVariableName}", this);

        if (!currentValueRef.IsValid)
            Debug.LogWarning($"[{nameof(BasicSliderVisualizer)}] ForceRefresh：在 FSM 中找不到當前值變數: {currentValueVariableName}", this);

        // 重置狀態：殺掉進行中的 tween，強制重新初始化並瞬間定位
        if (currentTweener != null) currentTweener.Kill();
        isInitialized = false;
        lastValue = float.NaN;

        CheckInitialization();
    }

    /// <summary>用 DOTween 平滑移動 Slider 數值，會先 Kill 掉舊的 tween。</summary>
    private void UpdateSlider(float target)
    {
        if (currentTweener != null) currentTweener.Kill();
        currentTweener = slider.DOValue(target, tweenDuration).SetEase(tweenEase);
    }

    /// <summary>等待 FSM 最大值就緒後才開始運作；首次定位採瞬間到位，不做漸變。</summary>
    private bool CheckInitialization()
    {
        if (isInitialized) return true;
        if (maxValueRef == null || !maxValueRef.IsValid) return false;

        float max = maxValueRef.Value;
        if (max > 0)
        {
            slider.maxValue = max;

            float startValue = Mathf.Clamp(currentValueRef.Value, slider.minValue, slider.maxValue);
            slider.value = startValue;      // 初始化直接定位
            lastValue = currentValueRef.Value;

            isInitialized = true;
            return true;
        }
        return false;
    }

    void OnDisable()
    {
        if (currentTweener != null) currentTweener.Kill();
    }
}

/// <summary>
/// 小工具：包裝一個 FSM 變數，自動判斷它是 FsmFloat 還是 FsmInt，
/// 對外一律以 float 形式讀取，讓上層不必在意原始型別。
/// </summary>
public class FsmFloatOrInt
{
    private readonly FsmFloat fsmFloat;
    private readonly FsmInt fsmInt;

    /// <summary>是否成功對應到一個 Float 或 Int 變數。</summary>
    public bool IsValid => fsmFloat != null || fsmInt != null;

    public FsmFloatOrInt(PlayMakerFSM fsm, string variableName)
    {
        if (fsm == null || string.IsNullOrEmpty(variableName)) return;

        // 優先抓 Float，抓不到再嘗試 Int
        fsmFloat = fsm.FsmVariables.FindFsmFloat(variableName);
        if (fsmFloat == null)
        {
            fsmInt = fsm.FsmVariables.FindFsmInt(variableName);
        }
    }

    /// <summary>以 float 形式回傳當前數值；若皆無效則回傳 0。</summary>
    public float Value
    {
        get
        {
            if (fsmFloat != null) return fsmFloat.Value;
            if (fsmInt != null) return fsmInt.Value;
            return 0f;
        }
    }
}
