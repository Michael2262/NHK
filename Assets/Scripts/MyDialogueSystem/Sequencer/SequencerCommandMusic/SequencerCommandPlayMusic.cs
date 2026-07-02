using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandPlayMusic : SequencerCommand
    {
        public void Awake()
        {
            string musicKey = GetParameter(0);
            float fadeDuration = GetParameterAsFloat(1, 1.0f);
            // 參數 2：淡化方式。填 Q/queue(或 S/sequential) = 序列式(先淡出到靜音再淡入)；
            //         留空或填 C/crossfade = 交叉淡化(預設)。
            MusicManager.FadeMode mode = MusicManager.ParseFadeMode(GetParameter(2));

            // 1. 先嘗試尋找場景中的控制器
            var sceneController = Object.FindFirstObjectByType<SceneMusicController>();

            if (sceneController != null)
            {
                // 如果場景有控制器，使用 PlayOverride 以確保 Flag 變動不會搶走音樂控制權
                sceneController.PlayOverride(musicKey, fadeDuration, mode);
            }
            else if (MusicManager.Instance != null)
            {
                // 如果場景是空的（簡單場景），則直接對接 Manager
                MusicManager.Instance.PlayMusic(musicKey, fadeDuration, mode);
            }
            else
            {
                Debug.LogWarning("PlayMusic 指令找不到任何音樂控制器或 MusicManager 實例。");
            }

            Stop();
        }
    }
}