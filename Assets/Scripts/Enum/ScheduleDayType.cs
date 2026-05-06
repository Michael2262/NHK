// (放在 RiskAgentData.cs 檔案的最下面，所有 '}' 之後)

/// <summary>
/// 定義日程表的日期類型
/// </summary>
public enum ScheduleDayType
{
    EveryDay,    // 每天 (預設)
    WeekdayOnly, // 僅平日
    WeekendOnly  // 僅假日
}