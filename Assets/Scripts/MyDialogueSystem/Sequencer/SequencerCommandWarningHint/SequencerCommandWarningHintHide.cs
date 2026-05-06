using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 用法: WarningHintHide()
    /// 立刻隱藏警告提示。
    /// </summary>
    public class SequencerCommandWarningHintHide : SequencerCommand
    {
        public void Awake()
        {
            if (WarningHintController.Instance != null)
                WarningHintController.Instance.HideWarning();

            Stop();
        }
    }
}
