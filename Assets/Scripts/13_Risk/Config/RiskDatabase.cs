using UnityEngine;
using System.Collections.Generic;
using System.Linq; // 方便用 Linq 查詢

/// <summary>
/// 【[CreateAssetMenu]】
/// 職責：作為一個「檔案櫃」(.asset 檔案)，儲存「所有」風險來源 (家人) 的日程表。
/// </summary>
[CreateAssetMenu(menuName = "Game/Config/Risk Database")]
public class RiskDatabase : ScriptableObject
{
    [Tooltip("遊戲中所有家人的靜態日程表")]
    public List<RiskAgentData> allAgents;

    /// <summary>
    /// 方便 Manager 透過 ID 找到特定家人的資料
    /// </summary>
    public RiskAgentData FindAgentByID(string id)
    {
        return allAgents.Find(agent => agent.agentID == id);
    }

    /// <summary>
    /// 查詢在特定時間點，所有家人的「有效」行為，並加入假日邏輯
    /// </summary>
    public List<RiskAction> GetActiveActions(string agentID, int phase, int slot, bool isWeekend) 
    {
        RiskAgentData agent = FindAgentByID(agentID);
        if (agent == null) return new List<RiskAction>();

        // 找出所有符合「時間」的區塊
        var potentialBlocks = agent.schedule.Where(b => b.phaseIndex == phase && b.slotIndex == slot);
        if (!potentialBlocks.Any()) return new List<RiskAction>();

        // 假日規則優先權邏輯
        RiskScheduleBlock chosenBlock = null;

        if (isWeekend)
        {
            // 優先找「僅假日」的
            chosenBlock = potentialBlocks.FirstOrDefault(b => b.dayType == ScheduleDayType.WeekendOnly);
        }
        else
        {
            // 優先找「僅平日」的
            chosenBlock = potentialBlocks.FirstOrDefault(b => b.dayType == ScheduleDayType.WeekdayOnly);
        }

        //如果找不到特定日的，才找「每日」
        if (chosenBlock == null)
        {
            chosenBlock = potentialBlocks.FirstOrDefault(b => b.dayType == ScheduleDayType.EveryDay);
        }

        //如果找到了符合條件的區塊
        if (chosenBlock != null)
        {
            return chosenBlock.possibleActions; // 回傳該區塊的行為列表
        }

        // 什麼都沒找到
        return new List<RiskAction>();
    }

}