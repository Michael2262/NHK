using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 掛在固定按鈕 Prefab 根物件，保存 UI 關聯並呈現控制器指定的內容。
/// Button 的 On Click 留空；文字物件不要同時掛 LocalizeUI。
/// </summary>
[DisallowMultipleComponent]
public class ToolButtonGroupItemView : MonoBehaviour
{
    public enum StatusDisplay { Hidden, OK, NG }

    [SerializeField] private Button button;
    [SerializeField] private TMP_Text text;
    [Tooltip("獨立的 OK／NG 提示 Image，不可使用 Button 的 Target Graphic。可留空。")]
    [SerializeField] private Image statusImage;
    [Header("狀態圖示")]
    [SerializeField] private Sprite okSprite;
    [SerializeField] private Sprite ngSprite;

    public Button Button => button;
    public bool IsConfigured => button != null && text != null
        && button.transform.IsChildOf(transform) && text.transform.IsChildOf(transform)
        && (statusImage == null || (statusImage.transform.IsChildOf(transform)
            && statusImage != button.targetGraphic));

    private Button _boundButton;
    private UnityAction _listener;

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

    public void Present(string caption, bool interactable, StatusDisplay status)
    {
        if (text != null) text.text = caption;
        if (button != null) button.interactable = interactable;
        if (statusImage != null)
        {
            Sprite statusSprite = status == StatusDisplay.OK ? okSprite
                : status == StatusDisplay.NG ? ngSprite : null;
            // 只開關 Image 元件，避免意外關閉文字或按鈕所在物件。
            statusImage.sprite = statusSprite;
            statusImage.enabled = statusSprite != null;
            statusImage.raycastTarget = false;
        }
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (text != null) text.text = string.Empty;
        if (button != null) button.interactable = false;
        if (statusImage != null)
        {
            statusImage.enabled = false;
            statusImage.sprite = null;
        }
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    private void OnDestroy() => Unbind();
}
