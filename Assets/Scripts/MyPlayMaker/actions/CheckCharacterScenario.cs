using UnityEngine;
using HutongGames.PlayMaker;
using System;
using System.Collections.Generic;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

/// <summary>
/// Playmaker Action：檢查角色（女主角或 Risk）的即時 Scenario 狀態。
/// 
/// 可檢查的條件：
/// - 角色是否存在於指定地點（或不在指定地點）
/// - 角色是否正在執行指定的動作（支援多個動作，任一符合即通過）
/// 
/// 條件邏輯參考自 ImageChangeByScenario。
/// 
/// 【使用方式】
/// 1. 設定 targetType（Heroine 或 Risk）
/// 2. 填入 characterID
/// 3. 設定地點檢查模式與地點 ID（可選）
/// 4. 設定要匹配的動作列表（可選，留空 = 不檢查動作）
/// 5. 設定符合 / 不符合時的事件
/// </summary>
[ActionCategory("Scenario")]
[Tooltip("檢查角色是否在指定地點、是否正在進行指定動作，根據結果發送不同事件。")]
public class CheckCharacterScenario : FsmStateAction
{
    public enum TargetType { Heroine, Risk }

    public enum LocationCheckMode
    {
        DontCheck,       // 不檢查地點（只要角色存在即可）
        IsAtLocation,    // 角色「在」指定地點
        IsNotAtLocation  // 角色「不在」指定地點
    }

    [Header("目標設定")]
    [Tooltip("檢查對象類型")]
    public TargetType targetType = TargetType.Heroine;

    [RequiredField]
    [Tooltip("角色 ID（例如 sister、mother）")]
    public FsmString characterID;

    [Header("地點條件（可選）")]
    [Tooltip("地點檢查模式。DontCheck = 不限地點（只要角色存在即可）")]
    public LocationCheckMode locationCheck = LocationCheckMode.DontCheck;

    [Tooltip("要比對的地點 ID（僅 IsAtLocation / IsNotAtLocation 時需要填）")]
    public FsmString locationID;

    [Header("動作條件（可選，任一符合即通過）")]
    [Tooltip("要匹配的動作名稱 1。留空 = 不檢查動作。")]
    public FsmString matchAction1;

    [Tooltip("要匹配的動作名稱 2（可選）")]
    public FsmString matchAction2;

    [Tooltip("要匹配的動作名稱 3（可選）")]
    public FsmString matchAction3;

    [Header("每次 Check 完是否要 everyFrame")]
    [Tooltip("是否每幀持續檢查（預設 false = 只檢查一次）")]
    public bool everyFrame = false;

    [Header("結果事件")]
    [Tooltip("條件符合時發送的事件")]
    public FsmEvent trueEvent;

    [Tooltip("條件不符合時發送的事件")]
    public FsmEvent falseEvent;

    [Header("結果儲存（可選）")]
    [Tooltip("將結果存入 Bool 變數")]
    [UIHint(UIHint.Variable)]
    public FsmBool storeResult;

    [Tooltip("將找到的地點 ID 存入變數")]
    [UIHint(UIHint.Variable)]
    public FsmString storeFoundLocation;

    [Tooltip("將找到的動作名稱存入變數")]
    [UIHint(UIHint.Variable)]
    public FsmString storeFoundAction;

    public override void Reset()
    {
        targetType = TargetType.Heroine;
        characterID = null;
        locationCheck = LocationCheckMode.DontCheck;
        locationID = null;
        matchAction1 = null;
        matchAction2 = null;
        matchAction3 = null;
        everyFrame = false;
        trueEvent = null;
        falseEvent = null;
        storeResult = null;
        storeFoundLocation = null;
        storeFoundAction = null;
    }

    public override void OnEnter()
    {
        DoCheck();

        if (!everyFrame)
            Finish();
    }

    public override void OnUpdate()
    {
        DoCheck();
    }

    private void DoCheck()
    {
        var scenario = GameStatusService.Instance?.Scenario;

        if (scenario == null || string.IsNullOrEmpty(characterID.Value))
        {
            SetResult(false, null, null);
            return;
        }

        // 在所有地點中搜尋目標角色
        string foundLocID = null;
        string foundAction = null;

        if (targetType == TargetType.Heroine)
            FindHeroine(scenario, out foundLocID, out foundAction);
        else
            FindRisk(scenario, out foundLocID, out foundAction);

        // 評估條件
        bool passed = true;

        // 地點條件
        if (!EvaluateLocation(foundLocID))
            passed = false;

        // 動作條件
        if (passed && !EvaluateAction(foundAction))
            passed = false;

        SetResult(passed, foundLocID, foundAction);
    }

    // ============================================================
    // 角色搜尋
    // ============================================================

    private void FindHeroine(CurrentScenarioModel scenario, out string foundLocID, out string foundAction)
    {
        foundLocID = null;
        foundAction = null;

        foreach (var kvp in scenario.AllLocationStates)
        {
            var match = kvp.Value.Heroines.Find(
                h => h.HeroineID.Equals(characterID.Value, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                foundLocID = kvp.Key;
                foundAction = match.Activity;
                return;
            }
        }
    }

    private void FindRisk(CurrentScenarioModel scenario, out string foundLocID, out string foundAction)
    {
        foundLocID = null;
        foundAction = null;

        foreach (var kvp in scenario.AllLocationStates)
        {
            var match = kvp.Value.Risks.Find(r =>
                !string.IsNullOrEmpty(r.inspectionTypeID) &&
                r.inspectionTypeID.IndexOf(characterID.Value, StringComparison.OrdinalIgnoreCase) >= 0);

            if (match != null)
            {
                foundLocID = kvp.Key;
                foundAction = match.inspectionTypeID;
                return;
            }
        }
    }

    // ============================================================
    // 條件評估
    // ============================================================

    private bool EvaluateLocation(string foundLocID)
    {
        switch (locationCheck)
        {
            case LocationCheckMode.DontCheck:
                // 不檢查地點，但角色必須存在（有被找到）
                return foundLocID != null;

            case LocationCheckMode.IsAtLocation:
                return foundLocID != null &&
                       foundLocID.Equals(locationID.Value, StringComparison.OrdinalIgnoreCase);

            case LocationCheckMode.IsNotAtLocation:
                return foundLocID == null ||
                       !foundLocID.Equals(locationID.Value, StringComparison.OrdinalIgnoreCase);

            default:
                return true;
        }
    }

    private bool EvaluateAction(string currentAction)
    {
        // 三個欄位都沒填 = 不檢查動作 = 通過
        bool hasAny = !string.IsNullOrEmpty(matchAction1.Value) ||
                      !string.IsNullOrEmpty(matchAction2.Value) ||
                      !string.IsNullOrEmpty(matchAction3.Value);

        if (!hasAny) return true;

        if (string.IsNullOrEmpty(currentAction)) return false;

        if (!string.IsNullOrEmpty(matchAction1.Value) &&
            currentAction.Equals(matchAction1.Value, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrEmpty(matchAction2.Value) &&
            currentAction.Equals(matchAction2.Value, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrEmpty(matchAction3.Value) &&
            currentAction.Equals(matchAction3.Value, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    // ============================================================
    // 結果處理
    // ============================================================

    private void SetResult(bool passed, string foundLocID, string foundAction)
    {
        // 儲存結果
        if (!storeResult.IsNone)
            storeResult.Value = passed;

        if (!storeFoundLocation.IsNone)
            storeFoundLocation.Value = foundLocID ?? "";

        if (!storeFoundAction.IsNone)
            storeFoundAction.Value = foundAction ?? "";

        // 發送事件
        Fsm.Event(passed ? trueEvent : falseEvent);
    }
}