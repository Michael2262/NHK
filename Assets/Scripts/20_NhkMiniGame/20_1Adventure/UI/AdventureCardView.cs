using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 單張牌的視圖（掛在牌的 prefab 上，由 AdventureCardPresenter 複製出來用）。
/// 只負責「怎麼演」——移動、翻面、換圖、淡出，不知道任何遊戲規則。
///
/// prefab 結構建議：
///   Card (RectTransform + Image + CanvasGroup + 本腳本)
/// Image 同時當牌背與牌面，翻面時直接換 sprite。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class AdventureCardView : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private Sprite _cardBack;

    private CanvasGroup _canvasGroup;
    private RectTransform _rect;

    public RectTransform Rect => _rect;

    private void Awake()
    {
        _rect = (RectTransform)transform;
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_image == null) _image = GetComponent<Image>();
        _canvasGroup.alpha = 1f;
    }

    /// <summary>顯示牌背。</summary>
    public void ShowBack()
    {
        if (_image != null && _cardBack != null) _image.sprite = _cardBack;
    }

    /// <summary>直接換圖（不做動畫）。傳 null 不動作。</summary>
    public void SetSprite(Sprite sprite)
    {
        if (sprite != null && _image != null) _image.sprite = sprite;
    }

    /// <summary>飛到指定位置。</summary>
    public Tween FlyTo(Vector2 anchoredPos, float duration)
        => _rect.DOAnchorPos(anchoredPos, duration).SetEase(Ease.OutCubic);

    /// <summary>翻面：橫向壓扁到 0 → 換成 face → 再展開。</summary>
    public Sequence FlipTo(Sprite face, float duration)
    {
        float half = Mathf.Max(0.01f, duration * 0.5f);
        var seq = DOTween.Sequence();
        seq.Append(_rect.DOScaleX(0f, half).SetEase(Ease.InQuad));
        seq.AppendCallback(() => SetSprite(face));
        seq.Append(_rect.DOScaleX(1f, half).SetEase(Ease.OutQuad));
        return seq;
    }

    /// <summary>淡出。</summary>
    public Tween FadeOut(float duration) => _canvasGroup.DOFade(0f, duration);

    private void OnDestroy()
    {
        _rect.DOKill();
        _canvasGroup.DOKill();
    }
}
