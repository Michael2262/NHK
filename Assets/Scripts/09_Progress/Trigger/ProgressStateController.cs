using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 根據進度 (Flag 或 Value) 決定場景物件狀態的控制器。
/// 職責：場景單例，作為該場景所有進度視覺表現的最終裁決者。
/// 
/// 【v3 改動】
/// - 不再實作 ISceneReadyHandler（初始化改由 Task_InitProgressState 在 Coordinator 管線中觸發）
/// - Evaluate() 本來就是 public，Task wrapper 直接呼叫即可
/// - OnEnable/OnDisable 的事件訂閱等持續運作邏輯完全不變
/// </summary>
public class ProgressStateController : MonoBehaviour
{
    public enum LogicType { All_Conditions_Met, Any_Condition_Met }
    public enum ComparisonType { Equal, GreaterThan, LessThan, NotEqual, GreaterOrEqual, LessOrEqual }

    [System.Serializable]
    public class ConditionEntry
    {
        [Tooltip("放入 ProgressFlagDefinition 或 ProgressValueDefinition")]
        public ProgressBaseDefinition Definition;

        [Header("布林檢查 (若是 Flag)")]
        public bool RequiredState = true;

        [Header("數值比較 (若是 Value)")]
        public ComparisonType Comparison = ComparisonType.Equal;
        public int TargetValue;
    }

    [System.Serializable]
    public class ObjectState
    {
        public string StateName = "新狀態";
        [Tooltip("當多個狀態同時符合時，優先級高 (數字大) 的會勝出")]
        public int Priority = 0;

        public LogicType Logic = LogicType.All_Conditions_Met;
        public List<ConditionEntry> Conditions = new List<ConditionEntry>();

        [Header("要開啟的物件")]
        public List<GameObject> ToActivate;
        [Header("要關閉的物件")]
        public List<GameObject> ToDeactivate;
    }

    [Header("狀態設定")]
    public List<ObjectState> States = new List<ObjectState>();

    private ProgressFlagModel _model;

    private void Awake()
    {
        if (GameStatusService.Instance != null)
            _model = GameStatusService.Instance.ProgressFlags;
    }

    private void OnEnable()
    {
        if (_model == null) return;

        _model.OnFlagChanged += HandleFlagChanged;
        _model.OnVariableChanged += HandleVariableChanged;
    }

    private void OnDisable()
    {
        if (_model == null) return;
        _model.OnFlagChanged -= HandleFlagChanged;
        _model.OnVariableChanged -= HandleVariableChanged;
    }

    private void HandleFlagChanged(string id, bool val) => Evaluate();
    private void HandleVariableChanged(string id, int val) => Evaluate();

    /// <summary>
    /// 評估所有狀態條件並套用結果。
    /// 由 Task_InitProgressState 在 Coordinator 管線中觸發初次評估，
    /// 之後由事件訂閱（Flag/Variable 變動）自動持續觸發。
    /// </summary>
    public void Evaluate()
    {
        if (_model == null || States.Count == 0) return;

        List<ObjectState> validStates = new List<ObjectState>();
        foreach (var state in States)
        {
            if (IsStateMet(state))
            {
                validStates.Add(state);
            }
        }

        validStates.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        foreach (var state in validStates)
        {
            ApplyState(state);
        }
    }

    private bool IsStateMet(ObjectState state)
    {
        if (state.Conditions.Count == 0) return true;

        foreach (var cond in state.Conditions)
        {
            bool isMet = CheckCondition(cond);
            if (state.Logic == LogicType.All_Conditions_Met && !isMet) return false;
            if (state.Logic == LogicType.Any_Condition_Met && isMet) return true;
        }
        return state.Logic == LogicType.All_Conditions_Met;
    }

    private bool CheckCondition(ConditionEntry entry)
    {
        if (entry.Definition == null) return false;

        if (entry.Definition is ProgressFlagDefinition)
        {
            return _model.Contains(entry.Definition.FlagID) == entry.RequiredState;
        }
        else if (entry.Definition is ProgressValueDefinition)
        {
            int currentVal = _model.GetValue(entry.Definition.FlagID);
            return entry.Comparison switch
            {
                ComparisonType.Equal => currentVal == entry.TargetValue,
                ComparisonType.GreaterThan => currentVal > entry.TargetValue,
                ComparisonType.LessThan => currentVal < entry.TargetValue,
                ComparisonType.NotEqual => currentVal != entry.TargetValue,
                ComparisonType.GreaterOrEqual => currentVal >= entry.TargetValue,
                ComparisonType.LessOrEqual => currentVal <= entry.TargetValue,
                _ => false
            };
        }
        return false;
    }

    private void ApplyState(ObjectState state)
    {
        foreach (var go in state.ToActivate)
            if (go != null && !go.activeSelf) go.SetActive(true);

        foreach (var go in state.ToDeactivate)
            if (go != null && go.activeSelf) go.SetActive(false);
    }
}