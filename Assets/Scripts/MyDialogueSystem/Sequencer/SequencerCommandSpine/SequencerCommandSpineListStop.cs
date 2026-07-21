using UnityEngine;
using PixelCrushers.DialogueSystem;
using MySpineSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：SpineListStop(控制器ID)
    /// 功能：透過 Controller ID 呼叫 SpinePlayByList.StopPlaying()，停止目前的動畫組播放並清除軌道。
    /// </summary>
    public class SequencerCommandSpineListStop : SequencerCommand
    {
        public void Awake()
        {
            string controllerID = GetParameter(0);

            if (string.IsNullOrEmpty(controllerID))
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("[Sequencer] SpineListStop: 控制器ID 為空。");
                Stop();
                return;
            }

            var playByList = SpinePlayByList.GetByControllerID(controllerID);
            if (playByList == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"[Sequencer] SpineListStop: 找不到 ID 為 '{controllerID}' 的 SpinePlayByList。");
                Stop();
                return;
            }

            playByList.StopPlaying();
            Stop();
        }
    }
}
