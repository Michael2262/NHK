using UnityEngine;
using PixelCrushers.DialogueSystem;
using MySpineSystem; // 引用你腳本中的命名空間

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandSpinePlay : SequencerCommand
    {
        public void Awake()
        {
            // 參數解析
            string controllerID = GetParameter(0);
            string trackInput = GetParameter(1);
            string animationName = GetParameter(2);
            int modeInt = GetParameterAsInt(3, 0); // 預設 0: ClearOnComplete
            float delaySeconds = GetParameterAsFloat(4, -1f); // 預設 -1 使用腳本內建值

            // 取得控制器
            var controller = SpineAnimationController.GetByID(controllerID);
            if (controller == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"[Sequencer] SpinePlay: 找不到 ID 為 '{controllerID}' 的控制器。");
                Stop();
                return;
            }

            // 解析軌道 (支援名稱如 "Body" 或數字如 "0")
            AnimationTrack track;
            if (!System.Enum.TryParse(trackInput, out track))
            {
                if (int.TryParse(trackInput, out int trackIndex))
                {
                    track = (AnimationTrack)trackIndex;
                }
                else
                {
                    if (DialogueDebug.logWarnings) Debug.LogWarning($"[Sequencer] SpinePlay: 無法解析軌道 '{trackInput}'。");
                    Stop();
                    return;
                }
            }

            // 執行播放
            controller.PlayAnimation(track, animationName, (SpineAnimationController.ClearMode)modeInt, delaySeconds);

            Stop();
        }
    }
}