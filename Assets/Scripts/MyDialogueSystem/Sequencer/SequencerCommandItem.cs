using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：Item(動作, 物品ID, [數量/變數名])
    ///
    /// 直接操作主角背包 (ProtagonistInventoryModel)。
    ///
    /// 支援動作：
    /// 1. 新增：  Item(Add, key_room01)        -> 新增 1 個
    ///           Item(Add, potion_hp, 3)       -> 新增 3 個
    /// 2. 移除：  Item(Remove, potion_hp)       -> 移除 1 個
    ///           Item(Remove, potion_hp, 2)     -> 移除 2 個 (扣到 0 自動清除)
    /// 3. 設定：  Item(Set, gold_coin, 10)      -> 直接把數量設為 10 (≤0 即移除)
    /// 4. 查找：  Item(Check, key_room01)              -> 把數量寫進 Dialogue 變數 "Item_key_room01"
    ///           Item(Check, key_room01, hasKey)      -> 把數量寫進 Dialogue 變數 "hasKey"
    ///           之後對話條件式可用： Variable["Item_key_room01"] > 0
    ///
    /// 備註：「查找」若要在「對話條件式」直接判斷，建議改用 ItemLuaBridge 的
    ///       ItemCount("id") / HasItem("id") Lua 函式，無需先寫變數。
    /// </summary>
    public class SequencerCommandItem : SequencerCommand
    {
        public void Awake()
        {
            string action = GetParameter(0);
            string itemID = GetParameter(1);

            if (string.IsNullOrEmpty(itemID))
            {
                Debug.LogWarning("[SequencerCommandItem] 缺少物品ID。用法：Item(動作, 物品ID, [數量])");
                Stop();
                return;
            }

            var inventory = GameStatusService.Instance?.Inventory;
            if (inventory == null)
            {
                Debug.LogError("[SequencerCommandItem] 找不到 ProtagonistInventoryModel！請確認 GameStatusService 已初始化。");
                Stop();
                return;
            }

            // ───── 新增 ─────
            if (IsAction(action, "Add", "Give"))
            {
                int amount = GetParameterAsInt(2, 1);
                inventory.AddItem(itemID, amount);
                Debug.Log($"[SequencerCommandItem] Add：+{amount} 個 {itemID}，目前持有 {inventory.GetItemCount(itemID)}");
            }
            // ───── 移除 ─────
            else if (IsAction(action, "Remove", "Take", "Sub"))
            {
                int amount = GetParameterAsInt(2, 1);
                inventory.RemoveItem(itemID, amount);
                Debug.Log($"[SequencerCommandItem] Remove：-{amount} 個 {itemID}，目前持有 {inventory.GetItemCount(itemID)}");
            }
            // ───── 直接設定數量 ─────
            else if (IsAction(action, "Set", "SetCount"))
            {
                int count = GetParameterAsInt(2, 0);
                inventory.SetItemCount(itemID, count);
                Debug.Log($"[SequencerCommandItem] Set：{itemID} = {inventory.GetItemCount(itemID)}");
            }
            // ───── 查找 (寫入 Dialogue 變數) ─────
            else if (IsAction(action, "Check", "Get", "Query"))
            {
                int count = inventory.GetItemCount(itemID);
                string varName = GetParameter(2);
                if (string.IsNullOrEmpty(varName)) varName = "Item_" + itemID;
                DialogueLua.SetVariable(varName, count);
                Debug.Log($"[SequencerCommandItem] Check：{itemID} = {count}，已寫入變數 \"{varName}\"");
            }
            else
            {
                Debug.LogWarning($"[SequencerCommandItem] 未知的動作類型: {action}。可用：Add, Remove, Set, Check。");
            }

            Stop();
        }

        private bool IsAction(string input, params string[] targets)
        {
            foreach (var target in targets)
            {
                if (string.Equals(input, target, System.StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}
