using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：AddItem(物品ID, [數量])
    /// 
    /// 參數說明：
    /// - 物品ID (必填)：要新增的物品 ID
    /// - 數量 (選填，預設 1)：要新增的數量，必須為正整數
    /// 
    /// 使用範例：
    /// 1. AddItem(key_room01)
    ///    → 新增 1 個 key_room01 到背包
    /// 
    /// 2. AddItem(potion_hp, 3)
    ///    → 新增 3 個 potion_hp 到背包
    /// </summary>
    public class SequencerCommandAddItem : SequencerCommand
    {
        public void Awake()
        {
            // ============================================================
            // 獲取參數
            // ============================================================

            string itemID = GetParameter(0);
            int amount = GetParameterAsInt(1, 1);

            // ============================================================
            // 安全檢查
            // ============================================================

            if (string.IsNullOrEmpty(itemID))
            {
                Debug.LogWarning("[SequencerCommandAddItem] 物品ID為空，請檢查對話指令。");
                Stop();
                return;
            }

            if (amount <= 0)
            {
                Debug.LogWarning($"[SequencerCommandAddItem] 數量必須為正整數，收到: {amount}");
                Stop();
                return;
            }

            if (GameStatusService.Instance == null)
            {
                Debug.LogError("[SequencerCommandAddItem] 找不到 GameStatusService！");
                Stop();
                return;
            }

            // ============================================================
            // 執行邏輯
            // ============================================================

            var inventory = GameStatusService.Instance.Inventory;

            if (inventory == null)
            {
                Debug.LogError("[SequencerCommandAddItem] 找不到 ProtagonistInventoryModel！");
                Stop();
                return;
            }

            inventory.AddItem(itemID, amount);
            Debug.Log($"[SequencerCommandAddItem] 已新增 {amount} 個 {itemID} 到背包。目前持有: {inventory.GetItemCount(itemID)}");

            Stop();
        }
    }
}