using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 用法: HeroineUIHide()
    /// 立刻關閉女主角狀態面板。
    /// </summary>
    public class SequencerCommandHeroineUIHide : SequencerCommand
    {
        public void Awake()
        {
            if (HeroineUI.Instance != null)
                HeroineUI.Instance.Hide();

            Stop();
        }
    }
}
