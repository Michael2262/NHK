using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// NHK 版：將 ProtagonistStatusModel 的數值橋接到 Pixel Crushers Dialogue System 的 Lua 環境。
/// 可於 Dialogue Conditions / Script 使用：
///   GetStress() &lt;= 70;
///   GetLifePower() &gt;= 50;
///   GetStressGrade() == "High";
///   IsStressHigh();
///   GetSemen() &gt;= 100;
///   SetSemen(50); AddSemen(20); AddSemen(-10);
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

        // ── 射精感讀寫 ──
        Lua.RegisterFunction("GetSemen", this, SymbolExtensions.GetMethodInfo(() => GetSemen()));
        Lua.RegisterFunction("SetSemen", this, SymbolExtensions.GetMethodInfo(() => SetSemen((double)0)));
        Lua.RegisterFunction("AddSemen", this, SymbolExtensions.GetMethodInfo(() => AddSemen((double)0)));

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
        Lua.RegisterFunction("IsOverShoot", this, SymbolExtensions.GetMethodInfo(() => IsOverShoot()));
        Lua.RegisterFunction("IsBadHealthy", this, SymbolExtensions.GetMethodInfo(() => IsBadHealthy()));
        Lua.RegisterFunction("IsBadDependency", this, SymbolExtensions.GetMethodInfo(() => IsBadDependency()));

        // ── ShootTimes ──
        Lua.RegisterFunction("GetShootTimes", this, SymbolExtensions.GetMethodInfo(() => GetShootTimes()));
    }

    void OnDisable()
    {
        Lua.UnregisterFunction("GetStress");
        Lua.UnregisterFunction("GetLifePower");
        Lua.UnregisterFunction("GetSociality");
        Lua.UnregisterFunction("GetDependency");
        Lua.UnregisterFunction("GetMoney");
        Lua.UnregisterFunction("GetSkillPoints");
        Lua.UnregisterFunction("GetSemen");
        Lua.UnregisterFunction("SetSemen");
        Lua.UnregisterFunction("AddSemen");

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
        Lua.UnregisterFunction("IsOverShoot");
        Lua.UnregisterFunction("IsBadHealthy");
        Lua.UnregisterFunction("IsBadDependency");

        Lua.UnregisterFunction("GetShootTimes");
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

    // ───── 射精感：Lua 數值轉成整數後交由 Model 處理 ─────
    public double GetSemen() => GetModel()?.GetSemen() ?? 0;
    public void SetSemen(double value) => GetModel()?.SetSemen((int)value);
    /// <summary>正數增加、負數減少射精感；小數部分截去。</summary>
    public void AddSemen(double amount) => GetModel()?.AddSemen((int)amount);

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
    public bool IsOverShoot() => GetModel()?.IsOverShoot ?? false;
    public bool IsBadHealthy() => GetModel()?.BadHealthy ?? false;
    public bool IsBadDependency() => GetModel()?.BadDependency ?? false;

    // ───── ShootTimes ─────
    public double GetShootTimes() => GetModel()?.CheckShootTimes() ?? 0;
}
