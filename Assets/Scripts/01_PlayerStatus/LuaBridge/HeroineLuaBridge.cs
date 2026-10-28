using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>正式女主角數值與狀態的 Lua 入口；Conditions 使用 Get／Is，Script 使用 Set／Add。第一個參數皆為 heroineID。</summary>
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
        // 生理週期：機率為 0～1，階段名稱與 MenstrualCyclePhase 一致。
        Lua.RegisterFunction("GetCyclePhase", this,
            SymbolExtensions.GetMethodInfo(() => GetCyclePhase(string.Empty)));
        Lua.RegisterFunction("GetCyclePhaseDay", this,
            SymbolExtensions.GetMethodInfo(() => GetCyclePhaseDay(string.Empty)));
        Lua.RegisterFunction("GetCycleDay", this,
            SymbolExtensions.GetMethodInfo(() => GetCycleDay(string.Empty)));
        Lua.RegisterFunction("GetCycleLength", this,
            SymbolExtensions.GetMethodInfo(() => GetCycleLength(string.Empty)));
        Lua.RegisterFunction("IsCyclePaused", this,
            SymbolExtensions.GetMethodInfo(() => IsCyclePaused(string.Empty)));
        Lua.RegisterFunction("GetPregnancyProbability", this,
            SymbolExtensions.GetMethodInfo(() => GetPregnancyProbability(string.Empty)));
        Lua.RegisterFunction("GetPregnancyState", this,
            SymbolExtensions.GetMethodInfo(() => GetPregnancyState(string.Empty)));
        Lua.RegisterFunction("SetPregnancyState", this,
            SymbolExtensions.GetMethodInfo(() => SetPregnancyState(string.Empty, string.Empty)));
        Lua.RegisterFunction("GetEarlyPregnancyDaysElapsed", this,
            SymbolExtensions.GetMethodInfo(() => GetEarlyPregnancyDaysElapsed(string.Empty)));
        Lua.RegisterFunction("GetCurrentOvulationDayType", this,
            SymbolExtensions.GetMethodInfo(() => GetCurrentOvulationDayType(string.Empty)));
        Lua.RegisterFunction("SetPregnancyProbability", this,
            SymbolExtensions.GetMethodInfo(() => SetPregnancyProbability(string.Empty, (double)0)));
        Lua.RegisterFunction("SetCyclePhase", this,
            SymbolExtensions.GetMethodInfo(() => SetCyclePhase(string.Empty, string.Empty)));
        Lua.RegisterFunction("ClearPregnancy", this,
            SymbolExtensions.GetMethodInfo(() => ClearPregnancy(string.Empty)));
        Lua.RegisterFunction("TryConceive", this,
            SymbolExtensions.GetMethodInfo(() => TryConceive(string.Empty)));

        Lua.RegisterFunction("GetMood", this, SymbolExtensions.GetMethodInfo(() => GetMood(string.Empty)));
        Lua.RegisterFunction("SetMood", this, SymbolExtensions.GetMethodInfo(() => SetMood(string.Empty, (double)0)));
        Lua.RegisterFunction("AddMood", this, SymbolExtensions.GetMethodInfo(() => AddMood(string.Empty, (double)0)));

        // 獨立高潮度
        Lua.RegisterFunction("GetOrgasm", this,
            SymbolExtensions.GetMethodInfo(() => GetOrgasm(string.Empty)));
        Lua.RegisterFunction("SetOrgasm", this,
            SymbolExtensions.GetMethodInfo(() => SetOrgasm(string.Empty, (double)0)));
        Lua.RegisterFunction("AddOrgasm", this,
            SymbolExtensions.GetMethodInfo(() => AddOrgasm(string.Empty, (double)0)));

        // 獨立興奮度
        Lua.RegisterFunction("GetExcitement", this,
            SymbolExtensions.GetMethodInfo(() => GetExcitement(string.Empty)));
        Lua.RegisterFunction("SetExcitement", this,
            SymbolExtensions.GetMethodInfo(() => SetExcitement(string.Empty, (double)0)));
        Lua.RegisterFunction("AddExcitement", this,
            SymbolExtensions.GetMethodInfo(() => AddExcitement(string.Empty, (double)0)));
        Lua.RegisterFunction("WeightedAddExcitement", this,
            SymbolExtensions.GetMethodInfo(() => WeightedAddExcitement(string.Empty, (double)0)));

        // ── 情緒 ──
        Lua.RegisterFunction("GetCurrentEmotion", this,
            SymbolExtensions.GetMethodInfo(() => GetCurrentEmotion(string.Empty)));
        Lua.RegisterFunction("SetCurrentEmotion", this,
            SymbolExtensions.GetMethodInfo(() => SetCurrentEmotion(string.Empty, string.Empty)));

        // ── Awake：保留小數 ──
        Lua.RegisterFunction("GetAwake", this,
            SymbolExtensions.GetMethodInfo(() => GetAwake(string.Empty)));
        Lua.RegisterFunction("SetAwake", this,
            SymbolExtensions.GetMethodInfo(() => SetAwake(string.Empty, (double)0)));
        Lua.RegisterFunction("AddAwake", this,
            SymbolExtensions.GetMethodInfo(() => AddAwake(string.Empty, (double)0)));
        Lua.RegisterFunction("GetCurrentSleepState", this,
            SymbolExtensions.GetMethodInfo(() => GetCurrentSleepState(string.Empty)));

        // ── 性興味 ──
        Lua.RegisterFunction("GetInterest", this,
            SymbolExtensions.GetMethodInfo(() => GetInterest(string.Empty)));
        Lua.RegisterFunction("SetInterest", this,
            SymbolExtensions.GetMethodInfo(() => SetInterest(string.Empty, (double)0)));
        Lua.RegisterFunction("AddInterest", this,
            SymbolExtensions.GetMethodInfo(() => AddInterest(string.Empty, (double)0)));

        // ── 性欲 ──
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
        Lua.RegisterFunction("SetHCount", this,
            SymbolExtensions.GetMethodInfo(() => SetHCount(string.Empty, (double)0)));
        Lua.RegisterFunction("AddHCount", this,
            SymbolExtensions.GetMethodInfo(() => AddHCount(string.Empty, (double)0)));

        // ── 發情 ──
        Lua.RegisterFunction("IsInHeat", this,
            SymbolExtensions.GetMethodInfo(() => IsInHeat(string.Empty)));
        Lua.RegisterFunction("SetInHeat", this,
            SymbolExtensions.GetMethodInfo(() => SetInHeat(string.Empty, false)));
    }

    void OnDisable()
    {
        Lua.UnregisterFunction("GetCyclePhase");
        Lua.UnregisterFunction("GetCyclePhaseDay");
        Lua.UnregisterFunction("GetCycleDay");
        Lua.UnregisterFunction("GetCycleLength");
        Lua.UnregisterFunction("IsCyclePaused");
        Lua.UnregisterFunction("GetPregnancyProbability");
        Lua.UnregisterFunction("GetPregnancyState");
        Lua.UnregisterFunction("SetPregnancyState");
        Lua.UnregisterFunction("GetEarlyPregnancyDaysElapsed");
        Lua.UnregisterFunction("GetCurrentOvulationDayType");
        Lua.UnregisterFunction("SetPregnancyProbability");
        Lua.UnregisterFunction("SetCyclePhase");
        Lua.UnregisterFunction("ClearPregnancy");
        Lua.UnregisterFunction("TryConceive");

        Lua.UnregisterFunction("GetMood");
        Lua.UnregisterFunction("SetMood");
        Lua.UnregisterFunction("AddMood");

        Lua.UnregisterFunction("GetOrgasm");
        Lua.UnregisterFunction("SetOrgasm");
        Lua.UnregisterFunction("AddOrgasm");

        Lua.UnregisterFunction("GetExcitement");
        Lua.UnregisterFunction("SetExcitement");
        Lua.UnregisterFunction("AddExcitement");
        Lua.UnregisterFunction("WeightedAddExcitement");

        Lua.UnregisterFunction("GetCurrentEmotion");
        Lua.UnregisterFunction("SetCurrentEmotion");

        Lua.UnregisterFunction("GetAwake");
        Lua.UnregisterFunction("SetAwake");
        Lua.UnregisterFunction("AddAwake");
        Lua.UnregisterFunction("GetCurrentSleepState");

        Lua.UnregisterFunction("GetInterest");
        Lua.UnregisterFunction("SetInterest");
        Lua.UnregisterFunction("AddInterest");

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
        Lua.UnregisterFunction("SetHCount");
        Lua.UnregisterFunction("AddHCount");

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

    // 生理週期：只有明確呼叫 TryConceive 才擲骰；手動設定受精可用
    // SetCyclePhase("角色ID", "BeforePregnant")，解除並原位恢復用 ClearPregnancy。
    public string GetCyclePhase(string heroineID) => GetModel(heroineID)?.CurrentCyclePhase.ToString() ?? string.Empty;
    public double GetCyclePhaseDay(string heroineID) => GetModel(heroineID)?.CurrentPhaseDay ?? 0;
    public double GetCycleDay(string heroineID) => GetModel(heroineID)?.CurrentCycleDay ?? 0;
    public double GetCycleLength(string heroineID) => GetModel(heroineID)?.CycleLength ?? 0;
    public bool IsCyclePaused(string heroineID) => GetModel(heroineID)?.IsCyclePaused ?? false;
    /// <summary>回傳當日機率（0～1），例如 0.8 代表 80%。</summary>
    public double GetPregnancyProbability(string heroineID) => GetModel(heroineID)?.PregnancyProbability ?? 0f;
    public string GetPregnancyState(string heroineID) => GetModel(heroineID)?.Pregnancy.ToString() ?? string.Empty;
    public double GetEarlyPregnancyDaysElapsed(string heroineID) => GetModel(heroineID)?.EarlyPregnancyDaysElapsed ?? 0;
    public string GetCurrentOvulationDayType(string heroineID) => GetModel(heroineID)?.GetCurrentOvulationDayType().ToString() ?? string.Empty;

    /// <summary>狀態名稱：NotPregnant／EarlyPregnancy／Pregnant；變更與通知由 Model 處理。</summary>
    public void SetPregnancyState(string heroineID, string stateName)
    {
        if (!System.Enum.TryParse(stateName?.Trim(), true, out PregnancyState state)
            || !System.Enum.IsDefined(typeof(PregnancyState), state))
        {
            Debug.LogWarning($"[HeroineLuaBridge] 無法解析懷孕狀態：{stateName}");
            return;
        }
        GetModel(heroineID)?.SetPregnancyState(state);
    }

    /// <summary>設定當日機率（0～1）；Lua: SetPregnancyProbability("角色ID", 0.8)。</summary>
    public void SetPregnancyProbability(string heroineID, double value) => GetModel(heroineID)?.SetPregnancyProbability((float)value);
    public bool TryConceive(string heroineID) => GetModel(heroineID)?.TryConceive() ?? false;
    public void ClearPregnancy(string heroineID) => GetModel(heroineID)?.ClearPregnancy();

    public void SetCyclePhase(string heroineID, string phaseName)
    {
        if (!System.Enum.TryParse(phaseName?.Trim(), true, out MenstrualCyclePhase phase)
            || !System.Enum.IsDefined(typeof(MenstrualCyclePhase), phase))
        {
            Debug.LogWarning($"[HeroineLuaBridge] 無法解析生理週期階段：{phaseName}");
            return;
        }
        GetModel(heroineID)?.SetCyclePhase(phase);
    }

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

    // ─────────────────────────────────────────────
    // Awake 與性興味
    // ─────────────────────────────────────────────

    /// <summary>Awake（0～120）；Lua 可傳入小數，範圍由 Model 限制。</summary>
    public double GetAwake(string heroineID) => GetModel(heroineID)?.GetAwake() ?? 0f;
    public void SetAwake(string heroineID, double value) => GetModel(heroineID)?.SetAwake((float)value);
    public void AddAwake(string heroineID, double amount) => GetModel(heroineID)?.AddAwake((float)amount);

    /// <summary>回傳睡眠狀態名稱；Lua: GetCurrentSleepState("sister") == "DeepSleep"。找不到角色時回傳空字串。</summary>
    public string GetCurrentSleepState(string heroineID)
        => GetModel(heroineID)?.GetCurrentSleepState().ToString() ?? string.Empty;

    /// <summary>性興味（0～999）；Lua 數值超出 int 範圍時先截斷，避免轉型溢位。</summary>
    public double GetInterest(string heroineID) => GetModel(heroineID)?.GetInterest() ?? 0;
    public void SetInterest(string heroineID, double value)
    {
        if (double.IsNaN(value)) return;
        GetModel(heroineID)?.SetInterest(ClampInterestArgument(value));
    }
    public void AddInterest(string heroineID, double amount)
    {
        if (double.IsNaN(amount)) return;
        GetModel(heroineID)?.AddInterest(ClampInterestArgument(amount));
    }
    private static int ClampInterestArgument(double value)
        => (int)System.Math.Max(int.MinValue, System.Math.Min(int.MaxValue, value));

    // ─────────────────────────────────────────────
    // 性欲
    // ─────────────────────────────────────────────

    public double GetLibido(string heroineID)
    {
        return GetModel(heroineID)?.Libido ?? 0;
    }

    // Lua 數字轉為整數；Model 處理範圍，Weighted 別名不再套用倍率。
    public double GetExcitement(string heroineID) => GetModel(heroineID)?.GetExcitement() ?? 0;
    public void SetExcitement(string heroineID, double value) => GetModel(heroineID)?.SetExcitement((int)value);
    public void AddExcitement(string heroineID, double amount) => GetModel(heroineID)?.AddExcitement((int)amount);
    public void WeightedAddExcitement(string heroineID, double amount) => GetModel(heroineID)?.AddExcitement((int)amount);

    public double GetOrgasm(string heroineID) => GetModel(heroineID)?.GetOrgasm() ?? 0;
    public void SetOrgasm(string heroineID, double value) => GetModel(heroineID)?.SetOrgasm((int)value);
    public void AddOrgasm(string heroineID, double amount) => GetModel(heroineID)?.AddOrgasm((int)amount);

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
    /// 設定信賴值（0~999）。
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
    /// 設定好感度（0~999）。
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

    /// <summary>設定 H 次數；小數截去，下限由 Model 限制。Lua: SetHCount("sister", 3)。</summary>
    public void SetHCount(string heroineID, double value)
    {
        if (double.IsNaN(value)) return;
        GetModel(heroineID)?.SetHCount(ClampHCountArgument(value));
    }

    /// <summary>正數增加、負數減少 H 次數。Lua: AddHCount("sister", 1)。</summary>
    public void AddHCount(string heroineID, double amount)
    {
        if (double.IsNaN(amount)) return;
        GetModel(heroineID)?.AddHCount(ClampHCountArgument(amount));
    }

    private static int ClampHCountArgument(double value)
        => (int)System.Math.Max(int.MinValue, System.Math.Min(int.MaxValue, value));

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
    public double GetMood(string heroineID) => GetModel(heroineID)?.Mood ?? 0;
    public void SetMood(string heroineID, double value) => GetModel(heroineID)?.SetMood((int)value);
    public void AddMood(string heroineID, double amount) => GetModel(heroineID)?.AddMood((int)amount);
}
