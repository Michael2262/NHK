/// <summary>
/// 任何可以「容納 BackpackItemButton 並接收點擊事件」的 UI 都實作此介面。
/// 目前實作者:BackpackUI、GiftUI。
/// </summary>
public interface IItemButtonHost
{
    void OnItemSelected(ItemConfigData item, BackpackItemButton button);
}