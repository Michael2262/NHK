using UnityEngine;
using PixelCrushers.DialogueSystem;
using MySpineSystem; // 引用 AnimationTrack 所在的命名空間

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：SpineWait(控制器ID, 軌道)
    /// 功能：暫停對話序列，直到指定控制器的特定軌道動畫播放完成。
    /// </summary>
    public class SequencerCommandSpineWait : SequencerCommand
    {
        private SpineAnimationController _controller;
        private AnimationTrack _targetTrack;
        private bool _isSubscribed = false;

        public void Awake()
        {
            string controllerID = GetParameter(0);
            string trackInput = GetParameter(1);

            // 1. 透過靜態方法取得控制器實例
            _controller = SpineAnimationController.GetByID(controllerID);

            if (_controller == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"[Sequencer] SpineWait: 找不到 ID 為 '{controllerID}' 的控制器。");
                Stop();
                return;
            }

            // 2. 解析軌道 (支援名稱如 "Body" 或數字如 "0")
            if (!System.Enum.TryParse(trackInput, out _targetTrack))
            {
                if (int.TryParse(trackInput, out int trackIndex))
                {
                    _targetTrack = (AnimationTrack)trackIndex;
                }
                else
                {
                    if (DialogueDebug.logWarnings) Debug.LogWarning($"[Sequencer] SpineWait: 無法解析軌道 '{trackInput}'。");
                    Stop();
                    return;
                }
            }

            // 3. 訂閱控制器中的動畫完成事件
            _controller.OnAnimationCompleted += HandleAnimationCompleted;
            _isSubscribed = true;
        }

        private void HandleAnimationCompleted(AnimationTrack track, string animName)
        {
            // 4. 檢查完成的是否為我們在等待的軌道
            if (track == _targetTrack)
            {
                Cleanup();
                Stop(); // 告知 Sequencer 指令完成，對話繼續
            }
        }

        private void Cleanup()
        {
            if (_isSubscribed && _controller != null)
            {
                _controller.OnAnimationCompleted -= HandleAnimationCompleted;
                _isSubscribed = false;
            }
        }

        // 如果玩家跳過對話或物件被銷毀，確保取消訂閱防止報錯
        public void OnDestroy()
        {
            Cleanup();
        }
    }
}