using System;
using System.Collections.Generic;

/// <summary>
/// 網購系統的存檔資料結構。
/// </summary>
[Serializable]
public class DeliverySaveData
{
    public List<DeliveryOrder> PendingOrders = new List<DeliveryOrder>();
}
