using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 職責：在遊戲開始時，動態生成商店中所有可購買商品的 UI 按鈕。
/// </summary>
public class DebugShopPanel : MonoBehaviour
{
    [Header("UI 預製件與容器")]
    [SerializeField] private GameObject shopItemPrefab; // 拖曳你的 ShopItem_Prefab
    [SerializeField] private Transform container;       // 拖曳 ShopPanel_Container

    void Start()
    {
        // 使用 Invoke 確保 GameStatusService 已經完成初始化
        Invoke(nameof(PopulateShop), 0.1f);
    }

    /// <summary>
    /// 填充商店介面
    /// </summary>
    private void PopulateShop()
    {
        if (GameStatusService.Instance == null || GameStatusService.Instance.ItemDatabase == null)
        {
            Debug.LogError("DebugShopPanel: GameStatusService 或 ItemDatabase 尚未初始化！");
            return;
        }

        // 獲取資料庫中的所有商品設定
        //List<ItemConfigData> allItems = GameStatusService.Instance.ItemDatabase.AllItems;

        // 遍歷所有商品，為每一個都創建一個 UI 物件
        //foreach (var itemConfig in allItems)
        //{
            // 實例化 Prefab，並將其父物件設為 container
            //var newUIObject = Instantiate(shopItemPrefab, container);

            // 獲取該 UI 物件上的腳本組件
            //var uiComponent = newUIObject.GetComponent<DebugShopItemUI>();

            // 呼叫 Setup 方法，將商品數據傳遞給 UI
            //if (uiComponent != null)
            //{
                //uiComponent.Setup(itemConfig);
            //}
       // }
    }
}