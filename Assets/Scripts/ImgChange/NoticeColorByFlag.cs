using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoticeColorByFlag : MonoBehaviour
{
    public enum LogicType { All_Conditions_Met, Any_Condition_Met }
    public enum ComparisonType { Equal, GreaterThan, LessThan, NotEqual, GreaterOrEqual, LessOrEqual }

    [System.Serializable]
    public class ConditionEntry
    {
        public ProgressBaseDefinition Definition;

        [Header("布林 (Flag)")]
        public bool RequiredState = true;

        [Header("數值 (Value)")]
        public ComparisonType Comparison = ComparisonType.Equal;
        public int TargetValue;
    }

    [Header("目標 Image")]
    public Image TargetImage;

    [Header("條件設定")]
    public LogicType Logic = LogicType.All_Conditions_Met;
    public List<ConditionEntry> Conditions = new List<ConditionEntry>();

    [Header("Notice 顏色")]
    public Color NoticeColor = new Color(0.95f, 0.96f, 0.30f, 1f);

    private ProgressFlagModel _model;
    private Color _originalColor;

    private void Awake()
    {
        if (TargetImage == null) TargetImage = GetComponent<Image>();
        if (TargetImage != null) _originalColor = TargetImage.color;

        if (GameStatusService.Instance != null)
            _model = GameStatusService.Instance.ProgressFlags;
    }

    private void OnEnable()
    {
        if (_model == null) return;
        _model.OnFlagChanged += OnFlagChanged;
        _model.OnVariableChanged += OnVariableChanged;
        Evaluate();
    }

    private void OnDisable()
    {
        if (_model != null)
        {
            _model.OnFlagChanged -= OnFlagChanged;
            _model.OnVariableChanged -= OnVariableChanged;
        }
    }

    private void OnFlagChanged(string id, bool val) => TryEvaluate(id);
    private void OnVariableChanged(string id, int val) => TryEvaluate(id);

    private void TryEvaluate(string changedID)
    {
        foreach (var c in Conditions)
        {
            if (c.Definition != null && c.Definition.FlagID == changedID)
            {
                Evaluate();
                return;
            }
        }
    }

    [ContextMenu("Force Evaluate")]
    public void Evaluate()
    {
        if (Conditions.Count == 0 || _model == null || TargetImage == null) return;

        bool result = (Logic == LogicType.All_Conditions_Met);

        foreach (var c in Conditions)
        {
            bool met = CheckCondition(c);
            if (Logic == LogicType.All_Conditions_Met)
            {
                if (!met) { result = false; break; }
            }
            else
            {
                if (met) { result = true; break; }
            }
        }

        TargetImage.color = result ? NoticeColor : _originalColor;
    }

    private bool CheckCondition(ConditionEntry entry)
    {
        if (entry.Definition == null) return false;

        if (entry.Definition is ProgressFlagDefinition)
            return _model.Contains(entry.Definition.FlagID) == entry.RequiredState;

        if (entry.Definition is ProgressValueDefinition)
        {
            int val = _model.GetValue(entry.Definition.FlagID);
            return entry.Comparison switch
            {
                ComparisonType.Equal => val == entry.TargetValue,
                ComparisonType.GreaterThan => val > entry.TargetValue,
                ComparisonType.LessThan => val < entry.TargetValue,
                ComparisonType.NotEqual => val != entry.TargetValue,
                ComparisonType.GreaterOrEqual => val >= entry.TargetValue,
                ComparisonType.LessOrEqual => val <= entry.TargetValue,
                _ => false
            };
        }
        return false;
    }
}