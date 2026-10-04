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
///   AddStress(10); SetLifePower(50); ReduceMoney(100); ResetShootTimes();
/// 調整函式供 Script 使用；ReduceMoney / ReduceSkillPoints / ReduceShootTimes 會實際扣除，勿用於 Conditions。
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
        // ── 主角數值調整（Script） ──
        Lua.RegisterFunction("AddStress", this, SymbolExtensions.GetMethodInfo(() => AddStress((double)0)));
        Lua.RegisterFunction("ReduceStress", this, SymbolExtensions.GetMethodInfo(() => ReduceStress((double)0)));
        Lua.RegisterFunction("SetStress", this, SymbolExtensions.GetMethodInfo(() => SetStress((double)0)));
        Lua.RegisterFunction("AddLifePower", this, SymbolExtensions.GetMethodInfo(() => AddLifePower((double)0)));
        Lua.RegisterFunction("ReduceLifePower", this, SymbolExtensions.GetMethodInfo(() => ReduceLifePower((double)0)));
        Lua.RegisterFunction("SetLifePower", this, SymbolExtensions.GetMethodInfo(() => SetLifePower((double)0)));
        Lua.RegisterFunction("AddSociality", this, SymbolExtensions.GetMethodInfo(() => AddSociality((double)0)));
        Lua.RegisterFunction("ReduceSociality", this, SymbolExtensions.GetMethodInfo(() => ReduceSociality((double)0)));
        Lua.RegisterFunction("SetSociality", this, SymbolExtensions.GetMethodInfo(() => SetSociality((double)0)));
        Lua.RegisterFunction("AddDependency", this, SymbolExtensions.GetMethodInfo(() => AddDependency((double)0)));
        Lua.RegisterFunction("ReduceDependency", this, SymbolExtensions.GetMethodInfo(() => ReduceDependency((double)0)));
        Lua.RegisterFunction("SetDependency", this, SymbolExtensions.GetMethodInfo(() => SetDependency((double)0)));
        Lua.RegisterFunction("AddMoney", this, SymbolExtensions.GetMethodInfo(() => AddMoney((double)0)));
        Lua.RegisterFunction("ReduceMoney", this, SymbolExtensions.GetMethodInfo(() => ReduceMoney((double)0)));
        Lua.RegisterFunction("SetMoney", this, SymbolExtensions.GetMethodInfo(() => SetMoney((double)0)));
        Lua.RegisterFunction("AddSkillPoints", this, SymbolExtensions.GetMethodInfo(() => AddSkillPoints((double)0)));
        Lua.RegisterFunction("ReduceSkillPoints", this, SymbolExtensions.GetMethodInfo(() => ReduceSkillPoints((double)0)));
        Lua.RegisterFunction("SetSkillPoints", this, SymbolExtensions.GetMethodInfo(() => SetSkillPoints((double)0)));
        Lua.RegisterFunction("AddShootTimes", this, SymbolExtensions.GetMethodInfo(() => AddShootTimes((double)0)));
        Lua.RegisterFunction("ReduceShootTimes", this, SymbolExtensions.GetMethodInfo(() => ReduceShootTimes((double)0)));
        Lua.RegisterFunction("ResetShootTimes", this, SymbolExtensions.GetMethodInfo(() => ResetShootTimes()));

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
        Lua.UnregisterFunction("AddStress");
        Lua.UnregisterFunction("ReduceStress");
        Lua.UnregisterFunction("SetStress");
        Lua.UnregisterFunction("AddLifePower");
        Lua.UnregisterFunction("ReduceLifePower");
        Lua.UnregisterFunction("SetLifePower");
        Lua.UnregisterFunction("AddSociality");
        Lua.UnregisterFunction("ReduceSociality");
        Lua.UnregisterFunction("SetSociality");
        Lua.UnregisterFunction("AddDependency");
        Lua.UnregisterFunction("ReduceDependency");
        Lua.UnregisterFunction("SetDependency");
        Lua.UnregisterFunction("AddMoney");
        Lua.UnregisterFunction("ReduceMoney");
        Lua.UnregisterFunction("SetMoney");
        Lua.UnregisterFunction("AddSkillPoints");
        Lua.UnregisterFunction("ReduceSkillPoints");
        Lua.UnregisterFunction("SetSkillPoints");
        Lua.UnregisterFunction("AddShootTimes");
        Lua.UnregisterFunction("ReduceShootTimes");
        Lua.UnregisterFunction("ResetShootTimes");

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

    // ───── 主角數值調整：小數截去，數值限制與狀態修正交由 Model 處理 ─────
    public void AddStress(double value) => GetModel()?.AddStress((int)value);
    public void ReduceStress(double value) => GetModel()?.ReduceStress((int)value);
    public void SetStress(double value) => GetModel()?.SetStress((int)value);
    public void AddLifePower(double value) => GetModel()?.AddLifePower((int)value);
    public void ReduceLifePower(double value) => GetModel()?.ReduceLifePower((int)value);
    public void SetLifePower(double value) => GetModel()?.SetLifePower((int)value);
    public void AddSociality(double value) => GetModel()?.AddSociality((int)value);
    public void ReduceSociality(double value) => GetModel()?.ReduceSociality((int)value);
    public void SetSociality(double value) => GetModel()?.SetSociality((int)value);
    public void AddDependency(double value) => GetModel()?.AddDependency((int)value);
    public void ReduceDependency(double value) => GetModel()?.ReduceDependency((int)value);
    public void SetDependency(double value) => GetModel()?.SetDependency((int)value);
    public void AddMoney(double value) => GetModel()?.AddMoney((int)value);
    public void SetMoney(double value) => GetModel()?.SetMoney((int)value);
    /// <summary>以正數指定扣除量；不足時不扣除，回傳是否成功。僅供 Script 操作。</summary>
    public bool ReduceMoney(double amount) => GetModel()?.TryReduceMoney((int)amount) ?? false;
    public void AddSkillPoints(double value) => GetModel()?.AddSkillPoints((int)value);
    public void SetSkillPoints(double value) => GetModel()?.SetSkillPoints((int)value);
    /// <summary>以正數指定扣除量；不足時不扣除，回傳是否成功。僅供 Script 操作。</summary>
    public bool ReduceSkillPoints(double amount) => GetModel()?.TryReduceSkillPoints((int)amount) ?? false;
    public void AddShootTimes(double amount) => GetModel()?.AddShootTimes((int)amount);
    /// <summary>以正數指定扣除次數；低於下限時不扣除。僅供 Script 操作。</summary>
    public bool ReduceShootTimes(double amount) => GetModel()?.TryReduceShootTimes((int)amount) ?? false;
    public void ResetShootTimes() => GetModel()?.ResetShootTimes();

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
