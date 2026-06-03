using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：ItemImage(代號)
    ///
    /// 驅動場景上常駐的 ItemImageShower。
    ///
    /// 用法：
    ///   ItemImage(bento)  -> 顯示便當圖（Resources/SHOP/ItemImage/Item_bento）並淡入
    ///   ItemImage(stop)   -> 淡出並關閉顯示器
    ///                        （stop / hide / close / off 皆可）
    /// </summary>
    public class SequencerCommandItemImage : SequencerCommand
    {
        public void Awake()
        {
            string arg = GetParameter(0);

            if (string.IsNullOrEmpty(arg))
            {
                Debug.LogWarning("[SequencerCommandItemImage] 缺少參數。用法：ItemImage(代號) 或 ItemImage(stop)");
                Stop();
                return;
            }

            if (ItemImageShower.Instance == null)
            {
                Debug.LogWarning("[SequencerCommandItemImage] 找不到 ItemImageShower.Instance，請確認常駐場景上有此元件。");
                Stop();
                return;
            }

            // 關閉指令
            if (IsAction(arg, "stop", "hide", "close", "off"))
            {
                ItemImageShower.Instance.Hide();
            }
            // 顯示指定道具圖
            else
            {
                ItemImageShower.Instance.Show(arg);
            }

            Stop();
        }

        private bool IsAction(string input, params string[] targets)
        {
            foreach (var target in targets)
            {
                if (string.Equals(input, target, System.StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}
