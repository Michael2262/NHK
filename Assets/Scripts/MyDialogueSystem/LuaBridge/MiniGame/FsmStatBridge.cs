using UnityEngine;
using HutongGames.PlayMaker;
using PixelCrushers.DialogueSystem;
using Tooltip = UnityEngine.TooltipAttribute;

public class FsmStatBridge : MonoBehaviour
{
    [Header("PlayMaker 配置")]
    [Tooltip("掛載 FSM 的物件")]
    public GameObject targetObject;

    [Tooltip("要對接的 FSM 名稱")]
    public string fsmName = "EroMinigameFSM";

    private PlayMakerFSM fsm;

    void OnEnable()
    {
        InitializeFsm();

        // 註冊 Int & Float
        Lua.RegisterFunction("GetFsmInt", this, SymbolExtensions.GetMethodInfo(() => GetFsmInt(string.Empty)));
        Lua.RegisterFunction("GetFsmFloat", this, SymbolExtensions.GetMethodInfo(() => GetFsmFloat(string.Empty)));
        Lua.RegisterFunction("SetFsmInt", this, SymbolExtensions.GetMethodInfo(() => SetFsmInt(string.Empty, (double)0)));
        Lua.RegisterFunction("SetFsmFloat", this, SymbolExtensions.GetMethodInfo(() => SetFsmFloat(string.Empty, (double)0)));

        // 註冊 String & Bool
        Lua.RegisterFunction("GetFsmString", this, SymbolExtensions.GetMethodInfo(() => GetFsmString(string.Empty)));
        Lua.RegisterFunction("SetFsmString", this, SymbolExtensions.GetMethodInfo(() => SetFsmString(string.Empty, string.Empty)));
        Lua.RegisterFunction("GetFsmBool", this, SymbolExtensions.GetMethodInfo(() => GetFsmBool(string.Empty)));
        Lua.RegisterFunction("SetFsmBool", this, SymbolExtensions.GetMethodInfo(() => SetFsmBool(string.Empty, false)));

        // 註冊 Enum
        Lua.RegisterFunction("GetFsmEnum", this, SymbolExtensions.GetMethodInfo(() => GetFsmEnum(string.Empty)));
        Lua.RegisterFunction("SetFsmEnum", this, SymbolExtensions.GetMethodInfo(() => SetFsmEnum(string.Empty, string.Empty)));
    }

    void OnDisable()
    {
        Lua.UnregisterFunction("GetFsmInt");
        Lua.UnregisterFunction("GetFsmFloat");
        Lua.UnregisterFunction("SetFsmInt");
        Lua.UnregisterFunction("SetFsmFloat");
        Lua.UnregisterFunction("GetFsmString");
        Lua.UnregisterFunction("SetFsmString");
        Lua.UnregisterFunction("GetFsmBool");
        Lua.UnregisterFunction("SetFsmBool");
        Lua.UnregisterFunction("GetFsmEnum");
        Lua.UnregisterFunction("SetFsmEnum");
    }

    // --- Enum 操作 (以字串形式讀寫) ---
    public string GetFsmEnum(string varName)
    {
        EnsureFsm();
        if (fsm == null) return "";
        var v = fsm.FsmVariables.GetFsmEnum(varName);
        return (v != null) ? v.Value.ToString() : "";
    }

    public void SetFsmEnum(string varName, string value)
    {
        EnsureFsm();
        if (fsm == null) return;
        var v = fsm.FsmVariables.GetFsmEnum(varName);
        if (v != null)
        {
            try { v.Value = (System.Enum)System.Enum.Parse(v.EnumType, value); }
            catch { Debug.LogWarning($"[FsmStatBridge] 無法將 \"{value}\" 轉換為 {v.EnumType}"); }
        }
    }

    // --- String 操作 ---
    public string GetFsmString(string varName)
    {
        EnsureFsm();
        if (fsm == null) return "";
        var v = fsm.FsmVariables.GetFsmString(varName);
        return (v != null) ? v.Value : "";
    }

    public void SetFsmString(string varName, string value)
    {
        EnsureFsm();
        if (fsm == null) return;
        var v = fsm.FsmVariables.GetFsmString(varName);
        if (v != null) v.Value = value;
    }

    // --- Bool 操作 ---
    public bool GetFsmBool(string varName)
    {
        EnsureFsm();
        if (fsm == null) return false;
        var v = fsm.FsmVariables.GetFsmBool(varName);
        return (v != null) ? v.Value : false;
    }

    public void SetFsmBool(string varName, bool value)
    {
        EnsureFsm();
        if (fsm == null) return;
        var v = fsm.FsmVariables.GetFsmBool(varName);
        if (v != null) v.Value = value;
    }

    // --- Int & Float 操作 ---
    public int GetFsmInt(string varName) { EnsureFsm(); return (fsm != null && fsm.FsmVariables.GetFsmInt(varName) != null) ? fsm.FsmVariables.GetFsmInt(varName).Value : 0; }
    public float GetFsmFloat(string varName) { EnsureFsm(); return (fsm != null && fsm.FsmVariables.GetFsmFloat(varName) != null) ? fsm.FsmVariables.GetFsmFloat(varName).Value : 0f; }
    public void SetFsmInt(string varName, double value) { EnsureFsm(); if (fsm != null) { var v = fsm.FsmVariables.GetFsmInt(varName); if (v != null) v.Value = (int)value; } }
    public void SetFsmFloat(string varName, double value) { EnsureFsm(); if (fsm != null) { var v = fsm.FsmVariables.GetFsmFloat(varName); if (v != null) v.Value = (float)value; } }

    private void InitializeFsm() { if (targetObject != null) fsm = ActionHelpers.GetGameObjectFsm(targetObject, fsmName); }
    private void EnsureFsm() { if (fsm == null) InitializeFsm(); }
}