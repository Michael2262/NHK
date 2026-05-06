using System;
using System.Collections.Generic;

// 職責：ShopStatusModel 的存檔數據容器
[Serializable]
public class ShopSaveData
{
    // 儲存 <物品ID, 已購買次數> 的字典
    public Dictionary<string, int> PurchasedCounts = new Dictionary<string, int>();
}