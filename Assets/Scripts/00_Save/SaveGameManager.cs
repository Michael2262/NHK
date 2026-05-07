using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using PixelCrushers;

// 職責:處理檔案讀寫、數據序列化與反序列化。
//
// 【v4 改動】讀檔流程改走 SceneController 正規管線
// - 舊版:SaveSystem.LoadFromSlot 內部會用 LoadSceneMode.Single 卸載所有場景
//         (包括 GlobalStatusUI),且不觸發 SceneController 的 ReadyHandlers 管線
// - 新版:手動走 SceneController.ChangeScene → onFullyReady 裡套用存檔資料
//         需搭配 Unity Inspector 上 Save System 元件的 "Save Current Scene" 取消勾選
//
// 官方依據:https://www.pixelcrushers.com/phpbb/viewtopic.php?t=6615
//
// 存檔:呼叫 CollectSaveData() 來從 GameStatusService 中收集所有遊戲模型的當前狀態。
// 讀檔:透過 SceneController 的正規管線載入場景,完成後才套用數據。

public class SaveGameManager
{
    // 檔案名稱和路徑
    private const string SAVE_FILE_FORMAT = "/gamesave_{0}.json";

    public const int AUTOSAVE_SLOT_INDEX = 0; // 慣例上槽位 0 作為自動存檔

    // 核心依賴
    private readonly GameStatusService _gameStatusService;

    private const string GLOBAL_PROGRESS_FILE = "/global_progress.json";

    private string GlobalProgressPath =>
        Application.persistentDataPath + GLOBAL_PROGRESS_FILE;

    public SaveGameManager(GameStatusService gameStatusService)
    {
        _gameStatusService = gameStatusService;
    }

    // 輔助方法:根據槽位編號獲取完整路徑
    private string GetSavePath(int slotIndex)
    {
        return Application.persistentDataPath + string.Format(SAVE_FILE_FORMAT, slotIndex);
    }

    // ==========================================================
    // 存檔
    // ==========================================================

    public void SaveGame(int slotIndex)
    {
        try
        {
            GameSaveData saveData = CollectSaveData();

            // 填充元數據
            saveData.MetaData = new SaveSlotMetaData
            {
                IsEmpty = false,
                SaveTimestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                GameDay = _gameStatusService.Protagonist.Day,

                // v3.1:存下 Phase 資訊,供讀檔 UI 顯示「DAY X + 時段」
                PhaseIndex = _gameStatusService.Time.CurrentPhaseIndex,
                SlotInPhase = _gameStatusService.Time.CurrentSlotInPhase,
            };

            string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
            string path = GetSavePath(slotIndex);
            File.WriteAllText(path, json);
            Debug.Log($"Game Saved successfully to Slot {slotIndex} at: {path}");

            // 命令 Dialogue System 也存檔到同一個槽位
            SaveSystem.SaveToSlot(slotIndex);
            Debug.Log($"<color=cyan>Dialogue System data saved to slot {slotIndex}.</color>");
        }
        catch (Exception e)
        {
            Debug.LogError($"Save Game to Slot {slotIndex} Failed: {e.Message}");
        }
    }

    // ==========================================================
    // 讀檔(v4 新版:走 SceneController 正規管線)
    // ==========================================================

    /// <summary>
    /// 從槽位讀取遊戲資料。
    /// 
    /// 【v4 新版流程】
    /// 1. 從 Pixel Crushers 讀出 SavedGameData(僅取資料,不還原、不切場景)
    /// 2. 透過 SceneController.ChangeScene 走正規轉場管線
    ///    - GlobalStatusUI 會被保留(不像 LoadSceneMode.Single 會卸載)
    ///    - ExecuteSceneReadyHandlers 會正常執行
    ///    - HubController 等所有 ISceneReadyHandler 會被正確初始化
    /// 3. 場景完全就緒後(onFullyReady 回呼),才套用存檔資料
    /// 
    /// 【前置條件】
    /// Unity 場景中的 Save System 元件需要取消勾選 "Save Current Scene" 選項,
    /// 否則 Pixel Crushers 會試圖自己載入場景,造成衝突。
    /// </summary>
    public bool LoadGame(int slotIndex)
    {
        string path = GetSavePath(slotIndex);
        if (!File.Exists(path))
        {
            Debug.Log($"No save file found for Slot {slotIndex}.");
            return false;
        }

        // 1. 從 Pixel Crushers 的 storer 讀出 SavedGameData(不會觸發場景切換)
        SavedGameData savedGameData = null;
        try
        {
            savedGameData = SaveSystem.storer.RetrieveSavedGameData(slotIndex);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LoadGame] 讀取 Pixel Crushers 存檔失敗: {e.Message}");
            return false;
        }

        if (savedGameData == null)
        {
            Debug.LogError($"[LoadGame] Pixel Crushers 找不到槽位 {slotIndex} 的資料。請確認存檔時 SaveSystem.SaveToSlot 有被呼叫。");
            return false;
        }

        string targetSceneName = savedGameData.sceneName;
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError($"[LoadGame] 存檔中沒有記錄場景名稱,無法讀檔。");
            return false;
        }

        Debug.Log($"<color=cyan>[LoadGame] 準備切換到存檔場景: {targetSceneName} (slot {slotIndex})</color>");

        if (SceneController.Instance == null)
        {
            Debug.LogError("[LoadGame] SceneController.Instance 為 null,無法切換場景!");
            return false;
        }

        // ★ v4 改動:從 onFullyReady 改成 onBeforeHandlers
        // 原因:HubController 等 ReadyHandler 需要先看到已套用的存檔資料才能正確初始化
        // 所以資料注入必須在 Handlers 執行之前完成
        SceneController.ChangeScene(
            targetSceneName,
            -1,
            onBeforeHandlers: () =>
            {
                ApplyLoadedDataAfterSceneReady(slotIndex, savedGameData);
            },
            onFullyReady: null  // 不需要做任何事
        );

        return true;
    }

    /// <summary>
    /// 在場景完全就緒後(ReadyHandlers 全部跑完)套用存檔資料。
    /// 呼叫順序:自訂資料 → NotifyGameStatusLoaded → Pixel Crushers Saver
    /// </summary>
    private void ApplyLoadedDataAfterSceneReady(int slotIndex, SavedGameData savedGameData)
    {
        Debug.Log($"<color=lime>[LoadGame] 場景就緒,開始套用存檔資料...</color>");

        try
        {
            // 3-1. 套用自訂存檔資料(覆寫所有 Model)
            GameSaveData customData = GetSaveDataFromFile(slotIndex);
            if (customData != null)
            {
                ApplySaveData(customData);
                _gameStatusService.NotifyGameStatusLoaded();
            }
            else
            {
                Debug.LogError($"[LoadGame] 槽位 {slotIndex} 的自訂 JSON 檔讀取失敗!");
            }

            // 3-2. 套用 Pixel Crushers 存檔資料(Saver 元件會各自還原狀態)
            SaveSystem.ApplySavedGameData(savedGameData);
            Debug.Log($"<color=lime>[LoadGame] 讀檔完成!</color>");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LoadGame] 套用存檔資料時發生錯誤: {e.Message}\n{e.StackTrace}");
        }
    }

    // ==========================================================
    // 資料套用(v4 保留,供內部呼叫)
    // ==========================================================

    public void ApplySaveData(GameSaveData data)
    {
        if (data == null)
        {
            Debug.LogError("ApplySaveData 接收到空的存檔數據!");
            return;
        }

        // --- 核心數據 ---
        _gameStatusService.Protagonist.LoadFromSaveData(data.ProtagonistData);
        _gameStatusService.Time.LoadFromSaveData(data.TimeData);
        // --- 系統數據 ---
        _gameStatusService.Inventory.LoadFromSaveData(data.InventoryData);
        _gameStatusService.ShopStatus.LoadFromSaveData(data.ShopStatusData);
        _gameStatusService.Skills.LoadFromSaveData(data.SkillData);
        _gameStatusService.StatusEffectModel.LoadFromSaveData(data.StatusEffectData, _gameStatusService.StatusEffectDatabase);
        _gameStatusService.ProgressFlags.LoadFromSaveData(data.ProgressFlagData);
        _gameStatusService.PendingDelivery.LoadFromSaveData(data.DeliveryData);

        // --- 女主角數據 ---
        if (data.HeroineIDs.Count == data.HeroineSaveDataList.Count)
        {
            for (int i = 0; i < data.HeroineIDs.Count; i++)
            {
                string heroineId = data.HeroineIDs[i];
                HeroineSaveData heroineData = data.HeroineSaveDataList[i];
                if (_gameStatusService.Heroines.TryGetValue(heroineId, out HeroineStatusModel heroineModel))
                {
                    heroineModel.LoadFromSaveData(heroineData);
                }
            }
        }
        else
        {
            Debug.LogError("存檔損壞:HeroineIDs 和 HeroineSaveDataList 的長度不匹配!");
        }

        // --- RiskAgent (家人) 數據 ---
        if (data.RiskAgentIDs != null && data.RiskAgentSaveDataList != null &&
            data.RiskAgentIDs.Count == data.RiskAgentSaveDataList.Count)
        {
            for (int i = 0; i < data.RiskAgentIDs.Count; i++)
            {
                string agentId = data.RiskAgentIDs[i];
                RiskAgentSaveData agentData = data.RiskAgentSaveDataList[i];

                if (_gameStatusService.RiskAgents.TryGetValue(agentId, out RiskAgentModel agentModel))
                {
                    agentModel.LoadFromSaveData(agentData);
                }
            }
        }
        else
        {
            Debug.LogError("存檔損壞:RiskAgentIDs 和 RiskAgentSaveDataList 的長度不匹配或為 null!");
        }

        _gameStatusService.Scenario.LoadFromSaveData(data.ScenarioData);

        Debug.Log($"<color=lime>已成功將自定義遊戲數據應用到所有系統中。</color>");
    }

    // ==========================================================
    // 收集存檔資料
    // ==========================================================

    private GameSaveData CollectSaveData()
    {
        GameSaveData data = new GameSaveData();

        data.ProtagonistData = _gameStatusService.Protagonist.ToSaveData();
        data.TimeData = _gameStatusService.Time.ToSaveData();
        data.InventoryData = _gameStatusService.Inventory.ToSaveData();
        data.ShopStatusData = _gameStatusService.ShopStatus.ToSaveData();
        data.SkillData = _gameStatusService.Skills.ToSaveData();
        data.StatusEffectData = _gameStatusService.StatusEffectModel.ToSaveData();
        data.ProgressFlagData = _gameStatusService.ProgressFlags.ToSaveData();
        data.DeliveryData = _gameStatusService.PendingDelivery.ToSaveData();

        data.HeroineIDs.Clear();
        data.HeroineSaveDataList.Clear();
        foreach (var kvp in _gameStatusService.Heroines)
        {
            data.HeroineIDs.Add(kvp.Key);
            data.HeroineSaveDataList.Add(kvp.Value.ToSaveData());
        }

        data.RiskAgentIDs.Clear();
        data.RiskAgentSaveDataList.Clear();
        foreach (var kvp in _gameStatusService.RiskAgents)
        {
            data.RiskAgentIDs.Add(kvp.Key);
            data.RiskAgentSaveDataList.Add(kvp.Value.ToSaveData());
        }

        data.ScenarioData = _gameStatusService.Scenario.ToSaveData();

        return data;
    }

    // ==========================================================
    // 檔案讀取輔助
    // ==========================================================

    public GameSaveData GetSaveDataFromFile(int slotIndex)
    {
        Debug.Log($"讀檔的槽位為 {slotIndex}");
        string path = GetSavePath(slotIndex);
        Debug.Log($"讀檔的位置是 {path}");
        if (!File.Exists(path))
        {
            Debug.Log("空檔");
            return null;
        }
        try
        {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<GameSaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"從槽位 {slotIndex} 讀取 JSON 檔案失敗: {e.Message}");
            return null;
        }
    }

    public SaveSlotMetaData GetMetaDataForSlot(int slotIndex)
    {
        string path = GetSavePath(slotIndex);
        if (!File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            GameSaveData data = JsonConvert.DeserializeObject<GameSaveData>(json);
            return data?.MetaData;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not read metadata from slot {slotIndex}. File might be corrupted. Error: {e.Message}");
            return null;
        }
    }

    // ==========================================================
    // 全域進度存取方法(跨存檔)
    // ==========================================================

    public void SaveGlobalProgress(GlobalProgressModel model)
    {
        try
        {
            string json = JsonConvert.SerializeObject(model.ToSaveData(), Formatting.Indented);
            File.WriteAllText(GlobalProgressPath, json);
            Debug.Log($"Global progress saved to: {GlobalProgressPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Save global progress failed: {e.Message}");
        }
    }

    public GlobalProgressSaveData LoadGlobalProgress()
    {
        if (!File.Exists(GlobalProgressPath)) return null;
        try
        {
            string json = File.ReadAllText(GlobalProgressPath);
            return JsonConvert.DeserializeObject<GlobalProgressSaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Load global progress failed: {e.Message}");
            return null;
        }
    }

    public bool DeleteGlobalProgress()
    {
        if (File.Exists(GlobalProgressPath))
        {
            File.Delete(GlobalProgressPath);
            Debug.Log("Global progress file deleted.");
            return true;
        }
        return false;
    }

    // ==========================================================
    // 尋找最近存檔
    // ==========================================================

    public int FindLatestSaveSlot(int totalSlots = 30)
    {
        int latestSlot = -1;
        DateTime latestTime = DateTime.MinValue;

        for (int i = 0; i < totalSlots; i++)
        {
            SaveSlotMetaData meta = GetMetaDataForSlot(i);
            if (meta == null || meta.IsEmpty) continue;

            if (DateTime.TryParse(meta.SaveTimestamp, out DateTime saveTime))
            {
                if (saveTime > latestTime)
                {
                    latestTime = saveTime;
                    latestSlot = i;
                }
            }
        }

        return latestSlot;
    }

    // ==========================================================
    // 輔助功能
    // ==========================================================

    public bool DeleteSaveFile(int slotIndex)
    {
        string path = GetSavePath(slotIndex);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"Save file for slot {slotIndex} deleted.");
            return true;
        }
        return false;
    }
}
