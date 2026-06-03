using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 道具圖顯示器（單例）。
/// 放在「不會卸載的場景」上，負責把一個 Image 換成指定道具圖並淡入；
/// 也能把自己淡出關閉。出現／消失皆用 DOTween 對 CanvasGroup.alpha 漸變。
///
/// 圖片來源：Resources/SHOP/ItemImage/Item_{代號}
/// （檔名去掉 "Item_" 前綴即為呼叫代號，例如 Item_bento → bento）
///
/// 由 SequencerCommandItemImage 驅動：
///   ItemImage(bento) → Show("bento")
///   ItemImage(stop)  → Hide()
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ItemImageShower : MonoBehaviour
{
    // ══════════════════════════════════════════
    //  Singleton
    // ══════════════════════════════════════════
    public static ItemImageShower Instance { get; private set; }

    [Header("UI 引用")]
    [Tooltip("要被替換成道具圖的 Image。")]
    [SerializeField] private Image _targetImage;

    [Header("淡入／淡出")]
    [Tooltip("淡入時間（秒）。")]
    [SerializeField] private float _fadeInDuration = 0.3f;
    [Tooltip("淡出時間（秒）。")]
    [SerializeField] private float _fadeOutDuration = 0.3f;
    [SerializeField] private Ease _fadeInEase = Ease.OutQuad;
    [SerializeField] private Ease _fadeOutEase = Ease.InQuad;

    [Header("顯示選項")]
    [Tooltip("換圖後是否把 Image 還原成圖片原始尺寸。")]
    [SerializeField] private bool _setNativeSize = false;

    // Resources 內的固定路徑前綴
    private const string RESOURCE_FOLDER = "SHOP/ItemImage/";
    private const string FILE_PREFIX = "Item_";

    private CanvasGroup _canvasGroup;

    // ══════════════════════════════════════════
    //  Unity 生命週期
    // ══════════════════════════════════════════

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        _canvasGroup = GetComponent<CanvasGroup>();

        // 預設為關閉狀態（透明、不擋點擊）
        SetVisibleInstant(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_canvasGroup != null) _canvasGroup.DOKill();
    }

    // ══════════════════════════════════════════
    //  公開 API
    // ══════════════════════════════════════════

    /// <summary>
    /// 顯示指定代號的道具圖並淡入。
    /// </summary>
    /// <param name="itemCode">道具代號（檔名去掉 "Item_" 前綴）。</param>
    public void Show(string itemCode)
    {
        if (string.IsNullOrEmpty(itemCode))
        {
            Debug.LogWarning("[ItemImageShower] Show 收到空的道具代號。");
            return;
        }

        if (_targetImage == null)
        {
            Debug.LogError("[ItemImageShower] 尚未指定 _targetImage，無法換圖。");
            return;
        }

        string path = RESOURCE_FOLDER + FILE_PREFIX + itemCode;
        Sprite sprite = Resources.Load<Sprite>(path);

        if (sprite == null)
        {
            Debug.LogWarning($"[ItemImageShower] 找不到道具圖：Resources/{path} (代號={itemCode})");
            return;
        }

        _targetImage.sprite = sprite;
        _targetImage.enabled = true;
        if (_setNativeSize) _targetImage.SetNativeSize();

        FadeTo(1f, _fadeInDuration, _fadeInEase, true);
    }

    /// <summary>
    /// 淡出並關閉顯示器。
    /// </summary>
    public void Hide()
    {
        FadeTo(0f, _fadeOutDuration, _fadeOutEase, false);
    }

    public bool IsOpen => _canvasGroup != null && _canvasGroup.alpha > 0f;

    // ══════════════════════════════════════════
    //  內部
    // ══════════════════════════════════════════

    private void FadeTo(float targetAlpha, float duration, Ease ease, bool active)
    {
        if (_canvasGroup == null) return;

        // 淡入時先開啟互動；淡出時等漸變結束才關閉
        if (active)
        {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        _canvasGroup.DOKill();
        _canvasGroup.DOFade(targetAlpha, duration)
            .SetEase(ease)
            .OnComplete(() =>
            {
                if (!active)
                {
                    _canvasGroup.interactable = false;
                    _canvasGroup.blocksRaycasts = false;
                }
            });
    }

    /// <summary>不經漸變，立即設定顯示狀態（初始化用）。</summary>
    private void SetVisibleInstant(bool visible)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}
