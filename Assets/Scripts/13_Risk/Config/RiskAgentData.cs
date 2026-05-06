using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 【[System.Serializable]】
/// 職責：定義「單一風險來源」(例如一個家人) 的靜態資料。
/// 這就像 "資料夾"，會被 "RiskDatabase" (檔案櫃) 統一管理。
/// </summary>
[System.Serializable]
public class RiskAgentData
{
    [Tooltip("風險來源的唯一ID (例如: \"mother\")")]
    public string agentID;

    [Tooltip("顯示名稱 (例如: \"媽媽\")")]
    public string agentName;

    [Tooltip("此檢查者的完整日程表")]
    public List<RiskScheduleBlock> schedule;
}

/// <summary>
/// 【[System.Serializable]】
/// 職責：定義在「特定時間點」(Phase + Slot) 的所有可能行為
/// </summary>
[System.Serializable]
public class RiskScheduleBlock
{
    
    [Header("易讀性")]
    [Tooltip("此時段的描述 (例如: \"媽媽 - 下午看電視\")")]
    public string blockName; // (這個欄位純粹為了 Inspector 易讀性)

    [Header("WHEN: 時間條件")]
    [Tooltip("對應 TimeSystemModel 的 Phase 索引")]
    public int phaseIndex;

    [Tooltip("對應 TimeSystemModel 的 Slot 索引")]
    public int slotIndex;

    [Tooltip("此行程在哪種日期生效?")]
    public ScheduleDayType dayType = ScheduleDayType.EveryDay; // 預設為每天

    [Header("WHAT: 行為列表")]
    [Tooltip("在這個時間點，此檢查者「可能」執行的所有行為。")]
    public List<RiskAction> possibleActions;
}

/// <summary>
/// 【[System.Serializable]】
/// 職責：定義一個具體的「風險行為」(Fixed 或 Patrol)
/// </summary>
[System.Serializable]
public class RiskAction
{
    [Header("比對ID")]
    [Tooltip("程式邏輯會透過比對 inspectionTypeID 來決定要顯示哪一個 Risk 物件 (例如: \"static_stare\", \"123_wood_man\")")]
    public string inspectionTypeID;

    [Header("Core Logic")]
    [Tooltip("風險類型：Fixed (一開始就在), Patrol (隨機出現)")]
    public RiskActionType actionType;

    [Tooltip("觸發機率 (0-100)")]
    [Range(0, 100)]
    public int triggerChance = 100;

    [Header("類型專用參數")]
    
    [Tooltip("【Fixed 專用】: 此檢查者「固定」的地點 ID 列表。" +
             "Manager 會從中「隨機抽一個」作為本次的固定地點。")]
    public List<string> fixedLocationIDList; // (取代 fixedLocationID)

    [Tooltip("【Patrol 專用】: 此檢查者「絕對不會」巡邏的地點 ID 列表")]
    public List<string> excludedLocationIDs;

    [Header("Flag")]
    [Tooltip("可選：必須啟用此 Flag，這個行為才可能發生")]
    public string requiredFlag;
}

/// <summary>
/// 風險行為的類型 (不變)
/// </summary>
public enum RiskActionType
{
    Fixed,  // 一開始就在場
    Patrol  // 隨機出現
}