using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ProgressValueSetter
/// 負責批量操作與設定特定的進度數值 (Progress Value Definitions)。
/// </summary>
public class ProgressValueSetter : MonoBehaviour
{
    [Header("目標設定")]
    [Tooltip("指定要操作的 ProgressValueDefinition SO 檔案列表")]
    public List<ProgressValueDefinition> targetValues = new List<ProgressValueDefinition>();

    [Header("預設參數")]
    [Tooltip("執行 SetValues() 時要設定的目標數值")]
    public int valueToSet = 1;

    [Tooltip("執行 AddValues() 時要增加的量 (可為負數)")]
    public int amountToAdd = 1;

    // ───── 公用 API ─────

    /// <summary> 將所有目標數值設定為指定值 (valueToSet) </summary>
    public void SetValues()
    {
        ProcessAll(val => GameStatusService.Instance.ProgressFlags.SetValue(val.FlagID, valueToSet));
    }

    /// <summary> 將所有目標數值增加指定偏移量 (amountToAdd) </summary>
    public void AddValues()
    {
        ProcessAll(val => GameStatusService.Instance.ProgressFlags.AddValue(val.FlagID, amountToAdd));
    }

    /// <summary> 將所有目標數值歸零 (從系統中移除) </summary>
    public void ResetValues()
    {
        if (!IsReady()) return;
        foreach (var val in targetValues)
        {
            if (val != null) GameStatusService.Instance.ProgressFlags.RemoveFlag(val.FlagID);
        }
    }

    /// <summary> 
    /// 傳入自定義數值進行設定 
    /// (適用於 UnityEvent 呼叫，例如 Slider 或 InputField)
    /// </summary>
    public void SetValuesExplicit(int customValue)
    {
        ProcessAll(val => GameStatusService.Instance.ProgressFlags.SetValue(val.FlagID, customValue));
    }

    // ───── 內部輔助 ─────

    private void ProcessAll(System.Action<ProgressValueDefinition> action)
    {
        if (!IsReady()) return;
        foreach (var val in targetValues)
        {
            if (val != null) action?.Invoke(val);
        }
    }

    private bool IsReady()
    {
        if (targetValues == null || targetValues.Count == 0)
        {
            Debug.LogWarning($"[ProgressValueSetter] {gameObject.name} 的 targetValues 列表為空。");
            return false;
        }
        if (GameStatusService.Instance?.ProgressFlags == null) return false;
        return true;
    }

    // ───── Context Menu ─────

    [ContextMenu("Debug: Set Values")]
    private void MenuSetValues() => SetValues();

    [ContextMenu("Debug: Add Values")]
    private void MenuAddValues() => AddValues();

    [ContextMenu("Debug: Check All Values")]
    private void MenuCheckStatus()
    {
        if (!IsReady()) return;
        foreach (var val in targetValues)
        {
            if (val == null) continue;
            int currentVal = GameStatusService.Instance.ProgressFlags.GetValue(val.FlagID);
            Debug.Log($"[ProgressValueSetter] '{val.FlagID}': <color=cyan>{currentVal}</color> (預設: {val.DefaultValue})");
        }
    }
}