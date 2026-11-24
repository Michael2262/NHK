using System;

/// <summary>
/// 一筆網購訂單的資料。
/// 記錄下單時的實際入庫數量、購買份數與到貨日。
/// </summary>
[Serializable]
public class DeliveryOrder
{
    /// <summary>商品 ID</summary>
    public string ItemID;

    /// <summary>實際入庫個數；沿用舊存檔欄位，不因商品設定改動而重新換算。</summary>
    public int Amount;

    /// <summary>購得效果執行次數。舊存檔缺此欄位時，按舊版每份一個解讀。</summary>
    public int PurchaseCount;

    /// <summary>預定到貨的遊戲天數（下單當天 + 1）</summary>
    public int DeliveryDay;

    public DeliveryOrder(string itemID, int amount, int deliveryDay, int purchaseCount = 0)
    {
        ItemID = itemID;
        Amount = amount;
        PurchaseCount = purchaseCount > 0 ? purchaseCount : amount;
        DeliveryDay = deliveryDay;
    }
}
