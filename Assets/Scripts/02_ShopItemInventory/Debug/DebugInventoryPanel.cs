using System.Collections.Generic;
using UnityEngine;

public class DebugInventoryPanel : MonoBehaviour
{
    [SerializeField] private GameObject inventoryItemPrefab;
    [SerializeField] private Transform container;

    private Dictionary<string, DebugInventoryItemUI> _uiItems = new Dictionary<string, DebugInventoryItemUI>();

    void Start()
    {
        // ★ 除錯點 1：確認這個腳本自己有沒有啟動
        Debug.Log("[DebugInventoryPanel] Start() 方法被執行！準備初始化...");
        Invoke(nameof(Initialize), 0.1f);
    }

    private void Initialize()
    {
        if (GameStatusService.Instance == null || GameStatusService.Instance.Inventory == null)
        {
            Debug.LogError("[DebugInventoryPanel] 初始化失敗：GameStatusService 或 Inventory 不存在！");
            return;
        }

        // ★ 除錯點 2：確認事件訂閱有沒有成功
        Debug.Log("[DebugInventoryPanel] 初始化成功，正在訂閱 OnItemCountChanged 事件...");
        GameStatusService.Instance.Inventory.OnItemCountChanged += HandleItemCountChanged;
    }

    private void OnDestroy()
    {
        if (GameStatusService.Instance != null && GameStatusService.Instance.Inventory != null)
        {
            GameStatusService.Instance.Inventory.OnItemCountChanged -= HandleItemCountChanged;
        }
    }

    private void HandleItemCountChanged(string itemID, int newCount)
    {
        // ★ 除錯點 3：確認有沒有「聽到」廣播
        Debug.Log($"<color=green>[DebugInventoryPanel] 聽到了！物品 '{itemID}' 的數量變成了 {newCount}！</color>");

        if (newCount > 0)
        {
            if (_uiItems.TryGetValue(itemID, out DebugInventoryItemUI uiItem))
            {
                uiItem.Setup(itemID, newCount);
            }
            else
            {
                var newUIObject = Instantiate(inventoryItemPrefab, container);
                var newUIComponent = newUIObject.GetComponent<DebugInventoryItemUI>();
                newUIComponent.Setup(itemID, newCount);
                _uiItems[itemID] = newUIComponent;
            }
        }
        else
        {
            if (_uiItems.TryGetValue(itemID, out DebugInventoryItemUI uiItem))
            {
                Destroy(uiItem.gameObject);
                _uiItems.Remove(itemID);
            }
        }
    }
}