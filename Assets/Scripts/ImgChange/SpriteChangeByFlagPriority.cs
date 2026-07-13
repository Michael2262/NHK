using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 依「優先度規則清單」換圖：
/// 監聽 ProgressFlagModel 的 Flag / Variable 異動，
/// 由上往下逐條檢查規則，第一條符合的規則決定要換成哪張 Sprite（排越上面優先度越高）。
/// 若所有規則都不符合，則維持目前圖片不做任何變更。
/// 支援 UI Image 與 SpriteRenderer（擇一自動偵測，也可手動指定）。
/// </summary>
public class SpriteChangeByFlagPriority : MonoBehaviour
{
    public enum FlagCondition
    {
        IsOn,   // 該 Flag 為 On（Contains 為 true，數值 > 0 也算 On）
        IsOff   // 該 Flag 為 Off
    }

    [System.Serializable]
    public class FlagRule
    {
        [Tooltip("要監聽的 Flag（可放 FlagDefinition 或 ValueDefinition，數值 > 0 視為 On）")]
        public ProgressBaseDefinition Flag;

        [Tooltip("Flag 要處於什麼狀態才算符合")]
        public FlagCondition Condition = FlagCondition.IsOn;

        [Tooltip("符合時要換上的 Sprite")]
        public Sprite Sprite;
    }

    [Header("換圖目標（都不填會自動抓同物件上的 Image / SpriteRenderer）")]
    [SerializeField] private Image targetImage;
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("規則清單（排越上面優先度越高，取第一條符合者；都不符合則不改圖）")]
    [SerializeField] private List<FlagRule> rules = new List<FlagRule>();

    private ProgressFlagModel _model;

    private void Awake()
    {
        if (targetImage == null && targetRenderer == null)
        {
            targetImage = GetComponent<Image>();
            if (targetImage == null)
                targetRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetImage == null && targetRenderer == null)
            Debug.LogWarning($"[SpriteChangeByFlagPriority] {name} 找不到 Image 或 SpriteRenderer，無法換圖。", this);
    }

    private void OnEnable()
    {
        if (_model == null && GameStatusService.Instance != null)
            _model = GameStatusService.Instance.ProgressFlags;

        if (_model == null)
        {
            Debug.LogWarning($"[SpriteChangeByFlagPriority] {name} 取不到 ProgressFlagModel，規則不會生效。", this);
            return;
        }

        _model.OnFlagChanged += OnFlagChangedHandler;
        _model.OnVariableChanged += OnVariableChangedHandler;

        Evaluate();
    }

    private void OnDisable()
    {
        if (_model != null)
        {
            _model.OnFlagChanged -= OnFlagChangedHandler;
            _model.OnVariableChanged -= OnVariableChangedHandler;
        }
    }

    private void OnFlagChangedHandler(string id, bool value) => OnAnyChanged(id);
    private void OnVariableChangedHandler(string id, int value) => OnAnyChanged(id);

    /// <summary> 只有異動的 ID 出現在規則清單中才重新評估，避免無關 Flag 造成多餘運算 </summary>
    private void OnAnyChanged(string changedID)
    {
        foreach (var rule in rules)
        {
            if (rule.Flag != null && rule.Flag.FlagID == changedID)
            {
                Evaluate();
                return;
            }
        }
    }

    /// <summary> 由上往下找第一條符合的規則並套用其 Sprite；都不符合則維持現狀 </summary>
    [ContextMenu("Force Evaluate")]
    public void Evaluate()
    {
        if (_model == null) return;

        foreach (var rule in rules)
        {
            if (rule.Flag == null || rule.Sprite == null) continue;

            bool isOn = _model.Contains(rule.Flag.FlagID);
            bool met = (rule.Condition == FlagCondition.IsOn) ? isOn : !isOn;

            if (met)
            {
                ApplySprite(rule.Sprite);
                return;
            }
        }
        // 所有規則都不符合 → 不改圖片
    }

    private void ApplySprite(Sprite sprite)
    {
        if (targetImage != null)
        {
            if (targetImage.sprite != sprite) targetImage.sprite = sprite;
        }
        else if (targetRenderer != null)
        {
            if (targetRenderer.sprite != sprite) targetRenderer.sprite = sprite;
        }
    }
}
