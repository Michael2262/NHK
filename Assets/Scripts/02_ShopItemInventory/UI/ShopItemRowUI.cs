using PixelCrushers.DialogueSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 掛載在商店商品列表行 Prefab 上的腳本。
/// 負責顯示單一商品的靜態與動態資訊，並處理購買按鈕。
/// 文字以 Key 直接查詢 Text Table，每次刷新時套用目前語言。
/// </summary>
public class ShopItemRowUI : MonoBehaviour
{
    [Header("UI 引用 (Prefab 內部拖曳)")]
    [SerializeField] private Image _itemIcon;
    [Tooltip("商品名稱的 TMP 元件；請移除同物件上的 LocalizeUI，避免覆寫文字。")]
    [SerializeField] private TextMeshProUGUI _itemNameText;
    [Tooltip("商品說明的 TMP 元件；請移除同物件上的 LocalizeUI，避免覆寫文字。")]
    [SerializeField] private TextMeshProUGUI _itemDescriptionText;
    [SerializeField] private TextMeshProUGUI _quantityText; // 持有數量
    [SerializeField] private TextMeshProUGUI _priceOrStatusText; // 顯示價格或「售罄」
    [SerializeField] private Button _purchaseButton;

    // 內部狀態
    private ItemConfigData _itemConfig;

    private void Awake()
    {
        _purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
    }



    /// <summary>
    /// 由 ShopUI 在 Instantiate 後呼叫，進行初始化。
    /// </summary>
    public void Setup(ItemConfigData itemConfig)
    {
        _itemConfig = itemConfig;

        // --- 設定靜態資料 ---
        _itemIcon.sprite = _itemConfig.Icon;

        // --- 設定文字與動態資料 ---
        UpdateDynamicData();
    }

    /// <summary>
    /// 更新目前語言的文字、持有數量與售罄狀態。
    /// 由 Setup() 與 ShopUI 呼叫，重開商店時也會重新查表。
    /// </summary>
    public void UpdateDynamicData()
    {
        if (_itemConfig == null) return;

        if (_itemNameText != null)
            _itemNameText.text = Localize(_itemConfig.DisplayNameKey);
        if (_itemDescriptionText != null)
            _itemDescriptionText.text = Localize(_itemConfig.DescriptionKey);

        // 1. 更新持有數量
        int quantity = GameStatusService.Instance.Inventory.GetItemCount(_itemConfig.ItemID);
        _quantityText.text = string.Format(Localize("Store.Quantity"), quantity);

        // 2. 更新購買按鈕狀態（售價或售罄）
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
            _priceOrStatusText.text = Localize("SHOP_SOLD_OUT");
            _purchaseButton.interactable = false;
        }
        else
        {
            _priceOrStatusText.text = string.Format(Localize("Store.Price"), _itemConfig.Price);
            // ★ (可選) 在這裡可以再加一層檢查：玩家金錢是否足夠
            // bool hasEnoughMoney = GameStatusService.Instance.Protagonist.Money >= _itemConfig.Price;
            // _purchaseButton.interactable = hasEnoughMoney;
            _purchaseButton.interactable = true; // 暫時先都設為可互動
        }
    }

    private string Localize(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "";

        string translated = DialogueManager.GetLocalizedText(key);
        if (string.IsNullOrEmpty(translated) || translated == key)
        {
            Debug.LogWarning($"[ShopItemRowUI] Text Table 找不到 Key：{key}", this);
            return key;
        }
        return translated;
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
