using UnityEngine;

/// <summary>
/// 【橋接器】(適配新版 Pre-calculation 架構)
/// 職責：接收 Unity UI Button 事件，轉發給 ScenarioManager 或 Model。
/// </summary>
public class ScenarioManagerBridge : MonoBehaviour
{
    /// <summary>
    /// 【核心功能】前往地點
    /// </summary>
    /// <param name="locationID">地點 ID (例如 "LivingRoom")</param>
    public void GoToLocationFromMap(string locationID)
    {
        if (ScenarioManager.Instance == null)
        {
            Debug.LogError("ScenarioManagerBridge: 找不到 ScenarioManager.Instance！");
            return;
        }

        // 呼叫新的 API：設定玩家位置並載入場景
        ScenarioManager.Instance.OnPlayerSelectLocation(locationID);
    }

    /// <summary>
    /// 【精簡功能】僅更新邏輯位置 (新增方法)
    /// 職責：轉發指令給 ScenarioManager 以標記 IsInScenario 並更新 LocationID。
    /// </summary>
    /// <param name="locationID">地點 ID (例如 "LivingRoom")</param>
    public void ChangeLocationID(string locationID)
    {
        if (ScenarioManager.Instance == null)
        {
            Debug.LogError("ScenarioManagerBridge: 找不到 ScenarioManager.Instance！");
            return;
        }

        // 轉發給 ScenarioManager 執行精簡版位移
        ScenarioManager.Instance.ChangeLocation(locationID);
    }

    

    /// <summary>
    /// 【除錯/測試用】強制重算全地圖
    /// 如果你覺得運氣不好，按這個按鈕可以讓所有人重新擲骰子換位置。
    /// </summary>
    public void ForceRerollWorld()
    {
        if (ScenarioManager.Instance == null) return;

        Debug.Log("[Bridge] 強制重算世界狀態 (Reroll)...");
        ScenarioManager.Instance.RecalculateWorldState();
    }

    /// <summary>
    /// 【返回地圖用】清除情境狀態
    /// 當玩家按下「返回地圖」按鈕時呼叫。
    /// </summary>
    public void ExitScenarioMode()
    {
        if (GameStatusService.Instance != null)
        {
            GameStatusService.Instance.Scenario.ExitScenario();
        }
    }

    // ==========================================================
    // 強制移動 API
    // ==========================================================

    /// <summary>
    /// 【演出控制】強制移動女主角 (標準版)
    /// 給程式腳本、Dialogue System 或 Animation Event 呼叫。
    /// </summary>
    public void ForceMoveHeroine(string heroineID, string targetLocationID, string newActivity)
    {
        if (ScenarioManager.Instance != null)
        {
            ScenarioManager.Instance.ForceMoveHeroine(heroineID, targetLocationID, newActivity);
        }
    }

    /// <summary>
    /// 【UI 按鈕專用】強制移動女主角 (解析字串版)
    /// 因為 Unity Button Inspector 只能傳 1 個字串，所以用這個方法。
    /// <para>用法：在 Button 的 String 欄位填入 "sister,LivingRoom,Napping" (用逗號分隔)</para>
    /// </summary>
    public void ForceMoveHeroineUI(string command)
    {
        if (string.IsNullOrEmpty(command)) return;

        // 解析字串: "ID,Location,Activity"
        string[] parts = command.Split(',');
        if (parts.Length >= 3)
        {
            string hID = parts[0].Trim();
            string loc = parts[1].Trim();
            string act = parts[2].Trim();

            ForceMoveHeroine(hID, loc, act);
        }
        else if (parts.Length == 2)
        {
            // 支援只傳 ID 和地點 (假設是 Idle 或者消失)
            string hID = parts[0].Trim();
            string loc = parts[1].Trim();
            // 如果地點是空字串或 "null"，視為消失
            if (loc.ToLower() == "null" || loc == "")
                ForceMoveHeroine(hID, null, "");
            else
                Debug.LogWarning($"[Bridge] 參數不足，未指定 Activity。若要移動請補上。");
        }
        else
        {
            Debug.LogWarning($"[Bridge] 指令格式錯誤: '{command}'。\n正確格式範例: 'sister,LivingRoom,Napping' 或 'sister,null,null'(消失)");
        }
    }

    /// <summary>
    /// 【UI 按鈕專用】強制移動風險角色
    /// 用法：在 Button 填入 "father,Kitchen,GenericPatrol"
    /// </summary>
    public void ForceMoveRiskUI(string command)
    {
        if (string.IsNullOrEmpty(command)) return;

        string[] parts = command.Split(',');
        if (parts.Length >= 3)
        {
            string aID = parts[0].Trim();
            string loc = parts[1].Trim();
            string act = parts[2].Trim();

            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.ForceMoveRisk(aID, loc, act);
            }
        }
        else
        {
            Debug.LogWarning($"[Bridge] 風險移動格式錯誤。正確格式: 'agentID,Location,ActionID'");
        }
    }
}