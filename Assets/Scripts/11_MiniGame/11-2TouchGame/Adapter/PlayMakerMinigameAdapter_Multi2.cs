using UnityEngine;
using System.Collections.Generic;
using HutongGames.PlayMaker;

#region 數據結構定義
public enum MinigameEndReason
{
    CaughtByGhost,  // 被抓到 (例如被鬼發現)
    ManualExit,     // 玩家中途退出
    GirlLeft,       // 女主角離開
    GirlOvergrown,  // 女主角高潮太多次
    NextMiniGame,   // 進入下一個小遊戲
    OutOfStamina,   // 舊名稱保留：NHK 中視為壓力過高 / 無法繼續
    OutOfStress,    // NHK：壓力過高
    Other           // 其他原因
}

public class FsmReportData
{
    public int FsmIndex;
    public string HeroineID;
    public string HeroineName;
    public int Score;               // 基礎得分（從 config 查表）
    public int LocalExcitement;     // fsm_LocalExcitement (Exp)
    public int LocalExcitedLv;      // fsm_LocalExcitedLv  (Level)
    public int LocalOrgasmTimes;    // fsm_LocalOrgasmTimes
    public int PersonalSuspicion;   // 小遊戲結束時的個人可疑度最終值
    public int GameScore;           // fsm_GameScore (遊戲得分，由 FSM 回報)
    public int LocalAffinityLv;     // fsm_LocalAffinityLv  (親密度等級)
    public int LocalAffinityExp;    // fsm_LocalAffinityExp (親密度經驗)
    public HeroineEmotionType Emotion; // fsm_Emotion (目前情緒)
    public MinigameEndReason Reason;
    public string RawReason;
}
#endregion

public class PlayMakerMinigameAdapter_Multi2 : MonoBehaviour, IMinigameController
{
    [Header("FSM 配置")]
    public PlayMakerFSM[] targetFSMs = new PlayMakerFSM[3];

    [Header("結果處理器")]
    public MinigameResultHandler resultHandler;

    [Header("結束理由 / Score 對應表")]
    public MinigameEndReasonConfig endReasonConfig;

    [Header("時間配置")]
    [UnityEngine.Tooltip("若勾選，小遊戲結束返回時會自動消耗一個時段 (Slot +1)")]
    public bool AdvanceTimeOnFinish => _advanceTimeOnFinish;

    [SerializeField] private bool _advanceTimeOnFinish = false;

    public event System.Action OnGameFinished;

    private MinigameContext _context;
    private List<HeroineStatusModel> _heroinesList;
    private int _activeFSMCount;
    private int _finishedFSMCount;
    private List<FsmReportData> _receivedReports = new List<FsmReportData>();

    public List<string> CurrentLocationRiskIDs { get; private set; } = new List<string>();

    void Start()
    {
        if (MinigameManager.Instance != null)
            MinigameManager.Instance.RegisterMinigameController(this);
    }

    private struct LewdnessSnapshot { public int StartLevel; public int StartExp; }
    private Dictionary<string, LewdnessSnapshot> _lewdnessSnapshots = new Dictionary<string, LewdnessSnapshot>();

    public void Initialize(MinigameContext context)
    {
        this._context = context;
        this._heroinesList = context.ActiveHeroines;
        _receivedReports.Clear();
        _activeFSMCount = 0;

        Debug.Log($"<color=cyan>[MinigameAdapter] 初始化開始。當前互動中的女主角數量: {_heroinesList.Count}</color>");

        CheckAndLogCurrentLocationRisks();
        List<string> combinedRiskIDs = CalculateFinalRiskIDs();
        Debug.Log($"<color=yellow>[MinigameAdapter] 最終傳送給鬼怪系統的 ID 清單: {string.Join(", ", combinedRiskIDs)}</color>");

        SyncStressToGlobals();

        WoodenMan.WoodenManGameManager woodenManager = GameObject.FindAnyObjectByType<WoodenMan.WoodenManGameManager>();
        if (woodenManager != null) { Debug.Log("[MinigameAdapter] 找到 WoodenManGameManager，執行 SetupGhosts..."); woodenManager.SetupGhosts(combinedRiskIDs); }
        else Debug.LogWarning("[MinigameAdapter] 找不到 WoodenManGameManager。");

        InitializeFSMs();

        if (_activeFSMCount == 0) { Debug.LogError("[Adapter] 無法初始化任何 FSM。"); OnGameFinished?.Invoke(); }

        _lewdnessSnapshots.Clear();
        foreach (var h in _heroinesList)
            _lewdnessSnapshots[h.HeroineID] = new LewdnessSnapshot { StartLevel = h.LewdnessLevel, StartExp = h.LewdnessExp };
    }

    public void StartGame()
    {
        if (_activeFSMCount == 0) return;
        foreach (var fsm in targetFSMs)
            if (fsm != null) fsm.enabled = true;
    }

    /// <summary>
    /// FSM 呼叫此方法回報結束。Score 由 endReasonConfig 查表決定，FSM 不再提供。
    /// </summary>
    public void ReportGameFinishedFromFSM(int fsmIndex,
        int fsm_LocalExcitement, int fsm_LocalExcitedLv,
        int fsm_LocalOrgasmTimes,
        int fsm_PersonalSuspicion,
        int fsm_GameScore,
        int fsm_LocalAffinityLv, int fsm_LocalAffinityExp,
        HeroineEmotionType fsm_Emotion,
        MinigameEndReason reason)
    {
        if (_heroinesList == null) return;

        int score = (endReasonConfig != null) ? endReasonConfig.GetBaseScore(reason) : 0;

        string hID = (fsmIndex >= 0 && fsmIndex < _heroinesList.Count) ? _heroinesList[fsmIndex].HeroineID : "";
        string hName = (fsmIndex >= 0 && fsmIndex < _heroinesList.Count) ? _heroinesList[fsmIndex].Name : "";

        FsmReportData report = new FsmReportData
        {
            FsmIndex = fsmIndex,
            HeroineID = hID,
            HeroineName = hName,
            Score = score,
            LocalExcitement = fsm_LocalExcitement,
            LocalExcitedLv = fsm_LocalExcitedLv,
            LocalOrgasmTimes = fsm_LocalOrgasmTimes,
            PersonalSuspicion = fsm_PersonalSuspicion,
            GameScore = fsm_GameScore,
            LocalAffinityLv = fsm_LocalAffinityLv,
            LocalAffinityExp = fsm_LocalAffinityExp,
            Emotion = fsm_Emotion,
            Reason = reason
        };

        _receivedReports.Add(report);

        if (resultHandler != null)
            resultHandler.ReceiveSingleReport(report, _context);

        if (_receivedReports.Count >= _activeFSMCount)
        {
            if (resultHandler != null) resultHandler.NotifyAllHeroinesFinished();
            else FinalizeAndExit();
        }
    }

    public void FinalizeAndExit()
    {
        OnGameFinished?.Invoke();
    }

    #region 私有輔助方法

    private void SyncStressToGlobals()
    {
        if (_context?.Protagonist != null)
        {
            int stress = _context.Protagonist.Stress;

            FsmInt globalStressVar = PlayMakerGlobals.Instance.Variables.FindFsmInt("global_Stress");
            if (globalStressVar != null) globalStressVar.Value = stress;

            // Compatibility: 舊 FSM 若仍讀 global_Suspicion，暫時給相同壓力值。
            FsmInt globalSuspVar = PlayMakerGlobals.Instance.Variables.FindFsmInt("global_Suspicion");
            if (globalSuspVar != null) globalSuspVar.Value = stress;
        }
    }

    private void InitializeFSMs()
    {
        for (int i = 0; i < _heroinesList.Count; i++)
        {
            if (i >= targetFSMs.Length) break;
            PlayMakerFSM fsm = targetFSMs[i];
            if (fsm == null) continue;
            var heroine = _heroinesList[i];

            fsm.FsmVariables.GetFsmString("fsm_HeroineID").Value = heroine.HeroineID;

            FsmInt fsmLv = fsm.FsmVariables.FindFsmInt("fsm_LocalExcitedLv"); if (fsmLv != null) fsmLv.Value = heroine.BaseExcitementLevel;

            // fsm_LocalExcitement改 由 FSM 自行管理
            //FsmInt fsmExp = fsm.FsmVariables.FindFsmInt("fsm_LocalExcitement"); if (fsmExp != null) fsmExp.Value = heroine.BaseExcitementExp;

            FsmInt fsmLewdLevel = fsm.FsmVariables.FindFsmInt("fsm_LewdnessLevel"); if (fsmLewdLevel != null) fsmLewdLevel.Value = heroine.LewdnessLevel;
            FsmInt fsmLewdExp = fsm.FsmVariables.FindFsmInt("fsm_LewdnessExp"); if (fsmLewdExp != null) fsmLewdExp.Value = heroine.LewdnessExp;

            // 個人可疑度改 由 FSM 自行管理
            //FsmInt fsmPersonalSusp = fsm.FsmVariables.FindFsmInt("fsm_PersonalSuspicion"); if (fsmPersonalSusp != null) fsmPersonalSusp.Value = heroine.PersonalSuspicion;
            FsmInt fsmPersonalSuspMax = fsm.FsmVariables.FindFsmInt("fsm_PersonalSuspicionMax"); if (fsmPersonalSuspMax != null) fsmPersonalSuspMax.Value = heroine.PersonalSuspicionMax;

            // 親密度
            FsmInt fsmAffinityLv = fsm.FsmVariables.FindFsmInt("fsm_LocalAffinityLv"); if (fsmAffinityLv != null) fsmAffinityLv.Value = heroine.BaseAffinityLevel;
            FsmInt fsmAffinityExp = fsm.FsmVariables.FindFsmInt("fsm_LocalAffinityExp"); if (fsmAffinityExp != null) fsmAffinityExp.Value = heroine.BaseAffinityExp;
            int affinityThreshold = heroine.GetCurrentAffinityThreshold(heroine.BaseAffinityLevel);
            FsmInt fsmAffinityMax = fsm.FsmVariables.FindFsmInt("fsm_AffinityMax"); if (fsmAffinityMax != null) fsmAffinityMax.Value = affinityThreshold;
            FsmBool fsmIsAffinityMaxLv = fsm.FsmVariables.FindFsmBool("IsAffinityMaxLv"); if (fsmIsAffinityMaxLv != null) fsmIsAffinityMaxLv.Value = heroine.IsAffinityLevelLocked(heroine.BaseAffinityLevel);

            // 情緒
            FsmEnum fsmEmotion = fsm.FsmVariables.FindFsmEnum("fsm_Emotion"); if (fsmEmotion != null) fsmEmotion.Value = heroine.CurrentEmotion;

            int currentThreshold = heroine.GetCurrentExcitementThreshold(heroine.BaseExcitementLevel);
            FsmInt fsmMaxExp = fsm.FsmVariables.FindFsmInt("fsm_ExcitementMax"); if (fsmMaxExp != null) fsmMaxExp.Value = currentThreshold;
            FsmBool fsmIsMaxLv = fsm.FsmVariables.FindFsmBool("IsExcitementMaxLv"); if (fsmIsMaxLv != null) fsmIsMaxLv.Value = heroine.IsExcitementLevelLocked(heroine.BaseExcitementLevel);

            _activeFSMCount++;
        }
    }

    private void CheckAndLogCurrentLocationRisks()
    {
        CurrentLocationRiskIDs.Clear();
        if (_context?.Scenario == null) return;
        string currentLocID = _context.Scenario.LocationID;
        LocationState state = _context.Scenario.GetState(currentLocID);
        if (state?.Risks != null)
            foreach (var risk in state.Risks)
                if (!string.IsNullOrEmpty(risk.inspectionTypeID)) CurrentLocationRiskIDs.Add(risk.inspectionTypeID);
    }

    /// <summary>強制結束所有尚未回報的女主角。</summary>
    public void ForceFinishRemaining(MinigameEndReason reason)
    {
        if (_heroinesList == null || _activeFSMCount <= 0) return;

        HashSet<int> finishedIndices = new HashSet<int>();
        foreach (var report in _receivedReports) finishedIndices.Add(report.FsmIndex);

        for (int i = 0; i < _activeFSMCount; i++)
        {
            if (!finishedIndices.Contains(i))
            {
                int currentExp = 0, currentLv = 0, currentOrgasm = 0, currentPersonalSusp = 0;
                int currentGameScore = 0;
                int currentAffinityLv = 0, currentAffinityExp = 0;
                HeroineEmotionType currentEmotion = HeroineEmotionType.Idle;

                if (targetFSMs[i] != null)
                {
                    var fsmExp = targetFSMs[i].FsmVariables.FindFsmInt("fsm_LocalExcitement");
                    var fsmLv = targetFSMs[i].FsmVariables.FindFsmInt("fsm_LocalExcitedLv");
                    var fsmOrgasm = targetFSMs[i].FsmVariables.FindFsmInt("fsm_LocalOrgasmTimes");
                    var fsmSusp = targetFSMs[i].FsmVariables.FindFsmInt("fsm_PersonalSuspicion");
                    var fsmGameScore = targetFSMs[i].FsmVariables.FindFsmInt("fsm_GameScore");
                    var fsmAffLv = targetFSMs[i].FsmVariables.FindFsmInt("fsm_LocalAffinityLv");
                    var fsmAffExp = targetFSMs[i].FsmVariables.FindFsmInt("fsm_LocalAffinityExp");
                    var fsmEmotion = targetFSMs[i].FsmVariables.FindFsmEnum("fsm_Emotion");

                    if (fsmExp != null) currentExp = fsmExp.Value;
                    if (fsmLv != null) currentLv = fsmLv.Value;
                    if (fsmOrgasm != null) currentOrgasm = fsmOrgasm.Value;
                    if (fsmSusp != null) currentPersonalSusp = fsmSusp.Value;
                    if (fsmGameScore != null) currentGameScore = fsmGameScore.Value;
                    if (fsmAffLv != null) currentAffinityLv = fsmAffLv.Value;
                    if (fsmAffExp != null) currentAffinityExp = fsmAffExp.Value;
                    if (fsmEmotion != null) currentEmotion = (HeroineEmotionType)fsmEmotion.Value;

                    targetFSMs[i].SendEvent("FORCE_STOP");
                }

                Debug.Log($"[Adapter] 正在強制結束槽位 {i} ({reason})");
                ReportGameFinishedFromFSM(i, currentExp, currentLv, currentOrgasm, currentPersonalSusp,
                    currentGameScore, currentAffinityLv, currentAffinityExp, currentEmotion, reason);
            }
        }
    }

    private List<string> CalculateFinalRiskIDs()
    {
        List<string> finalIDs = new List<string>(CurrentLocationRiskIDs);
        if (_context?.Scenario == null) return finalIDs;
        LocationState state = _context.Scenario.GetState(_context.Scenario.LocationID);
        if (state?.Heroines != null)
        {
            HashSet<string> interactiveHeroineIDs = new HashSet<string>();
            foreach (var h in _heroinesList) interactiveHeroineIDs.Add(h.HeroineID.Trim().ToLower());
            foreach (var hData in state.Heroines)
            {
                string currentHID = hData.HeroineID?.Trim().ToLower();
                if (!interactiveHeroineIDs.Contains(currentHID) && !string.IsNullOrEmpty(hData.Activity))
                    finalIDs.Add(hData.Activity);
            }
        }
        return finalIDs;
    }

    #endregion
}