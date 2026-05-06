using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 一個附加在 Slider 上的輔助腳本。
/// 它提供一個公開方法 SetTargetValue() 讓 FSM 呼叫。
/// 它會在自己的 Update() 中，自動將 Slider 的值平滑地 "Damp" (阻尼) 到目標值。
/// </summary>
[RequireComponent(typeof(Slider))]
public class UiSliderSmoother : MonoBehaviour
{
    [Tooltip("Slider 追上目標值所需的大約時間（秒）。值越小，追越快。")]
    [SerializeField] private float smoothTime = 0.1f;

    private Slider slider;
    private float targetValue;
    private float currentVelocity; // SmoothDamp 需要的內部變數

    void Awake()
    {
        // 自動獲取 Slider 組件
        slider = GetComponent<Slider>();

        targetValue = 0;
    }

    /// <summary>
    /// (供 PlayMaker FSM 呼叫)
    /// 設定 Slider 的最大值，只需呼叫一次。
    /// </summary>
    public void SetMaxValue(float maxValue)
    {
        slider.maxValue = maxValue;
    }

    /// <summary>
    /// (供 PlayMaker FSM 呼叫)
    /// 設定 Slider 應該追蹤的「目標值」。
    /// </summary>
    public void SetTargetValue(float newTarget)
    {
        this.targetValue = newTarget;
    }

    /// <summary>
    /// (供 PlayMaker FSM 呼叫)
    /// 將 Slider 立即歸零。
    /// </summary>
    public void ResetSlider()
    {
        targetValue = 0;
        slider.value = 0;
        currentVelocity = 0;
    }

    /// <summary>
    /// 每幀自動執行，平滑地更新 Slider 的 value。
    /// </summary>
    void Update()
    {
        // 使用 Mathf.SmoothDamp 來平滑地將目前值 (slider.value) 
        // 趨近於目標值 (targetValue)
        float newDisplayValue = Mathf.SmoothDamp(
            slider.value,
            targetValue,
            ref currentVelocity,
            smoothTime
        );

        //Debug.Log($"slider.value={slider.value}, targetValue={targetValue}, new={newDisplayValue}");


        // 將平滑後的值套用回 Slider
        slider.value = newDisplayValue;
    }
}