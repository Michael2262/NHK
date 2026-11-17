using UnityEngine;

/// <summary>顯示指定女主角的 Awake，上下限可分別沿用 Model 或自訂。</summary>
public class AwakeSliderVisualizer : HeroineValueSliderVisualizer
{
    [SerializeField] private Color fillColor = Color.white;

    protected override Color FillColor => fillColor;
    protected override float ModelMinimum => HeroineStatusModel.AwakeMin;
    protected override float ModelMaximum => HeroineStatusModel.AwakeMax;
    protected override float CurrentValue => Model.Awake;
    protected override void Subscribe(HeroineStatusModel model) => model.OnAwakeChanged += HandleValueChanged;
    protected override void Unsubscribe(HeroineStatusModel model) => model.OnAwakeChanged -= HandleValueChanged;

    public void SetAwake(float value)
    {
        if (TryGetModel()) Model.SetAwake(value);
    }

    public void AddAwake(float amount)
    {
        if (TryGetModel()) Model.AddAwake(amount);
    }
}
