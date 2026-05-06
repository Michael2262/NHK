using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// Sequencer Command: ClearStoryQueue()
    /// 清除 StoryManager 中所有未播放的隊列對話。不會影響正在播放的對話。
    /// 
    /// 用法: ClearStoryQueue()
    /// </summary>
    public class SequencerCommandClearStoryQueue : SequencerCommand
    {
        public void Awake()
        {
            if (StoryManager.Instance != null)
            {
                StoryManager.Instance.ClearConversationQueue();
            }
            else
            {
                Debug.LogError("找不到 StoryManager 實例！");
            }

            Stop();
        }
    }
}
