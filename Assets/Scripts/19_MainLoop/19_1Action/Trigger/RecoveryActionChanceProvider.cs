using UnityEngine;

/// <summary>
/// 白天 2「外界 / 復歸行動」成功率計算器。
///
/// 職責：
/// - 根據 ProtagonistStatusModel 的壓力、生活力、社會恐懼與長期統計計算成功率。
/// - 將成功率代入 ActionOverlayTrigger。
///
/// 不負責：
/// - 跑條演出。
/// - 顯示成功 / 失敗。
/// - 觸發成功 / 失敗後續 UnityEvent。
/// </summary>
public class RecoveryActionChanceProvider : MonoBehaviour
{
    public enum RecoveryActionType
    {
        Message,        // 處理訊息
        GoOutside,      // 出門一下
        JobPreparation, // 求職準備
        EscapeReality,  // 逃避現實：通常不需要成功/失敗，若使用則幾乎必定成功
        Custom          // 自訂基礎成功率與修正
    }

    [Header("行動類型")]
    public RecoveryActionType actionType = RecoveryActionType.GoOutside;

    [Tooltip("若指定，ApplyChanceToTrigger() 會把成功率寫入此 Trigger。未指定時會抓同物件上的 ActionOverlayTrigger。")]
    public ActionOverlayTrigger targetTrigger;

    [Header("通用限制")]
    [Range(0f, 1f)] public float minChance = 0.1f;
    [Range(0f, 1f)] public float maxChance = 0.9f;

    [Header("Custom 用設定")]
    [Range(0f, 1f)] public float customBaseChance = 0.5f;
    [Tooltip("Custom 模式：生活力每 10 點提供多少成功率加成。")]
    public float customLifePowerPer10Bonus = 0.02f;
    [Tooltip("Custom 模式：壓力每 10 點造成多少成功率懲罰。")]
    public float customStressPer10Penalty = 0.02f;
    [Tooltip("Custom 模式：社會恐懼每 10 點造成多少成功率懲罰。")]
    public float customSocialFearPer10Penalty = 0.02f;

    [Header("Debug")]
    [SerializeField, Range(0f, 1f)] private float lastCalculatedChance;
    [SerializeField] private string lastDebugSummary;

    private void Reset()
    {
        targetTrigger = GetComponent<ActionOverlayTrigger>();
    }

    /// <summary>
    /// 計算目前成功率。回傳值為 0～1。
    /// </summary>
    public float CalculateSuccessChance()
    {
        ProtagonistStatusModel protagonist = GameStatusService.Instance?.Protagonist;
        if (protagonist == null)
        {
            Debug.LogWarning($"{nameof(RecoveryActionChanceProvider)}: 找不到 GameStatusService.Instance.Protagonist，使用 customBaseChance 作為 fallback。", this);
            lastCalculatedChance = Mathf.Clamp(customBaseChance, minChance, maxChance);
            lastDebugSummary = "Fallback: No protagonist model.";
            return lastCalculatedChance;
        }

        float chance;
        switch (actionType)
        {
            case RecoveryActionType.Message:
                chance = CalculateMessageChance(protagonist);
                break;

            case RecoveryActionType.GoOutside:
                chance = CalculateGoOutsideChance(protagonist);
                break;

            case RecoveryActionType.JobPreparation:
                chance = CalculateJobPreparationChance(protagonist);
                break;

            case RecoveryActionType.EscapeReality:
                // 逃避現實的定位是「穩定降壓但有長期代價」，通常不走成功/失敗。
                // 若真的啟用 outcome，給高成功率。
                chance = 0.95f;
                break;

            case RecoveryActionType.Custom:
            default:
                chance = CalculateCustomChance(protagonist);
                break;
        }

        chance = Mathf.Clamp(chance, minChance, maxChance);
        lastCalculatedChance = chance;
        lastDebugSummary = BuildDebugSummary(protagonist, chance);
        return chance;
    }

    /// <summary>
    /// 將計算後的成功率寫入 targetTrigger。
    /// 可掛到 Button.OnClick，放在 ActionOverlayTrigger.Execute() 前。
    /// </summary>
    public void ApplyChanceToTrigger()
    {
        ActionOverlayTrigger trigger = targetTrigger != null ? targetTrigger : GetComponent<ActionOverlayTrigger>();
        if (trigger == null)
        {
            Debug.LogWarning($"{nameof(RecoveryActionChanceProvider)}: 找不到 ActionOverlayTrigger，無法代入成功率。", this);
            return;
        }

        trigger.SetSuccessChance(CalculateSuccessChance());
    }

    /// <summary>
    /// 提供給 ActionOverlayTrigger 或其他腳本直接指定目標。
    /// </summary>
    public void ApplyChanceToTrigger(ActionOverlayTrigger trigger)
    {
        if (trigger == null)
        {
            Debug.LogWarning($"{nameof(RecoveryActionChanceProvider)}: 傳入的 ActionOverlayTrigger 為 null。", this);
            return;
        }

        trigger.SetSuccessChance(CalculateSuccessChance());
    }

    private float CalculateMessageChance(ProtagonistStatusModel p)
    {
        float chance = 0.65f;

        if (p.Stress >= 70) chance -= 0.15f;
        if (p.Stress >= 85) chance -= 0.15f;

        if (p.SocialFear >= 70) chance -= 0.10f;
        if (p.SocialFear >= 85) chance -= 0.10f;

        if (p.ConsecutiveEscapeDays >= 2) chance -= 0.10f;

        if (p.LifePower >= 50) chance += 0.05f;

        return chance;
    }

    private float CalculateGoOutsideChance(ProtagonistStatusModel p)
    {
        float chance = 0.50f;

        if (p.LifePower >= 50) chance += 0.10f;
        if (p.LifePower >= 70) chance += 0.10f;

        if (p.SocialFear >= 70) chance -= 0.20f;
        if (p.SocialFear >= 85) chance -= 0.15f;

        if (p.Stress >= 70) chance -= 0.15f;
        if (p.Stress >= 85) chance -= 0.15f;

        if (p.OutsideSuccessCount >= 2) chance += 0.10f;
        if (p.OutsideFailCount >= 2) chance -= 0.10f;

        return chance;
    }

    private float CalculateJobPreparationChance(ProtagonistStatusModel p)
    {
        float chance = 0.55f;

        if (p.LifePower >= 50) chance += 0.10f;
        if (p.LifePower >= 70) chance += 0.10f;

        if (p.Stress >= 70) chance -= 0.15f;
        if (p.Stress >= 85) chance -= 0.15f;

        if (p.SocialFear >= 75) chance -= 0.15f;

        if (p.DaysIgnoredReality >= 3) chance -= 0.10f;

        return chance;
    }

    private float CalculateCustomChance(ProtagonistStatusModel p)
    {
        float chance = customBaseChance;
        chance += (p.LifePower / 10f) * customLifePowerPer10Bonus;
        chance -= (p.Stress / 10f) * customStressPer10Penalty;
        chance -= (p.SocialFear / 10f) * customSocialFearPer10Penalty;
        return chance;
    }

    private string BuildDebugSummary(ProtagonistStatusModel p, float chance)
    {
        return $"{actionType}: chance={chance:P0}, Stress={p.Stress}, LifePower={p.LifePower}, SocialFear={p.SocialFear}, " +
               $"Dependency={p.Dependency}, OutsideSuccess={p.OutsideSuccessCount}, OutsideFail={p.OutsideFailCount}, " +
               $"EscapeDays={p.ConsecutiveEscapeDays}, IgnoredReality={p.DaysIgnoredReality}";
    }

    [ContextMenu("Debug Calculate Chance")]
    private void DebugCalculateChance()
    {
        float chance = CalculateSuccessChance();
        Debug.Log(lastDebugSummary + $" ({chance})", this);
    }
}
