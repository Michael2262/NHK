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
        if (enableDebugLog)
            Debug.Log($"<color=yellow>[ResultHandler] 收到報告: Index={report.FsmIndex}, Name={report.HeroineName}</color>");

        if (context == null)
        {
            Debug.LogError("[ResultHandler] context 為 null！無法處理報告。");
            return;
        }

        if (report.FsmIndex < 0 || report.FsmIndex >= context.ActiveHeroines.Count)
        {
            Debug.LogError($"[ResultHandler] FsmIndex ({report.FsmIndex}) 超出範圍！女主角數量: {context.ActiveHeroines.Count}");
            return;
        }

        // 1. 取得該女主角 Model
        HeroineStatusModel heroine = context.ActiveHeroines[report.FsmIndex];

        // 2. 同步主角可疑度
        //暫時先移除此功能，因為目前 FSM 會直接增加本源可疑度，等未來有需要再考慮是否改為 FSM 不直接修改，而由這裡統一同步。
        //SyncFinalSuspicion(context);

        // 3. 從 PlayMaker Global 讀取本局全域數值
        int shootTimes = ReadGlobalInt("global_ShootTimes");
        int overShootTimes = ReadGlobalInt("global_OverShootTimes");
        bool dangerScene = ReadGlobalBool("global_DangerScene");
        bool challengeAccepted = ReadGlobalBool("global_ChallengeAccepted");

        // 4. 建立 breakdown（包含所有分項數值）
        LewdnessBreakdown breakdown = BuildBreakdown(
            report, heroine, shootTimes, overShootTimes, dangerScene, challengeAccepted);

        // 5. 寫入後台暫定區
        UpdateOneHeroine(report, heroine, breakdown.TotalAddedExp);

        // 6. 注入基本資料至 ResultUIFSM
        if (resultUIFSM != null)
        {
            int i = report.FsmIndex;

            if (enableDebugLog)
                Debug.Log($"<color=cyan>[ResultHandler] 注入資料至 FSM，插槽 {i}</color>");

            SetFsmVar($"fsm_HeroineID_{i}", report.HeroineID);
            SetFsmVar($"fsm_HeroineName_{i}", report.HeroineName);
            SetFsmVar($"fsm_Score_{i}", report.Score);
            SetFsmVar($"fsm_GameScore_{i}", report.GameScore);
            SetFsmVar($"fsm_Reason_{i}", report.Reason);
            SetFsmVar($"fsm_Excitement_{i}", report.LocalExcitement);
            SetFsmVar($"fsm_ExcitedLv_{i}", report.LocalExcitedLv);
            SetFsmVar($"fsm_OrgasmTimes_{i}", report.LocalOrgasmTimes);
            SetFsmVar($"fsm_PersonalSuspicion_{i}", report.PersonalSuspicion);
            SetFsmVar($"fsm_AffinityLv_{i}", report.LocalAffinityLv);
            SetFsmVar($"fsm_AffinityExp_{i}", report.LocalAffinityExp);
            SetFsmVar($"fsm_Emotion_{i}", report.Emotion);
            SetFsmVar($"fsm_StartLewdLevel_{i}", heroine.LewdnessLevel);
            SetFsmVar($"fsm_StartLewdExp_{i}", heroine.LewdnessExp);
            SetFsmVar($"fsm_AddedLewdExp_{i}", breakdown.TotalAddedExp);
            SetFsmVar($"fsm_LewdExpMax_{i}", GetLewdnessThreshold(heroine.LewdnessLevel));

            string eventName = $"HEROINE_{i}_FINISHED";
            if (enableDebugLog)
                Debug.Log($"<color=green>[ResultHandler] 發送事件: {eventName}</color>");

            resultUIFSM.SendEvent(eventName);
        }
        else
        {
            Debug.LogWarning("<color=red>[ResultHandler] resultUIFSM 為 null！</color>");
        }

        // 7. 啟動對應插槽的 Slider 演出
        StartSliderPerformanceForSlot(report.FsmIndex, breakdown);
    }

    // ─────────────────────────────────────────────────────────
    // Breakdown 建立
    // ─────────────────────────────────────────────────────────

    private LewdnessBreakdown BuildBreakdown(
        FsmReportData report,
        HeroineStatusModel heroine,
        int shootTimes, int overShootTimes,
        bool dangerScene, bool challengeAccepted)
    {
        var bd = new LewdnessBreakdown
        {
            SlotIndex = report.FsmIndex,
            HeroineName = report.HeroineName,
            StartLevel = heroine.LewdnessLevel,
            StartExp = heroine.LewdnessExp,
            ExpThreshold = GetLewdnessThreshold(heroine.LewdnessLevel),
            BaseScore = report.Score,
            GameScore = report.GameScore,
            Reason = report.Reason,
        };

        if (endReasonConfig != null)
        {
            bd.GameScoreConverted = Mathf.RoundToInt(report.GameScore * endReasonConfig.gameScoreToExpRatio);
            bd.ReasonDisplayName = endReasonConfig.GetDisplayName(report.Reason);

            bd.LocalExcitedLv = report.LocalExcitedLv;
            bd.ExcitedLvBonus = report.LocalExcitedLv * endReasonConfig.excitedLvBonus;
            bd.OrgasmTimes = report.LocalOrgasmTimes;
            bd.OrgasmTimesBonus = report.LocalOrgasmTimes * endReasonConfig.orgasmTimesBonus;
            bd.ShootTimes = shootTimes;
            bd.ShootTimesBonus = shootTimes * endReasonConfig.shootTimesBonus;
            bd.OverShootTimes = overShootTimes;
            bd.OverShootTimesBonus = overShootTimes * endReasonConfig.overShootTimesBonus;

            bd.DangerScene = dangerScene;
            bd.ChallengeAccepted = challengeAccepted;
            bd.DangerSceneMultiplier = endReasonConfig.dangerSceneMultiplier;
            bd.ChallengeAcceptedMultiplier = endReasonConfig.challengeAcceptedMultiplier;

            bd.TotalAddedExp = endReasonConfig.CalculateLewdnessExp(
                reason: report.Reason,
                gameScore: report.GameScore,
                excitedLv: report.LocalExcitedLv,
                orgasmTimes: report.LocalOrgasmTimes,
                shootTimes: shootTimes,
                overShootTimes: overShootTimes,
                dangerScene: dangerScene,
                challengeAccepted: challengeAccepted);
        }
        else
        {
            // fallback：只有基礎得分
            bd.GameScoreConverted = 0;
            bd.ReasonDisplayName = report.Reason.ToString();
            bd.TotalAddedExp = report.Score;
        }

        return bd;
    }

    // ─────────────────────────────────────────────────────────
    // Slider 演出啟動
    // ─────────────────────────────────────────────────────────

    private void StartSliderPerformanceForSlot(int slotIndex, LewdnessBreakdown breakdown)
    {
        if (slotIndex < 0 || slotIndex >= sliderPerformances.Count)
        {
            if (enableDebugLog)
                Debug.LogWarning($"[ResultHandler] 找不到插槽 {slotIndex} 的 SliderPerformance。");
            return;
        }

        var perf = sliderPerformances[slotIndex];
        if (perf == null) return;

        // 只存資料，FSM 決定時機後呼叫無參數的 StartSliderPerformance()
        perf.PrepareBreakdown(breakdown, resultUIFSM);
    }

    // ─────────────────────────────────────────────────────────
    // 寫入 Model
    // ─────────────────────────────────────────────────────────

    private void UpdateOneHeroine(FsmReportData report, HeroineStatusModel heroine, int addedExp)
    {
        if (addedExp > 0 && MinigameManager.Instance != null)
            MinigameManager.Instance.AccumulateLewdness(heroine.HeroineID, addedExp);

        heroine.SetExcitement(report.LocalExcitedLv, report.LocalExcitement);
        heroine.SetAffinity(report.LocalAffinityLv, report.LocalAffinityExp);
        heroine.ReplaceEmotionCard(MapReportEmotionToCard(report.Emotion));
        heroine.SetPersonalSuspicion(report.PersonalSuspicion);
        heroine.ApplySuspicionRelief();
    }

    // ─────────────────────────────────────────────────────────
    // PlayMaker Global 讀取
    // ─────────────────────────────────────────────────────────

    private int ReadGlobalInt(string varName)
    {
        var v = PlayMakerGlobals.Instance?.Variables?.FindFsmInt(varName);
        if (v != null) return v.Value;
        if (enableDebugLog) Debug.LogWarning($"[ResultHandler] 找不到 Global Int: {varName}，視為 0。");
        return 0;
    }

    private bool ReadGlobalBool(string varName)
    {
        var v = PlayMakerGlobals.Instance?.Variables?.FindFsmBool(varName);
        if (v != null) return v.Value;
        if (enableDebugLog) Debug.LogWarning($"[ResultHandler] 找不到 Global Bool: {varName}，視為 false。");
        return false;
    }

    // ─────────────────────────────────────────────────────────
    // 其他輔助
    // ─────────────────────────────────────────────────────────

    private int GetLewdnessThreshold(int level)
    {
        if (GameStatusService.Instance == null) return 100;
        var config = GameStatusService.Instance.HeroineConfig;
        if (config != null && config.lewdnessExpTable != null)
        {
            int idx = Mathf.Clamp(level, 0, config.lewdnessExpTable.Count - 1);
            return config.lewdnessExpTable[idx];
        }
        return 100;
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

    private void SyncFinalSuspicion(MinigameContext context)
    {
        if (context == null || context.Protagonist == null)
        {
            if (enableDebugLog) Debug.Log("[ResultHandler] 主角為 null，略過壓力同步。");
            return;
        }
        var globalSusp = PlayMakerGlobals.Instance?.Variables?.FindFsmInt("global_Suspicion");
        if (globalSusp != null) context.Protagonist.SetStress(globalSusp.Value);
    }

    private HeroineEmotionCardType MapReportEmotionToCard(object rawEmotion)
    {
        if (rawEmotion == null) return HeroineEmotionCardType.Angry;

        string emotionName = rawEmotion.ToString();
        switch (emotionName)
        {
            case "Shy":
            case "Embarrassed":
            case "Lewd":
            case "Happy":
                return HeroineEmotionCardType.Shy;

            case "Worried":
            case "Fear":
            case "Sad":
                return HeroineEmotionCardType.Worried;

            case "Maternal":
            case "Care":
            case "Kind":
                return HeroineEmotionCardType.Maternal;

            case "Relaxed":
            case "Idle":
            case "Normal":
            case "Calm":
                return HeroineEmotionCardType.Relaxed;

            case "Disappointed":
            case "Disgust":
                return HeroineEmotionCardType.Disappointed;

            case "Angry":
            default:
                return HeroineEmotionCardType.Angry;
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