using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 休息次數顯示：一段文字（3 / 3），並可連動一個 CanvasGroup
/// （通常綁休息按鈕）—— 次數用完時變半透明且不可點擊，有次數時恢復。
/// 訂閱 AdventureController 的休息事件自動更新，不需要外部手動呼叫。
/// </summary>
public class AdventureRestView : MonoBehaviour
{
    [Header("參照")]
    [SerializeField] private AdventureController _controller;

    [Tooltip("顯示 剩餘/上限 的文字")]
    [SerializeField] private TMP_Text _label;

    [Tooltip("休息次數用完時要變灰、不可點的對象（通常是休息按鈕的 CanvasGroup）")]
    [SerializeField] private CanvasGroup _restButtonGroup;

    [Header("文字格式")]
    [Tooltip("{0}=剩餘次數 {1}=次數上限")]
    [SerializeField] private string _format = "{0} / {1}";

    [Header("次數用完時的表現")]
    [Tooltip("沒有剩餘次數時的透明度")]
    [Range(0f, 1f)]
    [SerializeField] private float _disabledAlpha = 0.4f;

    [Tooltip("透明度變化的秒數（0 = 直接切換）")]
    [SerializeField] private float _fadeDuration = 0.2f;

    private Tween _fadeTween;

    private void OnEnable()
    {
        if (_controller == null) return;
        _controller.onRestChanged.AddListener(OnRestChanged);
        Refresh();
    }

    private void OnDisable()
    {
        if (_controller == null) return;
        _controller.onRestChanged.RemoveListener(OnRestChanged);
    }

    private void OnRestChanged(int _) => Refresh();

    /// <summary>依目前 Run 狀態重畫一次。（也可外部手動呼叫）</summary>
    public void Refresh()
    {
        var run = _controller != null ? _controller.Run : null;

        // 還沒開始一輪時顯示滿值（3 / 3）
        int remaining = run != null ? run.RestRemaining : AdventureRunModel.MAX_REST_COUNT;
        int max = AdventureRunModel.MAX_REST_COUNT;

        if (_label != null)
            _label.text = string.Format(_format, remaining, max);

        ApplyButtonState(remaining > 0);
    }

    /// <summary>切換休息按鈕的可用外觀與互動。</summary>
    private void ApplyButtonState(bool usable)
    {
        if (_restButtonGroup == null) return;

        _restButtonGroup.interactable = usable;

        float targetAlpha = usable ? 1f : _disabledAlpha;

        _fadeTween?.Kill();
        if (_fadeDuration > 0f && isActiveAndEnabled)
            _fadeTween = _restButtonGroup.DOFade(targetAlpha, _fadeDuration);
        else
            _restButtonGroup.alpha = targetAlpha;
    }

    private void OnDestroy() => _fadeTween?.Kill();
}
