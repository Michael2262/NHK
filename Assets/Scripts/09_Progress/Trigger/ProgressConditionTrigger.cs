using System.Collections.Generic;
using UnityEngine;

public class ProgressConditionTrigger : MonoBehaviour
{
    // 定義邏輯類型：全部符合 (AND) 或 任一符合 (OR)
    public enum LogicType
    {
        All_Conditions_Met, // 必須全部達成
        Any_Condition_Met   // 只要達成其中一個
    }

    [System.Serializable]
    public class ConditionEntry
    {
        [Tooltip("要檢查的 Flag 定義檔 (ScriptableObject)。")]
        public ProgressFlagDefinition FlagDef;

        [Tooltip("期望這個 Flag 是什麼狀態？\n\nTrue (打勾) = 必須【已啟動】才算符合\nFalse (不勾) = 必須【未啟動】才算符合")]
        public bool RequiredState = true;
    }

    // ───── Inspector 設定區 ─────

    [Header("核心邏輯設定")]
    [Tooltip("多個條件之間的判斷邏輯：\n\nAll = 所有條件都必須符合 (AND)\nAny = 只要有一個條件符合即可 (OR)")]
    public LogicType Logic = LogicType.All_Conditions_Met;

    [Tooltip("【效能設定】是否每一幀 (Update) 都檢查？\n\nFalse (預設/推薦) = 只有當 Flag 變動時才檢查 (效能最好，事件驅動)\nTrue = 每一幀都檢查 (效能較差，僅用於除錯或混合了非 Flag 的動態條件)")]
    public bool CheckEveryFrame = false;

    [Header("觸發條件")]
    [Tooltip("請在此新增所有需要檢查的 Flag 條件清單。")]
    public List<ConditionEntry> Conditions = new List<ConditionEntry>();

    [Header("執行結果 (符合條件時)")]
    [Tooltip("當上述條件【符合】時，要【顯示 / 啟用】的物件。\n(當條件不符時，這些物件會被隱藏)")]
    public List<GameObject> TargetsToActivate = new List<GameObject>();

    [Tooltip("當上述條件【符合】時，要【隱藏 / 停用】的物件。\n(當條件不符時，這些物件會被顯示)")]
    public List<GameObject> TargetsToDeactivate = new List<GameObject>();

    // ───── 內部變數 ─────

    private ProgressFlagModel _model;
    private bool _isSubscribed = false; // 防止重複訂閱

    // ───── 生命周期 ─────

    private void Awake()
    {
        // 獲取 Model 引用
        if (GameStatusService.Instance != null)
        {
            _model = GameStatusService.Instance.ProgressFlags;
        }
    }

    private void OnEnable()
    {
        if (_model == null) return;

        // 1. 物件啟用時，無論如何先檢查一次狀態 (確保畫面正確)
        Evaluate();

        // 2. 根據設定決定是否訂閱事件
        UpdateSubscriptionStatus();
    }

    private void OnDisable()
    {
        UnsubscribeEvent();
    }

    // 如果你有開啟 CheckEveryFrame，就會在這裡每幀執行
    private void Update()
    {
        if (CheckEveryFrame)
        {
            Evaluate();
        }
    }

    // 當你在 Inspector 調整數值時，自動更新訂閱狀態 (僅限 Play Mode)
    private void OnValidate()
    {
        if (Application.isPlaying && gameObject.activeInHierarchy)
        {
            UpdateSubscriptionStatus();
        }
    }

    // ───── 事件處理與訂閱管理 ─────

    private void UpdateSubscriptionStatus()
    {
        if (_model == null) return;

        // 如果開啟了「每幀檢查」，就不需要訂閱事件 (避免多餘運算)
        // 如果關閉了「每幀檢查」，就必須訂閱事件 (否則不會更新)
        if (CheckEveryFrame)
        {
            UnsubscribeEvent();
        }
        else
        {
            SubscribeEvent();
        }
    }

    private void SubscribeEvent()
    {
        if (!_isSubscribed && _model != null)
        {
            _model.OnFlagChanged += OnFlagChangedHandler;
            _isSubscribed = true;
        }
    }

    private void UnsubscribeEvent()
    {
        if (_isSubscribed && _model != null)
        {
            _model.OnFlagChanged -= OnFlagChangedHandler;
            _isSubscribed = false;
        }
    }

    // 事件回調：當任何 Flag 改變時被呼叫
    private void OnFlagChangedHandler(string flagID, bool isOn)
    {
        // 優化：先過濾，確認變動的 Flag 是否跟我們清單裡的有關係
        // 如果變動的 Flag 根本不在我們的 Conditions 裡，就不用浪費時間重新運算 Evaluate
        bool isRelevant = false;
        foreach (var cond in Conditions)
        {
            if (cond.FlagDef != null && cond.FlagDef.FlagID == flagID)
            {
                isRelevant = true;
                break;
            }
        }

        if (isRelevant)
        {
            Evaluate();
        }
    }

    // ───── 核心邏輯 ─────

    /// <summary>
    /// 檢查所有條件並執行顯示/隱藏
    /// </summary>
    public void Evaluate()
    {
        // 若沒有設定條件，預設不處理或視為通過？這裡保守起見，若無條件則不動作
        if (Conditions.Count == 0) return;

        bool finalResult = (Logic == LogicType.All_Conditions_Met);

        if (Logic == LogicType.All_Conditions_Met)
        {
            // AND 模式：預設 true，只要有一個失敗就變 false
            foreach (var cond in Conditions)
            {
                if (!CheckCondition(cond))
                {
                    finalResult = false;
                    break;
                }
            }
        }
        else
        {
            // OR 模式：預設 false，只要有一個成功就變 true
            finalResult = false;
            foreach (var cond in Conditions)
            {
                if (CheckCondition(cond))
                {
                    finalResult = true;
                    break;
                }
            }
        }

        ApplyResult(finalResult);
    }

    private bool CheckCondition(ConditionEntry entry)
    {
        if (entry.FlagDef == null) return false;
        if (_model == null) return false; // 安全檢查

        // 檢查 Model 是否包含該 Flag
        bool isFlagActive = _model.Contains(entry.FlagDef.FlagID);

        // 比對「當前狀態」是否等於「期望狀態」
        // 例如：期望是 false (RequiredState=false)，實際是 false (isFlagActive=false) -> 結果為 true (符合條件)
        return isFlagActive == entry.RequiredState;
    }

    private void ApplyResult(bool passed)
    {
        // 處理「符合則顯示」的清單
        foreach (var go in TargetsToActivate)
        {
            if (go != null)
            {
                // 只有狀態真的需要改變時才呼叫 SetActive (微幅優化)
                if (go.activeSelf != passed)
                    go.SetActive(passed);
            }
        }

        // 處理「符合則隱藏」的清單
        foreach (var go in TargetsToDeactivate)
        {
            if (go != null)
            {
                bool shouldBeActive = !passed;
                if (go.activeSelf != shouldBeActive)
                    go.SetActive(shouldBeActive);
            }
        }
    }

    // 供外部測試用 (右鍵 component 可以選單執行)
    [ContextMenu("Force Evaluate Now")]
    public void ForceEvaluate() => Evaluate();
}