// (放在 RiskAgentData.cs 檔案的最下面，所有 '}' 之後)

/// <summary>
/// 定義日程表的日期類型
/// </summary>
public enum ScheduleDayType
{
    [UnityEngine.InspectorName("每天")]
    EveryDay = 0,
    [UnityEngine.InspectorName("僅平日")]
    WeekdayOnly = 1,
    [UnityEngine.InspectorName("僅週末")]
    WeekendOnly = 2
}
