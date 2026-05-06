using UnityEngine;

/// <summary>
/// 【Debug 測試腳本】
/// 允許您在 Play Mode 中，透過 Inspector 指定一個 ProgressFlagDefinition SO，
/// 並透過多種生命週期選項測試 Flag 的開啟與關閉。
/// </summary>
public class ProgressFlagDebugger : MonoBehaviour
{
    [Header("測試目標")]
    [Tooltip("請從 Project 視窗拖曳您想測試的 ProgressFlagDefinition SO 檔案到這裡")]
    public ProgressFlagDefinition flagToDebug;

    // ───── Add (開啟) 各類生命週期 ─────

    [ContextMenu("執行：開啟為【永久 Persistent】Flag")]
    private void AddAsPersistent()
    {
        if (!IsReady()) return;
        GameStatusService.Instance.ProgressFlags.AddPersistentFlag(flagToDebug.FlagID);
        Debug.Log($"[Debug] Flag '{flagToDebug.FlagID}' 已開啟 【永久 Persistent】。");
    }

    [ContextMenu("執行：開啟為【場景 Scene】Flag")]
    private void AddAsScene()
    {
        if (!IsReady()) return;
        GameStatusService.Instance.ProgressFlags.AddSceneFlag(flagToDebug.FlagID);
        Debug.Log($"[Debug] Flag '{flagToDebug.FlagID}' 已開啟 【場景 Scene】。");
    }

    [ContextMenu("執行：開啟為【時段 Slot】Flag")]
    private void AddAsSlot()
    {
        if (!IsReady()) return;
        GameStatusService.Instance.ProgressFlags.AddSlotFlag(flagToDebug.FlagID);
        Debug.Log($"[Debug] Flag '{flagToDebug.FlagID}' 已開啟 【時段 Slot】。");
    }

    [ContextMenu("執行：開啟為【階段 Phase】Flag")]
    private void AddAsPhase()
    {
        if (!IsReady()) return;
        GameStatusService.Instance.ProgressFlags.AddPhaseFlag(flagToDebug.FlagID);
        Debug.Log($"[Debug] Flag '{flagToDebug.FlagID}' 已開啟 【階段 Phase】。");
    }

    [ContextMenu("執行：開啟為【每日 Daily】Flag")]
    private void AddAsDaily()
    {
        if (!IsReady()) return;
        GameStatusService.Instance.ProgressFlags.AddDailyFlag(flagToDebug.FlagID);
        Debug.Log($"[Debug] Flag '{flagToDebug.FlagID}' 已開啟 【每日 Daily】。");
    }

    // ───── Remove (關閉) ─────

    [ContextMenu("執行：關閉 (Remove) 指定 Flag")]
    private void RemoveSelectedFlag()
    {
        if (!IsReady()) return;
        // RemoveFlag 會同時嘗試從所有清單中移除
        GameStatusService.Instance.ProgressFlags.RemoveFlag(flagToDebug.FlagID);
        Debug.Log($"[Debug] Flag '{flagToDebug.FlagID}' 已從所有清單中移除。");
    }

    // ───── Check (檢查) ─────

    [ContextMenu("執行：檢查指定 Flag 當前狀態")]
    private void CheckSelectedFlagStatus()
    {
        if (!IsReady()) return;
        // Contains 會檢查所有桶子
        bool isActive = GameStatusService.Instance.ProgressFlags.Contains(flagToDebug.FlagID);
        Debug.Log($"[Debug] Flag '{flagToDebug.FlagID}' 當前狀態為: {(isActive ? "<color=green>開啟 (Active)</color>" : "<color=red>關閉 (Inactive)</color>")}");
    }

    // ───── 輔助方法 ─────

    private bool IsReady()
    {
        if (flagToDebug == null)
        {
            Debug.LogWarning("請先在 Inspector 的 'Flag To Debug' 欄位中指定一個 Flag Definition SO。", this);
            return false;
        }

        if (GameStatusService.Instance == null || GameStatusService.Instance.ProgressFlags == null)
        {
            Debug.LogError("GameStatusService 或 ProgressFlags 尚未初始化！", this);
            return false;
        }
        return true;
    }
}