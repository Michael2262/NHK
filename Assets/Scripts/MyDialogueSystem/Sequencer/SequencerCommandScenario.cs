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
    /// 4. 指定活動並排他切換所屬行程 Flag：Scenario(Activate, 女主角ID, 活動ID)
    /// 5. 依 Flag 還原：Scenario(RefreshHeroines)。舊 RerollHeroines 僅保留為刷新別名。
    /// 6. 手動修改 Flag 的跨幀批次：Scenario(BeginBatch) → 修改 Flag → Scenario(EndBatch)。啟動與離場請在批次外呼叫。
    /// 7. 指定 Flag 並抽選一次活動：Scenario(ActivateFlag, 女主角ID, 狀態FlagID)
    /// 8. 關閉所有行程 Flag 並離場：Scenario(Deactivate, 女主角ID)
    /// </summary>
    public class SequencerCommandScenario : SequencerCommand
    {
        public void Awake()
        {
            string action = GetParameter(0);
            var flagSchedule = GameStatusService.Instance?.HeroineFlagSchedule;
            if (IsAction(action, "Activate", "ActivateFlag", "Deactivate", "RefreshHeroines", "RerollHeroines", "BeginBatch", "EndBatch"))
            {
                if (flagSchedule == null) Debug.LogWarning("[ScenarioCommand] 目前未啟用 Flag 規則排程。");
                else if (IsAction(action, "Activate")) flagSchedule.TryActivateActivity(GetParameter(1), GetParameter(2));
                else if (IsAction(action, "ActivateFlag")) flagSchedule.TryActivateFlag(GetParameter(1), GetParameter(2));
                else if (IsAction(action, "Deactivate")) flagSchedule.TryDeactivate(GetParameter(1));
                else if (IsAction(action, "BeginBatch")) flagSchedule.BeginBatch();
                else if (IsAction(action, "EndBatch")) flagSchedule.EndBatch();
                else flagSchedule.Refresh();
                Stop();
                return;
            }
            if (ScenarioManager.Instance == null)
            {
                Debug.LogWarning("[ScenarioCommand] 場景中找不到 ScenarioManager 實例。");
                Stop();
                return;
            }

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
