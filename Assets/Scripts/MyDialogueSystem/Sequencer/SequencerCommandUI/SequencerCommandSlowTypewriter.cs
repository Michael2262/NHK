using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// SlowTypewriter()：本句每秒 15 字；SlowTypewriter(5)：本句每秒 5 字。
    /// 只支援直接放在該句 Sequence，不使用 @時間 或 @Message 排程。
    /// NhkUISubtitlePanel 在顯示前套用速度，換句／關閉時自動還原。
    /// 本命令等待打字完成，避免只有這個指令時 Sequence 立刻結束。
    /// 不強制 Continue，玩家仍可依原本的繼續鈕設定操作。
    /// </summary>
    public class SequencerCommandSlowTypewriter : SequencerCommand
    {
        private Subtitle subtitle;
        private NhkUISubtitlePanel[] panels;

        public void Awake()
        {
            subtitle = DialogueManager.currentConversationState?.subtitle;
            panels = FindObjectsByType<NhkUISubtitlePanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Update();
        }

        public void Update()
        {
            if (!isPlaying) return;
            if (subtitle != null && panels != null)
            {
                foreach (var panel in panels)
                {
                    if (panel != null && panel.IsWaitingForSlowTypewriter(subtitle)) return;
                }
            }
            Stop();
        }
    }
}
