using UnityEngine;

/// <summary>
/// 條件判斷腳本：
/// 檢查指定的 HeroineID 的 ExcitementLevel 是否高於指定的閾值。
/// </summary>
public class ExcitementConditionChecker : MonoBehaviour, IConditionChecker
{
    [Header("條件設置")]
    [Tooltip("要檢查的女主角 ID (必須與 HeroineStatusConfig 中的 ID 一致)")]
    [SerializeField] private string heroineID;

    [Tooltip("興奮度等級必須 *高於或等於* 這個值 (ExcitementLevel >= Threshold)")]
    [SerializeField] private int excitementLevelThreshold = 0;

    /// <summary>
    /// 執行條件檢查。
    /// </summary>
    /// <returns>True 如果興奮度高於閾值，否則為 False。</returns>
    public bool CheckCondition()
    {
        // 1. 檢查 GameStatusService 是否存在
        if (GameStatusService.Instance == null)
        {
            Debug.LogError($"[{nameof(ExcitementConditionChecker)}] GameStatusService.Instance 為 null，無法執行檢查。", this);
            return false;
        }

        // 2. 檢查 HeroineID 是否已設定
        if (string.IsNullOrEmpty(heroineID))
        {
            Debug.LogWarning($"[{nameof(ExcitementConditionChecker)}] 此檢查器上的 HeroineID 尚未設定。", this);
            return false;
        }

        // 3. 嘗試從 GameStatusService 獲取指定的女主角 Model
        //    (我們使用 Heroines.TryGetValue 來安全地獲取，避免找不到 ID 時報錯)
        if (!GameStatusService.Instance.Heroines.TryGetValue(heroineID, out HeroineStatusModel heroine))
        {
            Debug.LogError($"[{nameof(ExcitementConditionChecker)}] 在 GameStatusService.Heroines 中找不到 ID 為 '{heroineID}' 的女主角。", this);
            return false; // 找不到女主角，條件不成立
        }

        // 4. 執行核心邏輯：比較興奮度等級
        bool conditionMet = heroine.TotalExcitementLevel >= excitementLevelThreshold;

        /*
        // (可選) 用於除錯的日誌
        if (conditionMet)
        {
            Debug.Log($"[{nameof(ExcitementConditionChecker)}] 條件滿足! {heroineID} 的興奮度 ({heroine.ExcitementLevel}) > {excitementLevelThreshold}");
        }
        */

        return conditionMet;
    }
}