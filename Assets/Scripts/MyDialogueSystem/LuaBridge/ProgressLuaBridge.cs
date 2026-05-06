using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// 將 ProgressFlagModel 的 Flag 和 Value 橋接到 Dialogue System 的 Lua 環境。
/// 掛載於 GameStatusService 同一個 GameObject 上。
/// </summary>
public class ProgressLuaBridge : MonoBehaviour
{
    void OnEnable()
    {
        RegisterLuaFunctions();
    }

    void OnDisable()
    {
        UnregisterLuaFunctions();
    }

    // ==========================================================
    // Lua 函數註冊 / 取消註冊
    // ==========================================================

    private void RegisterLuaFunctions()
    {
        // Flag 相關
        Lua.RegisterFunction("Flag", this, typeof(ProgressLuaBridge).GetMethod("GetFlag"));
        Lua.RegisterFunction("SetFlag", this, typeof(ProgressLuaBridge).GetMethod("SetFlag"));
        Lua.RegisterFunction("RemoveFlag", this, typeof(ProgressLuaBridge).GetMethod("RemoveFlag"));

        // Value 相關
        Lua.RegisterFunction("Value", this, typeof(ProgressLuaBridge).GetMethod("GetValue"));
        Lua.RegisterFunction("SetValue", this, typeof(ProgressLuaBridge).GetMethod("SetValue"));
        Lua.RegisterFunction("AddValue", this, typeof(ProgressLuaBridge).GetMethod("AddValue"));

        Debug.Log("ProgressLuaBridge: Lua functions registered.");
    }

    private void UnregisterLuaFunctions()
    {
        Lua.UnregisterFunction("Flag");
        Lua.UnregisterFunction("SetFlag");
        Lua.UnregisterFunction("RemoveFlag");
        Lua.UnregisterFunction("Value");
        Lua.UnregisterFunction("SetValue");
        Lua.UnregisterFunction("AddValue");

        Debug.Log("ProgressLuaBridge: Lua functions unregistered.");
    }

    // ==========================================================
    // Flag 相關 Lua 函數
    // ==========================================================

    /// <summary>
    /// 檢查 Flag 是否存在。
    /// Lua 用法: Flag("QuestStarted") → 回傳 true/false
    /// </summary>
    public bool GetFlag(string flagName)
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("ProgressLuaBridge: GameStatusService not available.");
            return false;
        }
        return GameStatusService.Instance.ProgressFlags.Contains(flagName);
    }

    /// <summary>
    /// 新增 Flag。
    /// Lua 用法: SetFlag("QuestStarted") 或 SetFlag("QuestStarted", "Scene")
    /// 可用的 lifetime: "Persistent", "Scene", "UntilNextSlot", "UntilNextPhase", "UntilNextDay"
    /// </summary>
    public void SetFlag(string flagName, string lifetime = "Persistent")
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("ProgressLuaBridge: GameStatusService not available.");
            return;
        }

        FlagLifetime flagLifetime = lifetime switch
        {
            "Scene" => FlagLifetime.Scene,
            "UntilNextSlot" => FlagLifetime.UntilNextSlot,
            "UntilNextPhase" => FlagLifetime.UntilNextPhase,
            "UntilNextDay" => FlagLifetime.UntilNextDay,
            _ => FlagLifetime.Persistent
        };

        GameStatusService.Instance.ProgressFlags.AddFlag(flagName, flagLifetime);
    }

    /// <summary>
    /// 移除 Flag。
    /// Lua 用法: RemoveFlag("QuestStarted")
    /// </summary>
    public void RemoveFlag(string flagName)
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("ProgressLuaBridge: GameStatusService not available.");
            return;
        }
        GameStatusService.Instance.ProgressFlags.RemoveFlag(flagName);
    }

    // ==========================================================
    // Value 相關 Lua 函數
    // ==========================================================

    /// <summary>
    /// 取得數值。
    /// Lua 用法: Value("Affection") → 回傳數值
    /// </summary>
    public double GetValue(string key)
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("ProgressLuaBridge: GameStatusService not available.");
            return 0;
        }
        return GameStatusService.Instance.ProgressFlags.GetValue(key);
    }

    /// <summary>
    /// 設定數值。
    /// Lua 用法: SetValue("Affection", 50)
    /// </summary>
    public void SetValue(string key, double value)
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("ProgressLuaBridge: GameStatusService not available.");
            return;
        }
        GameStatusService.Instance.ProgressFlags.SetValue(key, (int)value);
    }

    /// <summary>
    /// 增減數值。
    /// Lua 用法: AddValue("Affection", 10) 或 AddValue("Affection", -5)
    /// </summary>
    public void AddValue(string key, double amount)
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogWarning("ProgressLuaBridge: GameStatusScope not available.");
            return;
        }
        GameStatusService.Instance.ProgressFlags.AddValue(key, (int)amount);
    }
}