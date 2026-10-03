using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// SetAutoDialogue(true)：啟用強制自動換句，當句也等待。
    /// SetAutoDialogue(false)：退出，還原原本的繼續模式與字幕取消輸入。
    /// 使用 NhkDialogueUI；只支援單一對話。每句讀取 StoryManager 的最新等待秒數。
    /// </summary>
    public class SequencerCommandSetAutoDialogue : SequencerCommand
    {
        public void Awake()
        {
            if (!bool.TryParse(GetParameter(0, string.Empty), out bool enable))
            {
                Debug.LogWarning("[SetAutoDialogue] 請填入 true 或 false。", this);
                Stop();
                return;
            }

            var bridge = NhkAutoDialogueBridge.Find();
            if (!enable)
            {
                if (bridge != null)
                {
                    bridge.DisableAutoDialogue();
                    if (bridge.IsDefaultWaitActive)
                        sequencer.PlayCommand("NhkDefaultDialogueWait", false, 0, null, null);
                }
                Stop();
                return;
            }

            if (!DialogueManager.isConversationActive || DialogueManager.conversationView == null ||
                DialogueManager.allowSimultaneousConversations ||
                !(DialogueManager.dialogueUI is NhkDialogueUI) || StoryManager.Instance == null)
            {
                Debug.LogWarning("[SetAutoDialogue] 需要正在播放的單一對話、NhkDialogueUI 與 StoryManager；請關閉 Allow Simultaneous Conversations。", this);
                Stop();
                return;
            }

            if (bridge == null)
                bridge = DialogueManager.instance.gameObject.AddComponent<NhkAutoDialogueBridge>();
            bridge.enabled = true;
            bridge.EnableAutoDialogue();

            // 當句的 OnConversationLine 已經結束，直接在同一個 Sequencer 補等待。
            // 重複 true 或同句已有自動等待時，由 Session 避免重複起算。
            sequencer.PlayCommand("NhkAutoDialogueWait", false, 0, null, null);
            Stop();
        }
    }

}
