using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 用法: GiftUIShow(heroineID)
    /// 範例: GiftUIShow(Heroine_A)
    /// 以指定 ID 開啟送禮面板(不阻塞對話)。
    /// heroineID 為必要參數:沒有合理預設,因為送禮必須明確指定對象。
    /// </summary>
    public class SequencerCommandGiftUIShow : SequencerCommand
    {
        public void Awake()
        {
            string heroineID = GetParameter(0);

            if (GiftUI.Instance == null)
            {
                Debug.LogWarning("Dialogue System: GiftUIShow 找不到 GiftUI.Instance。");
                Stop();
                return;
            }

            if (string.IsNullOrEmpty(heroineID))
            {
                Debug.LogWarning("Dialogue System: GiftUIShow 缺少 heroineID 參數,無法開啟。");
                Stop();
                return;
            }

            GiftUI.Instance.ShowForHeroine(heroineID);
            Stop();
        }
    }
}