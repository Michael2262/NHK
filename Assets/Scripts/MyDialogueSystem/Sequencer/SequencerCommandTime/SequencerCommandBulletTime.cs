using System;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>例如 BulletTime(Begin)、BulletTime(EndAtNearestMini)。</summary>
    public sealed class SequencerCommandBulletTime : SequencerCommand
    {
        private void Awake()
        {
            string command = GetParameter(0, "").Trim();
            if (Enum.TryParse(command, true, out BulletTimeOperation operation) &&
                Enum.IsDefined(typeof(BulletTimeOperation), operation))
                BulletTimeBridge.TryExecuteCurrent(operation);
            else
                Debug.LogWarning($"[BulletTime] 無效操作「{command}」。請使用 Begin、AdvanceOneMinute、EndAtNearestMini 或 EndAtConfiguredTime。");
            Stop();
        }
    }
}
