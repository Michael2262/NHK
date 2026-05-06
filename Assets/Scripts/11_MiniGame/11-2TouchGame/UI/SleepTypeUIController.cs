using UnityEngine;
using UnityEngine.UI;
using HutongGames.PlayMaker;
using DG.Tweening;
using Tooltip = UnityEngine.TooltipAttribute;

public class SleepTypeUIController : MonoBehaviour
{
    [Header("FSM 設定")]
    public GameObject targetObject;
    public string fsmName = "HeroineEmotionFSM";
    public string enumVarName = "SleepType";

    [Header("UI 顯示 — 依 SleepType 順序放入")]
    [Tooltip("依序: DeepSleep, Sleep, HalfAwake, Groggy, Awake")]
    public GameObject[] sleepTypeObjects; // 5 個對應的 GameObject

    [Header("跳動圓圈")]
    public RectTransform pulseCircle; // 黑色圓圈的 RectTransform

    [Header("跳動設定")]
    [Tooltip("每個 SleepType 對應的跳動速度，依序: DeepSleep → Awake")]
    public float[] pulseSpeeds = new float[] { 0.5f, 1f, 2f, 3.5f, 6f };

    [Tooltip("跳動的縮放幅度")]
    [Range(0.01f, 0.3f)]
    public float pulseIntensity = 0.08f;

    [Header("變色設定")]
    [Tooltip("達到此 index (含) 時切換為高亮色，低於則回到原色")]
    public int colorChangeThreshold = 3; // 例如 Groggy=3 時開始變色

    [Tooltip("圓圈原始顏色")]
    public Color normalColor = Color.black;

    [Tooltip("達到門檻後的顏色")]
    public Color highlightColor = Color.red;

    private const float COLOR_FADE_DURATION = 0.3f;

    private PlayMakerFSM fsm;
    private FsmEnum cachedVar;
    private int lastIndex = -1;
    private float currentSpeed;
    private Vector3 baseScale;

    private Graphic pulseGraphic;   // Image / RawImage 都吃 Graphic
    private Tween colorTween;
    private bool isHighlighted = false;

    void Start()
    {
        fsm = ActionHelpers.GetGameObjectFsm(targetObject, fsmName);
        if (fsm != null)
            cachedVar = fsm.FsmVariables.GetFsmEnum(enumVarName);

        if (pulseCircle != null)
        {
            baseScale = pulseCircle.localScale;
            pulseGraphic = pulseCircle.GetComponent<Graphic>();
            if (pulseGraphic != null)
                pulseGraphic.color = normalColor;
        }

        // 初始化一次
        CheckAndUpdate();
    }

    void Update()
    {
        CheckAndUpdate();
        AnimatePulse();
    }

    void OnDestroy()
    {
        // 防止物件被銷毀時 tween 還在跑
        if (colorTween != null && colorTween.IsActive())
            colorTween.Kill();
    }

    private void CheckAndUpdate()
    {
        if (cachedVar == null) return;

        int currentIndex = System.Convert.ToInt32(cachedVar.Value);
        if (currentIndex == lastIndex) return;

        lastIndex = currentIndex;
        OnSleepTypeChanged(currentIndex);
    }

    private void OnSleepTypeChanged(int index)
    {
        // 切換顯示對應的 GameObject，關閉其他
        for (int i = 0; i < sleepTypeObjects.Length; i++)
        {
            if (sleepTypeObjects[i] != null)
                sleepTypeObjects[i].SetActive(i == index);
        }

        // 更新跳動速度
        if (index < pulseSpeeds.Length)
            currentSpeed = pulseSpeeds[index];

        // 處理變色
        UpdatePulseColor(index);
    }

    private void UpdatePulseColor(int index)
    {
        if (pulseGraphic == null) return;

        bool shouldHighlight = index >= colorChangeThreshold;
        if (shouldHighlight == isHighlighted) return; // 狀態沒變就不動

        isHighlighted = shouldHighlight;

        // 中斷正在進行的 tween，避免疊加
        if (colorTween != null && colorTween.IsActive())
            colorTween.Kill();

        Color targetColor = shouldHighlight ? highlightColor : normalColor;
        colorTween = pulseGraphic.DOColor(targetColor, COLOR_FADE_DURATION)
                                 .SetEase(Ease.OutQuad);
    }

    private void AnimatePulse()
    {
        if (pulseCircle == null) return;

        float pulse = 1f + Mathf.Sin(Time.time * currentSpeed * Mathf.PI * 2f) * pulseIntensity;
        pulseCircle.localScale = baseScale * pulse;
    }
}