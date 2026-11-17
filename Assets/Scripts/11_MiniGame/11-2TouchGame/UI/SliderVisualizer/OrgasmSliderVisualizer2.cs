using UnityEngine;

/// <summary>顯示指定女主角的 Orgasm，上下限可分別沿用 Model 或自訂。</summary>
public class OrgasmSliderVisualizer2 : HeroineValueSliderVisualizer
{
    [SerializeField] private Color fillColor = Color.white;

    protected override Color FillColor => fillColor;
    protected override float ModelMinimum => HeroineStatusModel.OrgasmMin;
    protected override float ModelMaximum => Model != null ? Model.OrgasmMax : HeroineStatusModel.OrgasmLimit;
    protected override float CurrentValue => Model.Orgasm;
    protected override void Subscribe(HeroineStatusModel model) => model.OnOrgasmChanged += HandleOrgasmChanged;
    protected override void Unsubscribe(HeroineStatusModel model) => model.OnOrgasmChanged -= HandleOrgasmChanged;

    private void HandleOrgasmChanged(int value) => HandleValueChanged(value);

    public void SetOrgasm(int value)
    {
        if (TryGetModel()) Model.SetOrgasm(value);
    }

    public void AddOrgasm(int amount)
    {
        if (TryGetModel()) Model.AddOrgasm(amount);
    }
}
