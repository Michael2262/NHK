using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 清水模小遊戲控制器
/// 用於測試結算流程，點擊畫面即可產生隨機結果
/// </summary>
public class StubMinigameController : MonoBehaviour, IMinigameController
{
    #region 表現等級設定

    /// <summary>
    /// 單一表現等級的數值範圍設定
    /// </summary>
    [System.Serializable]
    public class PerformanceRange
    {
        [Header("等級名稱 (僅供辨識)")]
        public string label = "普通";

        [Header("分數範圍")]
        public int scoreMin = 0;
        public int scoreMax = 100;

        [Header("興奮度經驗值範圍")]
        public int excitementExpMin = 0;
        public int excitementExpMax = 50;

        [Header("興奮度等級範圍")]
        public int excitementLvMin = 0;
        public int excitementLvMax = 3;

        [Header("結束原因機率權重")]
        [Tooltip("依序為: 被抓到, 手動退出, 女主角離開, 高潮過多, 進入下一關, 其他")]
        public float[] reasonWeights = new float[] { 0.1f, 0.1f, 0.1f, 0.1f, 0.5f, 0.1f };

        /// <summary>
        /// 根據權重隨機選擇結束原因
        /// </summary>
        public MinigameEndReason GetRandomReason()
        {
            float total = 0f;
            foreach (var w in reasonWeights) total += w;

            float roll = Random.Range(0f, total);
            float cumulative = 0f;

            for (int i = 0; i < reasonWeights.Length && i < 6; i++)
            {
                cumulative += reasonWeights[i];
                if (roll <= cumulative)
                    return (MinigameEndReason)i;
            }
            return MinigameEndReason.Other;
        }
    }

    [Header("=== 4 組表現等級設定 ===")]
    [Tooltip("S 級 - 完美表現")]
    public PerformanceRange rankS = new PerformanceRange
    {
        label = "S - 完美",
        scoreMin = 900, scoreMax = 1000,
        excitementExpMin = 80, excitementExpMax = 100,
        excitementLvMin = 2, excitementLvMax = 3,
        reasonWeights = new float[] { 0f, 0f, 0f, 0.2f, 0.8f, 0f }
    };

    [Tooltip("A 級 - 良好表現")]
    public PerformanceRange rankA = new PerformanceRange
    {
        label = "A - 良好",
        scoreMin = 600, scoreMax = 899,
        excitementExpMin = 50, excitementExpMax = 79,
        excitementLvMin = 1, excitementLvMax = 2,
        reasonWeights = new float[] { 0.05f, 0.05f, 0.1f, 0.1f, 0.65f, 0.05f }
    };

    [Tooltip("B 級 - 普通表現")]
    public PerformanceRange rankB = new PerformanceRange
    {
        label = "B - 普通",
        scoreMin = 300, scoreMax = 599,
        excitementExpMin = 20, excitementExpMax = 49,
        excitementLvMin = 0, excitementLvMax = 1,
        reasonWeights = new float[] { 0.15f, 0.1f, 0.15f, 0.05f, 0.5f, 0.05f }
    };

    [Tooltip("C 級 - 較差表現")]
    public PerformanceRange rankC = new PerformanceRange
    {
        label = "C - 較差",
        scoreMin = 0, scoreMax = 299,
        excitementExpMin = 0, excitementExpMax = 19,
        excitementLvMin = 0, excitementLvMax = 0,
        reasonWeights = new float[] { 0.4f, 0.2f, 0.2f, 0f, 0.1f, 0.1f }
    };

    #endregion

    #region 控制選項

    [Header("=== 控制選項 ===")]
    [Tooltip("選擇要使用哪個表現等級 (0=S, 1=A, 2=B, 3=C, 4=隨機)")]
    [Range(0, 4)]
    public int selectedRankIndex = 4;

    [Tooltip("是否為每位女主角獨立隨機等級")]
    public bool randomRankPerHeroine = true;

    [Tooltip("結算模式：true=全部同時結算, false=逐一結算(每次點擊結算一位)")]
    public bool settleAllAtOnce = true;

    [Tooltip("小遊戲結束後是否消耗時段")]
    [SerializeField] private bool _advanceTimeOnFinish = false;

    #endregion

    #region 結果處理器

    [Header("=== 結果處理器 ===")]
    public MinigameResultHandler resultHandler;

    #endregion

    #region IMinigameController 實作

    public event System.Action OnGameFinished;
    public bool AdvanceTimeOnFinish => _advanceTimeOnFinish;

    private MinigameContext _context;
    private List<HeroineStatusModel> _heroinesList;
    private int _currentSettleIndex = 0;
    private bool _isInitialized = false;
    private List<FsmReportData> _generatedReports = new List<FsmReportData>();

    [Header("=== 進階選項 ===")]
    [Tooltip("是否在 Start 時自動向 MinigameManager 註冊。\n" +
             "• true (預設): 正式流程，從 MGM 進入小遊戲\n" +
             "• false: 清水模，完全由 Mocker 注入資料")]
    public bool autoRegisterToMGM = true;

    void Start()
    {
        if (!autoRegisterToMGM)
        {
            Debug.Log("[StubMinigame] autoRegisterToMGM 為 false，等待 Mocker 注入...");
            return;
        }

        if (MinigameManager.Instance == null)
        {
            Debug.LogWarning("[StubMinigame] 找不到 MinigameManager，等待 Mocker 注入...");
            return;
        }

        // 檢查 MGM 是否已準備好資料（從正式流程進入時才會有）
        // 如果是直接在清水模場景啟動，MGM 不會有 targetHeroineIDs
        if (!MinigameManager.Instance.HasPendingMinigameData())
        {
            Debug.Log("[StubMinigame] MGM 沒有待處理的小遊戲資料，等待 Mocker 注入...");
            return;
        }

        // 正式流程：向 MGM 註冊
        MinigameManager.Instance.RegisterMinigameController(this);
    }

    public void Initialize(MinigameContext context)
    {
        _context = context;
        _heroinesList = context.ActiveHeroines;
        _currentSettleIndex = 0;
        _generatedReports.Clear();
        _isInitialized = true;

        Debug.Log($"<color=cyan>[StubMinigame] 初始化完成，女主角數量: {_heroinesList.Count}</color>");
        Debug.Log("<color=yellow>[StubMinigame] 點擊畫面任意處開始結算</color>");
    }

    public void StartGame()
    {
        // 清水模不需要特別的啟動邏輯
        Debug.Log("<color=green>[StubMinigame] 遊戲已啟動，等待點擊...</color>");
    }

    #endregion

    #region 點擊觸發

    void Update()
    {
        if (!_isInitialized || _heroinesList == null || _heroinesList.Count == 0)
            return;

        // 使用 New Input System 偵測點擊
        bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool touchPressed = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

        if (mouseClicked || touchPressed)
        {
            OnClick();
        }
    }

    private void OnClick()
    {
        if (settleAllAtOnce)
        {
            SettleAllHeroines();
        }
        else
        {
            SettleNextHeroine();
        }
    }

    #endregion

    #region 結算邏輯

    [Header("=== 結束流程控制 ===")]
    [Tooltip("是否等待 ResultUIFSM 演出完成後再跳轉場景。\n" +
             "• true: 由 FSM 呼叫 FinalizeAndExit()\n" +
             "• false: 自動延遲後跳轉")]
    public bool waitForFSMToFinish = true;

    [Tooltip("若不等待 FSM，自動跳轉的延遲秒數")]
    public float autoExitDelay = 2.0f;

    /// <summary>
    /// 一次結算所有女主角
    /// </summary>
    private void SettleAllHeroines()
    {
        Debug.Log("<color=orange>[StubMinigame] 開始結算所有女主角...</color>");

        for (int i = 0; i < _heroinesList.Count; i++)
        {
            GenerateAndSendReport(i);
        }

        // 通知全體結束
        if (resultHandler != null)
        {
            resultHandler.NotifyAllHeroinesFinished();
        }

        _isInitialized = false; // 防止重複點擊
        
        // 根據設定決定結束方式
        if (waitForFSMToFinish)
        {
            Debug.Log("<color=yellow>[StubMinigame] 等待 ResultUIFSM 完成演出後呼叫 FinalizeAndExit()...</color>");
            // 不自動觸發，等待 FSM 呼叫 FinalizeAndExit()
        }
        else
        {
            Debug.Log($"<color=yellow>[StubMinigame] 將在 {autoExitDelay} 秒後自動跳轉...</color>");
            Invoke(nameof(TriggerGameFinished), autoExitDelay);
        }
    }

    /// <summary>
    /// 逐一結算（每次點擊結算一位）
    /// </summary>
    private void SettleNextHeroine()
    {
        if (_currentSettleIndex >= _heroinesList.Count)
        {
            Debug.Log("<color=orange>[StubMinigame] 所有女主角已結算完畢</color>");
            return;
        }

        Debug.Log($"<color=orange>[StubMinigame] 結算第 {_currentSettleIndex + 1} 位女主角...</color>");

        GenerateAndSendReport(_currentSettleIndex);
        _currentSettleIndex++;

        // 檢查是否全部結算完畢
        if (_currentSettleIndex >= _heroinesList.Count)
        {
            if (resultHandler != null)
            {
                resultHandler.NotifyAllHeroinesFinished();
            }

            // 根據設定決定結束方式
            if (waitForFSMToFinish)
            {
                Debug.Log("<color=yellow>[StubMinigame] 等待 ResultUIFSM 完成演出後呼叫 FinalizeAndExit()...</color>");
            }
            else
            {
                Invoke(nameof(TriggerGameFinished), autoExitDelay);
            }
        }
    }

    /// <summary>
    /// 產生並發送單一女主角的結算報告
    /// </summary>
    private void GenerateAndSendReport(int index)
    {
        if (index < 0 || index >= _heroinesList.Count) return;

        var heroine = _heroinesList[index];
        PerformanceRange range = GetPerformanceRange();

        // 隨機產生數據
        int score = Random.Range(range.scoreMin, range.scoreMax + 1);
        int excitementExp = Random.Range(range.excitementExpMin, range.excitementExpMax + 1);
        int excitementLv = Random.Range(range.excitementLvMin, range.excitementLvMax + 1);
        MinigameEndReason reason = range.GetRandomReason();

        // 建立報告
        FsmReportData report = new FsmReportData
        {
            FsmIndex = index,
            HeroineID = heroine.HeroineID,
            HeroineName = heroine.Name,
            Score = score,
            LocalExcitement = excitementExp,
            LocalExcitedLv = excitementLv,
            Reason = reason,
            RawReason = reason.ToString()
        };

        _generatedReports.Add(report);

        // Log 結果
        Debug.Log($"<color=lime>[StubMinigame] 女主角 {heroine.Name} 結算結果:</color>\n" +
                  $"  等級: {range.label}\n" +
                  $"  分數: {score}\n" +
                  $"  興奮度: Lv{excitementLv} / Exp{excitementExp}\n" +
                  $"  結束原因: {reason}");

        // 發送給 ResultHandler
        if (resultHandler != null)
        {
            Debug.Log($"<color=cyan>[StubMinigame] 發送報告給 ResultHandler (Context null? {_context == null})</color>");
            resultHandler.ReceiveSingleReport(report, _context);
        }
        else
        {
            Debug.LogWarning("<color=red>[StubMinigame] resultHandler 為 null！請在 Inspector 中指定 MinigameResultHandler。</color>");
        }
    }

    /// <summary>
    /// 根據設定取得要使用的表現等級
    /// </summary>
    private PerformanceRange GetPerformanceRange()
    {
        int rankToUse = selectedRankIndex;

        // 如果設定為隨機 (4) 或每位女主角獨立隨機
        if (selectedRankIndex == 4 || randomRankPerHeroine)
        {
            rankToUse = Random.Range(0, 4);
        }

        switch (rankToUse)
        {
            case 0: return rankS;
            case 1: return rankA;
            case 2: return rankB;
            case 3: return rankC;
            default: return rankB;
        }
    }

    private void TriggerGameFinished()
    {
        FinalizeAndExit();
    }

    /// <summary>
    /// 結束小遊戲並通知 MGM
    /// 可由外部（如 ResultHandler）呼叫
    /// </summary>
    public void FinalizeAndExit()
    {
        Debug.Log("<color=cyan>[StubMinigame] FinalizeAndExit - 準備結束小遊戲</color>");

        // 防止重複呼叫
        if (!_isInitialized && _generatedReports.Count == 0)
        {
            Debug.LogWarning("[StubMinigame] FinalizeAndExit 被重複呼叫，跳過。");
            return;
        }

        _isInitialized = false;

        // 輸出結算摘要
        Debug.Log($"<color=cyan>[StubMinigame] 結算摘要：共 {_generatedReports.Count} 位女主角</color>");
        foreach (var report in _generatedReports)
        {
            Debug.Log($"  - {report.HeroineName}: 分數={report.Score}, 原因={report.Reason}");
        }

        // 觸發事件通知 MGM
        Debug.Log("<color=cyan>[StubMinigame] 觸發 OnGameFinished 事件</color>");
        OnGameFinished?.Invoke();
    }

    /// <summary>
    /// 強制結束所有尚未結算的女主角
    /// 對應原版 Adapter 的 ForceFinishRemaining
    /// </summary>
    /// <param name="reason">統一填入的結束理由</param>
    public void ForceFinishRemaining(MinigameEndReason reason)
    {
        if (_heroinesList == null || _heroinesList.Count == 0) return;

        Debug.Log($"<color=yellow>[StubMinigame] 強制結束剩餘女主角，原因: {reason}</color>");

        // 找出已結算的索引
        HashSet<int> finishedIndices = new HashSet<int>();
        foreach (var report in _generatedReports)
        {
            finishedIndices.Add(report.FsmIndex);
        }

        // 結算尚未完成的
        for (int i = 0; i < _heroinesList.Count; i++)
        {
            if (!finishedIndices.Contains(i))
            {
                Debug.Log($"[StubMinigame] 強制結算槽位 {i}");
                GenerateAndSendReportWithReason(i, reason);
            }
        }

        // 通知全體結束
        if (resultHandler != null)
        {
            resultHandler.NotifyAllHeroinesFinished();
        }

        // 結束遊戲
        FinalizeAndExit();
    }

    /// <summary>
    /// 產生並發送指定結束原因的報告
    /// </summary>
    private void GenerateAndSendReportWithReason(int index, MinigameEndReason forcedReason)
    {
        if (index < 0 || index >= _heroinesList.Count) return;

        var heroine = _heroinesList[index];
        PerformanceRange range = GetPerformanceRange();

        // 強制結束時，分數通常較低
        int score = Random.Range(0, range.scoreMin);
        int excitementExp = Random.Range(range.excitementExpMin, range.excitementExpMax + 1);
        int excitementLv = Random.Range(range.excitementLvMin, range.excitementLvMax + 1);

        FsmReportData report = new FsmReportData
        {
            FsmIndex = index,
            HeroineID = heroine.HeroineID,
            HeroineName = heroine.Name,
            Score = score,
            LocalExcitement = excitementExp,
            LocalExcitedLv = excitementLv,
            Reason = forcedReason,
            RawReason = forcedReason.ToString()
        };

        _generatedReports.Add(report);

        Debug.Log($"<color=yellow>[StubMinigame] 強制結算 {heroine.Name}: 分數={score}, 原因={forcedReason}</color>");

        if (resultHandler != null)
        {
            resultHandler.ReceiveSingleReport(report, _context);
        }
    }

    #endregion

    #region 編輯器輔助

    /// <summary>
    /// 取得所有表現等級（供編輯器或除錯用）
    /// </summary>
    public PerformanceRange[] GetAllRanks()
    {
        return new PerformanceRange[] { rankS, rankA, rankB, rankC };
    }

    /// <summary>
    /// 在編輯器中手動觸發結算（供測試按鈕使用）
    /// </summary>
    [ContextMenu("手動觸發結算")]
    public void DebugTriggerSettle()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[StubMinigame] 請在播放模式下使用此功能");
            return;
        }
        OnClick();
    }

    #endregion
}
