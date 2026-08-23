using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// 將 HeroineStatusModel 的數值橋接到 Pixel Crushers Dialogue System 的 Lua 環境。
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
/// ── 情緒 ──
///   GetCurrentEmotion("sister")                    → 主導情緒（獨立儲存值）
///   SetCurrentEmotion("sister", "Shy")             → 設定主導情緒
///   GetDominantEmotion("sister")                   → 大宗情緒（卡池最多者）
///
/// ── 性慾 ──
///   GetLibido("sister")                            → 性慾值 (0~150)
///   SetLibido("sister", 50)                        → 設定性慾值
///   AddLibido("sister", 10)                        → 增減性慾值
///
/// ── 信賴 ──
///   GetTrust("sister")                             → 信賴值 (0~150)
///   SetTrust("sister", 50)                         → 設定信賴值
///   AddTrust("sister", 10)                         → 增減信賴值
///
/// ── 好感度 ──
///   GetAffinity("sister")                          → 好感度 (0~150)
///   SetAffinity("sister", 50)                      → 設定好感度
///   AddAffinity("sister", 10)                      → 增減好感度
///
/// ── H 次數 ──
///   GetHCount("sister")                            → H 次數
///
/// ── 發情 ──
///   IsInHeat("sister")                             → 是否正在發情（存檔獨立開關）
///   SetInHeat("sister", true)                      → 設定發情開關
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

        // ── 情緒 ──
        Lua.RegisterFunction("GetCurrentEmotion", this,
            SymbolExtensions.GetMethodInfo(() => GetCurrentEmotion(string.Empty)));
        Lua.RegisterFunction("SetCurrentEmotion", this,
            SymbolExtensions.GetMethodInfo(() => SetCurrentEmotion(string.Empty, string.Empty)));
        Lua.RegisterFunction("GetDominantEmotion", this,
            SymbolExtensions.GetMethodInfo(() => GetDominantEmotion(string.Empty)));

        // ── 性慾 ──
        Lua.RegisterFunction("GetLibido", this,
            SymbolExtensions.GetMethodInfo(() => GetLibido(string.Empty)));
        Lua.RegisterFunction("SetLibido", this,
            SymbolExtensions.GetMethodInfo(() => SetLibido(string.Empty, (double)0)));
        Lua.RegisterFunction("AddLibido", this,
            SymbolExtensions.GetMethodInfo(() => AddLibido(string.Empty, (double)0)));

        // ── 信賴 ──
        Lua.RegisterFunction("GetTrust", this,
            SymbolExtensions.GetMethodInfo(() => GetTrust(string.Empty)));
        Lua.RegisterFunction("SetTrust", this,
            SymbolExtensions.GetMethodInfo(() => SetTrust(string.Empty, (double)0)));
        Lua.RegisterFunction("AddTrust", this,
            SymbolExtensions.GetMethodInfo(() => AddTrust(string.Empty, (double)0)));

        // ── 好感度 ──
        Lua.RegisterFunction("GetAffinity", this,
            SymbolExtensions.GetMethodInfo(() => GetAffinity(string.Empty)));
        Lua.RegisterFunction("SetAffinity", this,
            SymbolExtensions.GetMethodInfo(() => SetAffinity(string.Empty, (double)0)));
        Lua.RegisterFunction("AddAffinity", this,
            SymbolExtensions.GetMethodInfo(() => AddAffinity(string.Empty, (double)0)));

        // ── H 次數 ──
        Lua.RegisterFunction("GetHCount", this,
            SymbolExtensions.GetMethodInfo(() => GetHCount(string.Empty)));

        // ── 發情 ──
        Lua.RegisterFunction("IsInHeat", this,
            SymbolExtensions.GetMethodInfo(() => IsInHeat(string.Empty)));
        Lua.RegisterFunction("SetInHeat", this,
            SymbolExtensions.GetMethodInfo(() => SetInHeat(string.Empty, false)));
    }

    void OnDisable()
    {
        Lua.UnregisterFunction("GetEmotionCardCount");
        Lua.UnregisterFunction("GetEmotionDeckMax");
        Lua.UnregisterFunction("GetIntimateMoodScore");
        Lua.UnregisterFunction("GetNegativeMoodScore");
        Lua.UnregisterFunction("GetCareMoodScore");
        Lua.UnregisterFunction("IsDisappointmentHigh");
        Lua.UnregisterFunction("IsMaternalHigh");
        Lua.UnregisterFunction("IsShyHigh");

        Lua.UnregisterFunction("GetCurrentEmotion");
        Lua.UnregisterFunction("SetCurrentEmotion");
        Lua.UnregisterFunction("GetDominantEmotion");

        Lua.UnregisterFunction("GetLibido");
        Lua.UnregisterFunction("SetLibido");
        Lua.UnregisterFunction("AddLibido");

        Lua.UnregisterFunction("GetTrust");
        Lua.UnregisterFunction("SetTrust");
        Lua.UnregisterFunction("AddTrust");

        Lua.UnregisterFunction("GetAffinity");
        Lua.UnregisterFunction("SetAffinity");
        Lua.UnregisterFunction("AddAffinity");

        Lua.UnregisterFunction("GetHCount");

        Lua.UnregisterFunction("IsInHeat");
        Lua.UnregisterFunction("SetInHeat");
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

    public double GetEmotionDeckMax(string heroineID)
    {
        return GetModel(heroineID)?.EmotionDeckMaxCount ?? 0;
    }

    public double GetIntimateMoodScore(string heroineID)
    {
        return GetModel(heroineID)?.GetIntimateMoodScore() ?? 0;
    }

    public double GetNegativeMoodScore(string heroineID)
    {
        return GetModel(heroineID)?.GetNegativeMoodScore() ?? 0;
    }

    public double GetCareMoodScore(string heroineID)
    {
        return GetModel(heroineID)?.GetCareMoodScore() ?? 0;
    }

    public bool IsDisappointmentHigh(string heroineID) => GetModel(heroineID)?.IsDisappointmentHigh() ?? false;
    public bool IsMaternalHigh(string heroineID) => GetModel(heroineID)?.IsMaternalHigh() ?? false;
    public bool IsShyHigh(string heroineID) => GetModel(heroineID)?.IsShyHigh() ?? false;

    // ─────────────────────────────────────────────
    // 情緒
    // ─────────────────────────────────────────────

    public string GetCurrentEmotion(string heroineID)
    {
        var m = GetModel(heroineID);
        return m != null ? m.CurrentEmotion.ToString() : string.Empty;
    }

    public void SetCurrentEmotion(string heroineID, string emotionName)
    {
        var m = GetModel(heroineID);
        if (m == null) return;

        if (!System.Enum.TryParse(emotionName?.Trim(), true, out HeroineEmotionCardType emotion))
        {
            Debug.LogWarning($"[HeroineLuaBridge] SetCurrentEmotion 無法解析情緒: {emotionName}");
            return;
        }

        m.SetCurrentEmotion(emotion);
    }

    public string GetDominantEmotion(string heroineID)
    {
        var m = GetModel(heroineID);
        return m != null ? m.DominantEmotion.ToString() : string.Empty;
    }

    // ─────────────────────────────────────────────
    // 性慾
    // ─────────────────────────────────────────────

    public double GetLibido(string heroineID)
    {
        return GetModel(heroineID)?.Libido ?? 0;
    }

    public void SetLibido(string heroineID, double value)
    {
        GetModel(heroineID)?.SetLibido((int)value);
    }

    public void AddLibido(string heroineID, double amount)
    {
        GetModel(heroineID)?.AddLibido((int)amount);
    }

    // ─────────────────────────────────────────────
    // 信賴
    // ─────────────────────────────────────────────

    /// <summary>
    /// 取得信賴值。
    /// Lua: GetTrust("sister") >= 50
    /// </summary>
    public double GetTrust(string heroineID)
    {
        return GetModel(heroineID)?.Trust ?? 0;
    }

    /// <summary>
    /// 設定信賴值（0~150）。
    /// Lua: SetTrust("sister", 50)
    /// </summary>
    public void SetTrust(string heroineID, double value)
    {
        GetModel(heroineID)?.SetTrust((int)value);
    }

    /// <summary>
    /// 增減信賴值。
    /// Lua: AddTrust("sister", 10) / AddTrust("sister", -5)
    /// </summary>
    public void AddTrust(string heroineID, double amount)
    {
        GetModel(heroineID)?.AddTrust((int)amount);
    }

    // ─────────────────────────────────────────────
    // 好感度
    // ─────────────────────────────────────────────

    /// <summary>
    /// 取得好感度。
    /// Lua: GetAffinity("sister") >= 50
    /// </summary>
    public double GetAffinity(string heroineID)
    {
        return GetModel(heroineID)?.Affinity ?? 0;
    }

    /// <summary>
    /// 設定好感度（0~150）。
    /// Lua: SetAffinity("sister", 50)
    /// </summary>
    public void SetAffinity(string heroineID, double value)
    {
        GetModel(heroineID)?.SetAffinity((int)value);
    }

    /// <summary>
    /// 增減好感度。
    /// Lua: AddAffinity("sister", 10) / AddAffinity("sister", -5)
    /// </summary>
    public void AddAffinity(string heroineID, double amount)
    {
        GetModel(heroineID)?.AddAffinity((int)amount);
    }

    // ─────────────────────────────────────────────
    // H 次數
    // ─────────────────────────────────────────────

    public double GetHCount(string heroineID)
    {
        return GetModel(heroineID)?.HCount ?? 0;
    }

    // ─────────────────────────────────────────────
    // 發情
    // ─────────────────────────────────────────────

    /// <summary>
    /// 是否正在發情。
    /// Lua: IsInHeat("sister")
    /// </summary>
    public bool IsInHeat(string heroineID) => GetModel(heroineID)?.IsInHeat ?? false;

    /// <summary>
    /// 設定發情開關。
    /// Lua: SetInHeat("sister", true) / SetInHeat("sister", false)
    /// </summary>
    public void SetInHeat(string heroineID, bool value)
    {
        GetModel(heroineID)?.SetInHeat(value);
    }
}
