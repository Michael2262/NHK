using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageChangeByFlag : MonoBehaviour
{
    public enum LogicType { All_Conditions_Met, Any_Condition_Met }
    // 為了保持腳本獨立，我們在此重新定義比較類型，或者你也可以引用 ProgressStateController 的
    public enum ComparisonType { Equal, GreaterThan, LessThan, NotEqual, GreaterOrEqual, LessOrEqual }

    [System.Serializable]
    public class ConditionEntry
    {
        [Tooltip("可放入 FlagDefinition (布林) 或 ValueDefinition (數值)")]
        public ProgressBaseDefinition Definition;

        [Header("若是布林 (Flag)")]
        public bool RequiredState = true;

        [Header("若是數值 (Value)")]
        public ComparisonType Comparison = ComparisonType.Equal;
        public int TargetValue;
    }

    [Header("目標 UI 圖片")]
    public Image TargetImage;

    [Header("核心邏輯設定")]
    public LogicType Logic = LogicType.All_Conditions_Met;
    public List<ConditionEntry> Conditions = new List<ConditionEntry>();

    [Header("符合條件時 (True)")]
    public Sprite SpriteIfMet;
    public bool ChangeColorIfMet = false;
    public Color ColorIfMet = Color.white;
    public bool ChangeRotationIfMet = false;
    public float RotationIfMet = 0f;

    [Header("不符合條件時 (False)")]
    public Sprite SpriteIfNotMet;
    public bool ChangeColorIfNotMet = false;
    public Color ColorIfNotMet = Color.white;
    public bool ChangeRotationIfNotMet = false;
    public float RotationIfNotMet = 0f;

    private ProgressFlagModel _model;
    private Sprite _initialSprite;
    private Color _initialColor;
    private float _initialRotationZ;

    private void Awake()
    {
        if (TargetImage == null) TargetImage = GetComponent<Image>();
        if (TargetImage != null)
        {
            _initialSprite = TargetImage.sprite;
            _initialColor = TargetImage.color;
            _initialRotationZ = TargetImage.rectTransform.localEulerAngles.z;
        }

        if (GameStatusService.Instance != null)
            _model = GameStatusService.Instance.ProgressFlags;
    }

    private void OnEnable()
    {
        if (_model == null) return;

        // 同時訂閱布林與數值變動
        _model.OnFlagChanged += OnAnyChangedHandler;
        _model.OnVariableChanged += OnVariableChangedHandler;

        Evaluate();
    }

    private void OnDisable()
    {
        if (_model != null)
        {
            _model.OnFlagChanged -= OnAnyChangedHandler;
            _model.OnVariableChanged -= OnVariableChangedHandler;
        }
    }

    // 當布林 Flag 改變
    private void OnAnyChangedHandler(string id, bool val) => CheckRelevanceAndEvaluate(id);

    // 當數值 Value 改變
    private void OnVariableChangedHandler(string id, int val) => CheckRelevanceAndEvaluate(id);

    private void CheckRelevanceAndEvaluate(string changedID)
    {
        bool isRelevant = false;
        foreach (var cond in Conditions)
        {
            if (cond.Definition != null && cond.Definition.FlagID == changedID)
            {
                isRelevant = true;
                break;
            }
        }
        if (isRelevant) Evaluate();
    }

    [ContextMenu("Force Evaluate")]
    public void Evaluate()
    {
        if (Conditions.Count == 0 || _model == null || TargetImage == null) return;

        bool finalResult = (Logic == LogicType.All_Conditions_Met);

        foreach (var cond in Conditions)
        {
            bool isMet = CheckCondition(cond);
            if (Logic == LogicType.All_Conditions_Met)
            {
                if (!isMet) { finalResult = false; break; }
            }
            else // Any_Condition_Met
            {
                if (isMet) { finalResult = true; break; }
            }
        }

        ApplyResult(finalResult);
    }

    private bool CheckCondition(ConditionEntry entry)
    {
        if (entry.Definition == null) return false;

        // 邏輯 1: 處理布林 Flag
        if (entry.Definition is ProgressFlagDefinition)
        {
            return _model.Contains(entry.Definition.FlagID) == entry.RequiredState;
        }

        // 邏輯 2: 處理數值 Value
        if (entry.Definition is ProgressValueDefinition)
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

    private void ApplyResult(bool passed)
    {
        if (TargetImage == null) return;

        // 圖片切換
        Sprite nextSprite = passed ? SpriteIfMet : SpriteIfNotMet;
        TargetImage.sprite = nextSprite != null ? nextSprite : _initialSprite;

        // 顏色切換
        bool changeColor = passed ? ChangeColorIfMet : ChangeColorIfNotMet;
        TargetImage.color = changeColor ? (passed ? ColorIfMet : ColorIfNotMet) : _initialColor;

        // 旋轉切換
        bool changeRot = passed ? ChangeRotationIfMet : ChangeRotationIfNotMet;
        float targetZ = changeRot ? (passed ? RotationIfMet : RotationIfNotMet) : _initialRotationZ;

        Vector3 rot = TargetImage.rectTransform.localEulerAngles;
        if (!Mathf.Approximately(rot.z, targetZ))
        {
            rot.z = targetZ;
            TargetImage.rectTransform.localEulerAngles = rot;
        }
    }
}