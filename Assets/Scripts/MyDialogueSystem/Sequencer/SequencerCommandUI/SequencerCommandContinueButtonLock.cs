using System.Globalization;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// ContinueButton(on)      → 解除一層鎖，在當句請求顯示按鈕（仍遵守其他鎖與 Auto）。
    /// ContinueButton(off)     → 上鎖並隱藏按鈕，與 on 配對；本身不阻止 Sequence 自然結束。
    /// ContinueButton(reset)   → 清除鎖計數，不主動顯示按鈕。
    /// ContinueButton(wait, 3) → 隱藏按鈕，3 秒後在同句顯示並等待點擊。
    /// ContinueButton(auto, 3) → 隱藏按鈕，3 秒後自動前進，下一句交回正常按鈕控制。
    /// ContinueButton()        → 等同 on。wait / auto 必須提供非負、有限的秒數。
    ///
    /// 舊 ContinueButtonLock 指令已移除，沒有相容別名。暫留原檔名以保留既有 .meta；
    /// Sequencer 依類別名稱尋找指令，與時間指令集中於同一檔案的做法相同。
    /// </summary>
    public class SequencerCommandContinueButton : SequencerCommand
    {
        private NhkContinueButtonSession session;
        private double finishTime;
        private bool timing;
        private bool autoAdvance;

        public void Awake()
        {
            string arg = GetParameter(0, "on").Trim().ToLowerInvariant();

            switch (arg)
            {
                case "on":
                case "true":
                case "unlock":
                case "1":
                    NhkUISubtitlePanel.PopContinueButtonLock();
                    session = new NhkContinueButtonSession(sequencer, Stop);
                    session.RequestShow();
                    return;

                case "off":
                case "false":
                case "lock":
                case "0":
                    NhkUISubtitlePanel.PushContinueButtonLock();
                    break;

                case "reset":
                case "clear":
                    NhkUISubtitlePanel.ResetContinueButtonLock();
                    break;

                case "wait":
                case "auto":
                    BeginTimedWait(arg == "auto");
                    return;

                default:
                    Debug.LogWarning($"[ContinueButton] 未知模式「{arg}」，請使用 on、off、reset、wait 或 auto。", this);
                    break;
            }

            Stop();
        }

        private void BeginTimedWait(bool advance)
        {
            string value = GetParameter(1, "");
            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds) ||
                float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
            {
                Debug.LogWarning("[ContinueButton] wait / auto 需要有效的非負秒數，例如 ContinueButton(wait, 3)。", this);
                Stop();
                return;
            }

            session = new NhkContinueButtonSession(sequencer, Stop);
            if (!session.IsCurrentLine || (!advance && !session.CanShowButton))
            {
                Debug.LogWarning("[ContinueButton] wait / auto 必須用於目前對話的 Sequence；wait 另需支援繼續按鈕的對話 UI。", this);
                session.Cancel();
                return;
            }

            autoAdvance = advance;
            finishTime = (double)DialogueTime.time + seconds;
            timing = true;
            session.Lock();
        }

        public void Update()
        {
            if (!isPlaying) return;
            // 先檢查當句是否仍有效，避免舊計時影響下一句。
            session?.Tick();
            if (!isPlaying || !timing || DialogueTime.time < finishTime) return;
            timing = false;
            session.Complete(autoAdvance);
        }

        public void OnConversationLineEnd(Subtitle subtitle) => session?.OnLineEnd(subtitle);

        public void OnDestroy() => session?.Dispose();
    }
}
