using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>舊 GiftUIShow 命令已退役；立即結束，避免既有對話流程等待送禮。</summary>
    public class SequencerCommandGiftUIShow : SequencerCommand
    {
        public void Awake()
        {
            Debug.LogWarning("[GiftUIShow] 道具送禮功能已移除，請清理對話中的 GiftUIShow 命令。");
            Stop();
        }
    }
}
