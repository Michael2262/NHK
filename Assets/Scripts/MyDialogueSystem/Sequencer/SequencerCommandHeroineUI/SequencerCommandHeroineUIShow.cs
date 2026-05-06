using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 用法: HeroineUIShow(heroineID)
    /// 範例: HeroineUIShow(Heroine_A)
    /// 以指定 ID 開啟女主角狀態面板（不阻塞對話）。
    /// 無參數時預設開啟順位 0。
    /// </summary>
    public class SequencerCommandHeroineUIShow : SequencerCommand
    {
        public void Awake()
        {
            string heroineID = GetParameter(0);

            if (HeroineUI.Instance == null)
            {
                Debug.LogWarning("Dialogue System: HeroineUIShow 找不到 HeroineUI.Instance。");
                Stop();
                return;
            }

            if (string.IsNullOrEmpty(heroineID))
                HeroineUI.Instance.ShowByOrder(0);
            else
                HeroineUI.Instance.Show(heroineID);

            Stop();
        }
    }
}
