using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// 依據 CurrentScenarioModel 中「女主角」或「Risk」的即時狀態進行路由。
/// 可判斷：角色ID、是否在某地點、Action名稱（多選任一符合）。
/// 用法與 ProgressSequenceRouter 相同：綁定在 Button 的 OnClick() 呼叫 Trigger()。
/// </summary>
[AddComponentMenu("Game/UI/Scenario Sequence Router")]
public class ScenarioSequenceRouter : MonoBehaviour
{
    // ============================================================
    // 條件定義
    // ============================================================

    public enum TargetType
    {
        Heroine,
        Risk
    }

    public enum LocationCheckMode
    {
        DontCheck,          // 不檢查地點（僅靠角色ID / Action 判斷）
        IsAtLocation,       // 角色「在」指定地點
        IsNotAtLocation     // 角色「不在」指定地點
    }

    [Serializable]
    public class ScenarioConditionBranch
    {
        [Header("說明")]
        [Tooltip("僅供 Inspector 辨識用")]
        public string label;

        [Header("目標類型")]
        public TargetType targetType = TargetType.Heroine;

        [Header("角色 ID")]
        [Tooltip("填寫 heroineID 或 agentID（例如 sister、mother）")]
        public string characterID;

        [Header("地點檢查")]
        public LocationCheckMode locationCheck = LocationCheckMode.DontCheck;

        [Tooltip("當 locationCheck 不是 DontCheck 時，填寫要比對的地點 ID")]
        public string locationID;

        [Header("Action / Activity 檢查（多選任一符合即可）")]
        [Tooltip("留空 = 不檢查 Action。填寫多筆時，角色的 Activity/inspectionTypeID 符合其中任何一筆即算通過")]
        public List<string> matchActions = new List<string>();

        [Header("觸發")]
        public UnityEvent onTriggered;
    }

    // ============================================================
    // Inspector 設定
    // ============================================================

    [Header("分支清單 (依序檢查)")]
    public List<ScenarioConditionBranch> branches = new List<ScenarioConditionBranch>();

    [Header("預設事件 (皆不符合時觸發)")]
    public UnityEvent onDefault;

    // ============================================================
    // 公開方法
    // ============================================================

    /// <summary>
    /// 綁定在 Unity Button 的 OnClick() 事件
    /// </summary>
    public void Trigger()
    {
        foreach (var branch in branches)
        {
            if (EvaluateBranch(branch))
            {
                branch.onTriggered.Invoke();
                return;
            }
        }

        // 全部都不符合，執行預設事件
        onDefault.Invoke();
    }

    // ============================================================
    // 核心判斷
    // ============================================================

    private bool EvaluateBranch(ScenarioConditionBranch branch)
    {
        if (string.IsNullOrEmpty(branch.characterID)) return false;

        var scenario = GameStatusService.Instance?.Scenario;
        if (scenario == null) return false;

        if (branch.targetType == TargetType.Heroine)
            return EvaluateHeroine(branch, scenario);
        else
            return EvaluateRisk(branch, scenario);
    }

    // ---------- 女主角 ----------

    private bool EvaluateHeroine(ScenarioConditionBranch branch, CurrentScenarioModel scenario)
    {
        // 在全地圖中搜尋該角色
        // foundLocID: 她目前所在的地點, foundActivity: 她目前的 Activity
        string foundLocID = null;
        string foundActivity = null;

        foreach (var kvp in scenario.AllLocationStates)
        {
            var match = kvp.Value.Heroines.Find(
                h => h.HeroineID.Equals(branch.characterID, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                foundLocID = kvp.Key;
                foundActivity = match.Activity;
                break;
            }
        }

        // ---- 地點檢查 ----
        if (!CheckLocation(branch, foundLocID))
            return false;

        // ---- Action 檢查 ----
        if (!CheckAction(branch, foundActivity))
            return false;

        return true;
    }

    // ---------- Risk ----------

    private bool EvaluateRisk(ScenarioConditionBranch branch, CurrentScenarioModel scenario)
    {
        // Risk 的比對方式：inspectionTypeID 裡包含 agentID，或直接用 inspectionTypeID 比對
        string foundLocID = null;
        string foundActionID = null;

        foreach (var kvp in scenario.AllLocationStates)
        {
            // 嘗試找到符合的 RiskAction
            var match = kvp.Value.Risks.Find(r =>
                !string.IsNullOrEmpty(r.inspectionTypeID) &&
                r.inspectionTypeID.IndexOf(branch.characterID, StringComparison.OrdinalIgnoreCase) >= 0);

            if (match != null)
            {
                foundLocID = kvp.Key;
                foundActionID = match.inspectionTypeID;
                break;
            }
        }

        // ---- 地點檢查 ----
        if (!CheckLocation(branch, foundLocID))
            return false;

        // ---- Action 檢查 ----
        if (!CheckAction(branch, foundActionID))
            return false;

        return true;
    }

    // ============================================================
    // 共用檢查方法
    // ============================================================

    /// <summary>
    /// 地點檢查。foundLocID 為 null 代表角色不在任何地點上。
    /// </summary>
    private bool CheckLocation(ScenarioConditionBranch branch, string foundLocID)
    {
        switch (branch.locationCheck)
        {
            case LocationCheckMode.DontCheck:
                // 不檢查地點，但角色至少必須存在於地圖上
                // （如果你希望「角色不在地圖上」也算通過，可以移除這行）
                return foundLocID != null;

            case LocationCheckMode.IsAtLocation:
                return foundLocID != null &&
                       foundLocID.Equals(branch.locationID, StringComparison.OrdinalIgnoreCase);

            case LocationCheckMode.IsNotAtLocation:
                // 角色不存在 或 在別的地點 → 都算「不在該地點」
                return foundLocID == null ||
                       !foundLocID.Equals(branch.locationID, StringComparison.OrdinalIgnoreCase);

            default:
                return true;
        }
    }

    /// <summary>
    /// Action / Activity 檢查。matchActions 為空 = 不檢查（直接通過）。
    /// 若有填寫，則 currentAction 符合其中任一筆即通過。
    /// </summary>
    private bool CheckAction(ScenarioConditionBranch branch, string currentAction)
    {
        // 沒有設定任何 Action 條件 → 直接通過
        if (branch.matchActions == null || branch.matchActions.Count == 0)
            return true;

        // 有設定但角色目前沒有 Action → 不通過
        if (string.IsNullOrEmpty(currentAction))
            return false;

        // 任一符合即可
        return branch.matchActions.Any(
            a => currentAction.Equals(a, StringComparison.OrdinalIgnoreCase));
    }
}
