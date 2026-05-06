using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 用法: HeroineUIShowTimed(heroineID, seconds)
    /// 範例: HeroineUIShowTimed(Heroine_A, 4)
    /// 顯示面板指定秒數後自動關閉，對話繼續。
    /// 無秒數參數時預設 2 秒。
    /// </summary>
    public class SequencerCommandHeroineUIShowTimed : SequencerCommand
    {
        private float timer;

        public void Awake()
        {
            string heroineID = GetParameter(0);
            timer = GetParameterAsFloat(1, 2f);

            if (HeroineUI.Instance == null)
            {
                Debug.LogWarning("Dialogue System: HeroineUIShowTimed 找不到 HeroineUI.Instance。");
                Stop();
                return;
            }

            if (string.IsNullOrEmpty(heroineID))
                HeroineUI.Instance.ShowByOrder(0);
            else
                HeroineUI.Instance.Show(heroineID);
        }

        public void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                if (HeroineUI.Instance != null)
                    HeroineUI.Instance.Hide();
                Stop();
            }
        }
    }
}
