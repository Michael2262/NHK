using UnityEngine;
using PixelCrushers.DialogueSystem;
using MySpineSystem; // 引用你腳本中的命名空間

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// SpinePlay(控制器ID, 軌道, 動畫名稱, 播放策略, 延遲秒數)
    /// 策略 0～3 沿用 Controller；Line 播一次保留到句末，LineLoop 循環到句末。
    /// Line / LineLoop 不使用延遲秒數，也不阻擋 Sequence 完成。
    /// </summary>
    public class SequencerCommandSpinePlay : SequencerCommand
    {
        public void Awake()
        {
            // 參數解析
            string controllerID = GetParameter(0);
            string trackInput = GetParameter(1);
            string animationName = GetParameter(2);
            string strategy = (GetParameter(3) ?? string.Empty).Trim();
            bool lineLoop = string.Equals(strategy, "LineLoop", System.StringComparison.OrdinalIgnoreCase);
            bool clearOnLineEnd = lineLoop || string.Equals(strategy, "Line", System.StringComparison.OrdinalIgnoreCase);
            float delaySeconds = GetParameterAsFloat(4, -1f); // 預設 -1 使用腳本內建值

            // 取得控制器
            var controller = SpineAnimationController.GetByID(controllerID);
            if (controller == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"[Sequencer] SpinePlay: 找不到 ID 為 '{controllerID}' 的控制器。");
                Stop();
                return;
            }

            // 解析軌道 (支援名稱如 "Body" 或數字如 "0")
            AnimationTrack track;
            if (!System.Enum.TryParse(trackInput, out track))
            {
                if (int.TryParse(trackInput, out int trackIndex))
                {
                    track = (AnimationTrack)trackIndex;
                }
                else
                {
                    if (DialogueDebug.logWarnings) Debug.LogWarning($"[Sequencer] SpinePlay: 無法解析軌道 '{trackInput}'。");
                    Stop();
                    return;
                }
            }

            if (clearOnLineEnd)
            {
                // 必須使用這個 Sequencer 所屬的對話，避免同時對話時綁到別句。
                var conversation = sequencer != null ? sequencer.activeConversationRecord?.conversationController : null;
                // 第一個節點開始播放時，套件可能尚未建立 ActiveConversationRecord。
                // 只接受視圖與本 Sequencer 相符的目前對話作為備援。
                if (conversation == null && sequencer != null && DialogueManager.hasInstance)
                {
                    var current = DialogueManager.conversationController;
                    if (current != null && current.conversationView == sequencer.conversationView)
                        conversation = current;
                }
                var subtitle = conversation?.currentState?.subtitle;
                if (conversation == null || !conversation.isActive || subtitle == null ||
                    sequencer.conversationView == null || !DialogueManager.hasInstance)
                {
                    if (DialogueDebug.logWarnings) Debug.LogWarning("[Sequencer] SpinePlay: Line / LineLoop 必須在對話句子的 Sequence 中使用，已略過播放。");
                    Stop();
                    return;
                }

                var entry = controller.PlayAnimation(track, animationName, SpineAnimationController.ClearMode.KeepTrack);
                if (entry != null)
                {
                    // 使用 Spine 原生循環，保留同一個 TrackEntry 以便識別播放所有權。
                    entry.Loop = lineLoop;
                    var cleanup = DialogueManager.instance.gameObject.AddComponent<SpineDialogueLineCleanup>();
                    cleanup.Initialize(controller, track, entry, conversation, subtitle);
                }
            }
            else
            {
                // 舊數字模式與省略參數的行為保持不變。
                int modeInt = GetParameterAsInt(3, 0);
                controller.PlayAnimation(track, animationName, (SpineAnimationController.ClearMode)modeInt, delaySeconds);
            }

            Stop();
        }
    }
}
