using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 用法: AddSuspicionDecayAmount(heroineID, amount)
    /// 範例: AddSuspicionDecayAmount(Heroine_A, 5)
    /// 增減該女主角每次自動衰減可疑度的數值。
    /// </summary>
    public class SequencerCommandAddSuspicionDecayAmount : SequencerCommand
    {
        public void Awake()
        {
            string heroineID = GetParameter(0);
            int amount = GetParameterAsInt(1);

            if (GameStatusService.Instance != null && GameStatusService.Instance.Heroines.ContainsKey(heroineID))
            {
                GameStatusService.Instance.Heroines[heroineID].AddSuspicionDecayAmount(amount);
            }
            else
            {
                Debug.LogWarning($"Dialogue System: AddSuspicionDecayAmount 找不到 ID 為 '{heroineID}' 的女主角。");
            }

            Stop();
        }
    }
}
