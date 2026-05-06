using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// Sequencer Command: SwitchGroup(groupName)
/// 切換互動叢集。
/// 
/// 用法：
///   SwitchGroup(Touch)
///   SwitchGroup(Lick)
///   SwitchGroup(Sex)
/// 
/// 參數：
///   groupName: InteractionGroup enum 名稱（不分大小寫）
/// </summary>

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandSwitchGroup : SequencerCommand
{
    private void Awake()
    {
        string groupName = GetParameter(0);

        if (string.IsNullOrEmpty(groupName))
        {
            if (DialogueDebug.logWarnings)
                Debug.LogWarning("[SequencerCommandSwitchGroup] 缺少參數 groupName");
        }
        else if (InteractionZoneController.Instance != null)
        {
            InteractionZoneController.Instance.SwitchGroup(groupName);
        }
        else
        {
            if (DialogueDebug.logWarnings)
                Debug.LogWarning("[SequencerCommandSwitchGroup] InteractionZoneController.Instance 為 null");
        }

        Stop();
    }
}

}