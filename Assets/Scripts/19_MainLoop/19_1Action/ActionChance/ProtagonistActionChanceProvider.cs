using UnityEngine;

/// <summary>主角行動成功率；舊四維加權已退役，使用自訂基礎百分比。</summary>
public class ProtagonistActionChanceProvider : ActionChanceProvider
{
    [Tooltip("基礎成功率（百分比）；請重新檢查原本依賴四維加權的場景設定。")]
    public float customBase = 0f;
    protected override float CalculateChance(ProtagonistStatusModel p) => customBase / 100f;
    protected override float GetFallbackChance() => 0.3f;
    protected override string BuildDebugSummary(ProtagonistStatusModel p, float chance)
        => $"主角行動：基礎 {customBase}%，最終 {chance:P0}";
}
