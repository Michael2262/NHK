using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>內部命令：依當句原生 {{end}} 時長等待，玩家仍可依原設定按繼續。</summary>
    public class SequencerCommandNhkDefaultDialogueWait : SequencerCommand
    {
        private NhkAutoDialogueBridge bridge;
        private int generation;
        private float finishTime;

        public void Awake()
        {
            bridge = NhkAutoDialogueBridge.Find();
            var subtitle = DialogueManager.currentConversationState?.subtitle;
            if (bridge == null || HasExplicitTiming(subtitle?.sequence) ||
                !bridge.TryBeginDefaultWait(subtitle))
            {
                Stop();
                return;
            }

            generation = bridge.DefaultWaitGeneration;
            // ConversationView 在播放 Sequence 前已算好 {{end}}，包含語系文字、停頓與覆寫設定。
            float delay = Mathf.Max(0, sequencer.subtitleEndTime);
            finishTime = DialogueTime.time + delay;
            if (DialogueDebug.logInfo)
                Debug.Log($"[SetDefaultWait] 本句預設等待 {delay:0.###} 秒。", this);
        }

        private static bool HasExplicitTiming(string sequence)
        {
            if (string.IsNullOrWhiteSpace(sequence)) return false;
            // 執行時快捷字串已展開；也保留既有 {{wait}} / {{default}} 展開後的 Delay。
            foreach (var command in new SequenceParser().Parse(sequence))
            {
                if (command.command == "Delay" || command.command == "None" || command.command == "Continue")
                    return true;
            }
            return false;
        }

        public void Update()
        {
            if (!isPlaying) return;
            if (bridge == null || bridge.IsActive || !bridge.IsDefaultWaitActive ||
                generation != bridge.DefaultWaitGeneration || DialogueTime.time >= finishTime)
                Stop();
        }
    }
}
