using PixelCrushers.DialogueSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 負責管理背包 UI 的所有顯示、篩選和互動邏輯。
/// (單例模式，使用 CanvasGroup 控制顯隱)
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class BackpackUI : MonoBehaviour, IItemButtonHost
{
    // ══════════════════════════════════════════
    //  Singleton
    // ══════════════════════════════════════════
    public static BackpackUI Instance { get; private set; }

    private CanvasGroup _canvasGroup;

    // --- UI 引用 ---
    [Header("左側詳細面板")]
    [SerializeField] private GameObject _detailPanelRoot;
    [SerializeField] private Image _itemIcon;
    [SerializeField] private TextMeshProUGUI _itemQuantityText;
    [SerializeField] private TextMeshProUGUI _itemNameTMP;
    [SerializeField] private TextMeshProUGUI _itemDescriptionTMP;

    [Header("右側道具列表")]
    [SerializeField] private Transform _itemButtonContainer;
    [SerializeField] private GameObject _itemButtonPrefab;

    [Header("篩選器按鈕")]
    [SerializeField] private Button _filterAllButton;
    [UnityEngine.Serialization.FormerlySerializedAs("_filterConsumableButton")]
    [SerializeField] private Button _filterUseButton;
    [UnityEngine.Serialization.FormerlySerializedAs("_filterPermanentButton")]
    [SerializeField] private Button _filterClaimButton;

    // --- 內部狀態 ---
    // 沿用原分類編號，舊 Permanent = 3 對應新的 Claim。
    public enum ItemFilterType { All = 0, Use = 1, Claim = 3 }
    private ItemFilterType _currentFilter = ItemFilterType.All;
    private ItemConfigData _selectedItem = null;
    private List<GameObject> _spawnedItemButtons = new List<GameObject>();
    private Coroutine _refreshCoroutine = null;

    // ★ 追蹤目前被選中的 BackpackItemButton，用於高亮管理
    private BackpackItemButton _selectedButton = null;

    // ───── Unity 生命週期 ─────

    private void Awake()
    {
        // Singleton
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        _canvasGroup = GetComponent<CanvasGroup>();

        _filterAllButton.onClick.AddListener(() => SetFilter(ItemFilterType.All));
        _filterUseButton.onClick.AddListener(() => SetFilter(ItemFilterType.Use));
        _filterClaimButton.onClick.AddListener(() => SetFilter(ItemFilterType.Claim));

        // ★ Awake 就設好篩選按鈕狀態，避免打開時閃爍
        UpdateFilterButtonStates();

        // 預設關閉
        SetCanvasGroupActive(false);
    }

    private void Start()
    {
        _detailPanelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        PixelCrushers.UILocalizationManager.languageChanged += OnLanguageChanged;
        if (GameStatusService.Instance == null) return;
        GameStatusService.Instance.Inventory.OnItemCountChanged += OnInventoryChanged;
        GameStatusService.Instance.OnGameStatusLoaded += StartRefreshListCoroutine;
    }

    private void OnDisable()
    {
        if (GameStatusService.Instance != null)
        {
            GameStatusService.Instance.Inventory.OnItemCountChanged -= OnInventoryChanged;
            GameStatusService.Instance.OnGameStatusLoaded -= StartRefreshListCoroutine;
        }
        PixelCrushers.UILocalizationManager.languageChanged -= OnLanguageChanged;
        StopRefreshListCoroutine();
    }

    // ══════════════════════════════════════════
    //  公開 API：顯示 / 隱藏
    // ══════════════════════════════════════════

    public void Show()
    {
        SetCanvasGroupActive(true);
        StartRefreshListCoroutine();
    }

    public void Hide()
    {
        ClearDetailPanel();
        SetCanvasGroupActive(false);
    }

    public bool IsOpen => _canvasGroup.alpha > 0f;

    private void SetCanvasGroupActive(bool active)
    {
        _canvasGroup.alpha = active ? 1f : 0f;
        _canvasGroup.interactable = active;
        _canvasGroup.blocksRaycasts = active;
    }

    // --- 事件處理 ---

    private void OnInventoryChanged(string itemID, int newCount)
    {
        StartRefreshListCoroutine();
        if (_selectedItem != null) { UpdateDetailPanel(_selectedItem); }
    }

    private void OnLanguageChanged(string language)
    {
        StartRefreshListCoroutine();
    }

    private string Localize(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "";
        string translated = DialogueManager.GetLocalizedText(key);
        if (string.IsNullOrEmpty(translated) || translated == key)
        {
            Debug.LogWarning($"[BackpackUI] Text Table 找不到 Key：{key}", this);
            return key;
        }
        return translated;
    }

    // --- 核心方法 (Controller) ---

    public void SetFilter(ItemFilterType newFilter)
    {
        _currentFilter = newFilter;
        UpdateFilterButtonStates();
        StartRefreshListCoroutine();
        ClearDetailPanel();
    }

    /// <summary>
    /// 更新篩選按鈕的視覺狀態：
    /// 選中的按鈕 interactable=false（變灰），其餘 interactable=true。
    /// </summary>
    private void UpdateFilterButtonStates()
    {
        _filterAllButton.interactable = (_currentFilter != ItemFilterType.All);
        _filterUseButton.interactable = (_currentFilter != ItemFilterType.Use);
        _filterClaimButton.interactable = (_currentFilter != ItemFilterType.Claim);
    }

    /// <summary>
    /// 由 BackpackItemButton 呼叫，通知背包「某個道具被選中了」。
    /// </summary>
    public void OnItemSelected(ItemConfigData item, BackpackItemButton button)
    {
        // ★ 再次點擊同一個按鈕 → 取消選中，關閉詳細面板
        if (_selectedButton == button && button != null)
        {
            ClearDetailPanel();
            return;
        }

        // 取消前一個按鈕的高亮
        if (_selectedButton != null)
            _selectedButton.SetSelected(false);

        _selectedItem = item;
        _selectedButton = button;

        // 設定新按鈕的高亮
        if (_selectedButton != null)
            _selectedButton.SetSelected(true);

        UpdateDetailPanel(item);
    }

    // --- 核心方法 (View) ---

    private void StartRefreshListCoroutine()
    {
        StopRefreshListCoroutine();
        if (!isActiveAndEnabled) return;
        _refreshCoroutine = StartCoroutine(RefreshItemListRoutine());
    }

    private void StopRefreshListCoroutine()
    {
        if (_refreshCoroutine != null) { StopCoroutine(_refreshCoroutine); _refreshCoroutine = null; }
    }

    private IEnumerator RefreshItemListRoutine()
    {
        yield return null;
        foreach (var button in _spawnedItemButtons) { Destroy(button); }
        _spawnedItemButtons.Clear();
        _selectedButton = null; // 清除按鈕引用（物件已被銷毀）

        var inventory = GameStatusService.Instance.Inventory;
        var catalog = GameStatusService.Instance.ItemCatalog;
        var allOwnedItems = inventory.GetAllItems();

        foreach (var itemKVP in allOwnedItems)
        {
            if (itemKVP.Value <= 0) continue;
            ItemConfigData itemConfig = catalog.GetItemConfig(itemKVP.Key);
            if (itemConfig == null) continue;
            if (ShouldDisplayInFilter(itemConfig, _currentFilter))
            {
                GameObject buttonGO = Instantiate(_itemButtonPrefab, _itemButtonContainer);
                _spawnedItemButtons.Add(buttonGO);
                var itemButtonScript = buttonGO.GetComponent<BackpackItemButton>();
                if (itemButtonScript != null)
                {
                    itemButtonScript.Setup(itemConfig, this);

                    // ★ 如果這個道具是目前選中的，恢復高亮狀態
                    if (_selectedItem != null && itemConfig.ItemID == _selectedItem.ItemID)
                    {
                        _selectedButton = itemButtonScript;
                        itemButtonScript.SetSelected(true);
                    }
                }
            }
        }
        _refreshCoroutine = null;

        if (_selectedItem != null)
        {
            if (inventory.GetItemCount(_selectedItem.ItemID) > 0 && ShouldDisplayInFilter(_selectedItem, _currentFilter))
            { UpdateDetailPanel(_selectedItem); }
            else { ClearDetailPanel(); }
        }
        else { ClearDetailPanel(); }
    }

    private bool ShouldDisplayInFilter(ItemConfigData item, ItemFilterType filter)
    {
        switch (filter)
        {
            case ItemFilterType.All: return true;
            case ItemFilterType.Use: return item.Category == ItemCategory.Use;
            case ItemFilterType.Claim: return item.Category == ItemCategory.Claim;
            default: return false;
        }
    }

    private void UpdateDetailPanel(ItemConfigData item)
    {
        if (item == null) { ClearDetailPanel(); return; }
        _detailPanelRoot.SetActive(true);

        // ★ 設定道具圖示
        if (_itemIcon != null)
        {
            _itemIcon.sprite = item.Icon;
            _itemIcon.enabled = (item.Icon != null);
        }

        if (_itemNameTMP != null) _itemNameTMP.text = Localize(item.DisplayNameKey);
        if (_itemDescriptionTMP != null) _itemDescriptionTMP.text = Localize(item.DescriptionKey);

        int quantity = GameStatusService.Instance.Inventory.GetItemCount(item.ItemID);
        _itemQuantityText.text = string.Format(Localize("Store.Quantity"), quantity);
    }

    private void ClearDetailPanel()
    {
        // 取消高亮
        if (_selectedButton != null)
        {
            _selectedButton.SetSelected(false);
            _selectedButton = null;
        }

        _selectedItem = null;
        _detailPanelRoot.SetActive(false);
        if (_itemNameTMP != null) { _itemNameTMP.text = string.Empty; }
        if (_itemDescriptionTMP != null) { _itemDescriptionTMP.text = string.Empty; }
        _itemQuantityText.text = string.Empty;
    }
}
