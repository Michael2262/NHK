using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：CookingImage(代號)
    ///
    /// 驅動場景上的 CookingImagePresenter。
    ///
    /// 用法：
    ///   CookingImage(omelet) -> 播放 Resources/Cooking/Cooking_omelet
    ///   CookingImage(stop)   -> 提前淡出目前圖片
    ///                           （stop / hide / close / off 皆可）
    /// </summary>
    public class SequencerCommandCookingImage : SequencerCommand
    {
        public void Awake()
        {
            string arg = GetParameter(0);

            if (string.IsNullOrWhiteSpace(arg))
            {
                Debug.LogWarning("[SequencerCommandCookingImage] 缺少參數。用法：CookingImage(代號) 或 CookingImage(stop)");
                Stop();
                return;
            }

            if (CookingImagePresenter.Instance == null)
            {
                Debug.LogWarning("[SequencerCommandCookingImage] 找不到 CookingImagePresenter.Instance，請確認場景上有此元件。");
                Stop();
                return;
            }

            if (IsAction(arg, "stop", "hide", "close", "off"))
                CookingImagePresenter.Instance.Hide();
            else
                CookingImagePresenter.Instance.Show(arg);

            Stop();
        }

        private static bool IsAction(string input, params string[] targets)
        {
            foreach (string target in targets)
            {
                if (string.Equals(input, target, System.StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }
    }
}
