using UnityEngine;

/// <summary>
/// 職責：提供給 Unity Events (如按鈕、動畫事件) 呼叫的女主角狀態除錯接口，
/// 並將請求安全地轉發給 HeroineStatusModel 執行。
/// 女主角 ID 直接在 Inspector 中指定。
/// </summary>
public class HeroineStatusDebugBridge : MonoBehaviour
{
    [Header("目標女主角設定")]
    [Tooltip("要操作的女主角唯一 ID (例如 \"Sayo\"、\"Mika\")")]
    [SerializeField] private string heroineID = "";

    // 輔助屬性：安全地獲取目標女主角的 Model 實例
    private HeroineStatusModel Model
    {
        get
        {
            if (string.IsNullOrEmpty(heroineID))
            {
                Debug.LogError("[HeroineStatusDebugBridge] heroineID 尚未設定！請在 Inspector 中填入女主角 ID。");
                return null;
            }

            if (GameStatusService.Instance == null)
            {
                Debug.LogError("[HeroineStatusDebugBridge] GameStatusService 尚未初始化或不存在！");
                return null;
            }

            if (GameStatusService.Instance.Heroines.TryGetValue(heroineID, out HeroineStatusModel heroine))
            {
                return heroine;
            }

            Debug.LogError($"[HeroineStatusDebugBridge] 找不到 ID 為 '{heroineID}' 的女主角！");
            return null;
        }
    }

    // ==========================================================
    // 開發度等級 (LewdnessLevel)
    // ==========================================================

    /// <summary>
    /// 直接設定開發度等級與經驗值。
    /// </summary>
    public void SetLewdnessLevel(int level)
    {
        var m = Model;
        if (m == null) return;
        m.SetLewdness(level, 0);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 設定 LewdnessLevel = {level}");
    }

    /// <summary>
    /// 設定開發度等級與經驗值（同時指定）。
    /// </summary>
    public void SetLewdnessLevelAndExp(int level, int exp)
    {
        var m = Model;
        if (m == null) return;
        m.SetLewdness(level, exp);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 設定 LewdnessLevel = {level}, LewdnessExp = {exp}");
    }

    // ==========================================================
    // 開發度經驗值 (LewdnessExp)
    // ==========================================================

    /// <summary>
    /// 增加開發度經驗值（可觸發升級）。
    /// </summary>
    public void AddLewdnessExp(int amount)
    {
        var m = Model;
        if (m == null) return;
        m.AddLewdnessExp(amount);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 增加 LewdnessExp: +{amount} → 當前 Lv.{m.LewdnessLevel} Exp.{m.LewdnessExp}");
    }

    /// <summary>
    /// 減少開發度經驗值。
    /// </summary>
    public void ReduceLewdnessExp(int amount)
    {
        var m = Model;
        if (m == null) return;
        m.ReduceLewdnessExp(amount);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 減少 LewdnessExp: -{amount} → 當前 Lv.{m.LewdnessLevel} Exp.{m.LewdnessExp}");
    }

    /// <summary>
    /// 輸出目前的開發度等級與經驗值到 Console。
    /// </summary>
    public void LogLewdnessStatus()
    {
        var m = Model;
        if (m == null) return;
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) LewdnessLevel = {m.LewdnessLevel}, LewdnessExp = {m.LewdnessExp}");
    }

    // ==========================================================
    // 親密度等級 (BaseAffinityLevel)
    // ==========================================================

    /// <summary>
    /// 直接設定親密度等級（經驗值重置為 0）。
    /// </summary>
    public void SetAffinityLevel(int level)
    {
        var m = Model;
        if (m == null) return;
        m.SetAffinity(level, 0);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 設定 AffinityLevel = {level}");
    }

    /// <summary>
    /// 設定親密度等級與經驗值（同時指定）。
    /// </summary>
    public void SetAffinityLevelAndExp(int level, int exp)
    {
        var m = Model;
        if (m == null) return;
        m.SetAffinity(level, exp);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 設定 AffinityLevel = {level}, AffinityExp = {exp}");
    }

    /// <summary>
    /// 鎖定或解鎖指定親密度等級（鎖定後無法靠 Exp 自動升級）。
    /// </summary>
    public void SetAffinityLevelLock(int level, bool isLocked)
    {
        var m = Model;
        if (m == null) return;
        m.SetAffinityLevelLock(level, isLocked);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) AffinityLevel {level} 鎖定狀態設為 {isLocked}");
    }

    // ==========================================================
    // 親密度經驗值 (BaseAffinityExp)
    // ==========================================================

    /// <summary>
    /// 增加親密度經驗值（可觸發升級）。
    /// </summary>
    public void AddAffinityExp(int amount)
    {
        var m = Model;
        if (m == null) return;
        m.AddAffinityExp(amount);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 增加 AffinityExp: +{amount} → 當前 Lv.{m.BaseAffinityLevel} Exp.{m.BaseAffinityExp}");
    }

    /// <summary>
    /// 減少親密度經驗值（不降級，最低降至當前等級 Exp = 0）。
    /// </summary>
    public void ReduceAffinityExp(int amount)
    {
        var m = Model;
        if (m == null) return;
        m.ReduceAffinityExp(amount);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 減少 AffinityExp: -{amount} → 當前 Lv.{m.BaseAffinityLevel} Exp.{m.BaseAffinityExp}");
    }

    /// <summary>
    /// 輸出目前的親密度等級與經驗值到 Console。
    /// </summary>
    public void LogAffinityStatus()
    {
        var m = Model;
        if (m == null) return;
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) AffinityLevel = {m.BaseAffinityLevel}, AffinityExp = {m.BaseAffinityExp}");
    }

    // ==========================================================
    // 一鍵輸出全部狀態 (Debug Summary)
    // ==========================================================

    /// <summary>
    /// 輸出目前四項數值的完整摘要到 Console。
    /// </summary>
    public void LogAllStatus()
    {
        var m = Model;
        if (m == null) return;
        Debug.Log(
            $"[HeroineStatusDebugBridge] ===== {heroineID} 狀態摘要 =====\n" +
            $"  LewdnessLevel  : {m.LewdnessLevel}  (Exp: {m.LewdnessExp})\n" +
            $"  AffinityLevel  : {m.BaseAffinityLevel}  (Exp: {m.BaseAffinityExp})"
        );
    }
}
