using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ProgressFlagSetter
/// 負責批量操作與設定特定的進度標記 (Progress Flags)。
/// </summary>
public class ProgressFlagSetter : MonoBehaviour
{
    [Header("目標設定")]
    [Tooltip("指定要操作的 ProgressFlagDefinition SO 檔案列表")]
    public List<ProgressFlagDefinition> targetFlags = new List<ProgressFlagDefinition>();

    [Header("預設參數")]
    [Tooltip("當呼叫 SetFlagsActiveDefault() 時，若為開啟，是否標記為場景級別 (false=永久, true=場景)")]
    public bool defaultAsTemporary = false;

    // ───── 公用 API (支援批次處理與 Bool 切換) ─────

    /// <summary>
    /// 核心 API：根據 bool 值決定開啟或關閉整批 Flag。
    /// 開啟時會依據 defaultAsTemporary 決定生命週期；關閉時則全部移除。
    /// </summary>
    /// <param name="active">true = 開啟, false = 關閉</param>
    public void SetFlagsActive(bool active)
    {
        if (!IsReady()) return;

        if (active)
        {
            // 依據 Inspector 預設值開啟
            Trigger();
        }
        else
        {
            // 全部關閉
            SetFlagsInactive();
        }
    }

    /// <summary> 使用 Inspector 預設值開啟所有 Flag </summary>
    public void Trigger()
    {
        if (defaultAsTemporary) SetAllAsScene();
        else SetAllAsPersistent();
    }

    /// <summary> 關閉 (移除) 所有目標 Flag </summary>
    public void SetFlagsInactive()
    {
        if (!IsReady()) return;
        foreach (var flag in targetFlags)
        {
            if (flag != null) GameStatusService.Instance.ProgressFlags.RemoveFlag(flag.FlagID);
        }
    }

    // ───── 各種生命週期的批次設定 ─────

    public void SetAllAsPersistent() => ProcessAll(id => GameStatusService.Instance.ProgressFlags.AddPersistentFlag(id));
    public void SetAllAsScene() => ProcessAll(id => GameStatusService.Instance.ProgressFlags.AddSceneFlag(id));
    public void SetAllAsSlot() => ProcessAll(id => GameStatusService.Instance.ProgressFlags.AddSlotFlag(id));
    public void SetAllAsPhase() => ProcessAll(id => GameStatusService.Instance.ProgressFlags.AddPhaseFlag(id));
    public void SetAllAsDaily() => ProcessAll(id => GameStatusService.Instance.ProgressFlags.AddDailyFlag(id));

    /// <summary> 切換整批狀態 (若列表中第一個 Flag 是開的，就全部關掉；反之亦然) </summary>
    public void ToggleFlags()
    {
        if (!IsReady()) return;
        // 以第一個 Flag 作為判斷基準
        bool firstActive = GameStatusService.Instance.ProgressFlags.Contains(targetFlags[0].FlagID);
        SetFlagsActive(!firstActive);
    }

    // ───── 內部輔助 ─────

    private void ProcessAll(System.Action<string> action)
    {
        if (!IsReady()) return;
        foreach (var flag in targetFlags)
        {
            if (flag != null) action?.Invoke(flag.FlagID);
        }
    }

    private bool IsReady()
    {
        if (targetFlags == null || targetFlags.Count == 0)
        {
            Debug.LogWarning($"[ProgressFlagSetter] {gameObject.name} 的 targetFlags 列表為空。");
            return false;
        }
        if (GameStatusService.Instance?.ProgressFlags == null) return false;
        return true;
    }

    // ───── Context Menu (Inspector 右鍵選單) ─────

    [ContextMenu("Debug: Set All Active (Default)")]
    private void MenuSetAllActive() => SetFlagsActive(true);

    [ContextMenu("Debug: Set All Inactive")]
    private void MenuSetAllInactive() => SetFlagsActive(false);

    [ContextMenu("Debug: Check All Status")]
    private void MenuCheckStatus()
    {
        if (!IsReady()) return;
        foreach (var flag in targetFlags)
        {
            if (flag == null) continue;
            bool active = GameStatusService.Instance.ProgressFlags.Contains(flag.FlagID);
            Debug.Log($"[ProgressFlagSetter] '{flag.FlagID}': {(active ? "<color=green>ON</color>" : "<color=red>OFF</color>")}");
        }
    }
}