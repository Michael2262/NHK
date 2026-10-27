using System;
using UnityEngine;

/// <summary>
/// 職責：提供給 Unity Events (如按鈕、動畫事件) 呼叫的女主角狀態除錯接口，
/// 並將請求安全地轉發給 HeroineStatusModel 執行。
/// 女主角 ID 直接在 Inspector 中指定。
///
/// 已對齊 NHK 版 HeroineStatusModel：操作當前情緒（CurrentEmotion）、
/// 性興味（Interest）、性欲（Libido）、興奮度（Excitement）、高潮度（Orgasm）、信賴（Trust）、好感度（Affinity）、H 次數（HCount）。
/// 不再使用舊專案遺留的 Lewdness stub（Affinity 已是現役獨立數值）。
///
/// 為了讓 Button.onClick / UnityEvent 能無參數呼叫，多數操作同時提供：
/// - 帶 int 參數的版本（int 為 HeroineEmotionCardType 列舉索引）
/// - 無參數的 "Default" 版本（使用 Inspector 上的 defaultEmotion 欄位）
/// </summary>
public class HeroineStatusDebugBridge : MonoBehaviour
{
    [Header("目標女主角設定")]
    [Tooltip("要操作的女主角唯一 ID (例如 \"Sayo\"、\"Mika\")")]
    [SerializeField] private string heroineID = "";

    [Header("當前情緒預設值")]
    [Tooltip("無參數的 Default 版方法（SetCurrentEmotionDefault）會使用此情緒。")]
    [SerializeField] private HeroineEmotionCardType defaultEmotion = HeroineEmotionCardType.Angry;

    // Awake、性興味與心情：直接轉接至正式 Model。
    public float GetAwake() => Model?.GetAwake() ?? 0f;
    public void SetAwake(float value) => Model?.SetAwake(value);
    public void AddAwake(float amount) => Model?.AddAwake(amount);

    public int GetInterest() => Model?.GetInterest() ?? 0;
    public void SetInterest(int value) => Model?.SetInterest(value);
    public void AddInterest(int amount) => Model?.AddInterest(amount);

    public void SetMood(int value) => Model?.SetMood(value);
    public void AddMood(int amount) => Model?.AddMood(amount);

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

    /// <summary>把 int 轉成合法的 HeroineEmotionCardType；非法值回傳 defaultEmotion。</summary>
    private HeroineEmotionCardType ToEmotion(int emotionIndex)
    {
        if (Enum.IsDefined(typeof(HeroineEmotionCardType), emotionIndex))
            return (HeroineEmotionCardType)emotionIndex;

        Debug.LogWarning($"[HeroineStatusDebugBridge] 無效的情緒索引 {emotionIndex}，改用 {defaultEmotion}。");
        return defaultEmotion;
    }

    // ==========================================================
    // 當前情緒 (CurrentEmotion)
    // ==========================================================

    /// <summary>設定當前情緒（CurrentEmotion）。int = 列舉索引。</summary>
    public void SetCurrentEmotion(int emotionIndex)
    {
        var m = Model;
        if (m == null) return;
        var type = ToEmotion(emotionIndex);
        m.SetCurrentEmotion(type);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 設定 CurrentEmotion = {type}");
    }

    /// <summary>把當前情緒設為 defaultEmotion（無參數，供按鈕呼叫）。</summary>
    public void SetCurrentEmotionDefault() => SetCurrentEmotion((int)defaultEmotion);

    // ==========================================================
    // 性欲 (Libido)
    // ==========================================================

    /// <summary>增減性欲值（正增負減，自動 Clamp Model 定義範圍）。</summary>
    public void AddLibido(int amount)
    {
        var m = Model;
        if (m == null) return;
        m.AddLibido(amount);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) Libido {(amount >= 0 ? "+" : "")}{amount} → {m.Libido}");
    }

    /// <summary>直接設定性欲值（自動 Clamp Model 定義範圍）。</summary>
    public void SetLibido(int value)
    {
        var m = Model;
        if (m == null) return;
        m.SetLibido(value);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 設定 Libido = {m.Libido}");
    }

    /// <summary>套用一次每日性欲衰減。</summary>
    public void ApplyLibidoDecay()
    {
        var m = Model;
        if (m == null) return;
        m.ApplyLibidoDecay();
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 套用每日衰減 → Libido = {m.Libido}");
    }

    /// <summary>輸出目前性欲值到 Console。</summary>
    public void LogLibido()
    {
        var m = Model;
        if (m == null) return;
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) Libido = {m.Libido} / {HeroineStatusModel.LibidoMax}");
    }

    // ==========================================================
    // 興奮度 (Excitement)
    // ==========================================================

    /// <summary>一般增減興奮度，不套用性欲倍率。</summary>
    public void AddExcitement(int amount)
    {
        var m = Model;
        if (m == null) return;
        int before = m.GetExcitement();
        m.AddExcitement(amount);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 興奮度一般增減 {amount}：{before} → {m.GetExcitement()}");
    }

    /// <summary>舊方法別名；直接增減興奮度，已取消性欲倍率。</summary>
    public void WeightedAddExcitement(int amount)
    {
        // 保留舊 UnityEvent 方法名，已取消性欲倍率。
        AddExcitement(amount);
    }

    /// <summary>直接設定興奮度（0~100），不套用性欲倍率。</summary>
    public void SetExcitement(int value)
    {
        var m = Model;
        if (m == null) return;
        m.SetExcitement(value);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 設定興奮度 = {m.GetExcitement()}");
    }

    /// <summary>取得目前興奮度；無法取得 Model 時回傳 0。</summary>
    public int GetExcitement() => Model?.GetExcitement() ?? 0;

    /// <summary>手動套用一次興奮度每日結算，不推進遊戲時間。</summary>
    public void ApplyExcitementDailyReset()
    {
        var m = Model;
        if (m == null) return;
        int before = m.GetExcitement();
        m.ApplyExcitementDailyReset();
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 興奮度每日結算（發情：{m.IsInHeat}）：{before} → {m.GetExcitement()}");
    }

    /// <summary>輸出目前興奮度到 Console，供無參數按鈕呼叫。</summary>
    public void LogExcitement()
    {
        var m = Model;
        if (m == null) return;
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 興奮度 = {m.GetExcitement()} / {HeroineStatusModel.ExcitementMax}");
    }

    // ==========================================================
    // 高潮度 (Orgasm)
    // ==========================================================

    /// <summary>一般增減高潮度（正增負減，限制 0~100）。</summary>
    public void AddOrgasm(int amount)
    {
        var m = Model;
        if (m == null) return;
        int before = m.GetOrgasm();
        m.AddOrgasm(amount);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 高潮度增減 {amount}：{before} → {m.GetOrgasm()}");
    }

    /// <summary>直接設定高潮度（0~100）。</summary>
    public void SetOrgasm(int value)
    {
        var m = Model;
        if (m == null) return;
        m.SetOrgasm(value);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 設定高潮度 = {m.GetOrgasm()}");
    }

    /// <summary>取得目前高潮度；無法取得 Model 時回傳 0。</summary>
    public int GetOrgasm() => Model?.GetOrgasm() ?? 0;

    /// <summary>手動套用一次高潮度每日歸零，不推進遊戲時間。</summary>
    public void ApplyOrgasmDailyReset()
    {
        var m = Model;
        if (m == null) return;
        int before = m.GetOrgasm();
        m.ApplyOrgasmDailyReset();
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 高潮度每日歸零：{before} → {m.GetOrgasm()}");
    }

    /// <summary>輸出目前高潮度到 Console，供無參數按鈕呼叫。</summary>
    public void LogOrgasm()
    {
        var m = Model;
        if (m == null) return;
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 高潮度 = {m.GetOrgasm()} / {m.OrgasmMax}");
    }

    // ==========================================================
    // 信賴 (Trust)
    // ==========================================================

    /// <summary>增減信賴值（正增負減，自動 Clamp Model 定義範圍）。</summary>
    public void AddTrust(int amount)
    {
        var m = Model;
        if (m == null) return;
        m.AddTrust(amount);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) Trust {(amount >= 0 ? "+" : "")}{amount} → {m.Trust}");
    }

    /// <summary>直接設定信賴值（自動 Clamp Model 定義範圍）。</summary>
    public void SetTrust(int value)
    {
        var m = Model;
        if (m == null) return;
        m.SetTrust(value);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 設定 Trust = {m.Trust}");
    }

    /// <summary>輸出目前信賴值到 Console。</summary>
    public void LogTrust()
    {
        var m = Model;
        if (m == null) return;
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) Trust = {m.Trust} / {HeroineStatusModel.TrustMax}");
    }

    // ==========================================================
    // 好感度 (Affinity)
    // ==========================================================

    /// <summary>增減好感度（正增負減，自動 Clamp Model 定義範圍）。</summary>
    public void AddAffinity(int amount)
    {
        var m = Model;
        if (m == null) return;
        m.AddAffinity(amount);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) Affinity {(amount >= 0 ? "+" : "")}{amount} → {m.Affinity}");
    }

    /// <summary>直接設定好感度（自動 Clamp Model 定義範圍）。</summary>
    public void SetAffinity(int value)
    {
        var m = Model;
        if (m == null) return;
        m.SetAffinity(value);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 設定 Affinity = {m.Affinity}");
    }

    /// <summary>輸出目前好感度到 Console。</summary>
    public void LogAffinity()
    {
        var m = Model;
        if (m == null) return;
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) Affinity = {m.Affinity} / {HeroineStatusModel.AffinityMax}");
    }

    // ==========================================================
    // H 次數 (HCount)
    // ==========================================================

    /// <summary>增加 H 次數（預設 +1，可傳負值）。</summary>
    public void AddHCount(int amount = 1)
    {
        var m = Model;
        if (m == null) return;
        m.AddHCount(amount);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) HCount {(amount >= 0 ? "+" : "")}{amount} → {m.HCount}");
    }

    /// <summary>直接設定 H 次數。</summary>
    public void SetHCount(int value)
    {
        var m = Model;
        if (m == null) return;
        m.SetHCount(value);
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) 設定 HCount = {m.HCount}");
    }

    /// <summary>輸出目前 H 次數到 Console。</summary>
    public void LogHCount()
    {
        var m = Model;
        if (m == null) return;
        Debug.Log($"[HeroineStatusDebugBridge] ({heroineID}) HCount = {m.HCount}");
    }

    // ==========================================================
    // 一鍵輸出全部狀態 (Debug Summary)
    // ==========================================================

    /// <summary>輸出目前所有現役數值的完整摘要到 Console。</summary>
    public void LogAllStatus()
    {
        var m = Model;
        if (m == null) return;
        Debug.Log($"[HeroineStatusDebugBridge] {heroineID}: 情緒={m.CurrentEmotion}, " +
            $"Awake={m.Awake}, 信賴={m.Trust}, 好感={m.Affinity}, 心情={m.Mood}, 性興味={m.Interest}, 性欲={m.Libido}, " +
            $"興奮={m.Excitement}, 高潮={m.Orgasm}, H次數={m.HCount}, 發情={m.IsInHeat}");
    }
}
