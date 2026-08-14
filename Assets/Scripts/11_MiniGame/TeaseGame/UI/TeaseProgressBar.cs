using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 跑條顯示：讀 TeaseActionGate.Progress，驅動 Slider 或 Filled Image。
/// 空閒（未跑條）時可自動隱藏整條。
/// 純顯示層，不與 gate 邏輯耦合。
/// </summary>
public class TeaseProgressBar : MonoBehaviour
{
    [Header("來源")]
    [Tooltip("指定 gate；留空則用 TeaseActionGate.Instance。")]
    [SerializeField] private TeaseActionGate gate;

    [Header("顯示對象（擇一或並用）")]
    [Tooltip("要驅動的 Slider。")]
    [SerializeField] private Slider slider;

    [Tooltip("要驅動的 Image（Image Type 需設為 Filled）。")]
    [SerializeField] private Image fillImage;

    [Header("選項")]
    [Tooltip("空閒（未跑條）時隱藏此物件。留空則不自動隱藏。")]
    [SerializeField] private GameObject barRoot;

    private void Update()
    {
        var g = gate != null ? gate : TeaseActionGate.Instance;
        if (g == null) return;

        float p = g.Progress;

        if (slider != null) slider.value = p;
        if (fillImage != null) fillImage.fillAmount = p;

        if (barRoot != null) barRoot.SetActive(g.IsBusy);
    }
}
