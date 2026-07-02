using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandStopMusic : SequencerCommand
    {
        public void Awake()
        {
            float fadeDuration = GetParameterAsFloat(0, 1.0f);

            // 參數 1：停止後的行為。
            //   true / T  → 恢復場景音樂,交叉淡化(預設)
            //   false / F → 只停止,不恢復場景音樂
            //   queue / Q → 恢復場景音樂,序列式(先淡出到靜音再淡入)
            bool resumeScene = true;
            MusicManager.FadeMode mode = MusicManager.FadeMode.Crossfade;

            string modeParam = GetParameter(1);
            if (!string.IsNullOrEmpty(modeParam))
            {
                switch (modeParam.Trim().ToLowerInvariant())
                {
                    case "t":
                    case "true":
                        resumeScene = true;
                        mode = MusicManager.FadeMode.Crossfade;
                        break;
                    case "f":
                    case "false":
                        resumeScene = false;
                        break;
                    case "q":
                    case "queue":
                    case "s":
                    case "sequential":
                        resumeScene = true;
                        mode = MusicManager.FadeMode.Sequential;
                        break;
                    default:
                        // 無法辨識時沿用舊行為(視為 bool)
                        resumeScene = GetParameterAsBool(1, true);
                        break;
                }
            }

            var sceneController = Object.FindFirstObjectByType<SceneMusicController>();

            if (sceneController != null)
            {
                // 透過控制器停止，並決定是否恢復 RefreshSceneMusic
                sceneController.StopOverride(resumeScene, fadeDuration, mode);
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