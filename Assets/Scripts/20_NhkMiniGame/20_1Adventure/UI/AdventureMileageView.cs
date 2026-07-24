using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 里程進度視圖：一條 Slider + 一段文字（4 / 8）。
/// 訂閱 AdventureController 的里程事件自動更新，不需要外部手動呼叫。
/// </summary>
public class AdventureMileageView : MonoBehaviour
{
    [Header("參照")]
    [SerializeField] private AdventureController _controller;

    [Tooltip("進度條，依 目前里程 / 所需里程 顯示")]
    [SerializeField] private Slider _slider;

    [Tooltip("顯示 目前/所需 的文字，可留空")]
    [SerializeField] private TMP_Text _label;

    [Header("文字格式")]
    [Tooltip("{0}=目前里程 {1}=所需里程")]
    [SerializeField] private string _format = "{0} / {1}";

    [Header("Slider 漸變")]
    [Tooltip("Slider 補間到目標值的秒數（0 = 不補間，直接跳）")]
    [SerializeField] private float _sliderTweenDuration = 0.4f;
    [SerializeField] private Ease _sliderEase = Ease.OutCubic;

    private Tween _sliderTween;

    private void OnEnable()
    {
        if (_controller == null) return;
        _controller.onMileageChanged.AddListener(OnMileageChanged);
        _controller.onTotalMileageChanged.AddListener(OnTotalChanged);
        Refresh();
    }

    private void OnDisable()
    {
        if (_controller == null) return;
        _controller.onMileageChanged.RemoveListener(OnMileageChanged);
        _controller.onTotalMileageChanged.RemoveListener(OnTotalChanged);
    }

    private void OnMileageChanged(int _) => Refresh();
    private void OnTotalChanged(int _) => Refresh();

    /// <summary>依目前 Run 狀態重畫一次。Slider 會補間，文字立即更新。</summary>
    public void Refresh()
    {
        var run = _controller != null ? _controller.Run : null;
        int current = run != null ? run.CurrentMileage : 0;
        int total = run != null ? run.TotalMileage : 0;

        if (_slider != null)
        {
            _slider.minValue = 0f;
            _slider.maxValue = Mathf.Max(1, total); // 防止 total=0 時除以 0

            _sliderTween?.Kill();
            if (_sliderTweenDuration > 0f && isActiveAndEnabled)
                _sliderTween = _slider.DOValue(current, _sliderTweenDuration).SetEase(_sliderEase);
            else
                _slider.value = current;
        }

        if (_label != null)
            _label.text = string.Format(_format, current, total);
    }

    /// <summary>不補間、立刻把 Slider 設到目前值（例如剛開始一輪、避免從舊值滑過來）。</summary>
    public void SnapToCurrent()
    {
        _sliderTween?.Kill();
        var run = _controller != null ? _controller.Run : null;
        if (_slider != null) _slider.value = run != null ? run.CurrentMileage : 0;
    }

    private void OnDestroy() => _sliderTween?.Kill();
}
