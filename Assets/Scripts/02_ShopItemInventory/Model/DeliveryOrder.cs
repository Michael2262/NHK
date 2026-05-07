using System;

/// <summary>
/// 一筆網購訂單的資料。
/// 記錄「買了什麼、幾個、哪一天到貨」。
/// </summary>
[Serializable]
public class DeliveryOrder
{
    /// <summary>商品 ID</summary>
    public string ItemID;

    /// <summary>購買數量</summary>
    public int Amount;

    /// <summary>預定到貨的遊戲天數（下單當天 + 1）</summary>
    public int DeliveryDay;

    public DeliveryOrder(string itemID, int amount, int deliveryDay)
    {
        ItemID = itemID;
        Amount = amount;
        DeliveryDay = deliveryDay;
    }
}
