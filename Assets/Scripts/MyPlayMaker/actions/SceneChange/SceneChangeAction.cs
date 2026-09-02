using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace MyGame.Actions
{
    /// <summary>
    /// PlayMaker Action：登記下一個場景的入口資料並透過 SceneController 切換場景。
    /// 功能與 SceneChangeButton.SwitchScene() 一致。
    /// </summary>
    [ActionCategory("Scene")]
    [Tooltip("更新 Scenario 地點、登記入口 ID，並透過 SceneController 正規切換場景。")]
    public class SceneChangeAction : FsmStateAction
    {
        [Header("場景設定")]
        [RequiredField]
        [Tooltip("要切換到的目標場景名稱")]
        public FsmString targetScene;

        [Header("是否更新 Scenario?")]
        [Tooltip("是否將邏輯地點寫入 ScenarioManager。非遊戲內地點移動時可關閉，預設開啟。")]
        public FsmBool updateScenario;

        [Header("入口識別")]
        [Tooltip("進入下個場景時使用的入口 ID；下個場景的 Initializer 會依此執行不同邏輯。")]
        public FsmString entryID;

        public override void Reset()
        {
            targetScene = "Stage2";
            updateScenario = true;
            entryID = "DefaultEntry";
        }

        public override void OnEnter()
        {
            string sceneName = targetScene.Value;

            // 1. 安全檢查：確保 GameDataManager 存在
            if (GameDataManager.Instance == null)
            {
                Debug.LogError("[SceneChangeAction] 切換失敗：場景中找不到 GameDataManager 實例！");
                Finish();
                return;
            }

            // 2. 更新 Scenario 位置狀態
            if (ScenarioManager.Instance != null &&
                !string.IsNullOrEmpty(sceneName) &&
                updateScenario.Value)
            {
                ScenarioManager.Instance.ChangeLocation(sceneName);
            }

            // 3. 登記入口 ID
            GameDataManager.Instance.SetNextSceneEntry(entryID.Value);
            Debug.Log($"[SceneChangeAction] 已登記入口 ID: {entryID.Value}，準備前往場景: {sceneName}");

            // 4. 透過專案的正規轉場管線切換場景
            SceneController.ChangeScene(sceneName);
            Finish();
        }
    }
}
