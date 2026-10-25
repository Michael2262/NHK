using UnityEngine;

/// <summary>
/// 條件判斷腳本：
/// 檢查指定的 HeroineID 的 獨立 Excitement 是否達到明確設定的新數值門檻。
/// </summary>
public class ExcitementConditionChecker : MonoBehaviour, IConditionChecker
{
    [Header("條件設置")]
    [Tooltip("要檢查的女主角 ID (必須與 HeroineStatusConfig 中的 ID 一致)")]
    [SerializeField] private string heroineID;

    [Tooltip("興奮度等級必須 *高於或等於* 這個值 (ExcitementLevel >= Threshold)")]
    [SerializeField, HideInInspector] private int excitementLevelThreshold = 0;

    [Header("新版數值條件")]
    [SerializeField, InspectorName("使用新數值門檻"), Tooltip("必須手動啟用，避免將舊等級誤當成新數值。")]
    private bool useValueThreshold;
    [SerializeField, InspectorName("需要興奮值"), Range(0, HeroineStatusModel.ExcitementMax)] private int excitementValueThreshold;
    private bool _warned;

    /// <summary>已啟用新門檻且角色的興奮值達標時回傳 true。</summary>
    public bool CheckCondition()
    {
        if (!useValueThreshold)
        {
            if (!_warned) Debug.LogWarning("[ExcitementConditionChecker] 舊等級條件已停用。請勾選使用新數值門檻並重新設定門檻。", this);
            _warned = true;
            return false;
        }
        var heroine = GameStatusService.Instance?.GetHeroine(heroineID);
        return heroine != null && heroine.Excitement >= excitementValueThreshold;
    }
}
