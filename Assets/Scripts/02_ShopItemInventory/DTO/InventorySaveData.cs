using System;
using System.Collections.Generic;

// 職責：ProtagonistInventoryModel 的存檔數據容器
[Serializable]
public class InventorySaveData
{
    // 儲存 <物品ID, 數量> 的字典
    public Dictionary<string, int> ItemCounts = new Dictionary<string, int>();
}