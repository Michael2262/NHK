using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：Scenario(動作, 參數1, 參數2, 參數3)
    /// 
    /// 1. Update (重算全地圖): Scenario(Update)
    /// 2. Heroine (移動女主): Scenario(Heroine, 角色ID, 地點ID, 行為名稱)
    /// 3. Risk (移動風險): Scenario(Risk, 代理人ID, 地點ID, 行為ID)
    /// </summary>
    public class SequencerCommandScenario : SequencerCommand
    {
        public void Awake()
        {
            if (ScenarioManager.Instance == null)
            {
                Debug.LogWarning("[ScenarioCommand] 場景中找不到 ScenarioManager 實例。");
                Stop();
                return;
            }

            string action = GetParameter(0);

            // --- 1. 重新計算世界狀態 (根據 Flag 與時間重新分配所有人) ---
            if (IsAction(action, "Update", "Refresh"))
            {
                ScenarioManager.Instance.RecalculateWorldState();
            }

            // --- 2. 強制移動女主角 ---
            else if (IsAction(action, "Heroine", "MoveH"))
            {
                // 語法: Scenario(Heroine, Sister, Kitchen, Cooking)
                string hID = GetParameter(1);
                string locID = GetParameter(2);
                string activity = GetParameter(3);
                ScenarioManager.Instance.ForceMoveHeroine(hID, locID, activity);
            }

            // --- 3. 強制移動風險/家人單位 ---
            else if (IsAction(action, "Risk", "MoveR"))
            {
                // 語法: Scenario(Risk, Mother, LivingRoom, Search)
                string aID = GetParameter(1);
                string locID = GetParameter(2);
                string inspectID = GetParameter(3);
                ScenarioManager.Instance.ForceMoveRisk(aID, locID, inspectID);
            }
            else
            {
                Debug.LogWarning($"[ScenarioCommand] 未知的動作類型: {action}");
            }

            Stop();
        }

        private bool IsAction(string input, params string[] targets)
        {
            foreach (var target in targets)
            {
                if (string.Equals(input, target, System.StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}