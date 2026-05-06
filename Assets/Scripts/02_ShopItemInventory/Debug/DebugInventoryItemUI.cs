using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugInventoryItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemInfoText;
    [SerializeField] private Button useButton;

    private string _itemID;

    public void Setup(string itemID, int count)
    {
        _itemID = itemID;
        // 從 ItemDatabase 獲取顯示名稱
        var itemConfig = GameStatusService.Instance.ItemDatabase.GetItemConfig(itemID);
        //itemInfoText.text = $"{itemConfig.DisplayName} x{count}";

        useButton.onClick.RemoveAllListeners(); // 先移除舊的監聽器
        useButton.onClick.AddListener(OnUse);
    }

    private void OnUse()
    {
        if (string.IsNullOrEmpty(_itemID)) return;

        Debug.Log($"[UI] 嘗試使用: {_itemID}");
        // 呼叫背包的 TryUseItem 方法
        //GameStatusService.Instance.Inventory.TryUseItem(_itemID);
    }
}