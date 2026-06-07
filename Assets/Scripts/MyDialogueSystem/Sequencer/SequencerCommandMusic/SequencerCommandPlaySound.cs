using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 對話腳本中播放音效(SFX)的指令，對接 AudioManager。
    /// 呼叫語法：PlaySound(音效Key)
    /// </summary>
    public class SequencerCommandPlaySound : SequencerCommand
    {
        public void Awake()
        {
            string soundKey = GetParameter(0);

            if (string.IsNullOrEmpty(soundKey))
            {
                Debug.LogWarning("PlaySound 指令未提供音效 Key。");
                Stop();
                return;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(soundKey);
            }
            else
            {
                Debug.LogWarning("PlaySound 指令找不到 AudioManager 實例。");
            }

            Stop();
        }
    }
}
