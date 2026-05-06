using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 掛載在道具按鈕 Prefab 上的輔助腳本。
/// 顯示道具的 Icon 圖片,點擊後通知宿主 UI(IItemButtonHost)。
/// 選中時按鈕的 Highlight / Pressed / Selected 顏色會變為黃色 (#F2F44D)。
/// 
/// ★ 支援「空格佔位」模式:SetupAsEmpty() → 顯示 _itemIconEmpty,按鈕不可互動。
/// </summary>
[RequireComponent(typeof(Button))]
public class BackpackItemButton : MonoBehaviour
{
    [Header("Icon 圖示")]
    [Tooltip("顯示道具圖示的 Image 元件")]
    [SerializeField] private Image _itemIcon;

    [Tooltip("當此格為空格佔位時顯示的 Image 元件(例如 EMPTY 圖)")]
    [SerializeField] private Image _itemIconEmpty;

    // 內部數據
    private ItemConfigData _itemConfig;
    private IItemButtonHost _parentUI;
    private Button _button;

    // 顏色定義
    private static readonly Color SelectedHighlightColor = new Color(0.949f, 0.957f, 0.302f, 1f); // #F2F44D
    private ColorBlock _normalColors;
    private ColorBlock _selectedColors;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClicked);

        _normalColors = _button.colors;
        _selectedColors = _normalColors;
        _selectedColors.normalColor = SelectedHighlightColor;
        _selectedColors.highlightedColor = SelectedHighlightColor;
        _selectedColors.pressedColor = SelectedHighlightColor;
        _selectedColors.selectedColor = SelectedHighlightColor;
    }

    /// <summary>由宿主 UI 呼叫,初始化按鈕以顯示特定道具。</summary>
    public void Setup(ItemConfigData item, IItemButtonHost parentUI)
    {
        _itemConfig = item;
        _parentUI = parentUI;

        // 顯示道具圖
        if (_itemIcon != null)
        {
            if (_itemConfig != null && _itemConfig.Icon != null)
            {
                _itemIcon.sprite = _itemConfig.Icon;
                _itemIcon.enabled = true;
            }
            else
            {
                _itemIcon.enabled = false;
            }
        }

        // 隱藏空格圖
        if (_itemIconEmpty != null) _itemIconEmpty.enabled = false;

        // 恢復為一般(未選中)外觀與互動
        _button.interactable = true;
        _button.colors = _normalColors;
    }

    /// <summary>
    /// 將此按鈕設定為「空格佔位」模式:
    /// 顯示 _itemIconEmpty、隱藏道具圖、按鈕不可互動(點擊無反應、無高亮)。
    /// </summary>
    public void SetupAsEmpty()
    {
        _itemConfig = null;
        // 保留 _parentUI 引用(不需要清)

        if (_itemIcon != null) _itemIcon.enabled = false;
        if (_itemIconEmpty != null) _itemIconEmpty.enabled = true;

        _button.interactable = false;
        _button.colors = _normalColors;
    }

    /// <summary>由宿主 UI 呼叫,控制選中/取消選中的視覺狀態。</summary>
    public void SetSelected(bool selected)
    {
        if (_button == null) return;
        if (!_button.interactable) return; // 空格不能被選中
        _button.colors = selected ? _selectedColors : _normalColors;
    }

    private void OnClicked()
    {
        if (_parentUI != null && _itemConfig != null)
        {
            _parentUI.OnItemSelected(_itemConfig, this);
        }
    }
}