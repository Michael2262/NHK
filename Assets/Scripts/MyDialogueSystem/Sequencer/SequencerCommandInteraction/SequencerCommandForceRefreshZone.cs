using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// Sequencer Command: ForceRefreshZone()
/// 強制刷新互動區域的所有狀態。
/// 
/// 用法：
///   ForceRefreshZone()
/// </summary>

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
        public class SequencerCommandForceRefreshZone : SequencerCommand
        {
            private void Awake()
        {
             if (InteractionZoneController.Instance != null)
            {
                 InteractionZoneController.Instance.ForceRefresh();
            }
             else
            {
                 if (DialogueDebug.logWarnings)
                 Debug.LogWarning("[SequencerCommandForceRefreshZone] InteractionZoneController.Instance 為 null");
            }

         Stop();
        }
    }
}
