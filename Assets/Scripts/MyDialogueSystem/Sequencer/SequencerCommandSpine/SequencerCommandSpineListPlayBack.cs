using UnityEngine;
using PixelCrushers.DialogueSystem;
using MySpineSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：SpineListPlayBack(控制器ID, 組名)
    /// 功能：透過 Controller ID 呼叫 SpinePlayByList.PlayGroupAndGoBack()，
    /// 插播指定組一次後返回先前正在播放的組。
    /// 注意：目標組不能含 loop 後段或 nextGroupName（否則不會結束、無法返回）。
    /// </summary>
    public class SequencerCommandSpineListPlayBack : SequencerCommand
    {
        public void Awake()
        {
            string controllerID = GetParameter(0);
            string groupName = GetParameter(1);

            if (string.IsNullOrEmpty(controllerID) || string.IsNullOrEmpty(groupName))
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("[Sequencer] SpineListPlayBack: 控制器ID 或 組名 為空。");
                Stop();
                return;
            }

            var playByList = SpinePlayByList.GetByControllerID(controllerID);
            if (playByList == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"[Sequencer] SpineListPlayBack: 找不到 ID 為 '{controllerID}' 的 SpinePlayByList。");
                Stop();
                return;
            }

            playByList.PlayGroupAndGoBack(groupName);
            Stop();
        }
    }
}
