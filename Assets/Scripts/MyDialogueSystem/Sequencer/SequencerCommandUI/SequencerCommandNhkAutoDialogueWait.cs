using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 內部等待命令，不需手填。使用與 {{wait}} 相同的設定來源，
    /// 但可在自動模式退出時停止；當句已有 Delay 時，以原 Sequence 為主，不補自動等待。
    /// </summary>
    public class SequencerCommandNhkAutoDialogueWait : SequencerCommand
    {
        private NhkAutoDialogueBridge bridge;
        private int generation;
        private float finishTime;

        public void Awake()
        {
            bridge = NhkAutoDialogueBridge.Find();
            var subtitle = DialogueManager.currentConversationState?.subtitle;
            var story = StoryManager.Instance;
            if (bridge == null || story == null || HasDelayCommand(subtitle?.sequence) ||
                !bridge.TryBeginWait(subtitle))
            {
                Stop();
                return;
            }
            generation = bridge.Generation;
            // 每句取一次快照：拉桿的新秒數從下一句套用，不重設當句倒數。
            float delay = Mathf.Max(0, story.CustomDelay);
            finishTime = DialogueTime.time + delay;
            if (DialogueDebug.logInfo)
                Debug.Log($"[SetAutoDialogue] 本句自動等待 {delay:0.###} 秒。", this);
        }

        private static bool HasDelayCommand(string sequence)
        {
            if (string.IsNullOrWhiteSpace(sequence)) return false;

            // 到命令執行時才判斷：此時快捷字串已展開，且啟用模式的當句也走同一路徑。
            // 解析完整句子，所以 Delay 放在 SetAutoDialogue 前後都有效。
            // 使用原生解析器，避免把註解或其他指令參數中的 Delay(...) 誤認為命令。
            var commands = new SequenceParser().Parse(sequence);
            foreach (var command in commands)
            {
                if (command.command == "Delay") return true;
            }
            return false;
        }

        public void Update()
        {
            if (!isPlaying) return;
            if (bridge == null || !bridge.IsActive || generation != bridge.Generation ||
                DialogueTime.time >= finishTime)
                Stop();
        }
    }
}
