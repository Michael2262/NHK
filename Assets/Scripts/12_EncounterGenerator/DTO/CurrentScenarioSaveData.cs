using System;
using System.Collections.Generic;

/// <summary>
/// 【[System.Serializable]】
/// 職責：存檔和讀檔的數據容器。
/// 因應新架構，現在這裡會儲存「整個地圖」每一個房間的狀態，
/// 讓你在讀檔後，小地圖也能知道所有人在哪。
/// </summary>
[Serializable]
public class CurrentScenarioSaveData
{
    // 是否正處於一個「情境」中
    public bool IsInScenario;

    // 玩家當前所在的房間 ID
    public string LocationID;

    // ★ 核心改變：儲存所有地點的狀態 
    // Key: 地點ID (如 "LivingRoom"), Value: 該地點內的 NPC 和風險狀態
    public Dictionary<string, LocationState> AllLocationStates;

    // ==========================================================
    // 【NEW】今日行為鍊指派
    // Key: heroineID, Value: 今天抽中的鍊名稱 (chainName)
    // 存檔時紀錄，讀檔後可還原「今天這個女主角正在跑哪條鍊」
    // ==========================================================
    public Dictionary<string, string> TodaysChainByHeroine;
}

// ==========================================================
// 輔助資料結構 (必須放在這裡，讓 SaveSystem 能看懂)
// ==========================================================

/// <summary>
/// 代表「單一房間」內的狀況
/// </summary>
[Serializable]
public class LocationState
{
    // 這個房間裡有哪些女主角？
    public List<HeroineStateData> Heroines = new List<HeroineStateData>();

    // 這個房間裡有哪些風險？
    public List<RiskAction> Risks = new List<RiskAction>();
}

/// <summary>
/// 紀錄女主角的簡單狀態 (ID + 行為)
/// </summary>
[Serializable]
public class HeroineStateData
{
    public string HeroineID; // 例如 "sister"
    public string Activity;  // 例如 "WatchingTV"
}