using HutongGames.PlayMaker;
using PixelCrushers.DialogueSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 提早於所有預設順序腳本（含 PlayMakerFSM）Awake，
// 確保自動啟動的 FSM 呼叫本 Manager 時 Instance 已註冊完畢。
// （-900 是 GameStatusService，本 Manager 排在其後）
[DefaultExecutionOrder(-500)]
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
        // 每個場景各有一份 FsmValueManager（registeredFSMs 指向本場景物件），
        // 因此本場景這一份一律接管 Instance，不再「搶不到就自殺」——
        // 否則跨場景並存載入時，新場景這份會誤把自己 Destroy 掉，
        // 等舊場景卸載後 Instance 變 null，造成 SendEvent 全部失效。
        Instance = this;
        InitializeFSMs();

        // 註冊給 Lua (對話系統)：每場景重新註冊到目前這份 instance 是正確的
        PixelCrushers.DialogueSystem.Lua.RegisterFunction("GetIdInt", this, SymbolExtensions.GetMethodInfo(() => GetIntById(string.Empty, string.Empty)));
        PixelCrushers.DialogueSystem.Lua.RegisterFunction("GetIdFloat", this, SymbolExtensions.GetMethodInfo(() => GetFloatById(string.Empty, string.Empty)));
        PixelCrushers.DialogueSystem.Lua.RegisterFunction("GetIdBool", this, SymbolExtensions.GetMethodInfo(() => GetBoolById(string.Empty, string.Empty)));
        PixelCrushers.DialogueSystem.Lua.RegisterFunction("GetIdString", this, SymbolExtensions.GetMethodInfo(() => GetStringById(string.Empty, string.Empty)));
    }

    private void OnDestroy()
    {
        // 只有當「目前 Instance 仍是自己」時才清空，
        // 避免在新舊場景交疊時，舊場景這份被銷毀時誤清掉新場景已接管的 Instance。
        if (Instance == this) Instance = null;
    }

    private void InitializeFSMs()
    {
        foreach (var entry in registeredFSMs)
        {
            if (entry.targetObject == null) continue;

            if (!ResolveEntry(entry))
            {
                Debug.LogWarning($"[FsmValueManager] ID: {entry.id} 於 Awake 找不到指定的 FSM ({entry.fsmName})，將於首次使用時重試解析");
            }
        }
    }

    /// <summary>
    /// 解析 entry 對應的 PlayMakerFSM。
    /// 本 Manager 的 Awake 排序很早（-500），可能早於 PlayMaker 完成 FSM 初始化，
    /// 導致 FsmName 比對失敗；因此解析失敗不視為永久錯誤，
    /// 使用端（GetEntryById）會在每次存取時重試（惰性解析）。
    /// </summary>
    private bool ResolveEntry(FsmEntry entry)
    {
        if (entry.resolvedFsm != null) return true;
        if (entry.targetObject == null) return false;

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
        return entry.resolvedFsm != null;
    }

    /// <summary>
    /// 依 ID 取得 entry，並確保 resolvedFsm 已解析。
    /// 找不到或解析失敗時輸出診斷（含本 Manager 所屬場景與已註冊的 ID 清單），
    /// 方便判斷是「時序問題」還是「Instance 被別的場景接管」。
    /// </summary>
    private FsmEntry GetEntryById(string id, string context)
    {
        var entry = registeredFSMs.FirstOrDefault(e => e.id == id);
        if (entry == null)
        {
            Debug.LogWarning($"[FsmValueManager] {context}: 找不到 ID '{id}'。目前 Instance 屬於場景 [{gameObject.scene.name}]，已註冊的 ID: {string.Join(", ", registeredFSMs.Select(e => e.id))}");
            return null;
        }

        if (!ResolveEntry(entry))
        {
            Debug.LogWarning($"[FsmValueManager] {context}: ID '{id}' 的目標 FSM ({entry.fsmName}) 解析失敗（targetObject={(entry.targetObject == null ? "null" : entry.targetObject.name)}）");
            return null;
        }
        return entry;
    }

    #region 1. 針對特定 ID 操作
    public void SetValue(string id, string varName, object value)
    {
        var entry = GetEntryById(id, $"SetValue({varName})");
        if (entry != null) ApplyValue(entry, varName, value, false);
    }

    public void AdjustValue(string id, string varName, object delta)
    {
        var entry = GetEntryById(id, $"AdjustValue({varName})");
        if (entry != null) ApplyValue(entry, varName, delta, true);
    }
    #endregion

    #region 2. 取消特定 ID 的改動
    public void RevertValue(string id, string varName)
    {
        var entry = GetEntryById(id, $"RevertValue({varName})");
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
        var entry = GetEntryById(id, $"GetIntById({varName})");
        if (entry != null)
        {
            var v = entry.resolvedFsm.FsmVariables.GetFsmInt(varName);
            return (v != null) ? v.Value : 0;
        }
        return 0;
    }

    public float GetFloatById(string id, string varName)
    {
        var entry = GetEntryById(id, $"GetFloatById({varName})");
        if (entry != null)
        {
            var v = entry.resolvedFsm.FsmVariables.GetFsmFloat(varName);
            return (v != null) ? v.Value : 0f;
        }
        return 0f;
    }

    public bool GetBoolById(string id, string varName)
    {
        var entry = GetEntryById(id, $"GetBoolById({varName})");
        if (entry != null)
        {
            var v = entry.resolvedFsm.FsmVariables.GetFsmBool(varName);
            return (v != null) ? v.Value : false;
        }
        return false;
    }

    public string GetStringById(string id, string varName)
    {
        var entry = GetEntryById(id, $"GetStringById({varName})");
        if (entry != null)
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
        var entry = GetEntryById(id, $"SendEventById({eventName})");
        if (entry != null)
        {
            // 必須在 SendEvent「之前」體檢：若事件被接收，FSM 會立刻轉換狀態，事後檢查會誤判
            string failReason = DiagnoseEventDelivery(entry.resolvedFsm, eventName);

            Debug.Log($"[診斷] 對 ID={id} 送出「{eventName}」，FSM={entry.resolvedFsm.FsmName}，目前狀態={entry.resolvedFsm.ActiveStateName}，Active={entry.resolvedFsm.gameObject.activeInHierarchy}");
            entry.resolvedFsm.SendEvent(eventName);

            if (failReason != null)
            {
                Debug.LogWarning($"[FsmValueManager] SendEventById: ID '{id}' 收不到事件「{eventName}」—— {failReason}");
            }
        }
    }

    public void SendEventByGroup(string groupName, string eventName)
    {
        var entries = registeredFSMs.Where(e => e.group == groupName);
        foreach (var entry in entries)
        {
            if (ResolveEntry(entry))
            {
                string failReason = DiagnoseEventDelivery(entry.resolvedFsm, eventName);
                entry.resolvedFsm.SendEvent(eventName);

                if (failReason != null)
                {
                    Debug.LogWarning($"[FsmValueManager] SendEventByGroup({groupName}): ID '{entry.id}' 收不到事件「{eventName}」—— {failReason}");
                }
            }
        }
    }

    /// <summary>
    /// 檢查目標 FSM 目前是否有辦法接收該事件。
    /// PlayMaker 對無人接收的事件完全沉默，這裡補上診斷。
    /// 收得到回傳 null，收不到回傳原因說明。
    /// </summary>
    private static string DiagnoseEventDelivery(PlayMakerFSM fsm, string eventName)
    {
        if (!fsm.gameObject.activeInHierarchy)
            return $"GameObject [{fsm.gameObject.name}] 未啟用（activeInHierarchy=false）";

        if (!fsm.enabled)
            return "PlayMakerFSM 元件未啟用（enabled=false）";

        var f = fsm.Fsm;
        if (f == null || f.ActiveState == null)
            return "FSM 尚未啟動（還沒進入任何 State）";

        // 全域轉換
        foreach (var t in f.GlobalTransitions)
        {
            if (t.EventName == eventName) return null;
        }

        // 當前 State 的轉換
        foreach (var t in f.ActiveState.Transitions)
        {
            if (t.EventName == eventName) return null;
        }

        return $"當前狀態 [{f.ActiveStateName}] 沒有「{eventName}」的轉換，FSM 也沒有它的全域轉換（Global Transition）";
    }

    #endregion
    private void ApplyValue(FsmEntry entry, string varName, object value, bool isDelta)
    {
        // 確保 resolvedFsm 存在（失敗時重試解析，涵蓋 Group 系列呼叫）
        if (!ResolveEntry(entry)) return;

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