using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：
    ///   SubtitlePanel(hide)    → 隱藏「當下開著的整個字幕面板」（播 Hide 動畫），並記住以便還原。
    ///   SubtitlePanel(show)    → 還原剛剛被隱藏的字幕面板（播 Show 動畫）。
    ///   SubtitlePanel(reset)   → 只清掉記錄、不還原（保底用）。
    ///   SubtitlePanel()        → 等同 hide。
    ///   （也接受 off = hide、on = show。）
    ///
    /// 用途：演出期間想把「整個對話框」暫時收起來（秀立繪 / 過場 / 純演出），演完再 show 叫回來。
    ///
    /// 實作：直接調用 <see cref="NhkUISubtitlePanel.HideOpenPanels"/> /
    /// <see cref="NhkUISubtitlePanel.ShowHiddenPanels"/>，走面板原生 Open()/Close()，
    /// 因此會照常播 Show / Hide 動畫。
    ///
    /// 注意：hide / show 建議成對使用。面板若勾了 Clear Text On Close，hide 後文字會被清掉，
    /// show 回來是空框（下一句才會填字）。跨對話擔心殘留時可用 reset。
    /// </summary>
    public class SequencerCommandSubtitlePanel : SequencerCommand
    {
        public void Awake()
        {
            string arg = GetParameter(0, "hide").Trim().ToLowerInvariant();

            switch (arg)
            {
                case "show":
                case "on":
                case "open":
                    NhkUISubtitlePanel.ShowHiddenPanels();
                    break;

                case "reset":
                case "clear":
                    NhkUISubtitlePanel.ResetHiddenPanels();
                    break;

                default: // hide / off / close / 其他 → 隱藏
                    NhkUISubtitlePanel.HideOpenPanels();
                    break;
            }

            Stop();
        }
    }
}
