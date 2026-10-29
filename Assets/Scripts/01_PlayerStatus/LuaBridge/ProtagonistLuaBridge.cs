using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>正式主角 Lua 接口；條件式使用 Get／Is，修改數值使用 Set／Add／ForceAdd。</summary>
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
        Lua.RegisterFunction("ForceAddStamina", this, SymbolExtensions.GetMethodInfo(() => ForceAddStamina((double)0)));
        Lua.RegisterFunction("ForceAddShootTimes", this, SymbolExtensions.GetMethodInfo(() => ForceAddShootTimes((double)0)));
        Lua.RegisterFunction("SetShootTimes", this, SymbolExtensions.GetMethodInfo(() => SetShootTimes((double)0)));
        Lua.RegisterFunction("AddRoomMessLevel", this, SymbolExtensions.GetMethodInfo(() => AddRoomMessLevel((double)0)));
        Lua.RegisterFunction("SetRoomMessLevel", this, SymbolExtensions.GetMethodInfo(() => SetRoomMessLevel((double)0)));
        Lua.RegisterFunction("GetRoomMessLevel", this, SymbolExtensions.GetMethodInfo(() => GetRoomMessLevel()));
        Lua.RegisterFunction("AddStickerCount", this, SymbolExtensions.GetMethodInfo(() => AddStickerCount((double)0)));
        Lua.RegisterFunction("SetStickerCount", this, SymbolExtensions.GetMethodInfo(() => SetStickerCount((double)0)));
        Lua.RegisterFunction("GetStickerCount", this, SymbolExtensions.GetMethodInfo(() => GetStickerCount()));
        Lua.RegisterFunction("AddLust", this, SymbolExtensions.GetMethodInfo(() => AddLust((double)0)));
        Lua.RegisterFunction("SetLust", this, SymbolExtensions.GetMethodInfo(() => SetLust((double)0)));
        Lua.RegisterFunction("GetLust", this, SymbolExtensions.GetMethodInfo(() => GetLust()));
        Lua.RegisterFunction("GetHardness", this, SymbolExtensions.GetMethodInfo(() => GetHardness()));
        Lua.RegisterFunction("SetHardness", this, SymbolExtensions.GetMethodInfo(() => SetHardness((double)0)));
        Lua.RegisterFunction("AddHardness", this, SymbolExtensions.GetMethodInfo(() => AddHardness((double)0)));
        Lua.RegisterFunction("ForceAddHardness", this, SymbolExtensions.GetMethodInfo(() => ForceAddHardness((double)0)));
        Lua.RegisterFunction("AddShootTimesMax", this, SymbolExtensions.GetMethodInfo(() => AddShootTimesMax((double)0)));
        Lua.RegisterFunction("SetShootTimesMax", this, SymbolExtensions.GetMethodInfo(() => SetShootTimesMax((double)0)));
        Lua.RegisterFunction("GetShootTimesMax", this, SymbolExtensions.GetMethodInfo(() => GetShootTimesMax()));
        Lua.RegisterFunction("AddStaminaMax", this, SymbolExtensions.GetMethodInfo(() => AddStaminaMax((double)0)));
        Lua.RegisterFunction("SetStaminaMax", this, SymbolExtensions.GetMethodInfo(() => SetStaminaMax((double)0)));
        Lua.RegisterFunction("GetStaminaMax", this, SymbolExtensions.GetMethodInfo(() => GetStaminaMax()));
        Lua.RegisterFunction("AddStamina", this, SymbolExtensions.GetMethodInfo(() => AddStamina((double)0)));
        Lua.RegisterFunction("SetStamina", this, SymbolExtensions.GetMethodInfo(() => SetStamina((double)0)));
        Lua.RegisterFunction("GetStamina", this, SymbolExtensions.GetMethodInfo(() => GetStamina()));
        // ── 主角數值調整（Script） ──
        Lua.RegisterFunction("AddMoney", this, SymbolExtensions.GetMethodInfo(() => AddMoney((double)0)));
        Lua.RegisterFunction("ReduceMoney", this, SymbolExtensions.GetMethodInfo(() => ReduceMoney((double)0)));
        Lua.RegisterFunction("SetMoney", this, SymbolExtensions.GetMethodInfo(() => SetMoney((double)0)));
        Lua.RegisterFunction("AddSkillPoints", this, SymbolExtensions.GetMethodInfo(() => AddSkillPoints((double)0)));
        Lua.RegisterFunction("ReduceSkillPoints", this, SymbolExtensions.GetMethodInfo(() => ReduceSkillPoints((double)0)));
        Lua.RegisterFunction("SetSkillPoints", this, SymbolExtensions.GetMethodInfo(() => SetSkillPoints((double)0)));
        Lua.RegisterFunction("AddShootTimes", this, SymbolExtensions.GetMethodInfo(() => AddShootTimes((double)0)));

        // ── 數值直讀 ──
        Lua.RegisterFunction("GetMoney", this, SymbolExtensions.GetMethodInfo(() => GetMoney()));
        Lua.RegisterFunction("GetSkillPoints", this, SymbolExtensions.GetMethodInfo(() => GetSkillPoints()));

        // ── 射精感讀寫 ──
        Lua.RegisterFunction("GetSemen", this, SymbolExtensions.GetMethodInfo(() => GetSemen()));
        Lua.RegisterFunction("SetSemen", this, SymbolExtensions.GetMethodInfo(() => SetSemen((double)0)));
        Lua.RegisterFunction("AddSemen", this, SymbolExtensions.GetMethodInfo(() => AddSemen((double)0)));

        // ── Bool 快捷 ──
        Lua.RegisterFunction("IsOverShoot", this, SymbolExtensions.GetMethodInfo(() => IsOverShoot()));

        // ── ShootTimes ──
        Lua.RegisterFunction("GetShootTimes", this, SymbolExtensions.GetMethodInfo(() => GetShootTimes()));
    }

    void OnDisable()
    {
        Lua.UnregisterFunction("ForceAddStamina");
        Lua.UnregisterFunction("ForceAddShootTimes");
        Lua.UnregisterFunction("SetShootTimes");
        Lua.UnregisterFunction("AddRoomMessLevel");
        Lua.UnregisterFunction("SetRoomMessLevel");
        Lua.UnregisterFunction("GetRoomMessLevel");
        Lua.UnregisterFunction("AddStickerCount");
        Lua.UnregisterFunction("SetStickerCount");
        Lua.UnregisterFunction("GetStickerCount");
        Lua.UnregisterFunction("AddLust");
        Lua.UnregisterFunction("SetLust");
        Lua.UnregisterFunction("GetLust");
        Lua.UnregisterFunction("GetHardness");
        Lua.UnregisterFunction("SetHardness");
        Lua.UnregisterFunction("AddHardness");
        Lua.UnregisterFunction("ForceAddHardness");
        Lua.UnregisterFunction("AddShootTimesMax");
        Lua.UnregisterFunction("SetShootTimesMax");
        Lua.UnregisterFunction("GetShootTimesMax");
        Lua.UnregisterFunction("AddStaminaMax");
        Lua.UnregisterFunction("SetStaminaMax");
        Lua.UnregisterFunction("GetStaminaMax");
        Lua.UnregisterFunction("AddStamina");
        Lua.UnregisterFunction("SetStamina");
        Lua.UnregisterFunction("GetStamina");
        Lua.UnregisterFunction("AddMoney");
        Lua.UnregisterFunction("ReduceMoney");
        Lua.UnregisterFunction("SetMoney");
        Lua.UnregisterFunction("AddSkillPoints");
        Lua.UnregisterFunction("ReduceSkillPoints");
        Lua.UnregisterFunction("SetSkillPoints");
        Lua.UnregisterFunction("AddShootTimes");

        Lua.UnregisterFunction("GetMoney");
        Lua.UnregisterFunction("GetSkillPoints");
        Lua.UnregisterFunction("GetSemen");
        Lua.UnregisterFunction("SetSemen");
        Lua.UnregisterFunction("AddSemen");

        Lua.UnregisterFunction("IsOverShoot");

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

    public double GetMoney() => GetModel()?.Money ?? 0;
    public double GetSkillPoints() => GetModel()?.SkillPoints ?? 0;

    // ───── 射精感：Lua 數值轉成整數後交由 Model 處理 ─────
    public double GetSemen() => GetModel()?.GetSemen() ?? 0;
    public void SetSemen(double value) => GetModel()?.SetSemen((int)value);
    /// <summary>正數增加、負數減少射精感；小數部分截去。</summary>
    public void AddSemen(double amount) => GetModel()?.AddSemen((int)amount);

    // ───── 主角數值調整：小數截去，數值限制與狀態修正交由 Model 處理 ─────

    public void AddMoney(double value) => GetModel()?.AddMoney((int)value);
    public void SetMoney(double value) => GetModel()?.SetMoney((int)value);
    /// <summary>以正數指定扣除量；不足時不扣除，回傳是否成功。僅供 Script 操作。</summary>
    public bool ReduceMoney(double amount) => GetModel()?.TryReduceMoney((int)amount) ?? false;
    public void AddSkillPoints(double value) => GetModel()?.AddSkillPoints((int)value);
    public void SetSkillPoints(double value) => GetModel()?.SetSkillPoints((int)value);
    /// <summary>以正數指定扣除量；不足時不扣除，回傳是否成功。僅供 Script 操作。</summary>
    public bool ReduceSkillPoints(double amount) => GetModel()?.TryReduceSkillPoints((int)amount) ?? false;
    public void AddShootTimes(double amount) => GetModel()?.AddShootTimes((int)amount);

    // ───── Bool 快捷 ─────

    public bool IsOverShoot() => GetModel()?.IsOverShoot ?? false;

    // ───── ShootTimes ─────
    public double GetShootTimes() => GetModel()?.ShootTimes ?? 0;
    public double GetStamina() => GetModel()?.Stamina ?? 0;
    public void SetStamina(double value) => GetModel()?.SetStamina((int)value);
    public void AddStamina(double value) => GetModel()?.AddStamina((int)value);
    public double GetStaminaMax() => GetModel()?.StaminaMax ?? 0;
    public void SetStaminaMax(double value) => GetModel()?.SetStaminaMax((int)value);
    public void AddStaminaMax(double value) => GetModel()?.AddStaminaMax((int)value);
    public double GetShootTimesMax() => GetModel()?.ShootTimesMax ?? 0;
    public void SetShootTimesMax(double value) => GetModel()?.SetShootTimesMax((int)value);
    public void AddShootTimesMax(double value) => GetModel()?.AddShootTimesMax((int)value);
    public double GetLust() => GetModel()?.Lust ?? 0;
    public void SetLust(double value) => GetModel()?.SetLust((int)value);
    public void AddLust(double value) => GetModel()?.AddLust((int)value);

    /// <summary>條件式：GetHardness() &gt;= 50。</summary>
    public double GetHardness() => GetModel()?.GetHardness() ?? 0;
    /// <summary>Script：SetHardness(80)。小數截去，實際範圍交由 Model 限制。</summary>
    public void SetHardness(double value)
    {
        if (double.IsNaN(value)) return;
        GetModel()?.SetHardness(ClampHardnessArgument(value));
    }
    /// <summary>Script：AddHardness(20)／AddHardness(-10)。射精次數耗盡時正數無效。</summary>
    public void AddHardness(double amount)
    {
        if (double.IsNaN(amount)) return;
        GetModel()?.AddHardness(ClampHardnessArgument(amount));
    }
    /// <summary>Script：ForceAddHardness(20)。略過射精次數限制，數值仍為 0～100。</summary>
    public void ForceAddHardness(double amount)
    {
        if (double.IsNaN(amount)) return;
        GetModel()?.ForceAddHardness(ClampHardnessArgument(amount));
    }
    private static int ClampHardnessArgument(double value)
        => (int)System.Math.Max(int.MinValue, System.Math.Min(int.MaxValue, value));

    public double GetStickerCount() => GetModel()?.StickerCount ?? 0;
    public void SetStickerCount(double value) => GetModel()?.SetStickerCount((int)value);
    public void AddStickerCount(double value) => GetModel()?.AddStickerCount((int)value);
    public double GetRoomMessLevel() => GetModel()?.RoomMessLevel ?? 0;
    public void SetRoomMessLevel(double value) => GetModel()?.SetRoomMessLevel((int)value);
    public void AddRoomMessLevel(double value) => GetModel()?.AddRoomMessLevel((int)value);
    public void SetShootTimes(double value) => GetModel()?.SetShootTimes((int)value);
    public void ForceAddShootTimes(double value) => GetModel()?.ForceAddShootTimes((int)value);
    public void ForceAddStamina(double value) => GetModel()?.ForceAddStamina((int)value);
}
