using UnityEngine;

/// <summary>
/// 「嘗試出門」成功率計算器。
///
/// 公式：
///   基礎成功率 = 生活力 × 1.5 + 社會性 + 額外成功率  （視為百分比）
///   先扣除「壓力平減」＝ 壓力 × X（X 預設 1）
///   再扣除「重複點擊失敗率」（每次成功後累加，場景切換時重置）
///   最後套壓力懲罰：壓力 > 50 減半；> 80 除以 3
///   （以上各項都會被最後的壓力懲罰一併縮放）
///
/// 顯示用成功率則改扣「造假重複失敗率」，讓 chanceDisplay / Dialogue 變數
/// 看到的數字可以與實際擲骰不同（與 HeroineActionChanceProvider 相同機制）。
///
/// 最終結果會被 minChance / maxChance 夾住。
/// </summary>
public class GoOutsideChanceProvider : ActionChanceProvider
{
    [Header("嘗試出門 — 公式參數")]
    [Tooltip("生活力的權重倍率。預設 1.5。")]
    public float lifePowerWeight = 1.5f;

    [Tooltip("社會性的權重倍率。預設 1.0。")]
    public float socialityWeight = 1.0f;

    [Tooltip("壓力平減權重 X：成功率先扣除「壓力 × X」（百分比），" +
             "在重複點擊失敗率與壓力減半/除三之前套用。預設 1。")]
    public float stressPenaltyWeight = 1f;

    [Tooltip("壓力超過此值時，成功率減半。")]
    public int stressHalfThreshold = 50;

    [Tooltip("壓力超過此值時，成功率除以 3（優先於減半）。")]
    public int stressThirdThreshold = 80;

    [Tooltip("額外成功率（百分比，可為負）。直接與生活力、社會性加總進基礎成功率，" +
             "因此同樣會被壓力懲罰與重複點擊失敗率影響。")]
    public float extraSuccessRate = 0f;

    [Header("重複點擊失敗率")]
    [Tooltip("每次操作成功後，額外累加的失敗率（百分比）。")]
    [Range(0f, 100f)]
    public float repeatPenaltyPerSuccess = 10f;

    [Tooltip("重複失敗率的上限（百分比）。")]
    [Range(0f, 100f)]
    public float repeatPenaltyMax = 50f;

    [SerializeField, Range(0f, 100f)]
    private float currentRepeatPenalty = 0f;

    [Header("造假重複失敗率（僅影響顯示）")]
    [Tooltip("顯示用的『假』重複失敗率：每次成功後額外累加的造假失敗率（百分比）。\n" +
             "只會影響 chanceDisplay / Dialogue 變數上看到的數字，實際擲骰仍用上面真實的 repeatPenaltyPerSuccess。")]
    [Range(0f, 100f)]
    public float fakeRepeatPenaltyPerSuccess = 10f;

    [Tooltip("造假重複失敗率的上限（百分比）。僅影響顯示。")]
    [Range(0f, 100f)]
    public float fakeRepeatPenaltyMax = 50f;

    [SerializeField, Range(0f, 100f)]
    private float currentFakeRepeatPenalty = 0f;

    protected override float CalculateChance(ProtagonistStatusModel p)
    {
        // 實際擲骰：使用「真實」的重複失敗率。
        return ComputeChance(p, currentRepeatPenalty);
    }

    /// <summary>
    /// 顯示用成功率：除了重複失敗率改用「造假值」外，其餘與真實計算完全相同。
    /// </summary>
    protected override float CalculateDisplayChance(ProtagonistStatusModel p)
    {
        return ComputeChance(p, currentFakeRepeatPenalty);
    }

    /// <summary>
    /// 成功率公式的共用核心。差別只在傳入的重複失敗率（真實 or 造假）。
    /// 順序：基礎值（生活力＋社會性＋額外成功率）→ 扣壓力平減（壓力×X）→ 扣重複失敗率 → 壓力懲罰（減半/除三），
    /// 因此額外成功率、壓力平減與重複失敗率都會被最後的壓力懲罰一併縮放。
    /// </summary>
    private float ComputeChance(ProtagonistStatusModel p, float repeatPenalty)
    {
        // 基礎成功率（百分比轉 0~1）
        float chance = (p.LifePower * lifePowerWeight + p.Sociality * socialityWeight + extraSuccessRate) / 100f;

        // 壓力平減：扣除「壓力 × X」（百分比轉 0~1），在重複失敗率與壓力懲罰之前
        chance -= p.Stress * stressPenaltyWeight / 100f;

        // 重複點擊失敗率：在壓力懲罰前先平減（百分比轉 0~1）
        chance -= repeatPenalty / 100f;

        // 壓力懲罰：> 80 優先判定（除以 3），否則 > 50 減半
        if (p.Stress > stressThirdThreshold)
            chance /= 3f;
        else if (p.Stress > stressHalfThreshold)
            chance /= 2f;

        return chance;
    }

    protected override float GetFallbackChance() => 0.3f;

    protected override string BuildDebugSummary(ProtagonistStatusModel p, float chance)
    {
        float raw = (p.LifePower * lifePowerWeight + p.Sociality * socialityWeight + extraSuccessRate) / 100f;
        return $"GoOutside: raw={raw:P0}, final={chance:P0}, " +
               $"LifePower={p.LifePower}, Sociality={p.Sociality}, Extra={extraSuccessRate}%, " +
               $"Stress={p.Stress} (平減 {p.Stress * stressPenaltyWeight}%), " +
               $"RepeatPenalty(真實)={currentRepeatPenalty}%, RepeatPenalty(顯示造假)={currentFakeRepeatPenalty}%";
    }

    // ─────────────────────────────────────────────────────────────
    // 重複點擊失敗率
    // ─────────────────────────────────────────────────────────────

    protected override void OnEnable()
    {
        base.OnEnable();
        SceneController.OnSceneChanged += ResetRepeatPenalty;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        SceneController.OnSceneChanged -= ResetRepeatPenalty;
    }

    /// <summary>
    /// 操作成功後呼叫此方法，累加重複失敗率。
    /// 用法：將此方法掛到 ActionOverlayTrigger 的 onSuccessEvents。
    /// </summary>
    public void NotifySuccess()
    {
        currentRepeatPenalty = Mathf.Min(currentRepeatPenalty + repeatPenaltyPerSuccess, repeatPenaltyMax);
        currentFakeRepeatPenalty = Mathf.Min(currentFakeRepeatPenalty + fakeRepeatPenaltyPerSuccess, fakeRepeatPenaltyMax);

        // 成功後立即刷新顯示，讓玩家看到成功率已下降
        if (chanceDisplay != null)
            CalculateSuccessChance();
    }

    /// <summary>
    /// 重置重複失敗率。場景切換時自動呼叫。
    /// </summary>
    public void ResetRepeatPenalty()
    {
        currentRepeatPenalty = 0f;
        currentFakeRepeatPenalty = 0f;

        if (chanceDisplay != null)
            CalculateSuccessChance();
    }
}
