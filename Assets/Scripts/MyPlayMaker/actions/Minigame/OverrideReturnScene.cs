using UnityEngine;
using HutongGames.PlayMaker;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Playmaker Action：覆寫小遊戲結束後的回歸場景。
/// 
/// 【用途】
/// 在小遊戲的結算 FSM 中，根據不同結果（例如被抓到、特殊結局等），
/// 將回歸場景改為指定地點，而非原本進入小遊戲時記錄的場景。
/// 
/// 【使用方式】
/// 1. 在 FSM 的任意 State 中加入此 Action
/// 2. 設定 overrideSceneName（目標場景名稱）
/// 3. 可選：設定 updateScenario 來同步更新邏輯地點
/// 4. 此 Action 必須在 FinalizeAndExit / ReportGameFinished 之前執行
/// 
/// 【運作原理】
/// 呼叫 MinigameManager.DebugSetOriginScene() 覆寫 _originSceneName，
/// 這樣 HandleMinigameFinished 在步驟 5 返回場景時就會用新的地點。
/// </summary>
[ActionCategory("Minigame")]
[Tooltip("覆寫小遊戲結束後的回歸場景。必須在遊戲結束回報之前執行。")]
public class OverrideReturnScene : FsmStateAction
{
    [RequiredField]
    [Tooltip("要覆寫的目標場景名稱")]
    public FsmString overrideSceneName;

    [Tooltip("是否同步更新 ScenarioManager 的邏輯地點（預設 true）")]
    public FsmBool updateScenario;

    [Tooltip("要登記的入口 ID（預設 DefaultEntry）")]
    public FsmString entryID;

    public override void Reset()
    {
        overrideSceneName = null;
        updateScenario = false;  // 正常回歸流程不更新邏輯地點，預設關閉
        entryID = "";            // 留空 = 不覆寫入口 ID
    }

    public override void OnEnter()
    {
        string sceneName = overrideSceneName.Value;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[OverrideReturnScene] 場景名稱為空，跳過覆寫。");
            Finish();
            return;
        }

        // 1. 覆寫回歸場景
        if (MinigameManager.Instance != null)
        {
            MinigameManager.Instance.SetReturnScene(sceneName);
            Debug.Log($"[OverrideReturnScene] 回歸場景已覆寫為: {sceneName}");
        }
        else
        {
            Debug.LogError("[OverrideReturnScene] 找不到 MinigameManager！");
        }

        // 2. 同步更新邏輯地點
        if (updateScenario.Value && ScenarioManager.Instance != null)
        {
            ScenarioManager.Instance.ChangeLocation(sceneName);
            Debug.Log($"[OverrideReturnScene] 邏輯地點已更新為: {sceneName}");
        }

        // 3. 登記入口 ID（僅在有填寫時才覆寫）
        if (GameDataManager.Instance != null && !string.IsNullOrEmpty(entryID.Value))
        {
            GameDataManager.Instance.SetNextSceneEntry(entryID.Value);
            Debug.Log($"[OverrideReturnScene] 入口 ID 已登記: {entryID.Value}");
        }

        Finish();
    }
}