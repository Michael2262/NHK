using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// PlayMusicUntilPhase(音樂Key, 淡化秒數, Q) 指定本時段的預設曲目。
    /// 淡化預設為 1 秒交叉淡化，Q 表示先淡出再淡入。
    /// </summary>
    public class SequencerCommandPlayMusicUntilPhase : SequencerCommand
    {
        public void Awake()
        {
            var controller = Object.FindFirstObjectByType<SceneMusicController>();
            if (controller != null)
            {
                controller.PlayMusicUntilPhase(GetParameter(0), GetParameterAsFloat(1, 1.0f),
                    MusicManager.ParseFadeMode(GetParameter(2)));
            }
            else
            {
                Debug.LogWarning("[PlayMusicUntilPhase] 場景缺少 SceneMusicController，無法指定時段音樂。");
            }

            Stop();
        }
    }
}
