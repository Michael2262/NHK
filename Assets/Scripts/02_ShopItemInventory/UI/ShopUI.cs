using PixelCrushers.DialogueSystem;
using PixelCrushers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 負責管理商店 UI 的主腳本。
/// 支援多商店：可在 Inspector 指定專屬的 ItemDatabase，
/// 未指定時自動使用 GameStatusService 上的預設 ItemDatabase。
/// (使用 CanvasGroup 控制顯隱，協程刷新列表)
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
[DefaultExecutionOrder(-800)]
public class ShopUI : MonoBehaviour
{
    private CanvasGroup _canvasGroup;

    // --- 資料來源 ---
    [Header("商店資料來源")]
    [Tooltip("指定此商店要使用的 ItemDatabase。\n" +
             "留空 = 自動使用 GameStatusService 上的預設 ItemDatabase。")]
    [SerializeField] private ItemDatabase _assignedDatabase;

    private ItemDatabase ActiveDatabase
        => _assignedDatabase != null ? _assignedDatabase : GameStatusService.Instance.ItemDatabase;

    // --- UI 引用 ---
    [Header("UI 引用")]
    [SerializeField] private Button _filterAllButton;
    [SerializeField] private Button _filterUseButton;
    [SerializeField] private Button _filterGiftButton;
    [SerializeField] private Button _filterClaimButton;
    [SerializeField] private Transform _itemRowContainer;
    [SerializeField] private GameObject _itemRowPrefab;

    // --- 內部狀態 ---
    public enum ShopFilterType { All, Use, Gift, Claim }
    private ShopFilterType _currentFilter = ShopFilterType.All;
    private List<ShopItemRowUI> _spawnedItemRows = new List<ShopItemRowUI>();
    private string _quantityFormatString = "持有數量 : {0}";
    private string _priceFormatString = "售價:{0}";
    private string _soldOutString = "售罄";

    private Coroutine _refreshCoroutine = null;

    // ───── Unity 生命週期 ─────

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        _filterAllButton.onClick.AddListener(() => SetFilter(ShopFilterType.All));
        _filterUseButton.onClick.AddListener(() => SetFilter(ShopFilterType.Use));
        _filterGiftButton.onClick.AddListener(() => SetFilter(ShopFilterType.Gift));
        _filterClaimButton.onClick.AddListener(() => SetFilter(ShopFilterType.Claim));

        // ★ 在 Awake 就設好按鈕狀態，避免打開時閃爍
        UpdateFilterButtonStates();

        HideInternal();
    }

    private void Start()
    {
        CheckLocalizationStrings();
        StartRefreshListCoroutine();
    }

    private void OnEnable()
    {
        if (GameStatusService.Instance == null) return;

        GameStatusService.Instance.Inventory.OnItemCountChanged += OnInventoryOrPurchaseChanged;
        GameStatusService.Instance.ShopStatus.OnPurchasedCountChanged += OnInventoryOrPurchaseChanged;
        PixelCrushers.UILocalizationManager.languageChanged += OnLanguageChanged;
    }

    private void OnDisable()
    {
        if (GameStatusService.Instance != null)
        {
            GameStatusService.Instance.Inventory.OnItemCountChanged -= OnInventoryOrPurchaseChanged;
            GameStatusService.Instance.ShopStatus.OnPurchasedCountChanged -= OnInventoryOrPurchaseChanged;
        }
        PixelCrushers.UILocalizationManager.languageChanged -= OnLanguageChanged;
        StopRefreshListCoroutine();
    }

    // ───── 公開 API ─────

    public void Show()
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
        RefreshDynamicDataOnRows();
    }

    public void Hide()
    {
        HideInternal();
    }

    public bool IsOpen => _canvasGroup.alpha > 0f;

    public void SetDatabase(ItemDatabase newDatabase)
    {
        _assignedDatabase = newDatabase;
        StartRefreshListCoroutine();
    }

    // ───── 內部方法 ─────

    private void HideInternal()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    // --- 事件處理 ---

    private void OnInventoryOrPurchaseChanged(string itemID, int newCount)
    {
        RefreshDynamicDataOnRows();
    }

    private void OnLanguageChanged(string language)
    {
        CheckLocalizationStrings();
        StartRefreshListCoroutine();
    }

    private void CheckLocalizationStrings()
    {
        if (DialogueManager.instance == null || DialogueManager.displaySettings == null ||
            DialogueManager.displaySettings.localizationSettings == null ||
            DialogueManager.displaySettings.localizationSettings.textTable == null)
        {
            Debug.LogWarning("ShopUI CheckLocalizationStrings 失敗：DialogueManager 或其 TextTable 尚未準備好。");
            return;
        }

        var textTable = DialogueManager.displaySettings.localizationSettings.textTable;
        if (textTable.HasField("BACKPACK_QUANTITY"))
            _quantityFormatString = textTable.GetFieldTextForLanguage("BACKPACK_QUANTITY", Localization.GetCurrentLanguageID(textTable));
        else
            _quantityFormatString = "持有數量 : {0}";

        if (textTable.HasField("SHOP_PRICE_FORMAT"))
            _priceFormatString = textTable.GetFieldTextForLanguage("SHOP_PRICE_FORMAT", Localization.GetCurrentLanguageID(textTable));
        else
            _priceFormatString = "售價:{0}";

        if (textTable.HasField("SHOP_SOLD_OUT"))
            _soldOutString = textTable.GetFieldTextForLanguage("SHOP_SOLD_OUT", Localization.GetCurrentLanguageID(textTable));
        else
            _soldOutString = "售罄";
    }

    // --- 篩選器 ---

    public void SetFilter(ShopFilterType newFilter)
    {
        _currentFilter = newFilter;
        UpdateFilterButtonStates();
        StartRefreshListCoroutine();
    }

    /// <summary>
    /// 更新篩選按鈕的視覺狀態：
    /// 選中的按鈕 interactable=false（變灰），其餘 interactable=true。
    /// 在 Awake 和 SetFilter 中都會呼叫，確保第一次打開時顏色就已經正確。
    /// </summary>
    private void UpdateFilterButtonStates()
    {
        _filterAllButton.interactable = (_currentFilter != ShopFilterType.All);
        _filterUseButton.interactable = (_currentFilter != ShopFilterType.Use);
        _filterGiftButton.interactable = (_currentFilter != ShopFilterType.Gift);
        _filterClaimButton.interactable = (_currentFilter != ShopFilterType.Claim);
    }

    // --- 核心方法 (View) ---

    private void StartRefreshListCoroutine()
    {
        StopRefreshListCoroutine();
        _refreshCoroutine = StartCoroutine(RefreshShopListRoutine());
    }

    private void StopRefreshListCoroutine()
    {
        if (_refreshCoroutine != null)
        {
            StopCoroutine(_refreshCoroutine);
            _refreshCoroutine = null;
        }
    }

    private IEnumerator RefreshShopListRoutine()
    {
        foreach (var row in _spawnedItemRows)
        {
            Destroy(row.gameObject);
        }
        _spawnedItemRows.Clear();

        if (GameStatusService.Instance == null || ActiveDatabase == null)
        {
            Debug.LogWarning("[ShopUI] GameStatusService 或 ActiveDatabase 尚未準備好，跳過列表刷新。");
            _refreshCoroutine = null;
            yield break;
        }

        var unlockedList = ActiveDatabase.GetUnlockedShopList(GameStatusService.Instance);

        foreach (var itemConfig in unlockedList)
        {
            if (ShouldDisplayInFilter(itemConfig, _currentFilter))
            {
                GameObject rowGO = Instantiate(_itemRowPrefab, _itemRowContainer);
                var rowScript = rowGO.GetComponent<ShopItemRowUI>();

                if (rowScript != null)
                {
                    rowScript.Setup(itemConfig, this,
                                    _quantityFormatString,
                                    _priceFormatString,
                                    _soldOutString);
                    _spawnedItemRows.Add(rowScript);
                }
            }
        }

        yield return null;
        RefreshDynamicDataOnRows();

        _refreshCoroutine = null;
    }

    private void RefreshDynamicDataOnRows()
    {
        foreach (var row in _spawnedItemRows)
        {
            row.UpdateDynamicData();
        }
    }

    private bool ShouldDisplayInFilter(ItemConfigData item, ShopFilterType filter)
    {
        var targetRule = item.TargetingRule;
        switch (filter)
        {
            case ShopFilterType.All: return true;
            case ShopFilterType.Use: return (targetRule.Category == TargetCategory.ProtagonistOnly || targetRule.Category == TargetCategory.AnyCharacter);
            case ShopFilterType.Gift: return (targetRule.Category == TargetCategory.HeroineOnly || targetRule.Category == TargetCategory.AnyCharacter);
            case ShopFilterType.Claim: return (targetRule.Category == TargetCategory.NoBody);
            default: return false;
        }
    }
}