using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// 將 HeroineStatusModel 的數值橋接到 Pixel Crushers Dialogue System 的 Lua 環境。
/// 在對話節點的 Conditions / Script 欄位即可使用，例如：
///   GetLewdnessLevel("sister") >= 3;
///   IsInHeat("sister") == true;
///   GetCurrentEmotion("sister") == "Angry";
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
        // ── 數值類 (回傳 double，Lua 端會自動視為 number) ──
        Lua.RegisterFunction("GetLewdnessLevel", this,
            SymbolExtensions.GetMethodInfo(() => GetLewdnessLevel(string.Empty)));
        Lua.RegisterFunction("GetBaseAffinityLevel", this,
            SymbolExtensions.GetMethodInfo(() => GetBaseAffinityLevel(string.Empty)));
        Lua.RegisterFunction("GetBaseExcitementLevel", this,
            SymbolExtensions.GetMethodInfo(() => GetBaseExcitementLevel(string.Empty)));
        Lua.RegisterFunction("GetPersonalSuspicion", this,
            SymbolExtensions.GetMethodInfo(() => GetPersonalSuspicion(string.Empty)));
        Lua.RegisterFunction("GetHCount", this,
            SymbolExtensions.GetMethodInfo(() => GetHCount(string.Empty)));

        // ── 布林類 ──
        Lua.RegisterFunction("IsSuspicionAtMax", this,
            SymbolExtensions.GetMethodInfo(() => IsSuspicionAtMax(string.Empty)));
        Lua.RegisterFunction("IsInHeat", this,
            SymbolExtensions.GetMethodInfo(() => IsInHeat(string.Empty)));
        Lua.RegisterFunction("IsEnraged", this,
            SymbolExtensions.GetMethodInfo(() => IsEnraged(string.Empty)));

        // ── 列舉類 (Lua 沒有 enum，回傳字串做比對) ──
        Lua.RegisterFunction("GetCurrentEmotion", this,
            SymbolExtensions.GetMethodInfo(() => GetCurrentEmotion(string.Empty)));
    }

    void OnDisable()
    {
        Lua.UnregisterFunction("GetLewdnessLevel");
        Lua.UnregisterFunction("GetBaseAffinityLevel");
        Lua.UnregisterFunction("GetBaseExcitementLevel");
        Lua.UnregisterFunction("GetPersonalSuspicion");
        Lua.UnregisterFunction("GetHCount");
        Lua.UnregisterFunction("IsSuspicionAtMax");
        Lua.UnregisterFunction("IsInHeat");
        Lua.UnregisterFunction("IsEnraged");
        Lua.UnregisterFunction("GetCurrentEmotion");
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
    // 數值
    // ─────────────────────────────────────────────
    public double GetLewdnessLevel(string heroineID)       => GetModel(heroineID)?.LewdnessLevel ?? 0;
    public double GetBaseAffinityLevel(string heroineID)   => GetModel(heroineID)?.BaseAffinityLevel ?? 0;
    public double GetBaseExcitementLevel(string heroineID) => GetModel(heroineID)?.BaseExcitementLevel ?? 0;
    public double GetPersonalSuspicion(string heroineID)   => GetModel(heroineID)?.PersonalSuspicion ?? 0;
    public double GetHCount(string heroineID)              => GetModel(heroineID)?.HCount ?? 0;

    // ─────────────────────────────────────────────
    // 布林
    // ─────────────────────────────────────────────
    public bool IsSuspicionAtMax(string heroineID) => GetModel(heroineID)?.IsSuspicionAtMax ?? false;
    public bool IsInHeat(string heroineID)         => GetModel(heroineID)?.IsInHeat ?? false;
    public bool IsEnraged(string heroineID)        => GetModel(heroineID)?.IsEnraged ?? false;

    // ─────────────────────────────────────────────
    // 列舉 → 字串
    // ─────────────────────────────────────────────
    public string GetCurrentEmotion(string heroineID)
    {
        var m = GetModel(heroineID);
        return m != null ? m.CurrentEmotion.ToString() : string.Empty;
    }
}
