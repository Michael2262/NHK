using UnityEngine;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using Tooltip = UnityEngine.TooltipAttribute;

public class MinigameResultHandler : MonoBehaviour
{
    [Header("表單控制")]
    public PlayMakerFSM resultUIFSM;

    [Header("計分設定")]
    [Tooltip("若為 null 則只使用基礎得分，不計算額外加分與乘倍")]
    public MinigameEndReasonConfig endReasonConfig;

    [Header("Slider 演出元件（依插槽順序，最多3個）")]
    public List<LewdnessSliderPerformance> sliderPerformances = new List<LewdnessSliderPerformance>();

    [Header("除錯選項")]
    [Tooltip("啟用詳細的除錯訊息")]
    public bool enableDebugLog = true;

    /// <summary>
    /// 每當一位女主角結束，即時更新數據並通知 FSM
    /// </summary>
    public void ReceiveSingleReport(FsmReportData report, MinigameContext context)
    {
        if (report == null || context?.ActiveHeroines == null ||
            report.FsmIndex < 0 || report.FsmIndex >= context.ActiveHeroines.Count)
        {
            Debug.LogWarning("[MinigameResultHandler] 報告或女主角索引無效。", this);
            return;
        }
        Debug.LogWarning("[MinigameResultHandler] 舊等級／經驗／情緒卡結算已退役；本次只保留得分報告與結束通知，不修改女主角數值。", this);
        if (resultUIFSM == null) return;
        int i = report.FsmIndex;
        SetFsmVar($"fsm_HeroineID_{i}", report.HeroineID);
        SetFsmVar($"fsm_HeroineName_{i}", report.HeroineName);
        SetFsmVar($"fsm_Score_{i}", report.Score);
        SetFsmVar($"fsm_GameScore_{i}", report.GameScore);
        SetFsmVar($"fsm_Reason_{i}", report.Reason);
        SetFsmVar($"fsm_OrgasmTimes_{i}", report.LocalOrgasmTimes);
        // 仍在等舊演出結束事件的 FSM 必須於 Editor 移除該步驟。
        resultUIFSM.SendEvent($"HEROINE_{i}_FINISHED");
    }

    public void NotifyAllHeroinesFinished()
    {
        if (enableDebugLog)
            Debug.Log("<color=magenta>[ResultHandler] 全體女主角結算完畢</color>");

        if (resultUIFSM != null)
        {
            var allFinishVar = resultUIFSM.FsmVariables.FindFsmBool("AllFinish");
            if (allFinishVar != null)
            {
                allFinishVar.Value = true;
                if (enableDebugLog) Debug.Log("[ResultHandler] 設定 AllFinish = true");
            }
            resultUIFSM.SendEvent("ALL_HEROINES_FINISHED");
        }
    }

    // ─────────────────────────────────────────────────────────
    // SetFsmVar 多載
    // ─────────────────────────────────────────────────────────

    private void SetFsmVar(string varName, string value)
    {
        var v = resultUIFSM.FsmVariables.FindFsmString(varName);
        if (v != null) v.Value = value;
        else if (enableDebugLog) Debug.LogWarning($"[ResultHandler] 找不到 FSM 變數: {varName} (string)");
    }

    private void SetFsmVar(string varName, int value)
    {
        var v = resultUIFSM.FsmVariables.FindFsmInt(varName);
        if (v != null) v.Value = value;
        else if (enableDebugLog) Debug.LogWarning($"[ResultHandler] 找不到 FSM 變數: {varName} (int)");
    }

    private void SetFsmVar(string varName, MinigameEndReason value)
    {
        var v = resultUIFSM.FsmVariables.FindFsmEnum(varName);
        if (v != null) v.Value = value;
        else if (enableDebugLog) Debug.LogWarning($"[ResultHandler] 找不到 FSM 變數: {varName} (enum)");
    }

    private void SetFsmVar(string varName, System.Enum value)
    {
        var v = resultUIFSM.FsmVariables.FindFsmEnum(varName);
        if (v != null) v.Value = value;
        else if (enableDebugLog) Debug.LogWarning($"[ResultHandler] 找不到 FSM 變數: {varName} (enum)");
    }
}