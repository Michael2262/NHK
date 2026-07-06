using System.Collections.Generic;

/// <summary>
/// 任務目標存檔資料。
/// 三態中只存 Revealed / Completed 兩個集合；Hidden 是預設值不需要存，
/// 所以舊存檔載入後，之後才新增的目標自動就是「未顯示」，天生向下相容。
/// </summary>
[System.Serializable]
public class QuestObjectiveSaveData
{
    public List<string> RevealedIDs = new List<string>();  // 已顯示但未完成
    public List<string> CompletedIDs = new List<string>(); // 已完成
}
