using UnityEngine;
using PixelCrushers.DialogueSystem;
using MySpineSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：SpineListPlay(控制器ID, 組名)
    /// 功能：透過 Controller ID 呼叫 SpinePlayByList.PlayGroup()，從指定組開始播放。
    /// 若該組已在播放中（CurrentGroupName 相同）會自動略過不重播（skip 由 SpinePlayByList 內建）。
    /// </summary>
    public class SequencerCommandSpineListPlay : SequencerCommand
    {
        public void Awake()
        {
            string controllerID = GetParameter(0);
            string groupName = GetParameter(1);

            if (string.IsNullOrEmpty(controllerID) || string.IsNullOrEmpty(groupName))
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("[Sequencer] SpineListPlay: 控制器ID 或 組名 為空。");
                Stop();
                return;
            }

            var playByList = SpinePlayByList.GetByControllerID(controllerID);
            if (playByList == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"[Sequencer] SpineListPlay: 找不到 ID 為 '{controllerID}' 的 SpinePlayByList。");
                Stop();
                return;
            }

            playByList.PlayGroup(groupName);
            Stop();
        }
    }
}
