using UnityEngine;
using PixelCrushers.DialogueSystem;
using WoodenMan;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：WoodenMan(動作, 參數1, 參數2)
    /// </summary>
    public class SequencerCommandWoodenMan : SequencerCommand
    {
        public void Awake()
        {
            if (WoodenManGameManager.Instance == null)
            {
                Debug.LogWarning("[WoodenManCommand] 場景中找不到 WoodenManGameManager 實例。");
                Stop();
                return;
            }

            string action = GetParameter(0);

            // --- 1. 鬼怪啟動/關閉 ---
            if (IsAction(action, "Activate", "On"))
            {
                // 語法: WoodenMan(Activate, ActionID)
                WoodenManGameManager.Instance.ActivateGhostByID(GetParameter(1));
            }
            else if (IsAction(action, "Deactivate", "Off"))
            {
                // 語法: WoodenMan(Deactivate, ActionID)
                WoodenManGameManager.Instance.DeactivateGhostByID(GetParameter(1));
            }

            // --- 2. 計時器控制 ---
            else if (IsAction(action, "Start", "StartTimer"))
            {
                // 語法: WoodenMan(Start)
                WoodenManGameManager.Instance.StartGhostTimer();
            }
            else if (IsAction(action, "Stop", "StopTimer"))
            {
                // 語法: WoodenMan(Stop)
                WoodenManGameManager.Instance.StopGhostTimer();
            }

            // --- 3. 核心邏輯觸發 ---
            else if (IsAction(action, "Check", "TriggerCheck"))
            {
                // 語法: WoodenMan(Check, [機率0~1])
                float prob = GetParameterAsFloat(1, -1f);
                WoodenManGameManager.Instance.TriggerGhostCheck(prob);
            }
            else if (IsAction(action, "Danger", "AddDanger"))
            {
                // 語法: WoodenMan(Danger, 點數)
                int amount = GetParameterAsInt(1, 0);
                WoodenManGameManager.Instance.AddDangerPoints(amount);
            }

            // --- 4. 重設 ---
            else if (IsAction(action, "Reset"))
            {
                // 語法: WoodenMan(Reset)
                WoodenManGameManager.Instance.ResetAllGhosts();
            }
            else
            {
                Debug.LogWarning($"[WoodenManCommand] 未知的動作類型: {action}");
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