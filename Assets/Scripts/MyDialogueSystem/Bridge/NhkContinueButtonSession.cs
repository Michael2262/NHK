using System;

namespace PixelCrushers.DialogueSystem
{
    /// <summary>
    /// 單次對話操作的共用控制；不計時，也不推進遊戲時間。
    /// 呼叫端保持指令執行中、每幀呼叫 Tick，條件完成時呼叫 Complete。
    /// 當句結束與指令銷毀時，分別轉送 OnLineEnd 與 Dispose。
    /// </summary>
    internal sealed class NhkContinueButtonSession : IDisposable
    {
        private enum State { Running, CompleteWait, WaitingForClick, Reveal, Advance, Finished }

        private readonly ConversationView view;
        private readonly Subtitle subtitle;
        private readonly Action stopCommand;
        private State state;
        private bool heldLock;
        private bool buttonRequested;
        private NhkAutoDialogueBridge pausedAuto;
        private int pausedAutoGeneration;

        public NhkContinueButtonSession(Sequencer owner, Action stopCommand)
        {
            this.stopCommand = stopCommand;
            var currentView = DialogueManager.conversationView;
            // 獨立播放的 Sequence 不取得其他對話的控制權。
            if (owner != null && currentView != null && currentView.sequencer == owner)
            {
                view = currentView;
                subtitle = DialogueManager.currentConversationState?.subtitle;
            }
        }

        public bool IsCurrentLine => view != null && subtitle != null &&
            DialogueManager.isConversationActive && DialogueManager.conversationView == view &&
            ReferenceEquals(DialogueManager.currentConversationState?.subtitle, subtitle);

        public bool CanShowButton => IsCurrentLine && view.dialogueUI is AbstractDialogueUI;

        public void Lock()
        {
            if (state == State.Finished || heldLock) return;
            heldLock = true;
            NhkUISubtitlePanel.PushContinueButtonLock();
        }

        /// <summary>條件完成；統一延到 Tick 處理，避免零秒在 Awake 中跳句或被當句初始化覆蓋。</summary>
        public void Complete(bool autoAdvance)
        {
            if (state != State.Running) return;
            if (!IsCurrentLine || (!autoAdvance && !CanShowButton))
            {
                Cancel();
                return;
            }
            state = autoAdvance ? State.Advance : State.CompleteWait;
        }

        /// <summary>on 專用：等待其他鎖解除後顯示按鈕，再交回原本的對話流程。</summary>
        public void RequestShow()
        {
            if (state != State.Running) return;
            if (!CanShowButton)
            {
                Finish();
                return;
            }
            state = State.Reveal;
        }

        public void Tick()
        {
            if (state == State.Finished) return;
            if (view != null && !IsCurrentLine)
            {
                Finish();
                return;
            }

            if (state == State.Advance)
            {
                // 先釋放本次鎖，再走原生完成當句流程；不更改下一句的 Continue Mode。
                Finish();
                if (IsCurrentLine) view.HandleContinueButtonClick();
                return;
            }

            if (state == State.CompleteWait)
            {
                state = State.WaitingForClick;
                var auto = NhkAutoDialogueBridge.Find();
                if (auto != null && auto.IsActive)
                {
                    auto.DisableAutoDialogue();
                    pausedAuto = auto;
                    pausedAutoGeneration = auto.Generation;
                }
                ReleaseLock();
            }

            if (state != State.WaitingForClick && state != State.Reveal) return;
            if (NhkAutoDialogueBridge.Find()?.IsActive == true)
            {
                // 玩家重新開啟 Auto 時交還控制；on 本身也不覆蓋 Auto 模式。
                pausedAuto = null;
                Finish();
                return;
            }
            if (NhkUISubtitlePanel.IsContinueButtonLocked)
            {
                buttonRequested = false;
                return;
            }
            if (!buttonRequested && CanShowButton)
            {
                buttonRequested = true;
                ((AbstractDialogueUI)view.dialogueUI).ShowContinueButton(subtitle);
            }
            if (state == State.Reveal) Finish();
            // WaitingForClick 不結束指令，Optional 模式也會留在當句，直到原生點擊流程結束它。
        }

        public void OnLineEnd(Subtitle endedSubtitle)
        {
            if (ReferenceEquals(endedSubtitle, subtitle)) Finish();
        }

        public void Cancel()
        {
            if (state == State.Finished) return;
            Dispose();
            if (IsCurrentLine) view.SetupContinueButton();
            stopCommand?.Invoke();
        }

        private void Finish()
        {
            if (state == State.Finished) return;
            Dispose();
            stopCommand?.Invoke();
        }

        private void ReleaseLock()
        {
            if (!heldLock) return;
            heldLock = false;
            NhkUISubtitlePanel.PopContinueButtonLock();
        }

        public void Dispose()
        {
            if (state == State.Finished) return;
            state = State.Finished;
            ReleaseLock();
            var auto = pausedAuto;
            pausedAuto = null;
            // 在當句結束通知中恢復；延遲銷毀舊指令時，不改動新句或新對話。
            if (auto != null && !auto.IsActive && auto.Generation == pausedAutoGeneration && IsCurrentLine)
                auto.EnableAutoDialogue();
        }
    }
}
