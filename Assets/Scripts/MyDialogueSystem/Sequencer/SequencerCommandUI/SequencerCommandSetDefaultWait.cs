using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// SetDefaultWait(true)：當句與後續有台詞的句子自動補上原生 {{end}} 等待。
    /// SetDefaultWait(false)：停止補等待；不改變繼續按鈕與輸入設定。
    /// 明確的 Delay / None / Continue 優先；Auto 啟用時暫停補預設等待。
    /// 對話結束會自動關閉，不影響下一段對話。需要 NhkDialogueUI 與單一對話模式。
    /// </summary>
    public class SequencerCommandSetDefaultWait : SequencerCommand
    {
        public void Awake()
        {
            if (!bool.TryParse(GetParameter(0, string.Empty), out bool enable))
            {
                Debug.LogWarning("[SetDefaultWait] 請填入 true 或 false。", this);
                Stop();
                return;
            }

            var bridge = NhkAutoDialogueBridge.Find();
            if (!enable)
            {
                if (bridge != null) bridge.SetDefaultWait(false);
                Stop();
                return;
            }

            if (!DialogueManager.isConversationActive || DialogueManager.conversationView == null ||
                DialogueManager.allowSimultaneousConversations || !(DialogueManager.dialogueUI is NhkDialogueUI))
            {
                Debug.LogWarning("[SetDefaultWait] 需要正在播放的單一對話與 NhkDialogueUI；請關閉 Allow Simultaneous Conversations。", this);
                Stop();
                return;
            }

            if (bridge == null)
                bridge = DialogueManager.instance.gameObject.AddComponent<NhkAutoDialogueBridge>();
            bridge.enabled = true;
            bridge.SetDefaultWait(true);
            // 啟用當句已經經過逐句通知，直接加入同一個 Sequencer，不必重播原指令。
            sequencer.PlayCommand("NhkDefaultDialogueWait", false, 0, null, null);
            Stop();
        }
    }
}
