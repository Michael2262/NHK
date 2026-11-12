using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：
    ///   SubtitlePanel(hide, [目標])  → 隱藏目標中開著的 NHK 字幕面板，並記住以便還原。
    ///   SubtitlePanel(show, [目標])  → 只還原目標中仍有隱藏記錄的面板。
    ///   SubtitlePanel(reset, [目標]) → 只清掉目標的記錄、不還原。
    ///   SubtitlePanel()        → 等同 hide。
    ///   （也接受 off = hide、on = show。）
    ///
    /// 目標：省略 = 全部；shared = 主／共用框（不依目前說話者）；
    ///       actor = 所有 Dialogue Actor 指定的自訂框；其他文字 = Dialogue Actor 的 actor 名稱。
    /// 例如 SubtitlePanel(hide, SisterB)、SubtitlePanel(hide, actor)。
    ///
    /// 走面板原生 Open()/Close()，照常播 Show / Hide 動畫，不設定跨句隱藏鎖。
    /// 下一次該面板顯示台詞時會自然開啟，只清除自己的隱藏記錄。
    ///
    /// 注意：hide / show 建議成對使用。面板若勾了 Clear Text On Close，hide 後文字會被清掉，
    /// show 回來是空框（下一句才會填字）。跨對話擔心殘留時可用 reset。
    /// </summary>
    public class SequencerCommandSubtitlePanel : SequencerCommand
    {
        public void Awake()
        {
            string arg = GetParameter(0, "hide").Trim().ToLowerInvariant();
            string target = GetParameter(1, string.Empty).Trim();

            switch (arg)
            {
                case "show":
                case "on":
                case "open":
                    NhkUISubtitlePanel.ShowHiddenPanels(target);
                    break;

                case "reset":
                case "clear":
                    NhkUISubtitlePanel.ResetHiddenPanels(target);
                    break;

                case "hide":
                case "off":
                case "close":
                case "":
                    NhkUISubtitlePanel.HideOpenPanels(target);
                    break;

                default:
                    Debug.LogWarning($"[SubtitlePanel] 未知操作「{arg}」，請使用 hide、show 或 reset。", this);
                    break;
            }

            Stop();
        }
    }
}
