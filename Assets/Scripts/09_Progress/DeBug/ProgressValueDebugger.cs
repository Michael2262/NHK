using UnityEngine;

/// <summary>
/// 【Debug 測試腳本】
/// 針對 ProgressValueDefinition 進行數值操作測試。
/// </summary>
public class ProgressValueDebugger : MonoBehaviour
{
    [Header("測試目標")]
    [Tooltip("請拖曳 ProgressValueDefinition SO 檔案到這裡")]
    public ProgressValueDefinition valueToDebug;

    [Header("測試參數")]
    public int valueToSet = 10;
    public int amountToAdd = 1;

    [ContextMenu("執行：設定數值 (SetValue)")]
    private void SetSpecifiedValue()
    {
        if (!IsReady()) return;
        GameStatusService.Instance.ProgressFlags.SetValue(valueToDebug.FlagID, valueToSet);
        Debug.Log($"[Debug] Value '{valueToDebug.FlagID}' 已設定為: <color=cyan>{valueToSet}</color>");
    }

    [ContextMenu("執行：增加數值 (AddValue)")]
    private void AddSpecifiedValue()
    {
        if (!IsReady()) return;
        GameStatusService.Instance.ProgressFlags.AddValue(valueToDebug.FlagID, amountToAdd);
        int currentVal = GameStatusService.Instance.ProgressFlags.GetValue(valueToDebug.FlagID);
        Debug.Log($"[Debug] Value '{valueToDebug.FlagID}' 已增加 {amountToAdd}，當前為: <color=cyan>{currentVal}</color>");
    }

    [ContextMenu("執行：讀取當前狀態 (GetValue)")]
    private void GetSpecifiedValue()
    {
        if (!IsReady()) return;
        int currentVal = GameStatusService.Instance.ProgressFlags.GetValue(valueToDebug.FlagID);
        bool isActive = GameStatusService.Instance.ProgressFlags.Contains(valueToDebug.FlagID);

        Debug.Log($"[Debug] Value '{valueToDebug.FlagID}':\n" +
                  $"- 當前數值: <color=cyan>{currentVal}</color>\n" +
                  $"- 定義預設值: {valueToDebug.DefaultValue}\n" +
                  $"- 布林判定 (Value > 0): {(isActive ? "<color=green>TRUE</color>" : "<color=red>FALSE</color>")}");
    }

    private bool IsReady()
    {
        if (valueToDebug == null)
        {
            Debug.LogWarning("[ProgressValueDebugger] 未指定 ProgressValueDefinition。");
            return false;
        }
        if (GameStatusService.Instance?.ProgressFlags == null) return false;
        return true;
    }
}