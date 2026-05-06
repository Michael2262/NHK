using System;
using System.Collections.Generic;

// 這是一個輕量級的摘要類別,專門用於在「讀檔選單」中顯示資訊
// AI注意:JsonUtility 無法直接序列化 (儲存) Dictionary (字典) 這種資料結構!
[Serializable]
public class SaveSlotMetaData
{
    public bool IsEmpty = true;        // 這個槽位是否為空
    public string SaveTimestamp;       // 存檔時的真實世界時間 (例如 "2025/10/11 16:30")
    public int GameDay;                // 遊戲內的天數

    // ★ v3.1 新增:時間階段資訊(用於 UI 顯示「DAY X 早上/中午/晚上」)
    public int PhaseIndex = -1;        // 存檔時的 Phase Index (0 = 早上, 1 = 中午, ...)
    public int SlotInPhase = -1;       // 存檔時的 Slot In Phase (用於視覺換日判斷)

    // 您未來還可以加入玩家金錢、地點等資訊
}




// 職責:作為遊戲所有可存檔狀態的單一、可序列化容器。
// 這是 SaveGameManager 讀寫檔案的唯一結構。
[Serializable]
public class GameSaveData
{
    // GameSaveData 類別:這是自訂存檔的「根物件」。所有需要儲存的數據都會被打包到這個類別的實例中。

    public SaveSlotMetaData MetaData;
    // ==========================================================
    // 核心數據 (Protagonist, Time)
    // ==========================================================
    public ProtagonistSaveData ProtagonistData;
    public TimeSaveData TimeData;

    // ==========================================================
    // 系統數據 (Inventory, Shop, Skill)
    // ==========================================================
    public InventorySaveData InventoryData;
    public ShopSaveData ShopStatusData;
    public SkillSaveData SkillData;
    public ProtagonistStatusEffectSaveData StatusEffectData; //狀態效果的存檔數據
    public ProgressFlagSaveData ProgressFlagData;// 進度開關存檔數據

    // ==========================================================
    // 女主角數據 (已修改為 JsonUtility 相容格式)
    // ==========================================================
    // ★ 核心改造:不再使用 Dictionary
    // 我們用兩個 List 來模擬 Dictionary 的行為
    public List<string> HeroineIDs = new List<string>();
    public List<HeroineSaveData> HeroineSaveDataList = new List<HeroineSaveData>();

    // ★ RiskAgent 數據 (使用與 Heroine 相同的 List 模式)
    public List<string> RiskAgentIDs = new List<string>();
    public List<RiskAgentSaveData> RiskAgentSaveDataList = new List<RiskAgentSaveData>();

    public CurrentScenarioSaveData ScenarioData;//當前情境數據 (解決中途存檔問題)

    //建構函式,確保新欄位被初始化
    public GameSaveData()
    {
        MetaData = new SaveSlotMetaData();
        ProtagonistData = new ProtagonistSaveData();
        TimeData = new TimeSaveData();
        InventoryData = new InventorySaveData(); // (假設類別名稱)
        ShopStatusData = new ShopSaveData();
        SkillData = new SkillSaveData();
        StatusEffectData = new ProtagonistStatusEffectSaveData();
        ProgressFlagData = new ProgressFlagSaveData();

        // 初始化新增的欄位
        ScenarioData = new CurrentScenarioSaveData();
        // List 欄位會自動被 new List<>() 初始化
    }

}