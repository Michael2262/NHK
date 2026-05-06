using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 用法: HeroineUIHideOnEnd()
    /// 註冊監聽，當本次對話結束時自動關閉女主角狀態面板。
    /// 常搭配 HeroineUIShow 使用：
    ///   HeroineUIShow(Heroine_A); HeroineUIHideOnEnd()
    /// </summary>
    public class SequencerCommandHeroineUIHideOnEnd : SequencerCommand
    {
        public void Awake()
        {
            DialogueManager.instance.conversationEnded += OnConversationEnded;
            Stop();
        }

        private void OnConversationEnded(Transform actor)
        {
            DialogueManager.instance.conversationEnded -= OnConversationEnded;

            if (HeroineUI.Instance != null)
                HeroineUI.Instance.Hide();
        }
    }
}
