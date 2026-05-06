// 檔案：HeroineDebugUI.cs (新檔案)
using UnityEngine;
using TMPro; // 如果您使用 TextMeshPro

/// <summary>
/// 職責：顯示「單一」女主角的除錯資訊，並訂閱其數據變更事件。
/// </summary>
public class HeroineDebugUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textName;
    [SerializeField] private TextMeshProUGUI textAttack;
    [SerializeField] private TextMeshProUGUI textDefense;

    [SerializeField] private TextMeshProUGUI textExcitement;
    [SerializeField] private TextMeshProUGUI textOrgasm;


    private HeroineStatusModel _model; // 對應的數據模型

    /// <summary>
    /// 從外部初始化，告訴這個 UI 要監聽哪一位女主角。
    /// </summary>
    public void Initialize(HeroineStatusModel model)
    {
        _model = model;

        // 訂閱事件：當數值改變時，自動刷新 UI
        _model.OnAttackChanged += (delta) => RefreshUI();
        _model.OnDefenseChanged += (delta) => RefreshUI();

        _model.OnExcitementChanged += (delta) => RefreshUI(); // 經驗值/等級變化時刷新
        //_model.OnExcitementLevelChanged += (delta) => RefreshUI(); // 等級變化時刷新
        _model.OnOrgasmChanged += (delta) => RefreshUI(); // 快感值變化時刷新


        // 立即刷新一次，顯示初始數值
        RefreshUI();
    }

    /// <summary>
    /// 更新所有 UI 文本。
    /// </summary>
    private void RefreshUI()
    {
        if (_model == null) return;

        textName.text = _model.Name; // 顯示角色名字
        textAttack.text = $"攻擊力: {_model.BaseAttack}";
        textDefense.text = $"防禦力: {_model.BaseDefense}";

        // 顯示格式：興奮度 Lv.1 (150/200 Exp)
        //textExcitement.text = $"興奮度: Lv.{_model.BaseExcitementLevel} ({_model.BaseExcitementExp} Exp)";
        // 顯示格式：快感值: 50 / 100
        textOrgasm.text = $"快感值: {_model.Orgasm} / {_model.OrgasmMax}";

    }

    /// <summary>
    /// 當物件被銷毀時，務必取消訂閱，防止記憶體洩漏。
    /// </summary>
    private void OnDestroy()
    {
        if (_model != null)
        {
            _model.OnAttackChanged -= (delta) => RefreshUI();
            _model.OnDefenseChanged -= (delta) => RefreshUI();

            _model.OnExcitementChanged -= (delta) => RefreshUI();
            //_model.OnExcitementLevelChanged -= (delta) => RefreshUI();
            _model.OnOrgasmChanged -= (delta) => RefreshUI();

        }
    }
}