using System.Globalization;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>InsertCG(Actor ID, 圖片 ID, [跑條秒數], [auto/true、wait 或 shoot/false])。</summary>
    public sealed class SequencerCommandInsertCG : SequencerCommand
    {
        private NhkContinueButtonSession session;
        private bool autoAdvance;

        public void Awake()
        {
            string actorID = GetParameter(0);
            var actor = InsertCGActor.Find(actorID);
            if (actor == null)
            {
                Debug.LogWarning($"[InsertCG] 找不到已啟用的 Actor「{actorID}」。", this);
                Stop();
                return;
            }

            float? duration = null;
            string timeArgument = GetParameter(2);
            if (!string.IsNullOrWhiteSpace(timeArgument))
            {
                if (!float.TryParse(timeArgument, NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds) ||
                    float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
                {
                    Debug.LogWarning($"[InsertCG] 跑條時長「{timeArgument}」無效，請填非負秒數或留空。", this);
                    Stop();
                    return;
                }
                duration = seconds;
            }

            string imageID = GetParameter(1);
            switch (GetParameter(3, "").Trim().ToLowerInvariant())
            {
                case "shoot":
                case "false":
                    actor.TryPlay(imageID, duration, null);
                    Stop();
                    return;
                case "wait":
                    autoAdvance = false;
                    break;
                case "":
                case "auto":
                case "true":
                    autoAdvance = true;
                    break;
                default:
                    Debug.LogWarning("[InsertCG] 未知播放模式，請使用 auto、true、wait、shoot 或 false。", this);
                    Stop();
                    return;
            }

            session = new NhkContinueButtonSession(sequencer, Stop);
            if (session.IsCurrentLine) session.Lock();
            if (!actor.TryPlay(imageID, duration, OnFinished)) session.Cancel();
        }

        private void OnFinished(bool completed)
        {
            if (this == null || !isPlaying) return;
            if (completed) session.Complete(autoAdvance);
            else session.Cancel();
        }

        private void Update()
        {
            if (isPlaying) session?.Tick();
        }

        private void OnConversationLineEnd(Subtitle subtitle) => session?.OnLineEnd(subtitle);
        private void OnDestroy() => session?.Dispose();
    }
}
