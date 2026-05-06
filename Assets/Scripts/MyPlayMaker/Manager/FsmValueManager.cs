using HutongGames.PlayMaker;
using PixelCrushers.DialogueSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FsmValueManager : MonoBehaviour
{
    public static FsmValueManager Instance { get; private set; }

    [System.Serializable]
    public class FsmEntry
    {
        public string id;
        public string group;

        [Header("目標物件與 FSM 設定")]
        public GameObject targetObject;
        [UnityEngine.Tooltip("若不填，則會抓取物件上第一個 FSM")]
        public string fsmName;

        [HideInInspector]
        public PlayMakerFSM resolvedFsm; // 執行時解析出來的 FSM

        // 變數名稱 -> 原始數值
        public Dictionary<string, object> initialValues = new Dictionary<string, object>();
    }

    [Header("在 Inspector 中手動註冊")]
    public List<FsmEntry> registeredFSMs = new List<FsmEntry>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeFSMs();

            // 註冊給 Lua (對話系統)
            PixelCrushers.DialogueSystem.Lua.RegisterFunction("GetIdInt", this, SymbolExtensions.GetMethodInfo(() => GetIntById(string.Empty, string.Empty)));
            PixelCrushers.DialogueSystem.Lua.RegisterFunction("GetIdFloat", this, SymbolExtensions.GetMethodInfo(() => GetFloatById(string.Empty, string.Empty)));
            PixelCrushers.DialogueSystem.Lua.RegisterFunction("GetIdBool", this, SymbolExtensions.GetMethodInfo(() => GetBoolById(string.Empty, string.Empty)));
            PixelCrushers.DialogueSystem.Lua.RegisterFunction("GetIdString", this, SymbolExtensions.GetMethodInfo(() => GetStringById(string.Empty, string.Empty)));
        }
        else { Destroy(gameObject); }
    }

    private void InitializeFSMs()
    {
        foreach (var entry in registeredFSMs)
        {
            if (entry.targetObject == null) continue;

            if (string.IsNullOrEmpty(entry.fsmName))
            {
                entry.resolvedFsm = entry.targetObject.GetComponent<PlayMakerFSM>();
            }
            else
            {
                // 找尋名稱匹配的 FSM
                var fsms = entry.targetObject.GetComponents<PlayMakerFSM>();
                entry.resolvedFsm = fsms.FirstOrDefault(f => f.FsmName == entry.fsmName);
            }

            if (entry.resolvedFsm == null)
            {
                Debug.LogWarning($"[FsmValueManager] ID: {entry.id} 找不到指定的 FSM ({entry.fsmName})");
            }
        }
    }

    #region 1. 針對特定 ID 操作
    public void SetValue(string id, string varName, object value)
    {
        var entry = registeredFSMs.FirstOrDefault(e => e.id == id);
        if (entry != null) ApplyValue(entry, varName, value, false);
    }

    public void AdjustValue(string id, string varName, object delta)
    {
        var entry = registeredFSMs.FirstOrDefault(e => e.id == id);
        if (entry != null) ApplyValue(entry, varName, delta, true);
    }
    #endregion

    #region 2. 取消特定 ID 的改動
    public void RevertValue(string id, string varName)
    {
        var entry = registeredFSMs.FirstOrDefault(e => e.id == id);
        if (entry != null && entry.initialValues.ContainsKey(varName))
        {
            ApplyValue(entry, varName, entry.initialValues[varName], false);
        }
    }
    #endregion

    #region 3. 針對特定 Group 操作
    public void SetGroupValue(string groupName, string varName, object value)
    {
        var entries = registeredFSMs.Where(e => e.group == groupName);
        foreach (var entry in entries) ApplyValue(entry, varName, value, false);
    }

    public void AdjustGroupValue(string groupName, string varName, object delta)
    {
        var entries = registeredFSMs.Where(e => e.group == groupName);
        foreach (var entry in entries) ApplyValue(entry, varName, delta, true);
    }
    #endregion

    #region 4. 取消特定 Group 的改動
    public void RevertGroupValue(string groupName, string varName)
    {
        var entries = registeredFSMs.Where(e => e.group == groupName);
        foreach (var entry in entries)
        {
            if (entry.initialValues.ContainsKey(varName))
                ApplyValue(entry, varName, entry.initialValues[varName], false);
        }
    }
    #endregion

    #region 5. 全部恢復初始
    public void ResetAll()
    {
        foreach (var entry in registeredFSMs)
        {
            foreach (var kvp in entry.initialValues)
            {
                ApplyValue(entry, kvp.Key, kvp.Value, false);
            }
        }
    }
    #endregion

    #region 6. 讀取數值 (供 Lua 使用)
    public int GetIntById(string id, string varName)
    {
        var entry = registeredFSMs.FirstOrDefault(e => e.id == id);
        if (entry != null && entry.resolvedFsm != null)
        {
            var v = entry.resolvedFsm.FsmVariables.GetFsmInt(varName);
            return (v != null) ? v.Value : 0;
        }
        return 0;
    }

    public float GetFloatById(string id, string varName)
    {
        var entry = registeredFSMs.FirstOrDefault(e => e.id == id);
        if (entry != null && entry.resolvedFsm != null)
        {
            var v = entry.resolvedFsm.FsmVariables.GetFsmFloat(varName);
            return (v != null) ? v.Value : 0f;
        }
        return 0f;
    }

    public bool GetBoolById(string id, string varName)
    {
        var entry = registeredFSMs.FirstOrDefault(e => e.id == id);
        if (entry != null && entry.resolvedFsm != null)
        {
            var v = entry.resolvedFsm.FsmVariables.GetFsmBool(varName);
            return (v != null) ? v.Value : false;
        }
        return false;
    }

    public string GetStringById(string id, string varName)
    {
        var entry = registeredFSMs.FirstOrDefault(e => e.id == id);
        if (entry != null && entry.resolvedFsm != null)
        {
            var v = entry.resolvedFsm.FsmVariables.GetFsmString(varName);
            return (v != null) ? v.Value : "";
        }
        return "";
    }
    #endregion

    #region 7. 發送事件 (供 Sequence Command 使用)

    public void SendEventById(string id, string eventName)
    {
        var entry = registeredFSMs.FirstOrDefault(e => e.id == id);
        if (entry != null && entry.resolvedFsm != null)
        {
            entry.resolvedFsm.SendEvent(eventName);
        }
        else
        {
            Debug.LogWarning($"[FsmValueManager] 無法發送事件 {eventName}，找不到 ID: {id}");
        }
    }

    public void SendEventByGroup(string groupName, string eventName)
    {
        var entries = registeredFSMs.Where(e => e.group == groupName);
        foreach (var entry in entries)
        {
            if (entry.resolvedFsm != null)
            {
                entry.resolvedFsm.SendEvent(eventName);
            }
        }
    }

    #endregion
    private void ApplyValue(FsmEntry entry, string varName, object value, bool isDelta)
    {
        // 確保 resolvedFsm 存在
        if (entry.resolvedFsm == null) return;

        if (!entry.initialValues.ContainsKey(varName))
        {
            if (!CaptureVariable(entry, varName)) return;
        }

        // --- 處理各種類型 ---
        // 1. Int
        if (value is int iValue)
        {
            var v = entry.resolvedFsm.FsmVariables.GetFsmInt(varName);
            if (v != null) v.Value = isDelta ? v.Value + iValue : iValue;
        }
        // 2. Float
        else if (value is float fValue)
        {
            var v = entry.resolvedFsm.FsmVariables.GetFsmFloat(varName);
            if (v != null) v.Value = isDelta ? v.Value + fValue : fValue;
        }
        // 3. Bool
        else if (value is bool bValue)
        {
            var v = entry.resolvedFsm.FsmVariables.GetFsmBool(varName);
            if (v != null) v.Value = bValue;
        }
        // 4. String
        else if (value is string sValue)
        {
            var v = entry.resolvedFsm.FsmVariables.GetFsmString(varName);
            if (v != null) v.Value = sValue;
        }
    }

    private bool CaptureVariable(FsmEntry entry, string varName)
    {
        var fsmVars = entry.resolvedFsm.FsmVariables;

        // 檢查 Int
        var fInt = fsmVars.GetFsmInt(varName);
        if (fInt != null && !fInt.IsNone) { entry.initialValues[varName] = fInt.Value; return true; }

        // 檢查 Float
        var fFloat = fsmVars.GetFsmFloat(varName);
        if (fFloat != null && !fFloat.IsNone) { entry.initialValues[varName] = fFloat.Value; return true; }

        // 檢查 Bool
        var fBool = fsmVars.GetFsmBool(varName);
        if (fBool != null && !fBool.IsNone) { entry.initialValues[varName] = fBool.Value; return true; }

        // 檢查 String
        var fString = fsmVars.GetFsmString(varName);
        if (fString != null && !fString.IsNone) { entry.initialValues[varName] = fString.Value; return true; }

        Debug.LogWarning($"[FsmValueManager] 找不到變數: {varName} 於 ID: {entry.id}");
        return false;
    }
}