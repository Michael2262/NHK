using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 職責：作為 Unity Event (Inspector) 與 ProgressFlagModel 之間的橋樑。
/// 用途：掛在場景物件上，讓按鈕、觸發器、Timeline 或 Animation Event 可以呼叫 Flag 系統。
/// </summary>
public class ProgressFlagBridge : MonoBehaviour
{
    [Header("測試/預設參數 (可選)")]
    [Tooltip("如果你想透過 ContextMenu 測試，或是某些 UnityEvent 只能傳無參數方法，會優先使用這裡填入的 SO")]
    [SerializeField] private ProgressFlagDefinition targetFlagSO;

    private ProgressFlagModel Model
    {
        get
        {
            if (GameStatusService.Instance != null)
                return GameStatusService.Instance.ProgressFlags;
            return null;
        }
    }

    // ==========================================================
    // 1. 透過 ScriptableObject 操作 (推薦，最安全)
    // ==========================================================

    /// <summary> 【永久】新增標記 (會存檔) </summary>
    public void AddPersistentFlag(ProgressFlagDefinition flagDef)
    {
        if (Model == null || flagDef == null) return;
        Model.AddPersistentFlag(flagDef.FlagID);
        Debug.Log($"[Bridge] Flag Added (Persistent): {flagDef.FlagID}");
    }

    /// <summary> 【場景】新增標記 (切換場景消失) </summary>
    public void AddSceneFlag(ProgressFlagDefinition flagDef)
    {
        if (Model == null || flagDef == null) return;
        Model.AddSceneFlag(flagDef.FlagID);
        Debug.Log($"[Bridge] Flag Added (Scene): {flagDef.FlagID}");
    }

    /// <summary> 【時段】新增標記 (時間前進 Slot 即消失) </summary>
    public void AddSlotFlag(ProgressFlagDefinition flagDef)
    {
        if (Model == null || flagDef == null) return;
        Model.AddSlotFlag(flagDef.FlagID);
        Debug.Log($"[Bridge] Flag Added (UntilNextSlot): {flagDef.FlagID}");
    }

    /// <summary> 【階段】新增標記 (進入下個 Phase 即消失) </summary>
    public void AddPhaseFlag(ProgressFlagDefinition flagDef)
    {
        if (Model == null || flagDef == null) return;
        Model.AddPhaseFlag(flagDef.FlagID);
        Debug.Log($"[Bridge] Flag Added (UntilNextPhase): {flagDef.FlagID}");
    }

    /// <summary> 【每日】新增標記 (跨日即消失) </summary>
    public void AddDailyFlag(ProgressFlagDefinition flagDef)
    {
        if (Model == null || flagDef == null) return;
        Model.AddDailyFlag(flagDef.FlagID);
        Debug.Log($"[Bridge] Flag Added (Daily): {flagDef.FlagID}");
    }

    /// <summary> 移除標記 </summary>
    public void RemoveFlag(ProgressFlagDefinition flagDef)
    {
        if (Model == null || flagDef == null) return;
        Model.RemoveFlag(flagDef.FlagID);
        Debug.Log($"[Bridge] Flag Removed: {flagDef.FlagID}");
    }

    // ==========================================================
    // 2. 透過 String 操作 (靈活，適合動態字串或 Dialogue System)
    // ==========================================================

    public void AddPersistentFlagByID(string flagID)
    {
        if (Model == null || string.IsNullOrEmpty(flagID)) return;
        Model.AddPersistentFlag(flagID);
    }

    public void AddSceneFlagByID(string flagID)
    {
        if (Model == null || string.IsNullOrEmpty(flagID)) return;
        Model.AddSceneFlag(flagID);
    }

    public void AddSlotFlagByID(string flagID)
    {
        if (Model == null || string.IsNullOrEmpty(flagID)) return;
        Model.AddSlotFlag(flagID);
    }

    public void AddPhaseFlagByID(string flagID)
    {
        if (Model == null || string.IsNullOrEmpty(flagID)) return;
        Model.AddPhaseFlag(flagID);
    }

    public void AddDailyFlagByID(string flagID)
    {
        if (Model == null || string.IsNullOrEmpty(flagID)) return;
        Model.AddDailyFlag(flagID);
    }

    public void RemoveFlagByID(string flagID)
    {
        if (Model == null || string.IsNullOrEmpty(flagID)) return;
        Model.RemoveFlag(flagID);
    }

    // ==========================================================
    // 3. 分歧檢查 (Branching)
    // ==========================================================

    [Header("分歧事件 (用於 CheckFlagAndInvoke)")]
    public UnityEvent OnFlagTrue;
    public UnityEvent OnFlagFalse;

    /// <summary>
    /// 檢查 Flag 是否存在，並根據結果觸發對應的 UnityEvent。
    /// </summary>
    public void CheckFlagAndInvoke(ProgressFlagDefinition flagDef)
    {
        if (Model == null || flagDef == null) return;

        // Contains 會檢查所有生命週期的桶子
        if (Model.Contains(flagDef.FlagID))
        {
            OnFlagTrue?.Invoke();
        }
        else
        {
            OnFlagFalse?.Invoke();
        }
    }

    // ==========================================================
    // 4. 無參數方法 (使用 Inspector 欄位 targetFlagSO)
    // ==========================================================

    [ContextMenu("Add Persistent Flag (Target SO)")]
    public void AddTargetPersistentFlag()
    {
        if (targetFlagSO != null) AddPersistentFlag(targetFlagSO);
        else Debug.LogWarning("[Bridge] targetFlagSO is empty!");
    }

    [ContextMenu("Add Daily Flag (Target SO)")]
    public void AddTargetDailyFlag()
    {
        if (targetFlagSO != null) AddDailyFlag(targetFlagSO);
        else Debug.LogWarning("[Bridge] targetFlagSO is empty!");
    }

    [ContextMenu("Remove Target Flag (Target SO)")]
    public void RemoveTargetFlag()
    {
        if (targetFlagSO != null) RemoveFlag(targetFlagSO);
        else Debug.LogWarning("[Bridge] targetFlagSO is empty!");
    }
}