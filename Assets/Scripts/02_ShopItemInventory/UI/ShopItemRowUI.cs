using PixelCrushers.Wrappers; // for LocalizeUI
//using PixelCrushers.DialogueSystem; // for DialogueManager
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 掛載在商店商品列表行 Prefab 上的腳本。
/// 負責顯示單一商品的靜態與動態資訊，並處理購買按鈕。
/// (移除 DialogueManager 依賴，由 ShopUI 傳入本地化字串)
/// </summary>
public class ShopItemRowUI : MonoBehaviour
{
    [Header("UI 引用 (Prefab 內部拖曳)")]
    [SerializeField] private Image _itemIcon;
    [SerializeField] private LocalizeUI _itemNameLocalizer;
    [SerializeField] private LocalizeUI _itemDescriptionLocalizer;
    [SerializeField] private TextMeshProUGUI _quantityText; // 持有數量
    [SerializeField] private TextMeshProUGUI _priceOrStatusText; // 顯示價格或 "售鑿"
    [SerializeField] private Button _purchaseButton;

    // 內部狀態
    private ItemConfigData _itemConfig;
    private ShopUI _parentUI; // (可選) 如果需要回調 ShopUI
    // 移除預設值，由 Setup 傳入
    private string _quantityFormatString = "{0}";
    private string _priceFormatString = "{0}";
    private string _soldOutString = "Sold Out";

    private void Awake()
    {
        _purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);

    }

    private void OnEnable()
    {

        //UpdateDynamicData();
    }



    /// <summary>
    /// 由 ShopUI 在 Instantiate 後呼叫，進行初始化。
    /// </summary>
    public void Setup(ItemConfigData itemConfig, ShopUI parentUI, string quantityFormat, string priceFormat, string soldOutText)
    {
        _itemConfig = itemConfig;
        _parentUI = parentUI;

        // 將傳入的格式字串儲存起來
        _quantityFormatString = quantityFormat;
        _priceFormatString = priceFormat;
        _soldOutString = soldOutText;

        // --- 設定靜態資料 ---
        _itemIcon.sprite = _itemConfig.Icon;

        if (_itemNameLocalizer != null)
        {
            _itemNameLocalizer.fieldName = _itemConfig.DisplayNameKey;
            _itemNameLocalizer.UpdateText();
        }
        if (_itemDescriptionLocalizer != null)
        {
            _itemDescriptionLocalizer.fieldName = _itemConfig.DescriptionKey;
            _itemDescriptionLocalizer.UpdateText();
        }

        // --- 設定動態資料 ---
        UpdateDynamicData();
    }

    /// <summary>
    /// 【核心】更新需要即時反應的動態數據（持有數量、售鑿狀態）。
    /// 這個方法會被 Setup() 和 OnEnable() 呼叫，也會被 ShopUI 主動呼叫。
    /// </summary>
    public void UpdateDynamicData()
    {
        if (_itemConfig == null) return;

        // 1. 更新持有數量
        int quantity = GameStatusService.Instance.Inventory.GetItemCount(_itemConfig.ItemID);
        _quantityText.text = string.Format(_quantityFormatString, quantity);

        // 2. 更新購買按鈕狀態 (售價 vs 售鑿)
        int purchasedCount = 0;
        bool isSoldOut = false;

        // 檢查是否有購買上限
        if (_itemConfig.MaxPurchaseLimit > 0)
        {
            purchasedCount = GameStatusService.Instance.ShopStatus.GetPurchasedCount(_itemConfig.ItemID);
            isSoldOut = purchasedCount >= _itemConfig.MaxPurchaseLimit;
        }

        if (isSoldOut)
        {
            _priceOrStatusText.text = _soldOutString;
            _purchaseButton.interactable = false;
        }
        else
        {
            _priceOrStatusText.text = string.Format(_priceFormatString, _itemConfig.Price);
            // ★ (可選) 在這裡可以再加一層檢查：玩家金錢是否足夠
            // bool hasEnoughMoney = GameStatusService.Instance.Protagonist.Money >= _itemConfig.Price;
            // _purchaseButton.interactable = hasEnoughMoney;
            _purchaseButton.interactable = true; // 暫時先都設為可互動
        }
    }

    /// <summary>
    /// 當購買按鈕被點擊時。
    /// </summary>
    private void OnPurchaseButtonClicked()
    {
        if (_itemConfig == null) return;

        // 呼叫核心購買邏輯
        PurchaseResult result = GameStatusService.Instance.ShopService.TryPurchaseItem(_itemConfig.ItemID);

        // 【★ 建議 ★】在這裡根據 result 顯示對應的 UI 提示
        switch (result)
        {
            case PurchaseResult.Success:
                Debug.Log($"購買 {_itemConfig.DisplayName} 成功！");
                // 不需要手動刷新，因為 ShopService -> Inventory -> OnItemCountChanged -> ShopUI 會自動刷新
                break;
            case PurchaseResult.NotEnoughCoins:
                Debug.LogWarning("金幣不足！");
                StoryManager.Instance.ShowLocalizedBadMessage("Ststem.Content_MoneyNotEnough");
                break;
            case PurchaseResult.PurchaseLimitReached:
                Debug.LogWarning("已達購買上限！");
                StoryManager.Instance.ShowLocalizedBadMessage("Ststem.Content_PurchaseLimitReached");
                break;
            case PurchaseResult.ItemNotFound:
                // 這個理論上不該發生
                Debug.LogError("商品不存在？請檢查 ItemDatabase 設定！");
                break;
        }
    }


}