using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// 將 HeroineStatusModel 的數值橋接到 Pixel Crushers Dialogue System 的 Lua 環境。
/// NHK 版：核心為情緒卡池系統。
///
/// ── 情緒卡查詢 ──
///   GetEmotionCardCount("sister", "Shy")           → 該情緒卡的張數
///   GetEmotionDeckMax("sister")                    → 卡池上限
///   GetIntimateMoodScore("sister")                 → 親密情緒分數 (Shy+Relaxed+Maternal)
///   GetNegativeMoodScore("sister")                 → 負面情緒分數 (Angry+Disappointed)
///   GetCareMoodScore("sister")                     → 關懷情緒分數 (Worried+Maternal)
///   IsDisappointmentHigh("sister")                 → 失望卡 ≥ 3
///   IsMaternalHigh("sister")                       → 母性卡 ≥ 3
///   IsShyHigh("sister")                            → 害羞卡 ≥ 3
///
/// ── 主導情緒 ──
///   GetCurrentEmotion("sister")                    → "Angry" / "Shy" / ...
///
/// ── H 次數 ──
///   GetHCount("sister")                            → H 次數
///
/// 用法範例（Dialogue System Conditions）：
///   GetEmotionCardCount("sister", "Shy") >= 3
///   GetCurrentEmotion("sister") == "Maternal"
///   GetHCount("sister") >= 1
///   IsDisappointmentHigh("sister") == true
/// </summary>
public class HeroineLuaBridge : MonoBehaviour
{
    public static HeroineLuaBridge Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        // ── 情緒卡查詢 ──
        Lua.RegisterFunction("GetEmotionCardCount", this,
            SymbolExtensions.GetMethodInfo(() => GetEmotionCardCount(string.Empty, string.Empty)));
        Lua.RegisterFunction("GetEmotionDeckMax", this,
            SymbolExtensions.GetMethodInfo(() => GetEmotionDeckMax(string.Empty)));
        Lua.RegisterFunction("GetIntimateMoodScore", this,
            SymbolExtensions.GetMethodInfo(() => GetIntimateMoodScore(string.Empty)));
        Lua.RegisterFunction("GetNegativeMoodScore", this,
            SymbolExtensions.GetMethodInfo(() => GetNegativeMoodScore(string.Empty)));
        Lua.RegisterFunction("GetCareMoodScore", this,
            SymbolExtensions.GetMethodInfo(() => GetCareMoodScore(string.Empty)));
        Lua.RegisterFunction("IsDisappointmentHigh", this,
            SymbolExtensions.GetMethodInfo(() => IsDisappointmentHigh(string.Empty)));
        Lua.RegisterFunction("IsMaternalHigh", this,
            SymbolExtensions.GetMethodInfo(() => IsMaternalHigh(string.Empty)));
        Lua.RegisterFunction("IsShyHigh", this,
            SymbolExtensions.GetMethodInfo(() => IsShyHigh(string.Empty)));

        // ── 主導情緒 ──
        Lua.RegisterFunction("GetCurrentEmotion", this,
            SymbolExtensions.GetMethodInfo(() => GetCurrentEmotion(string.Empty)));

        // ── H 次數 ──
        Lua.RegisterFunction("GetHCount", this,
            SymbolExtensions.GetMethodInfo(() => GetHCount(string.Empty)));
    }

    void OnDisable()
    {
        // ── 情緒卡查詢 ──
        Lua.UnregisterFunction("GetEmotionCardCount");
        Lua.UnregisterFunction("GetEmotionDeckMax");
        Lua.UnregisterFunction("GetIntimateMoodScore");
        Lua.UnregisterFunction("GetNegativeMoodScore");
        Lua.UnregisterFunction("GetCareMoodScore");
        Lua.UnregisterFunction("IsDisappointmentHigh");
        Lua.UnregisterFunction("IsMaternalHigh");
        Lua.UnregisterFunction("IsShyHigh");

        // ── 主導情緒 ──
        Lua.UnregisterFunction("GetCurrentEmotion");

        // ── H 次數 ──
        Lua.UnregisterFunction("GetHCount");
    }

    // ─────────────────────────────────────────────
    // 共用查找
    // ─────────────────────────────────────────────
    private static HeroineStatusModel GetModel(string heroineID)
    {
        var svc = GameStatusService.Instance;
        if (svc == null || svc.Heroines == null) return null;
        if (string.IsNullOrEmpty(heroineID)) return null;
        svc.Heroines.TryGetValue(heroineID, out var model);
        if (model == null)
            Debug.LogWarning($"[HeroineLuaBridge] 找不到 HeroineID: {heroineID}");
        return model;
    }

    // ─────────────────────────────────────────────
    // 情緒卡查詢
    // ─────────────────────────────────────────────

    /// <summary>
    /// 取得指定情緒卡的張數。
    /// Lua: GetEmotionCardCount("sister", "Shy")
    /// emotionName 接受英文 enum 名稱：Angry / Shy / Worried / Maternal / Relaxed / Disappointed
    /// </summary>
    public double GetEmotionCardCount(string heroineID, string emotionName)
    {
        var model = GetModel(heroineID);
        if (model == null) return 0;

        if (!System.Enum.TryParse(emotionName?.Trim(), true, out HeroineEmotionCardType emotion))
        {
            Debug.LogWarning($"[HeroineLuaBridge] GetEmotionCardCount 無法解析情緒類型: {emotionName}");
            return 0;
        }

        return model.GetCardCount(emotion);
    }

    /// <summary>
    /// 取得情緒卡池上限張數。
    /// Lua: GetEmotionDeckMax("sister")
    /// </summary>
    public double GetEmotionDeckMax(string heroineID)
    {
        return GetModel(heroineID)?.EmotionDeckMaxCount ?? 0;
    }

    /// <summary>
    /// 親密情緒分數 = Shy + Relaxed + Maternal 張數。
    /// Lua: GetIntimateMoodScore("sister")
    /// </summary>
    public double GetIntimateMoodScore(string heroineID)
    {
        return GetModel(heroineID)?.GetIntimateMoodScore() ?? 0;
    }

    /// <summary>
    /// 負面情緒分數 = Angry + Disappointed 張數。
    /// Lua: GetNegativeMoodScore("sister")
    /// </summary>
    public double GetNegativeMoodScore(string heroineID)
    {
        return GetModel(heroineID)?.GetNegativeMoodScore() ?? 0;
    }

    /// <summary>
    /// 關懷情緒分數 = Worried + Maternal 張數。
    /// Lua: GetCareMoodScore("sister")
    /// </summary>
    public double GetCareMoodScore(string heroineID)
    {
        return GetModel(heroineID)?.GetCareMoodScore() ?? 0;
    }

    /// <summary>失望卡 ≥ 3。</summary>
    public bool IsDisappointmentHigh(string heroineID)
    {
        return GetModel(heroineID)?.IsDisappointmentHigh() ?? false;
    }

    /// <summary>母性卡 ≥ 3。</summary>
    public bool IsMaternalHigh(string heroineID)
    {
        return GetModel(heroineID)?.IsMaternalHigh() ?? false;
    }

    /// <summary>害羞卡 ≥ 3。</summary>
    public bool IsShyHigh(string heroineID)
    {
        return GetModel(heroineID)?.IsShyHigh() ?? false;
    }

    // ─────────────────────────────────────────────
    // 主導情緒 → 字串
    // ─────────────────────────────────────────────

    /// <summary>
    /// 取得當前主導情緒（由卡池自動計算）。
    /// Lua: GetCurrentEmotion("sister") == "Shy"
    /// </summary>
    public string GetCurrentEmotion(string heroineID)
    {
        var m = GetModel(heroineID);
        return m != null ? m.CurrentEmotion.ToString() : string.Empty;
    }

    // ─────────────────────────────────────────────
    // H 次數
    // ─────────────────────────────────────────────

    /// <summary>
    /// Lua: GetHCount("sister") >= 1
    /// </summary>
    public double GetHCount(string heroineID)
    {
        return GetModel(heroineID)?.HCount ?? 0;
    }
}