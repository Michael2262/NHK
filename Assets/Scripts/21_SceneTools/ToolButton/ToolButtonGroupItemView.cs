using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Serialization;

/// <summary>
/// 掛在固定按鈕 Prefab 根物件，保存 UI 關聯並呈現控制器指定的內容。
/// Button 的 On Click 留空；文字物件不要同時掛 LocalizeUI。
/// </summary>
[DisallowMultipleComponent]
public class ToolButtonGroupItemView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text text;
    [Tooltip("一般 Icon 與 Ng 共用的獨立 Image，不可使用 Button 的 Target Graphic。")]
    [FormerlySerializedAs("statusImage")]
    [SerializeField] private Image iconImage;
    [Tooltip("按鈕底色 Image；返回列會套用灰色，建議與外框分開。")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color ngTextColor = Color.red;

    public Button Button => button;
    public bool IsConfigured => button != null && text != null
        && button.transform.IsChildOf(transform) && text.transform.IsChildOf(transform)
        && transform is RectTransform
        && iconImage != null && iconImage.transform.IsChildOf(transform)
        && iconImage != button.targetGraphic
        && (backgroundImage == null || (backgroundImage.transform.IsChildOf(transform)
            && backgroundImage != iconImage));

    private Button _boundButton;
    private UnityAction _listener;
    private bool _captured;
    private Vector2 _originalPosition;
    private Color _originalTextColor;
    private Color _originalIconColor;
    private Color _originalBackgroundColor;

    private void CaptureDefaults()
    {
        if (_captured) return;
        _captured = true;
        _originalPosition = ((RectTransform)transform).anchoredPosition;
        _originalTextColor = text.color;
        _originalIconColor = iconImage.color;
        if (backgroundImage != null) _originalBackgroundColor = backgroundImage.color;
    }

    /// <summary>永遠從初始位置計算，不累積位移，也不更動寬度。</summary>
    public void SetOffset(Vector2 offset)
    {
        CaptureDefaults();
        ((RectTransform)transform).anchoredPosition = _originalPosition + offset;
    }

    /// <summary>只替換本視圖加入的監聽，不清除其他元件或 Inspector 事件。</summary>
    public void Bind(UnityAction listener)
    {
        Unbind();
        if (button == null) return;
        _boundButton = button;
        _listener = listener;
        _boundButton.onClick.AddListener(_listener);
    }

    public void Unbind()
    {
        if (_boundButton != null && _listener != null)
            _boundButton.onClick.RemoveListener(_listener);
        _boundButton = null;
        _listener = null;
    }

    public void Present(string caption, bool interactable, Sprite icon, bool isNg,
        bool isBack = false, Color backColor = default(Color))
    {
        CaptureDefaults();
        if (text != null) text.text = caption;
        text.color = isNg ? ngTextColor : _originalTextColor;
        if (backgroundImage != null)
            backgroundImage.color = isBack ? backColor : _originalBackgroundColor;
        if (button != null) button.interactable = interactable;
        if (iconImage != null)
        {
            // 只開關 Image 元件，避免意外關閉文字或按鈕所在物件。
            iconImage.sprite = icon;
            iconImage.color = isNg ? ngTextColor : _originalIconColor;
            iconImage.enabled = icon != null;
            iconImage.raycastTarget = false;
        }
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (text != null) text.text = string.Empty;
        if (button != null) button.interactable = false;
        if (iconImage != null)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
        }
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    private void OnDestroy() => Unbind();
}
