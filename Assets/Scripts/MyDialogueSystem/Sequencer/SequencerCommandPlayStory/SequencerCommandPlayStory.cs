using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// Sequencer Command: PlayStory(title, mode)
    /// 參數 0: 對話名稱 (String)
    /// 參數 1: 模式 (Skip, Interrupt, Queue, Priority)，預設為 Queue
    /// 
    /// 用法: PlayStory(MyConversation, Priority)
    /// </summary>
    public class SequencerCommandPlayStory : SequencerCommand
    {
        public void Awake()
        {
            string title = GetParameter(0);
            string modeStr = GetParameter(1, "Queue");

            if (string.IsNullOrEmpty(title))
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("Sequencer Command PlayStory(): 對話名稱不可為空");
                Stop();
                return;
            }

            DialogueMode mode;
            if (!System.Enum.TryParse(modeStr, true, out mode))
            {
                mode = DialogueMode.Queue;
            }

            if (StoryManager.Instance != null)
            {
                StoryManager.Instance.PlayConversation(title, mode);
            }
            else
            {
                Debug.LogError("找不到 StoryManager 實例！");
            }

            Stop();
        }
    }
}