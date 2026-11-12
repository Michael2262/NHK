using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// SetActorAutoDialogue(true)：依每句實際字幕面板分流。
    /// Actor 自訂框使用 Auto 等待（秒數來源同 {{wait}}），禁止手動換句；
    /// 主／共用框立即解除本模式的 Auto 限制，改補原生 {{end}} 預設等待。
    /// SetActorAutoDialogue(false)：還原啟用前的 Auto / DefaultWait 與手動輸入設定。
    /// 不解除 ContinueButton(off) 的獨立鎖；Delay 等明確時序沿用既有等待命令的規則。
    /// 後續明確呼叫 SetAutoDialogue / SetDefaultWait 會退出分流，改用該指令的設定。
    /// 對話結束會清理。需要 NhkDialogueUI、StoryManager 與單一對話模式。
    /// </summary>
    public class SequencerCommandSetActorAutoDialogue : SequencerCommand
    {
        public void Awake()
        {
            if (!bool.TryParse(GetParameter(0, string.Empty), out bool enable))
            {
                Debug.LogWarning("[SetActorAutoDialogue] 請填入 true 或 false。", this);
                Stop();
                return;
            }

            var bridge = NhkAutoDialogueBridge.Find();
            if (!DialogueManager.isConversationActive || DialogueManager.conversationView == null ||
                sequencer != DialogueManager.conversationView.sequencer ||
                DialogueManager.allowSimultaneousConversations ||
                !(DialogueManager.dialogueUI is NhkDialogueUI) || (enable && StoryManager.Instance == null))
            {
                Debug.LogWarning("[SetActorAutoDialogue] 請在目前單一對話的 Sequence 使用；需要 NhkDialogueUI，啟用時另需 StoryManager。", this);
                Stop();
                return;
            }

            if (enable)
            {
                if (bridge == null)
                    bridge = DialogueManager.instance.gameObject.AddComponent<NhkAutoDialogueBridge>();
                bridge.enabled = true;
                bridge.EnableActorAutoDialogue();
            }
            else if (bridge != null && bridge.IsActorAutoActive)
            {
                bridge.DisableActorAutoDialogue();
            }
            else
            {
                Stop();
                return;
            }

            // 當句已經經過逐句通知，直接補目前模式的等待，不重播原 Sequence。
            // 既有 Session 會避免同句重複等待；退出模式時也讓還原的模式接手。
            if (bridge.IsActive)
                sequencer.PlayCommand("NhkAutoDialogueWait", false, 0, null, null);
            else if (bridge.IsDefaultWaitActive)
                sequencer.PlayCommand("NhkDefaultDialogueWait", false, 0, null, null);
            Stop();
        }
    }
}
