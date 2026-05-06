using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// Sequencer Command: SetDescriptionPanel(open|close)
/// 開啟或關閉可操作區域說明面板。
/// 
/// 用法：
///   SetDescriptionPanel(open)
///   SetDescriptionPanel(close)
/// 
/// 參數：
///   state: "open" 或 "close"（不分大小寫）
/// </summary>

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandSetDescriptionPanel : SequencerCommand
{
    private void Awake()
    {
        string state = GetParameter(0);

        if (InteractionZoneController.Instance != null)
        {
            bool open = string.Equals(state, "open", System.StringComparison.OrdinalIgnoreCase);
            InteractionZoneController.Instance.SetDescriptionPanel(open);
        }
        else
        {
            if (DialogueDebug.logWarnings)
                Debug.LogWarning("[SequencerCommandSetDescriptionPanel] InteractionZoneController.Instance 為 null");
        }

        Stop();
    }
}
}
