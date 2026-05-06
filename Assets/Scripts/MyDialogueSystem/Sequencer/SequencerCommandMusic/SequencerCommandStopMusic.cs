using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandStopMusic : SequencerCommand
    {
        public void Awake()
        {
            float fadeDuration = GetParameterAsFloat(0, 1.0f);
            // 參數 1：是否在停止後恢復場景音樂 (預設為 true)
            bool resumeScene = GetParameterAsBool(1, true);

            var sceneController = Object.FindFirstObjectByType<SceneMusicController>();

            if (sceneController != null)
            {
                // 透過控制器停止，並決定是否恢復 RefreshSceneMusic
                sceneController.StopOverride(resumeScene, fadeDuration);
            }
            else if (MusicManager.Instance != null)
            {
                // 一般場景直接停止
                MusicManager.Instance.StopMusic(fadeDuration);
            }

            Stop();
        }
    }
}