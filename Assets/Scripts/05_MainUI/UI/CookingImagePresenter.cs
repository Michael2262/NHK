using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 料理圖片演出器（單例）。
/// 從 Resources/Cooking/Cooking_{代號} 載入圖片，從畫面左側飛到中央，
/// 停留指定時間後自動淡出。
///
/// 由 SequencerCommandCookingImage 驅動：
///   CookingImage(omelet) -> 播放 Resources/Cooking/Cooking_omelet
///   CookingImage(stop)   -> 提前淡出目前的料理圖片
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CookingImagePresenter : MonoBehaviour
{
    public static CookingImagePresenter Instance { get; private set; }

    [Header("UI 引用")]
    [Tooltip("要顯示料理圖片的 Image。若未指定，會嘗試取得同一物件上的 Image。")]
    [SerializeField] private Image _targetImage;

    [Header("位置（anchoredPosition）")]
    [Tooltip("圖片飛入前的起始位置，通常設在畫面左側外。")]
    [SerializeField] private Vector2 _spawnPosition = new Vector2(-1400f, 0f);

    [Tooltip("圖片停留的位置，通常是畫面正中央。")]
    [SerializeField] private Vector2 _centerPosition = Vector2.zero;

    [Header("時間")]
    [Min(0f)]
    [SerializeField] private float _flyDuration = 0.45f;

    [Min(0f)]
    [Tooltip("飛到中央後停留多久才淡出。")]
    [SerializeField] private float _holdDuration = 0.8f;

    [Min(0f)]
    [SerializeField] private float _fadeDuration = 0.35f;

    [Header("動畫曲線")]
    [SerializeField] private Ease _flyEase = Ease.OutCubic;
    [SerializeField] private Ease _fadeEase = Ease.InQuad;

    [Header("顯示選項")]
    [Tooltip("換圖後是否把 Image 還原成圖片原始尺寸。")]
    [SerializeField] private bool _setNativeSize;

    private const string ResourceFolder = "Cooking/";
    private const string FilePrefix = "Cooking_";

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Sequence _sequence;

    /// <summary>目前是否正在播放飛入、停留或淡出演出。</summary>
    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_targetImage == null) _targetImage = GetComponent<Image>();
        if (_targetImage != null) _rectTransform = _targetImage.rectTransform;

        SetHiddenInstant();
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[CookingImagePresenter] 場景上存在重複元件，已停用後建立的元件。");
            enabled = false;
        }
    }

    /// <summary>
    /// 載入指定代號的料理圖片並播放完整演出。
    /// 若上一段演出尚未完成，會直接換成新圖片並從頭播放。
    /// </summary>
    public void Show(string cookingCode)
    {
        if (string.IsNullOrWhiteSpace(cookingCode))
        {
            Debug.LogWarning("[CookingImagePresenter] Show 收到空的料理代號。");
            return;
        }

        if (_targetImage == null || _rectTransform == null)
        {
            Debug.LogError("[CookingImagePresenter] 尚未指定 _targetImage，無法播放料理圖片。");
            return;
        }

        string path = ResourceFolder + FilePrefix + cookingCode.Trim();
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogWarning($"[CookingImagePresenter] 找不到料理圖片：Resources/{path}（代號={cookingCode}）");
            return;
        }

        KillSequence();

        _targetImage.sprite = sprite;
        _targetImage.enabled = true;
        if (_setNativeSize) _targetImage.SetNativeSize();

        _rectTransform.anchoredPosition = _spawnPosition;
        _canvasGroup.alpha = 1f;
        IsPlaying = true;

        _sequence = DOTween.Sequence();
        _sequence.SetTarget(this);
        _sequence.Append(_rectTransform
            .DOAnchorPos(_centerPosition, Mathf.Max(0f, _flyDuration))
            .SetEase(_flyEase));
        _sequence.AppendInterval(Mathf.Max(0f, _holdDuration));
        _sequence.Append(_canvasGroup
            .DOFade(0f, Mathf.Max(0f, _fadeDuration))
            .SetEase(_fadeEase));
        _sequence.OnComplete(SetHiddenInstant);
    }

    /// <summary>提前結束目前演出，並從當下狀態淡出。</summary>
    public void Hide()
    {
        if (_canvasGroup == null) return;

        KillSequence();

        if (_canvasGroup.alpha <= 0f)
        {
            SetHiddenInstant();
            return;
        }

        IsPlaying = true;
        _sequence = DOTween.Sequence();
        _sequence.SetTarget(this);
        _sequence.Append(_canvasGroup
            .DOFade(0f, Mathf.Max(0f, _fadeDuration))
            .SetEase(_fadeEase));
        _sequence.OnComplete(SetHiddenInstant);
    }

    private void KillSequence()
    {
        if (_sequence == null) return;

        _sequence.Kill(false);
        _sequence = null;
    }

    private void SetHiddenInstant()
    {
        IsPlaying = false;
        _sequence = null;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        if (_targetImage != null) _targetImage.enabled = false;
    }

    private void OnDisable()
    {
        KillSequence();
        if (_rectTransform != null) _rectTransform.DOKill();
        if (_canvasGroup != null) _canvasGroup.DOKill();
        SetHiddenInstant();
        if (Instance == this) Instance = null;
    }

    private void OnDestroy()
    {
        if (_rectTransform != null) _rectTransform.DOKill();
        if (_canvasGroup != null) _canvasGroup.DOKill();
        if (Instance == this) Instance = null;
    }
}
