using UnityEngine;
using PixelCrushers.DialogueSystem;
using MySpineSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandSpineStop : SequencerCommand
    {
        public void Awake()
        {
            string controllerID = GetParameter(0);
            string trackInput = GetParameter(1); // 可選參數

            var controller = SpineAnimationController.GetByID(controllerID);
            if (controller == null)
            {
                Stop();
                return;
            }

            // 如果沒有填寫第二個參數，停止全部
            if (string.IsNullOrEmpty(trackInput))
            {
                controller.StopAll();
            }
            else
            {
                // 解析軌道
                AnimationTrack track;
                if (System.Enum.TryParse(trackInput, out track) || int.TryParse(trackInput, out int trackIndex))
                {
                    if (!System.Enum.IsDefined(typeof(AnimationTrack), trackInput) && int.TryParse(trackInput, out trackIndex))
                    {
                        track = (AnimationTrack)trackIndex;
                    }
                    controller.StopAnimation(track);
                }
            }

            Stop();
        }
    }
}