using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugGiftPanel : MonoBehaviour
{
    [Header("UI 元素")]
    [SerializeField] private GameObject heroineButtonPrefab;
    [SerializeField] private Transform heroineContainer;
    [SerializeField] private GameObject giftItemButtonPrefab;
    [SerializeField] private Transform itemContainer;
    [SerializeField] private TextMeshProUGUI selectedInfoText;
    [SerializeField] private Button confirmGiftButton;

    // 內部狀態
    private HeroineStatusModel _selectedHeroine;
    private string _selectedItemID;

    void Start()
    {
        confirmGiftButton.onClick.AddListener(OnConfirmGift);
        // 訂閱背包變化事件，以便在送禮後自動刷新物品列表
        GameStatusService.Instance.Inventory.OnItemCountChanged += (id, count) => PopulateItemList();

        // 初始化面板
        PopulateHeroineList();
        PopulateItemList();
        UpdateSelectionInfo();
    }

    // 動態生成女主角按鈕
    private void PopulateHeroineList()
    {
        foreach (Transform child in heroineContainer) Destroy(child.gameObject);

        foreach (var heroine in GameStatusService.Instance.Heroines.Values)
        {
            var btnObj = Instantiate(heroineButtonPrefab, heroineContainer);
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = heroine.Name;
            // 讓按鈕在被點擊時，呼叫我們的 SelectHeroine 方法
            btnObj.GetComponent<Button>().onClick.AddListener(() => SelectHeroine(heroine));
        }
    }

    // 動態生成背包物品按鈕
    private void PopulateItemList()
    {
        
        if(itemContainer != null)
            foreach (Transform child in itemContainer) Destroy(child.gameObject);

        var inventory = GameStatusService.Instance.Inventory;
        // 我們需要一個方法來獲取背包中所有物品的列表
        // (這需要在 ProtagonistInventoryModel 中新增一個輔助方法)
        foreach (var itemKvp in inventory.GetAllItems())
        {
            var itemConfig = GameStatusService.Instance.ItemDatabase.GetItemConfig(itemKvp.Key);
            if (itemConfig != null)
            {
                var btnObj = Instantiate(giftItemButtonPrefab, itemContainer);
                //btnObj.GetComponentInChildren<TextMeshProUGUI>().text = $"{itemConfig.DisplayName} x{itemKvp.Value}";
                btnObj.GetComponent<Button>().onClick.AddListener(() => SelectItem(itemKvp.Key));
            }
        }
    }

    // --- 處理選擇的邏輯 ---
    public void SelectHeroine(HeroineStatusModel heroine)
    {
        _selectedHeroine = heroine;
        UpdateSelectionInfo();
    }

    public void SelectItem(string itemID)
    {
        _selectedItemID = itemID;
        UpdateSelectionInfo();
    }

    // 更新提示文字和確認按鈕的狀態
    private void UpdateSelectionInfo()
    {
        if (_selectedHeroine != null && !string.IsNullOrEmpty(_selectedItemID))
        {
            var itemConfig = GameStatusService.Instance.ItemDatabase.GetItemConfig(_selectedItemID);
            //selectedInfoText.text = $"準備將 [{itemConfig.DisplayName}] 送給 [{_selectedHeroine.Name}]";
            confirmGiftButton.interactable = true; // 啟用按鈕
        }
        else
        {
            selectedInfoText.text = "請選擇一位女主角和要送的禮物...";
            confirmGiftButton.interactable = false; // 禁用按鈕
        }
    }

    // --- 核心送禮邏輯 ---
    private void OnConfirmGift()
    {
        if (_selectedHeroine == null || string.IsNullOrEmpty(_selectedItemID)) return;

        Debug.Log($"[UI] 確認送禮：將 {_selectedItemID} 送給 {_selectedHeroine.HeroineID}");

        // 呼叫我們之前做好的後端系統！
        // 系統會自動處理可用性檢查、消耗物品、應用效果等所有事
        //bool success = GameStatusService.Instance.Inventory.TryUseItem(_selectedItemID, _selectedHeroine);

        //if (success)
        //{
        //    Debug.Log("<color=green>送禮成功！</color>");
        //}
        //else
        //{
        //    Debug.Log("<color=red>送禮失敗！（可能是道具無法對她使用）</color>");
        //}

        // 清空選擇，為下一次送禮做準備
        //_selectedItemID = null;
        //UpdateSelectionInfo();
        // 背包物品列表會因為訂閱了事件而自動刷新
    }
}