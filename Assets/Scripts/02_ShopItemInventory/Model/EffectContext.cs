/// <summary>
/// 道具效果執行時的上下文容器。
/// 由 ShopService 注入；僅在實際收到商品時交給購得效果。
/// </summary>
public class EffectContext
{
    /// <summary>主角狀態（永遠有值）</summary>
    public ProtagonistStatusModel Protagonist;

    /// <summary>額外給予道具只操作庫存，不連鎖觸發購得效果。</summary>
    public ProtagonistInventoryModel Inventory;

    public ProgressFlagModel ProgressFlags;
}
