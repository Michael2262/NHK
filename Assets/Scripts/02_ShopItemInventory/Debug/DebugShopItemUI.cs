using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugShopItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private Button purchaseButton;

    private ItemConfigData _itemConfig;



    public void Setup(ItemConfigData itemConfig)
    {
        _itemConfig = itemConfig;
        //itemNameText.text = $"{_itemConfig.DisplayName} (${_itemConfig.Price})";
       purchaseButton.onClick.AddListener(OnPurchase);
    }

    public void OnPurchase()
    {
        if (_itemConfig == null) return;

        //Debug.Log($"[UI] 嘗試購買: {_itemConfig.DisplayName}");
        // 透過 GameStatusService 呼叫後端邏輯
        GameStatusService.Instance.ShopService.TryPurchaseItem(_itemConfig.ItemID);
    }
}