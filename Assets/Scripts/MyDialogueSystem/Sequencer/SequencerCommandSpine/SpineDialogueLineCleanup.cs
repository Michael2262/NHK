using UnityEngine;
using Spine;
using MySpineSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 將指定句子的結束通知轉接成清軌操作；由 SpinePlay 自動建立，不需手動掛載。
    /// 獨立於 SequencerCommand 存活，避免阻擋對話自動推進。
    /// </summary>
    [AddComponentMenu("")]
    public sealed class SpineDialogueLineCleanup : MonoBehaviour
    {
        private SpineAnimationController _controller;
        private AnimationTrack _track;
        private TrackEntry _entry;
        private ConversationController _conversation;
        private Subtitle _subtitle;
        private bool _finished;

        public void Initialize(SpineAnimationController controller, AnimationTrack track, TrackEntry entry,
            ConversationController conversation, Subtitle subtitle)
        {
            _controller = controller;
            _track = track;
            _entry = entry;
            _conversation = conversation;
            _subtitle = subtitle;
            // TrackEntry 會被物件池重用；被替換或釋放時立即放棄所有權。
            _entry.Interrupt += HandleEntryReleased;
            _entry.End += HandleEntryReleased;
            _entry.Dispose += HandleEntryReleased;
        }

        public void OnConversationLineEnd(Subtitle subtitle)
        {
            if (ReferenceEquals(subtitle, _subtitle)) Finish(true);
        }

        public void OnConversationEnd(Transform actor)
        {
            // 可能有同時進行的其他對話，只處理原本所屬的那一段。
            if (_conversation != null && !_conversation.isActive) Finish(true);
        }

        private void LateUpdate()
        {
            if (_finished) return;
            if (_controller == null || !_controller.isActiveAndEnabled || _conversation == null ||
                !_conversation.isActive || _conversation.conversationView == null ||
                !ReferenceEquals(_conversation.currentState?.subtitle, _subtitle))
            {
                // 對話中止、物件停用或視圖銷毀未送出句末通知時的保底清理。
                Finish(true);
            }
        }

        private void HandleEntryReleased(TrackEntry entry)
        {
            // 新動畫已接手，不可清除該軌道。
            Finish(false);
        }

        private void Finish(bool clearTrack)
        {
            if (_finished) return;
            _finished = true;
            var entry = _entry;
            if (entry != null)
            {
                entry.Interrupt -= HandleEntryReleased;
                entry.End -= HandleEntryReleased;
                entry.Dispose -= HandleEntryReleased;
            }
            _entry = null;
            _subtitle = null;
            _conversation = null;

            // 先解除事件再清軌，避免清軌觸發 End / Dispose 時重入。
            if (clearTrack && entry != null && _controller != null &&
                _controller.GetAnimationState()?.GetTrack((int)_track) == entry)
            {
                _controller.StopAnimation(_track);
            }
            _controller = null;
            Destroy(this);
        }

        private void OnDisable()
        {
            Finish(true);
        }

        private void OnDestroy()
        {
            Finish(true);
        }
    }
}
