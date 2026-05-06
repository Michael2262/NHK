using UnityEngine;
using PixelCrushers.DialogueSystem;
using System;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：ChangeScene(場景名稱, [轉場色Phase], [是否更新Scenario])
    /// 
    /// 參數說明：
    /// - 場景名稱 (必填)：目標場景的名稱
    /// - 轉場色Phase (選填，預設 -1)：
    ///     -2 = 直接轉場，不執行任何淡入淡出
    ///     -1 = 依照目前 Phase 自動決定顏色
    ///      0 = 白天 (白色)
    ///      1 = 黃昏
    ///      2 = 晚上 (黑色)
    ///      3 = 深夜 (黑色)
    /// - 是否更新Scenario (選填，預設 true)：
    ///     true  = 會呼叫 ScenarioManager.ChangeLocation() 更新邏輯地點
    ///     false = 僅切換場景，不更新邏輯地點
    /// 
    /// 【v3 改動】
    /// 現在會等待 SceneController 完成整個流程（含 ReadyHandlers）後才呼叫 Stop()。
    /// 確保後續的 Sequencer Command 不會在場景尚未完全就緒時執行。
    /// 
    /// 使用範例：
    /// 1. ChangeScene(Stage2)
    ///    → 切換到 Stage2，使用當前 Phase 顏色，更新邏輯地點
    /// 
    /// 2. ChangeScene(Stage2, 2)
    ///    → 切換到 Stage2，強制使用晚上(黑色)轉場，更新邏輯地點
    /// 
    /// 3. ChangeScene(Stage2, -1, false)
    ///    → 切換到 Stage2，使用當前 Phase 顏色，不更新邏輯地點
    /// 
    /// 4. ChangeScene(Stage2, -2)
    ///    → 切換到 Stage2，直接轉場無淡入淡出，更新邏輯地點
    /// 
    /// 5. ChangeScene(Stage2, -2, false)
    ///    → 切換到 Stage2，直接轉場無淡入淡出，不更新邏輯地點
    /// </summary>
    public class SequencerCommandChangeScene : SequencerCommand
    {
        private const int IMMEDIATE_TRANSITION = -2;

        public void Awake()
        {
            // ============================================================
            // 獲取參數
            // ============================================================

            string targetScene = GetParameter(0);
            int fadePhaseIndex = GetParameterAsInt(1, -1);
            bool updateScenario = GetParameterAsBool(2, true);
            string entryID = "DefaultEntry";

            // ============================================================
            // 安全檢查
            // ============================================================

            if (GameDataManager.Instance == null)
            {
                Debug.LogError("[SequencerCommandChangeScene] 切換失敗：找不到 GameDataManager！");
                Stop();
                return;
            }

            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogWarning("[SequencerCommandChangeScene] 場景名稱為空，請檢查對話指令。");
                Stop();
                return;
            }

            if (SceneController.Instance == null)
            {
                Debug.LogError("[SequencerCommandChangeScene] 找不到 SceneController 實例！");
                Stop();
                return;
            }

            // ============================================================
            // 執行邏輯
            // ============================================================

            // 1. 更新 Scenario 位置狀態
            if (updateScenario && ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.ChangeLocation(targetScene);
            }

            // 2. 登記入口 ID
            GameDataManager.Instance.SetNextSceneEntry(entryID);

            // 3. 執行場景切換（使用帶回呼的新 API，等場景完全就緒後才 Stop）
            if (fadePhaseIndex == IMMEDIATE_TRANSITION)
            {
                Debug.Log($"[SequencerCommandChangeScene] 直接轉場: {targetScene}, 更新Scenario: {updateScenario}");
                bool fullyReady = false;
                SceneController.ChangeSceneImmediate(targetScene, () => { fullyReady = true; });
                StartCoroutine(WaitAndStop(() => fullyReady, 10f));
            }
            else
            {
                Debug.Log($"[SequencerCommandChangeScene] 目標場景: {targetScene}, 轉場Phase: {fadePhaseIndex}, 更新Scenario: {updateScenario}");
                bool fullyReady = false;
                SceneController.ChangeScene(targetScene, fadePhaseIndex, () => { fullyReady = true; });
                StartCoroutine(WaitAndStop(() => fullyReady, 10f));
            }
        }

        private System.Collections.IEnumerator WaitAndStop(Func<bool> condition, float timeout)
        {
            float elapsed = 0f;
            while (!condition() && elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!condition())
            {
                Debug.LogWarning("[SequencerCommandChangeScene] 等待場景就緒超時！");
            }

            Stop();
        }
    }
}