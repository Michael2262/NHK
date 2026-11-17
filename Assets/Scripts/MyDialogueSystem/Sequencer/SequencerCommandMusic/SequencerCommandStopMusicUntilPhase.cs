using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// StopMusicUntilPhase(淡出秒數) 將本時段的預設 BGM 設為靜音，預設淡出 1 秒。
    /// </summary>
    public class SequencerCommandStopMusicUntilPhase : SequencerCommand
    {
        public void Awake()
        {
            var controller = Object.FindFirstObjectByType<SceneMusicController>();
            if (controller != null)
            {
                controller.StopMusicUntilPhase(GetParameterAsFloat(0, 1.0f));
            }
            else
            {
                Debug.LogWarning("[StopMusicUntilPhase] 場景缺少 SceneMusicController，無法設定本時段靜音。");
            }

            Stop();
        }
    }
}
