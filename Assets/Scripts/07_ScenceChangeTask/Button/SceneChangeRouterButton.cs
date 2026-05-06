using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// SceneChangeRouterButton
/// 結合了 SceneChangeButton 的跳轉邏輯與 ProgressSequenceRouter 的條件分支功能。
/// 每個分支可同時設定「Flag 條件」與「Scenario 條件（女主角 / Risk 即時狀態）」，
/// 兩者皆滿足時該分支才算通過。
/// </summary>
public class SceneChangeRouterButton : MonoBehaviour
{
    // ============================================================
    // Scenario 條件的列舉與定義
    // ============================================================

    public enum TargetType { Heroine, Risk }

    public enum LocationCheckMode
    {
        DontCheck,          // 不檢查地點
        IsAtLocation,       // 角色「在」指定地點
        IsNotAtLocation     // 角色「不在」指定地點
    }

    [Serializable]
    public class ScenarioCondition
    {
        [Tooltip("僅供 Inspector 辨識用")]
        public string label;

        public TargetType targetType = TargetType.Heroine;

        [Tooltip("填寫 heroineID 或 agentID（例如 sister、mother）")]
        public string characterID;

        [Header("地點檢查")]
        public LocationCheckMode locationCheck = LocationCheckMode.DontCheck;
        [Tooltip("當 locationCheck 不是 DontCheck 時，填寫要比對的地點 ID")]
        public string locationID;

        [Header("Action / Activity 檢查（多選任一符合即可）")]
        [Tooltip("留空 = 不檢查 Action。填寫多筆時，符合其中任何一筆即算通過")]
        public List<string> matchActions = new List<string>();
    }

    // ============================================================
    // 分支定義
    // ============================================================

    [Serializable]
    public class SceneBranch
    {
        public string label; // 僅供 Inspector 辨識

        [Header("--- Flag 條件 (可選) ---")]
        [Tooltip("留空 = 不檢查 Flag")]
        public ProgressFlagDefinition flagDefinition;
        [Tooltip("預設打勾為「Flag開啟時成立」；取消打勾則為「Flag關閉時成立」")]
        public bool triggerIfActive = true;

        [Header("--- Scenario 條件 (可選，AND 邏輯) ---")]
        [Tooltip("可設定多筆 Scenario 條件，全部滿足此分支才算通過。留空 = 不檢查 Scenario。")]
        public List<ScenarioCondition> scenarioConditions = new List<ScenarioCondition>();

        [Header("--- 跳轉設定 ---")]
        public string targetScene;
        public string entryID = "DefaultEntry";

        [Tooltip("是否要將邏輯地點寫入 ScenarioManager?")]
        public bool updateScenario = true;
    }

    // ============================================================
    // Inspector 設定
    // ============================================================

    [Header("分支路由 (依序檢查)")]
    public List<SceneBranch> branches = new List<SceneBranch>();

    [Header("預設路由 (當上述條件皆不符合時)")]
    [SerializeField] private string defaultTargetScene = "MainMap";
    [SerializeField] private string defaultEntryID = "DefaultEntry";
    [SerializeField] private bool defaultUpdateScenario = true;

    // ============================================================
    // 公開方法
    // ============================================================

    /// <summary>
    /// 綁定在 UI Button 的 OnClick()
    /// </summary>
    public void TriggerRouter()
    {
        // 1. 安全檢查：確保核心系統存在
        if (GameStatusService.Instance == null || GameDataManager.Instance == null)
        {
            Debug.LogError($"[SceneChangeRouterButton] 找不到 GameStatusService 或 GameDataManager！");
            return;
        }

        // 2. 依序檢查分支條件
        foreach (var branch in branches)
        {
            if (IsBranchSatisfied(branch))
            {
                PerformTransition(branch.targetScene, branch.entryID, branch.updateScenario);
                return;
            }
        }

        // 3. 若無符合條件，執行預設跳轉
        PerformTransition(defaultTargetScene, defaultEntryID, defaultUpdateScenario);
    }

    // ============================================================
    // 分支判斷：Flag 條件 AND Scenario 條件都必須通過
    // ============================================================

    private bool IsBranchSatisfied(SceneBranch branch)
    {
        // --- Flag 條件 ---
        if (branch.flagDefinition != null)
        {
            bool isActive = GameStatusService.Instance.ProgressFlags.Contains(branch.flagDefinition.FlagID);
            if (branch.triggerIfActive != isActive)
                return false;
        }

        // --- Scenario 條件（全部 AND） ---
        if (branch.scenarioConditions != null && branch.scenarioConditions.Count > 0)
        {
            var scenario = GameStatusService.Instance.Scenario;
            if (scenario == null) return false;

            foreach (var cond in branch.scenarioConditions)
            {
                if (!EvaluateScenarioCondition(cond, scenario))
                    return false;
            }
        }

        // Flag 與 Scenario 都沒設定的分支 → 視為無條件通過
        // （如果你不希望這樣，可以加上額外檢查）
        return true;
    }

    // ============================================================
    // Scenario 條件評估
    // ============================================================

    private bool EvaluateScenarioCondition(ScenarioCondition cond, CurrentScenarioModel scenario)
    {
        if (string.IsNullOrEmpty(cond.characterID)) return false;

        if (cond.targetType == TargetType.Heroine)
            return EvaluateHeroine(cond, scenario);
        else
            return EvaluateRisk(cond, scenario);
    }

    private bool EvaluateHeroine(ScenarioCondition cond, CurrentScenarioModel scenario)
    {
        string foundLocID = null;
        string foundActivity = null;

        foreach (var kvp in scenario.AllLocationStates)
        {
            var match = kvp.Value.Heroines.Find(
                h => h.HeroineID.Equals(cond.characterID, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                foundLocID = kvp.Key;
                foundActivity = match.Activity;
                break;
            }
        }

        if (!CheckLocation(cond, foundLocID)) return false;
        if (!CheckAction(cond, foundActivity)) return false;
        return true;
    }

    private bool EvaluateRisk(ScenarioCondition cond, CurrentScenarioModel scenario)
    {
        string foundLocID = null;
        string foundActionID = null;

        foreach (var kvp in scenario.AllLocationStates)
        {
            var match = kvp.Value.Risks.Find(r =>
                !string.IsNullOrEmpty(r.inspectionTypeID) &&
                r.inspectionTypeID.IndexOf(cond.characterID, StringComparison.OrdinalIgnoreCase) >= 0);

            if (match != null)
            {
                foundLocID = kvp.Key;
                foundActionID = match.inspectionTypeID;
                break;
            }
        }

        if (!CheckLocation(cond, foundLocID)) return false;
        if (!CheckAction(cond, foundActionID)) return false;
        return true;
    }

    // ============================================================
    // 共用檢查
    // ============================================================

    private bool CheckLocation(ScenarioCondition cond, string foundLocID)
    {
        switch (cond.locationCheck)
        {
            case LocationCheckMode.DontCheck:
                return foundLocID != null;

            case LocationCheckMode.IsAtLocation:
                return foundLocID != null &&
                       foundLocID.Equals(cond.locationID, StringComparison.OrdinalIgnoreCase);

            case LocationCheckMode.IsNotAtLocation:
                return foundLocID == null ||
                       !foundLocID.Equals(cond.locationID, StringComparison.OrdinalIgnoreCase);

            default:
                return true;
        }
    }

    private bool CheckAction(ScenarioCondition cond, string currentAction)
    {
        if (cond.matchActions == null || cond.matchActions.Count == 0)
            return true;

        if (string.IsNullOrEmpty(currentAction))
            return false;

        return cond.matchActions.Any(
            a => currentAction.Equals(a, StringComparison.OrdinalIgnoreCase));
    }

    // ============================================================
    // 跳轉執行（與原版完全一致）
    // ============================================================

    private void PerformTransition(string sceneName, string entryID, bool shouldUpdateScenario)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        // A. 更新 Scenario 位置 (如果設定為 true)
        if (ScenarioManager.Instance != null && shouldUpdateScenario)
        {
            ScenarioManager.Instance.ChangeLocation(sceneName);
        }

        // B. 登記入口 ID
        GameDataManager.Instance.SetNextSceneEntry(entryID);

        Debug.Log($"[Router] 準備前往: {sceneName}, 入口: {entryID}, 更新 Scenario: {shouldUpdateScenario}");

        // C. 呼叫 SceneController 執行切換
        SceneController.ChangeScene(sceneName);
    }
}