// 大冒險效果執行時的上下文容器。
// 每個 AdventureEffect 自行決定要操作哪個 Model / Run 狀態。
// 未來要擴充（女主角、時間、風險…）就往這裡加欄位。
public class AdventureContext
{
    /// <summary>主角狀態（永遠有值）</summary>
    public ProtagonistStatusModel Protagonist;

    /// <summary>主角背包（給 GiveItem 之類的效果用）</summary>
    public ProtagonistInventoryModel Inventory;

    /// <summary>進度旗標（給設 Flag / 標記通關用）</summary>
    public ProgressFlagModel ProgressFlags;

    /// <summary>目前這趟大冒險的執行狀態（給里程 / 結束之類的效果用）</summary>
    public AdventureRunModel Run;
}
