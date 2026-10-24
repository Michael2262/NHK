using UnityEngine;

/// <summary>出門成功率：自訂基礎值減去重複懲罰；舊四維貢獻已退役。</summary>
public class GoOutsideChanceProvider : ActionChanceProvider
{
    [Header("嘗試出門 — 公式參數")]
    [Tooltip("額外成功率（百分比，可為負）。作為基礎成功率，" +
             "因此同樣會被重複點擊失敗率影響。")]
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
        // 舊四維貢獻及壓力懲罰已退役，保留自訂值與重複懲罰。
        return (extraSuccessRate - repeatPenalty) / 100f;
    }

    protected override float GetFallbackChance() => 0.3f;

    protected override string BuildDebugSummary(ProtagonistStatusModel p, float chance)
    {
        return $"出門：基礎 {extraSuccessRate}%，重複懲罰 {currentRepeatPenalty}%，最終 {chance:P0}";
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
