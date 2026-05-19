using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// NHK 版：將 ProtagonistStatusModel 的數值橋接到 Pixel Crushers Dialogue System 的 Lua 環境。
/// 可於 Dialogue Conditions / Script 使用：
///   GetStress() &lt;= 70;
///   GetLifePower() &gt;= 50;
///   GetStressGrade() == "High";
///   IsStressHigh();
/// </summary>
public class ProtagonistLuaBridge : MonoBehaviour
{
    public static ProtagonistLuaBridge Instance { get; private set; }

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
        // ── 數值直讀 ──
        Lua.RegisterFunction("GetStress", this, SymbolExtensions.GetMethodInfo(() => GetStress()));
        Lua.RegisterFunction("GetLifePower", this, SymbolExtensions.GetMethodInfo(() => GetLifePower()));
        Lua.RegisterFunction("GetSociality", this, SymbolExtensions.GetMethodInfo(() => GetSociality()));
        Lua.RegisterFunction("GetDependency", this, SymbolExtensions.GetMethodInfo(() => GetDependency()));
        Lua.RegisterFunction("GetMoney", this, SymbolExtensions.GetMethodInfo(() => GetMoney()));
        Lua.RegisterFunction("GetSkillPoints", this, SymbolExtensions.GetMethodInfo(() => GetSkillPoints()));
        Lua.RegisterFunction("GetDay", this, SymbolExtensions.GetMethodInfo(() => GetDay()));

        // ── 分級查詢（回傳 "Low" / "Medium" / "High" / "Extreme"） ──
        Lua.RegisterFunction("GetStressGrade", this, SymbolExtensions.GetMethodInfo(() => GetStressGrade()));
        Lua.RegisterFunction("GetLifeGrade", this, SymbolExtensions.GetMethodInfo(() => GetLifeGrade()));
        Lua.RegisterFunction("GetSocialityGrade", this, SymbolExtensions.GetMethodInfo(() => GetSocialityGrade()));
        Lua.RegisterFunction("GetDependencyGrade", this, SymbolExtensions.GetMethodInfo(() => GetDependencyGrade()));

        // ── Bool 快捷 ──
        Lua.RegisterFunction("IsStressHigh", this, SymbolExtensions.GetMethodInfo(() => IsStressHigh()));
        Lua.RegisterFunction("IsStressExtreme", this, SymbolExtensions.GetMethodInfo(() => IsStressExtreme()));
        Lua.RegisterFunction("IsLifeLow", this, SymbolExtensions.GetMethodInfo(() => IsLifeLow()));
        Lua.RegisterFunction("IsLifeHigh", this, SymbolExtensions.GetMethodInfo(() => IsLifeHigh()));
        Lua.RegisterFunction("IsSocialityLow", this, SymbolExtensions.GetMethodInfo(() => IsSocialityLow()));
        Lua.RegisterFunction("IsSocialityHigh", this, SymbolExtensions.GetMethodInfo(() => IsSocialityHigh()));
        Lua.RegisterFunction("IsDependencyHigh", this, SymbolExtensions.GetMethodInfo(() => IsDependencyHigh()));
        Lua.RegisterFunction("IsDependencyExtreme", this, SymbolExtensions.GetMethodInfo(() => IsDependencyExtreme()));

        // ── Legacy aliases ──
        Lua.RegisterFunction("GetStamina", this, SymbolExtensions.GetMethodInfo(() => GetLifePower()));
        Lua.RegisterFunction("GetSocialFear", this, SymbolExtensions.GetMethodInfo(() => GetSocialFearCompat()));
        Lua.RegisterFunction("IsSocialFearHigh", this, SymbolExtensions.GetMethodInfo(() => IsSocialFearHighCompat()));
        // 舊名相容
        Lua.RegisterFunction("IsStressCritical", this, SymbolExtensions.GetMethodInfo(() => IsStressHigh()));
        Lua.RegisterFunction("IsStressCollapsed", this, SymbolExtensions.GetMethodInfo(() => IsStressExtreme()));
        Lua.RegisterFunction("IsLifeHealthy", this, SymbolExtensions.GetMethodInfo(() => IsLifeHigh()));
    }

    void OnDisable()
    {
        Lua.UnregisterFunction("GetStress");
        Lua.UnregisterFunction("GetLifePower");
        Lua.UnregisterFunction("GetSociality");
        Lua.UnregisterFunction("GetDependency");
        Lua.UnregisterFunction("GetMoney");
        Lua.UnregisterFunction("GetSkillPoints");
        Lua.UnregisterFunction("GetDay");

        Lua.UnregisterFunction("GetStressGrade");
        Lua.UnregisterFunction("GetLifeGrade");
        Lua.UnregisterFunction("GetSocialityGrade");
        Lua.UnregisterFunction("GetDependencyGrade");

        Lua.UnregisterFunction("IsStressHigh");
        Lua.UnregisterFunction("IsStressExtreme");
        Lua.UnregisterFunction("IsLifeLow");
        Lua.UnregisterFunction("IsLifeHigh");
        Lua.UnregisterFunction("IsSocialityLow");
        Lua.UnregisterFunction("IsSocialityHigh");
        Lua.UnregisterFunction("IsDependencyHigh");
        Lua.UnregisterFunction("IsDependencyExtreme");

        Lua.UnregisterFunction("GetStamina");
        Lua.UnregisterFunction("GetSocialFear");
        Lua.UnregisterFunction("IsSocialFearHigh");
        Lua.UnregisterFunction("IsStressCritical");
        Lua.UnregisterFunction("IsStressCollapsed");
        Lua.UnregisterFunction("IsLifeHealthy");
    }

    // ───── 內部取得 Model ─────
    private static ProtagonistStatusModel GetModel()
    {
        var svc = GameStatusService.Instance;
        if (svc == null)
        {
            Debug.LogWarning("[ProtagonistLuaBridge] GameStatusService.Instance 為 null");
            return null;
        }
        if (svc.Protagonist == null)
        {
            Debug.LogWarning("[ProtagonistLuaBridge] Protagonist 尚未初始化");
            return null;
        }
        return svc.Protagonist;
    }

    // ───── 數值直讀 ─────
    public double GetStress() => GetModel()?.Stress ?? 0;
    public double GetLifePower() => GetModel()?.LifePower ?? 0;
    public double GetSociality() => GetModel()?.Sociality ?? 0;
    public double GetDependency() => GetModel()?.Dependency ?? 0;
    public double GetMoney() => GetModel()?.Money ?? 0;
    public double GetSkillPoints() => GetModel()?.SkillPoints ?? 0;
    public double GetDay() => GameStatusService.Instance?.Time?.DayIndex ?? 1;

    // ───── 分級查詢 ─────
    public string GetStressGrade() => GetModel()?.GetStressGrade().ToString() ?? "Low";
    public string GetLifeGrade() => GetModel()?.GetLifeGrade().ToString() ?? "Low";
    public string GetSocialityGrade() => GetModel()?.GetSocialityGrade().ToString() ?? "Low";
    public string GetDependencyGrade() => GetModel()?.GetDependencyGrade().ToString() ?? "Low";

    // ───── Bool 快捷 ─────
    public bool IsStressHigh() => GetModel()?.IsStressHigh() ?? false;
    public bool IsStressExtreme() => GetModel()?.IsStressExtreme() ?? false;
    public bool IsLifeLow() => GetModel()?.IsLifeLow() ?? false;
    public bool IsLifeHigh() => GetModel()?.IsLifeHigh() ?? false;
    public bool IsSocialityLow() => GetModel()?.IsSocialityLow() ?? false;
    public bool IsSocialityHigh() => GetModel()?.IsSocialityHigh() ?? false;
    public bool IsDependencyHigh() => GetModel()?.IsDependencyHigh() ?? false;
    public bool IsDependencyExtreme() => GetModel()?.IsDependencyExtreme() ?? false;

    // ───── Legacy 相容 ─────
    public double GetSocialFearCompat() => 100 - (GetModel()?.Sociality ?? 0);
    public bool IsSocialFearHighCompat() => GetModel()?.IsSocialityLow() ?? false;
}