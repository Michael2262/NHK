using DG.Tweening.Core.Easing;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-900)]

// 職責：作為全局唯一的存取點 (Singleton)，持有並管理所有核心 Model 和 Manager 的生命週期。
// GameStatusService 繼承自 MonoBehaviour，必須附加到場景中的一個 GameObject 上。
public class GameStatusService : MonoBehaviour
{
    // ==========================================================
    // Singleton 設置
    // ==========================================================
    public static GameStatusService Instance { get; private set; }

    [Header("配置檔案 (Config)")]
    // 必須在 Unity Inspector 中拖曳相對應的 ScriptableObject
    [SerializeField] private HeroineStatusConfig heroineConfig; // 女主角數值配置
    [SerializeField] private TimeConfig timeConfig;             // 時間系統配置
    [SerializeField] private SkillConfig skillConfig;           // 技能樹配置
    [SerializeField] private ItemDatabase itemDatabase; // 拖曳你的 ItemDatabase SO 進來
    [SerializeField] public StatusEffectDatabase StatusEffectDatabase; // 狀態效果數據庫
    [SerializeField] private RiskDatabase riskDatabase;         // 風險代理人(家人)配置
    [SerializeField] private LocationDatabase locationDatabase; //  ScenarioManager 需要的地點資料庫
    [SerializeField] private DailyScheduleConfig dailyScheduleConfig; //
    [SerializeField] private DayTriggerConfig dayTriggerConfig; //
    [SerializeField] private StatChangePackageDatabase statChangePackageDatabase; // 數值變化套組資料庫



    [Header("存檔通知（已改為事件驅動，此欄位不再使用）")]
    [SerializeField] private SaveNotification saveNotification; // 保留欄位避免序列化報錯，但不再使用

    // 存檔完成事件：SaveNotification 等 UI 元件訂閱這些事件即可
    public static event Action OnAutoSaveCompleted;
    public static event Action OnManualSaveCompleted;

    // 註：女主角數值觸發 / 映射已退役，改由 ProgressUnlockManager 統一處理
    // (門檻規則 → Rule Asset；忠實映射 → Config 的 mirrors)。

    [Header("通用數值觸發設定")]
    [SerializeField] private List<ValueTriggerContainer> valueTriggerContainers = new List<ValueTriggerContainer>();
    public ValueTriggerManager ValueTriggerManager { get; private set; }

    [Header("進度解鎖系統")]
    [UnityEngine.Serialization.FormerlySerializedAs("heroineUnlockConfigs")]
    [SerializeField] private List<ProgressUnlockConfig> progressUnlockConfigs = new List<ProgressUnlockConfig>();

    public ProgressUnlockManager ProgressUnlockManager { get; private set; }

    // ==========================================================
    // Model 實例 (數據層) - 可供外部查詢
    // ==========================================================
    public ProtagonistStatusModel Protagonist { get; private set; }
    public ProtagonistInventoryModel Inventory { get; private set; }
    public ProtagonistSkillModel Skills { get; private set; }
    public TimeSystemModel Time { get; private set; }
    public SceneActionQueueModel SceneActionQueue { get; private set; }
    public ShopStatusModel ShopStatus { get; private set; }
    public PendingDeliveryModel PendingDelivery { get; private set; }
    public ProgressFlagModel ProgressFlags { get; private set; }// 進度開關模型

    // 處理多女主角：使用 Dictionary 來儲存多個 Heroine Model
    public Dictionary<string, HeroineStatusModel> Heroines { get; private set; }
        = new Dictionary<string, HeroineStatusModel>();

    // RiskAgent Model：也是用Dictionary
    public Dictionary<string, RiskAgentModel> RiskAgents { get; private set; }
        = new Dictionary<string, RiskAgentModel>();
    public ProtagonistStatusEffectModel StatusEffectModel { get; private set; }
    public CurrentScenarioModel Scenario { get; private set; } // 當前情境 Model
    public GlobalProgressModel GlobalProgress { get; private set; } // 全域進度（跨存檔）



    // ==========================================================
    // 公開的 Config 存取點 (讓 Manager 讀取)
    // ==========================================================
    public ItemDatabase ItemDatabase => itemDatabase; //讓外部可以安全地存取 ItemDatabase

    public HeroineStatusConfig HeroineConfig => heroineConfig;
    public LocationDatabase LocationDB => locationDatabase;
    public RiskDatabase RiskDB => riskDatabase;

    public TimeConfig TimeConfig => timeConfig;

    private static int _slotToLoad = -1; // 新增一個靜態變數，用於跨場景傳遞要加載的數據
    //private static GameSaveData _pendingSaveDataToLoad = null;//好像暫時用不到，先註解掉

    // ==========================================================
    // Manager/Service 實例 (邏輯層) - 可供外部呼叫行為
    // ==========================================================
    public TimeSystemManager TimeManager { get; private set; }
    public SkillSystemManager SkillManager { get; private set; }
    public ShopService ShopService { get; private set; } // ShopService 的實例
    public ItemUseService ItemUseService { get; private set; }
    public StatChangeService StatChangeService { get; private set; } // 數值變化套組執行核心
    public SaveGameManager SaveManager { get; private set; } // 存檔管理器

    private DailyScheduleProcessor _scheduleProcessor;

    private DayTriggerProcessor _dayTriggerProcessor;

    // ==========================================================
    // event - 通知外部做事
    // ==========================================================

    public event Action OnGameStatusLoaded; // 當遊戲數據被載入或重設時觸發

    void Awake()
    {

        // 確保 Singleton 唯一性
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 確保場景切換時不會被銷毀

            // 系統啟動流程
            InitializeGameModels();
            SubscribeToEvents();

        }
        else
        {
            Debug.LogWarning("發現重複的 GameStatusService，已銷毀。");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 初始化所有 Model 和 Manager 實例 (遊戲啟動時執行)。
    /// </summary>
    private void InitializeGameModels()
    {
        // --- 1. 實例化 Model (數據層) ---
        // ProtagonistStatusModel: 無參數建構函式
        Protagonist = new ProtagonistStatusModel();

        Inventory = new ProtagonistInventoryModel();
        ProgressFlags = new ProgressFlagModel();
        Skills = new ProtagonistSkillModel();
        ShopStatus = new ShopStatusModel();
        PendingDelivery = new PendingDeliveryModel();
        StatusEffectModel = new ProtagonistStatusEffectModel();

        // TimeSystemModel: 注入 TimeConfig
        Time = new TimeSystemModel(timeConfig);

        // 場景動作佇列：跨轉場暫存「時間推進 / 強制移動 / Flag 變更」，
        // 由下個場景 SceneReadyCoordinator 的 Task_ExecuteSceneActionQueue 消費
        SceneActionQueue = new SceneActionQueueModel(Time, timeConfig, ProgressFlags);

        // 佇列為一次性轉場資料，不存檔；新遊戲 / 讀檔完成時清空殘留命令並重置暫停狀態
        OnGameStatusLoaded += () => SceneActionQueue.ResetForNewSession();

        // 呼叫獨立方法來初始化女主角
        InitializeHeroineModels();
        // 也呼叫獨立方法來初始化家人
        InitializeRiskAgentModels();


        Scenario = new CurrentScenarioModel(); // 實例化情境 Model

        // --- 實例化 Manager/Service (邏輯層) ---
        // TimeSystemManager: 注入 Time Model 和 Time Config
        TimeManager = new TimeSystemManager(Time, timeConfig);
        // SkillSystemManager: 注入 Protagonist Model, Skill Model 和 Skill Config
        SkillManager = new SkillSystemManager(Protagonist, Skills, skillConfig, ProgressFlags, Time);
        // 實例化 ItemUseService
        ItemUseService = new ItemUseService(itemDatabase, this);
        // 實例化 ShopService，將所有它需要的依賴項從上面已經創建好的實例中傳入
        ShopService = new ShopService(Protagonist, Inventory, ShopStatus, PendingDelivery, itemDatabase, () => Time.DayIndex);
        // 數值變化套組服務（套用時透過 Instance 取 Protagonist / Heroines）
        StatChangeService = new StatChangeService(statChangePackageDatabase);

        // --- 執行依賴注入與事件訂閱 ---
        // NHK 版 ProtagonistStatusModel 不再使用 Attack / Defense / RecalculateTotalStats。

        // SaveManager 依賴 this (GameStatusService)，所以最後實例化
        SaveManager = new SaveGameManager(this);

        // 全域進度：實例化 Model 並從獨立檔案載入
        GlobalProgress = new GlobalProgressModel();
        var globalData = SaveManager.LoadGlobalProgress();
        GlobalProgress.LoadFromSaveData(globalData);
        Debug.Log("GlobalProgress initialized from file.");

        // 啟動通用數值觸發系統
        ValueTriggerManager = new ValueTriggerManager(ProgressFlags, valueTriggerContainers);

        // 訂閱讀檔事件，刷新所有規則
        OnGameStatusLoaded += () => ValueTriggerManager.RefreshAll();

        // 啟動進度解鎖系統
        RebuildProgressUnlockManager();
        OnGameStatusLoaded += () =>
        {
            RebuildProgressUnlockManager();     // 讀檔後 Heroines 被重建，重新訂閱
            ProgressUnlockManager.RefreshAllRules();
        };

        // NHK 版不使用主角 SuspicionGateManager。

        // 建立 DailyScheduleProcessor，讓它自動監聽時間事件並執行排程
        if (dailyScheduleConfig != null)
        {
            _scheduleProcessor = new DailyScheduleProcessor(

                Time,               // 用來讀取目前是哪個 Phase、哪個 Slot、是否假日
                ProgressFlags,      // 用來讀 EnableFlag、寫 TargetFlag / TargetValue
                dailyScheduleConfig // 你在 Inspector 拖入的規則設定檔
            );
        }
        else
        {
            Debug.LogWarning("[GameStatusService] dailyScheduleConfig 未指定，排程系統不會啟動。");
        }
        if (dayTriggerConfig != null)
            _dayTriggerProcessor = new DayTriggerProcessor(Time, ProgressFlags, dayTriggerConfig);

        // 套用 Flag / Value 預設值。
        // 這樣即使「直接 Play 進場景」(未走開新遊戲 / 讀檔流程) 也能看到預設旗標生效。
        // 正式流程 (StartNewGame / LoadGame) 會先清空 ProgressFlags 再重套，故此處不會殘留或衝突。
        ApplyAllDefaults();

        // 直接 Play 進場景時也做一次初始刷新，讓 mirror / 規則一開場就正確。
        // (正式流程 StartNewGame / LoadGame 之後仍會再經 OnGameStatusLoaded 刷新一次，idempotent)
        ProgressUnlockManager.RefreshAllRules();

        Debug.Log("GameStatusService: All core systems initialized.");


    }

    /// <summary>
    /// 訂閱核心事件：時間流逝。
    /// </summary>
    private void SubscribeToEvents()
    {
        // 1. 訂閱 TimeManager 的事件 (即有邏輯)
        TimeManager.OnDayPassed += HandleDayPassed;

        // 2. 訂閱 Time Model 的細節變化，用於自動清理 Flag
        // 當時間推進(消耗 Slot)時，清理 "UntilNextSlot" 類型的 Flag
        Time.OnTimeSlotAdvanced += (slotsAdvanced) =>
        {

            ProgressFlags.ClearSlotFlags();

            _scheduleProcessor?.NotifySlotAdvanced();
            _dayTriggerProcessor?.NotifySlotAdvanced();

            // ── 網購到貨檢查：P0 S1 ──
            if (Time.CurrentPhaseIndex == 0 && Time.CurrentSlotInPhase == 1)
            {
                PendingDelivery.ProcessDeliveries(Time.DayIndex, Inventory);
            }
        };

        // 當進入下一個大階段(Phase)時，清理 Flag 並觸發數值映射
        Time.OnPhaseChanged += () =>
        {
            ProgressFlags.ClearPhaseFlags();
            _scheduleProcessor?.NotifyPhaseChanged();
            _dayTriggerProcessor?.NotifyPhaseChanged();
            ValueTriggerManager.OnValueChanged("Phase", Time.CurrentPhaseIndex);

            //每次換 Phase 時，對所有女主角執可疑度衰減
            foreach (var heroine in Heroines.Values)
            {
                //heroine.ApplyExcitementDecay();
                //heroine.ApplySuspicionDecay();
            }
        };
        TimeManager.OnDayPassed += () =>
        {
            ValueTriggerManager.OnValueChanged("Phase", Time.CurrentPhaseIndex);
            //每次換Day 時，對所有女主興奮度衰減
            foreach (var heroine in Heroines.Values)
            {
                heroine.ApplyExcitementDecay();
                heroine.ApplySuspicionDecay();
                heroine.ApplyLibidoDecay();
                heroine.AdvanceCycleDay(); // 推進排卵週期計數器
            }
        };

        // ★ 初始化時觸發一次，確保遊戲開始時 Phase Flag 狀態正確
        ValueTriggerManager.OnValueChanged("Phase", Time.CurrentPhaseIndex, isRefresh: true);

        // 3. 訂閱 SceneController 的場景切換完成事件
        SceneController.OnSceneChanged += HandleSceneChanged;
    }

    // ==========================================================
    // 遊戲流程控制
    // ==========================================================

    /// <summary>
    /// 處理新遊戲開始時的流程。
    /// </summary>
    public void StartNewGame()
    {
        // ★★★ 重置 Dialogue System 的變數和狀態 ★★★
        SaveSystem.ResetGameState(); // 清除所有槽位的記憶
        DialogueManager.ResetDatabase(DatabaseResetOptions.KeepAllLoaded); // 重置當前的對話資料庫

        // 重置所有 Model 的數值
        Protagonist.NewGame();
        Skills.NewGame();
        Inventory.NewGame();
        ShopStatus.NewGame();
        PendingDelivery.NewGame();
        Time.NewGame();
        StatusEffectModel.NewGame();
        ProgressFlags.NewGame();
        ApplyAllDefaults();

        //新遊戲時刷新通用觸發規則
        ValueTriggerManager.OnValueChanged("Phase", Time.CurrentPhaseIndex, isRefresh: true);

        InitializeHeroineModels();// 呼叫獨立方法來初始化女主角

        RebuildProgressUnlockManager(); // Heroines 已重建，重新訂閱解鎖規則

        InitializeRiskAgentModels();// 也呼叫獨立方法來重置家人

        Scenario.NewGame();// 重設情境 Model

        NotifyGameStatusLoaded();//通知所有系統，數據已重置完畢

        Debug.Log("GameStatusService: New Game Started.");
    }

    // ==========================================================
    // 自動存檔
    // ==========================================================

    /// <summary>
    /// 自動存檔：將當前遊戲狀態存入槽位 0，並顯示通知。
    /// 用法：GameStatusService.Instance.AutoSave();
    /// </summary>
    public void AutoSave()
    {
        Debug.Log("<color=yellow>[AutoSave] 執行自動存檔到槽位 0...</color>");
        SaveManager.SaveGame(SaveGameManager.AUTOSAVE_SLOT_INDEX);

        // 發送事件，讓 SaveNotification 等 UI 自行響應
        OnAutoSaveCompleted?.Invoke();
    }

    // ==========================================================
    // 讀取最近存檔（Game Over 用）
    // ==========================================================

    /// <summary>
    /// 讀取最近一次的存檔（不分手動或自動）。
    /// 用法：GameStatusService.Instance.LoadLatestSave();
    /// 適合綁定在 Game Over 畫面的「從上一次存檔繼續」按鈕上。
    /// </summary>
    public bool LoadLatestSave()
    {
        int latestSlot = SaveManager.FindLatestSaveSlot();

        if (latestSlot == -1)
        {
            Debug.LogWarning("[LoadLatestSave] 找不到任何存檔！");
            return false;
        }

        Debug.Log($"<color=cyan>[LoadLatestSave] 找到最近存檔：槽位 {latestSlot}，正在讀取...</color>");
        SaveManager.LoadGame(latestSlot);
        return true;
    }

    /// <summary>
    /// 立刻將全域進度寫入磁碟。
    /// 建議在每次 UnlockFlag / SetValue 後呼叫，確保即使玩家沒存檔也不會遺失。
    /// </summary>
    public void SaveGlobalProgress()
    {
        SaveManager.SaveGlobalProgress(GlobalProgress);
    }

    /// <summary>
    /// 解鎖全域旗標並立刻寫入磁碟。
    /// 用法範例：GameStatusService.Instance.UnlockGlobalFlag("GALLERY_CG_ENDING_A");
    /// </summary>
    public void UnlockGlobalFlag(string flag)
    {
        GlobalProgress.UnlockFlag(flag);
        SaveGlobalProgress();
    }

    /// <summary>
    /// 設定全域數值並立刻寫入磁碟。
    /// 用法範例：GameStatusService.Instance.SetGlobalValue("ClearCount", 1);
    /// </summary>
    public void SetGlobalValue(string key, int value)
    {
        GlobalProgress.SetValue(key, value);
        SaveGlobalProgress();
    }

    /// <summary>
    /// 累加全域數值並立刻寫入磁碟。
    /// 用法範例：GameStatusService.Instance.AddGlobalValue("ClearCount", 1);
    /// </summary>
    public void AddGlobalValue(string key, int amount)
    {
        GlobalProgress.AddValue(key, amount);
        SaveGlobalProgress();
    }

    /// 公共接口：用於觸發 OnGameStatusLoaded 事件 (由 Service 內部呼叫)。
    public void NotifyGameStatusLoaded()
    {
        OnGameStatusLoaded?.Invoke();
    }

    public void ApplyDataAfterSceneLoad()
    {
        // 【v4 改動】讀檔流程已改走 SceneController 正規管線
        // (SaveGameManager.LoadGame → ChangeScene → onFullyReady → ApplyLoadedDataAfterSceneReady)
        // 
        // 此方法保留僅為向下相容(若 Unity Inspector 的 Save System 事件還有綁定此方法)。
        // 實際讀檔邏輯已全部移至 SaveGameManager.ApplyLoadedDataAfterSceneReady。

        if (_slotToLoad != -1)
        {
            Debug.LogWarning($"[GameStatusService] ApplyDataAfterSceneLoad 被呼叫,但讀檔流程已改走 SceneController 管線,此處不執行任何動作。Slot: {_slotToLoad}");
            _slotToLoad = -1;
        }
    }



    // 我們需要一個新的公共方法來啟動整個流程
    public void SetSlotIndex(int slotIndex)
    {
        _slotToLoad = slotIndex;
    }

    // ==========================================================
    // 協調邏輯：處理隔天重置 (NextDay)
    // ==========================================================

    /// <summary>
    /// 當時間系統通知新的一天開始時，協調所有 Model 執行重置。
    /// </summary>
    // 在 GameStatusService.cs 內
    private void HandleDayPassed()
    {
        Debug.Log($"GameStatusService: Starting NextDay Reset for Day {Time.DayIndex}.");

        // 1. 清除所有每日標記 (UntilNextDay)
        ProgressFlags.ClearDayFlags();

        // 2. 主角數值重置 (原本的邏輯)
        Protagonist.NextDay();

        // 3. 商店限制重置
        ShopStatus.ResetDailyLimits(itemDatabase);

        // 4. 狀態效果每日處理
        StatusEffectModel.HandleDayPassed(Protagonist);

        _dayTriggerProcessor?.NotifyDayPassed();

        Debug.Log("GameStatusService: All systems NextDay reset complete.");
        // 在這裡觸發 UI/Scene 事件，例如：Fade Out/In，或 UIManager.Instance.ShowDayStartUI()
    }

    // ==========================================================
    // 協調邏輯：處理場景切換 (Scene Change)
    // ==========================================================

    /// <summary>
    /// 當場景切換完成時 (舊場景已卸載，新場景剛載入)，執行清理工作。
    /// </summary>
    private void HandleSceneChanged()
    {
        Debug.Log("GameStatusService: Handling Scene Changed. Clearing temporary flags...");

        // 通知 ProgressFlags 清除所有標記為 "Temporary" 的 Flag
        // (前提是你的 ProgressFlagModel 已經照著上一步驟加了 OnSceneChanged 方法)
        ProgressFlags.OnSceneChanged();

        // 場景動作佇列：過期保險（撐過一次完整轉場仍未被消費的命令會被丟棄並警告）
        SceneActionQueue.HandleSceneChanged();
    }

    private void OnDestroy()
    {
        SceneController.OnSceneChanged -= HandleSceneChanged;

        // 解除解鎖系統的事件訂閱
        ProgressUnlockManager?.UnsubscribeAll();
        //_scheduleProcessor?.Dispose();
    }

    // ==========================================================
    // 負責初始化/重置所有女主角的方法
    // ==========================================================

    private void InitializeHeroineModels()
    {
        // 清空現有的字典，為重置做準備
        Heroines.Clear();

        // 遍歷 HeroineStatusConfig 中定義的所有角色
        foreach (var stat in heroineConfig.HeroineStats)
        {
            if (string.IsNullOrEmpty(stat.ID))
            {
                Debug.LogWarning("HeroineStatusConfig 中發現一個沒有 ID 的角色，已跳過。");
                continue;
            }

            // 為列表中的每一個角色，創建一個對應的 Model 實例
            Heroines[stat.ID] = new HeroineStatusModel(stat, heroineConfig);
            Debug.Log($"已成功創建/重置女主角: {stat.Name} (ID: {stat.ID})");
        }
    }
    // ==========================================================
    // 負責初始化/重置所有家人的方法
    // ==========================================================
    private void InitializeRiskAgentModels()
    {
        RiskAgents.Clear();

        // 檢查 Config 是否已在 Inspector 中指派
        if (riskDatabase == null)
        {
            Debug.LogError("RiskDatabase config is not assigned in GameStatusService! 請在 Inspector 中拖曳 .asset 檔案。");
            return;
        }

        // 遍歷 RiskDatabase 中定義的所有家人
        foreach (var agentData in riskDatabase.allAgents)
        {
            if (string.IsNullOrEmpty(agentData.agentID))
            {
                Debug.LogWarning("RiskDatabase 中發現一個沒有 agentID 的角色，已跳過。");
                continue;
            }

            // 為列表中的每一個家人，創建一個對應的 Model 實例
            RiskAgents[agentData.agentID] = new RiskAgentModel(agentData.agentID);
            Debug.Log($"已成功創建/重置風險代理人: {agentData.agentName} (ID: {agentData.agentID})");
        }
    }


    // ==========================================================
    // ProgressUnlockManager
    // ==========================================================
    private void RebuildProgressUnlockManager()
    {
        // 解除舊的事件訂閱，避免記憶體洩漏或重複觸發
        ProgressUnlockManager?.UnsubscribeAll();

        // 重新建立，對新的 Heroines 實例與主角訂閱
        ProgressUnlockManager = new ProgressUnlockManager(ProgressFlags, Heroines, Protagonist, progressUnlockConfigs);
    }
    // ==========================================================
    // 開啟初始flag、value設定
    // ==========================================================
    private void ApplyAllDefaults()
    {
        var allDefs = Resources.LoadAll<ProgressBaseDefinition>("Progress");

        foreach (var def in allDefs)
        {
            if (def is ProgressFlagDefinition flagDef)
            {
                if (flagDef.DefaultValue)
                    ProgressFlags.AddPersistentFlag(flagDef.FlagID);
            }
            else if (def is ProgressValueDefinition valDef)
            {
                if (valDef.DefaultValue != 0)
                    ProgressFlags.SetValue(valDef.FlagID, valDef.DefaultValue);
            }
        }
    }
}