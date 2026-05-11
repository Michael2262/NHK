using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// NHK 版：將 ProtagonistStatusModel 的數值橋接到 Pixel Crushers Dialogue System 的 Lua 環境。
/// 可於 Dialogue Conditions / Script 使用：
///   GetStress() <= 70;
///   GetLifePower() >= 50;
///   GetSociality() >= 40;
///   GetDependency() >= 40;
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
        Lua.RegisterFunction("GetStress", this, SymbolExtensions.GetMethodInfo(() => GetStress()));
        Lua.RegisterFunction("GetLifePower", this, SymbolExtensions.GetMethodInfo(() => GetLifePower()));
        Lua.RegisterFunction("GetSociality", this, SymbolExtensions.GetMethodInfo(() => GetSociality()));
        Lua.RegisterFunction("GetDependency", this, SymbolExtensions.GetMethodInfo(() => GetDependency()));
        Lua.RegisterFunction("GetMoney", this, SymbolExtensions.GetMethodInfo(() => GetMoney()));
        Lua.RegisterFunction("GetSkillPoints", this, SymbolExtensions.GetMethodInfo(() => GetSkillPoints()));
        Lua.RegisterFunction("GetDay", this, SymbolExtensions.GetMethodInfo(() => GetDay()));

        Lua.RegisterFunction("IsStressCritical", this, SymbolExtensions.GetMethodInfo(() => IsStressCritical()));
        Lua.RegisterFunction("IsStressCollapsed", this, SymbolExtensions.GetMethodInfo(() => IsStressCollapsed()));
        Lua.RegisterFunction("IsLifeHealthy", this, SymbolExtensions.GetMethodInfo(() => IsLifeHealthy()));
        Lua.RegisterFunction("IsSocialityHigh", this, SymbolExtensions.GetMethodInfo(() => IsSocialityHigh()));
        Lua.RegisterFunction("IsSocialityLow", this, SymbolExtensions.GetMethodInfo(() => IsSocialityLow()));
        Lua.RegisterFunction("IsDependencyHigh", this, SymbolExtensions.GetMethodInfo(() => IsDependencyHigh()));

        // Legacy aliases: keep old dialogue conditions from breaking. Prefer new names above.
        Lua.RegisterFunction("GetStamina", this, SymbolExtensions.GetMethodInfo(() => GetLifePower()));
        Lua.RegisterFunction("GetSocialFear", this, SymbolExtensions.GetMethodInfo(() => GetSocialFearCompat()));
        Lua.RegisterFunction("IsSocialFearHigh", this, SymbolExtensions.GetMethodInfo(() => IsSocialFearHighCompat()));
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
        Lua.UnregisterFunction("IsStressCritical");
        Lua.UnregisterFunction("IsStressCollapsed");
        Lua.UnregisterFunction("IsLifeHealthy");
        Lua.UnregisterFunction("IsSocialityHigh");
        Lua.UnregisterFunction("IsSocialityLow");
        Lua.UnregisterFunction("IsDependencyHigh");
        Lua.UnregisterFunction("GetStamina");
        Lua.UnregisterFunction("GetSocialFear");
        Lua.UnregisterFunction("IsSocialFearHigh");
    }

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

    public double GetStress() => GetModel()?.Stress ?? 0;
    public double GetLifePower() => GetModel()?.LifePower ?? 0;
    public double GetSociality() => GetModel()?.Sociality ?? 0;
    public double GetDependency() => GetModel()?.Dependency ?? 0;
    public double GetMoney() => GetModel()?.Money ?? 0;
    public double GetSkillPoints() => GetModel()?.SkillPoints ?? 0;
    public double GetDay() => GetModel()?.Day ?? 1;

    public bool IsStressCritical() => GetModel()?.IsStressCritical() ?? false;
    public bool IsStressCollapsed() => GetModel()?.IsStressCollapsed() ?? false;
    public bool IsLifeHealthy() => GetModel()?.IsLifeHealthy() ?? false;
    public bool IsSocialityHigh() => GetModel()?.IsSocialityHigh() ?? false;
    public bool IsSocialityLow() => GetModel()?.IsSocialityLow() ?? false;
    public bool IsDependencyHigh() => GetModel()?.IsDependencyHigh() ?? false;

    // ───── Legacy 相容：舊對話條件若仍使用 GetSocialFear / IsSocialFearHigh，回傳反轉值 ─────
    /// <summary>相容用：回傳 100 - Sociality，模擬舊版社會恐懼值（越高越恐懼）。</summary>
    public double GetSocialFearCompat() => 100 - (GetModel()?.Sociality ?? 0);
    /// <summary>相容用：Sociality 低 = 舊版 SocialFear 高。</summary>
    public bool IsSocialFearHighCompat() => GetModel()?.IsSocialityLow() ?? false;
}