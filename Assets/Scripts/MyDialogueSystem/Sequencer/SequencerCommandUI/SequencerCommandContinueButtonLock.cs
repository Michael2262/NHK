using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：
    ///   ContinueButtonLock(on)      → 壓住繼續鈕（玩家無法按繼續前進），與 off 配對。
    ///   ContinueButtonLock(off)     → 解除一層鎖。
    ///   ContinueButtonLock(reset)   → 強制清空鎖計數（保底用，擔心沒配對時）。
    ///   ContinueButtonLock()        → 等同 on。
    ///   （也接受 true/false、lock/unlock、1/0。）
    ///
    /// 用途：任何「不希望玩家中途按繼續跳過」的演出期間（跑條、動畫過場、等待某事件…），
    /// 在演出開始的節點 ContinueButtonLock(on)、演出結束（或下一個安全點）ContinueButtonLock(off)。
    ///
    /// 實作：直接調用 <see cref="NhkUISubtitlePanel.PushContinueButtonLock"/> /
    /// <see cref="NhkUISubtitlePanel.PopContinueButtonLock"/>，會攔截所有顯示繼續鈕的路徑，
    /// 不依賴「當前節點有沒有對白文字」，因此連空對白的演出句也擋得住。
    ///
    /// 注意：用計數配對，on / off 必須成對，否則會漏鎖或永久鎖。跨對話若擔心外洩，可在安全處
    /// ContinueButtonLock(reset)。ActionOverlay 的 blocking 模式已自帶配對，不需再手動包這個命令。
    /// </summary>
    public class SequencerCommandContinueButtonLock : SequencerCommand
    {
        public void Awake()
        {
            string arg = GetParameter(0, "on").Trim().ToLowerInvariant();

            switch (arg)
            {
                case "off":
                case "false":
                case "unlock":
                case "0":
                    NhkUISubtitlePanel.PopContinueButtonLock();
                    break;

                case "reset":
                case "clear":
                    NhkUISubtitlePanel.ResetContinueButtonLock();
                    break;

                default: // on / true / lock / 1 / 其他 → 視為上鎖
                    NhkUISubtitlePanel.PushContinueButtonLock();
                    break;
            }

            Stop();
        }
    }
}
