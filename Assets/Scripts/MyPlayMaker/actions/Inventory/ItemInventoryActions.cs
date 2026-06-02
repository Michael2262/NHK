using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    /// <summary>
    /// 背包操作的共用基底：解析物品 ID。
    /// 物品來源優先序：拖入的 ItemConfigData SO > 字串 ID 欄位。
    /// </summary>
    public abstract class InventoryActionBase : FsmStateAction
    {
        [Tooltip("拖入道具設定檔 (ItemConfigData)。優先採用此欄位的 ItemID。")]
        public ItemConfigData item;

        [Tooltip("若沒拖 ItemConfigData，改用此字串作為物品 ID。")]
        public FsmString itemID;

        protected string ResolveItemID()
        {
            if (item != null) return item.ItemID;
            if (itemID != null && !itemID.IsNone && !string.IsNullOrEmpty(itemID.Value))
                return itemID.Value;
            return null;
        }

        protected ProtagonistInventoryModel GetInventory()
        {
            var inv = GameStatusService.Instance != null
                ? GameStatusService.Instance.Inventory
                : null;
            if (inv == null)
                Debug.LogError("[Inventory Action] 找不到 ProtagonistInventoryModel！請確認 GameStatusService 已初始化。");
            return inv;
        }

        public override void Reset()
        {
            item = null;
            itemID = new FsmString { UseVariable = false };
        }
    }

    [ActionCategory("Inventory")]
    [Tooltip("向主角背包新增指定數量的道具。")]
    public class AddInventoryItem : InventoryActionBase
    {
        [Tooltip("新增數量 (正整數)。")]
        public FsmInt amount;

        public override void Reset()
        {
            base.Reset();
            amount = 1;
        }

        public override void OnEnter()
        {
            var inv = GetInventory();
            string id = ResolveItemID();
            if (inv != null && !string.IsNullOrEmpty(id))
                inv.AddItem(id, amount.Value);
            Finish();
        }
    }

    [ActionCategory("Inventory")]
    [Tooltip("從主角背包移除指定數量的道具 (扣到 0 自動清除)。")]
    public class RemoveInventoryItem : InventoryActionBase
    {
        [Tooltip("移除數量 (正整數)。")]
        public FsmInt amount;

        public override void Reset()
        {
            base.Reset();
            amount = 1;
        }

        public override void OnEnter()
        {
            var inv = GetInventory();
            string id = ResolveItemID();
            if (inv != null && !string.IsNullOrEmpty(id))
                inv.RemoveItem(id, amount.Value);
            Finish();
        }
    }

    [ActionCategory("Inventory")]
    [Tooltip("直接設定背包中某道具的數量 (≤0 即移除)。")]
    public class SetInventoryItemCount : InventoryActionBase
    {
        [Tooltip("要設定的數量。")]
        public FsmInt count;

        public override void Reset()
        {
            base.Reset();
            count = 0;
        }

        public override void OnEnter()
        {
            var inv = GetInventory();
            string id = ResolveItemID();
            if (inv != null && !string.IsNullOrEmpty(id))
                inv.SetItemCount(id, count.Value);
            Finish();
        }
    }

    [ActionCategory("Inventory")]
    [Tooltip("查找背包中某道具的數量，可存入變數並與指定值比較後送出事件。")]
    public class CheckInventoryItemCount : InventoryActionBase
    {
        public enum CompareType
        {
            Equal,
            NotEqual,
            GreaterThan,
            GreaterOrEqual,
            LessThan,
            LessOrEqual
        }

        [Tooltip("比較方式 (預設：持有量 >= 要比較的數量)。")]
        public CompareType compareType = CompareType.GreaterOrEqual;

        [Tooltip("要比較的數量。")]
        public FsmInt compareValue;

        [Tooltip("條件成立時發送。")]
        public FsmEvent trueEvent;

        [Tooltip("條件不成立時發送。")]
        public FsmEvent falseEvent;

        [UIHint(UIHint.Variable)]
        [Tooltip("將目前持有數量存入此變數。")]
        public FsmInt storeCount;

        [UIHint(UIHint.Variable)]
        [Tooltip("將比較結果存入此變數。")]
        public FsmBool storeResult;

        public bool everyFrame;

        public override void Reset()
        {
            base.Reset();
            compareType = CompareType.GreaterOrEqual;
            compareValue = 1;
            trueEvent = null;
            falseEvent = null;
            storeCount = new FsmInt { UseVariable = false };
            storeResult = new FsmBool { UseVariable = false };
            everyFrame = false;
        }

        public override void OnEnter()
        {
            DoCheck();
            if (!everyFrame) Finish();
        }

        public override void OnUpdate()
        {
            DoCheck();
        }

        private void DoCheck()
        {
            var inv = GetInventory();
            string id = ResolveItemID();

            int current = (inv != null && !string.IsNullOrEmpty(id))
                ? inv.GetItemCount(id)
                : 0;

            if (storeCount != null && !storeCount.IsNone)
                storeCount.Value = current;

            bool result = compareType switch
            {
                CompareType.Equal => current == compareValue.Value,
                CompareType.NotEqual => current != compareValue.Value,
                CompareType.GreaterThan => current > compareValue.Value,
                CompareType.GreaterOrEqual => current >= compareValue.Value,
                CompareType.LessThan => current < compareValue.Value,
                CompareType.LessOrEqual => current <= compareValue.Value,
                _ => false
            };

            if (storeResult != null && !storeResult.IsNone)
                storeResult.Value = result;

            Fsm.Event(result ? trueEvent : falseEvent);
        }
    }
}
