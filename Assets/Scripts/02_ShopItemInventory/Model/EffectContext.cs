/// <summary>
/// 道具效果執行時的上下文容器。
/// 取代舊的 ICharacterStatus，讓每個 Effect 自行決定要操作哪個 Model。
/// </summary>
public class EffectContext
{
    /// <summary>主角狀態（永遠有值）</summary>
    public ProtagonistStatusModel Protagonist;

    /// <summary>目標女主角狀態（送禮時有值，自用時為 null）</summary>
    public HeroineStatusModel Heroine;

    // ── 未來可擴充 ──
    // public InventoryModel Inventory;
    // public TimeModel Time;
}
