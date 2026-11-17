using UnityEngine;
using UnityEngine.Events;

/// <summary>顯示指定女主角的 Frustration，上下限可分別沿用 Model 或自訂。</summary>
public class FrustrationSliderVisualizer : HeroineValueSliderVisualizer
{
    [SerializeField] private Color fillColor = new Color(1f, 0.4f, 0.35f, 1f);

    [Header("快要不滿通知")]
    [Tooltip("填色抵達顯示上限減 2 時觸發，最低取顯示下限。降至門檻以下後可再觸發。")]
    [SerializeField] private UnityEvent onReachedNearFull = new UnityEvent();
    public UnityEvent OnReachedNearFull => onReachedNearFull;

    private bool nearFullArmed;
    private float NearFullThreshold => Mathf.Max(AppliedMinimum, AppliedMaximum
        - (HeroineStatusModel.FrustrationMax - HeroineStatusModel.FrustrationNearFullThreshold));

    protected override Color FillColor => fillColor;
    protected override float ModelMinimum => HeroineStatusModel.FrustrationMin;
    protected override float ModelMaximum => HeroineStatusModel.FrustrationMax;
    protected override float CurrentValue => Model.Frustration;
    protected override void Subscribe(HeroineStatusModel model) => model.OnFrustrationChanged += HandleValueChanged;
    protected override void Unsubscribe(HeroineStatusModel model) => model.OnFrustrationChanged -= HandleValueChanged;

    public void SetFrustration(float value)
    {
        if (TryGetModel()) Model.SetFrustration(value);
    }

    public void AddFrustration(float amount)
    {
        if (TryGetModel()) Model.AddFrustration(amount);
    }

    // 保留既有 UnityEvent 的整數參數。
    public void SetFrustration(int value) => SetFrustration((float)value);
    public void AddFrustration(int amount) => AddFrustration((float)amount);

    protected override void UpdateAdditionalThreshold(float value, bool synchronize)
    {
        if (synchronize || value < NearFullThreshold)
            nearFullArmed = value < NearFullThreshold;
    }

    protected override void NotifyAdditionalThreshold(float value)
    {
        if (!nearFullArmed || value < NearFullThreshold || DisplayedValue < NearFullThreshold) return;
        nearFullArmed = false;
        onReachedNearFull.Invoke();
    }
}
