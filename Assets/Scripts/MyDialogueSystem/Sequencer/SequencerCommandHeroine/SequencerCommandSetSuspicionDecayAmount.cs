using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 用法: SetSuspicionDecayAmount(heroineID, newValue)
    /// 範例: SetSuspicionDecayAmount(Heroine_A, 30)
    /// 設定該女主角每次自動衰減可疑度的數值。
    /// </summary>
    public class SequencerCommandSetSuspicionDecayAmount : SequencerCommand
    {
        public void Awake()
        {
            string heroineID = GetParameter(0);
            int newValue = GetParameterAsInt(1);

            if (GameStatusService.Instance != null && GameStatusService.Instance.Heroines.ContainsKey(heroineID))
            {
                GameStatusService.Instance.Heroines[heroineID].SetSuspicionDecayAmount(newValue);
            }
            else
            {
                Debug.LogWarning($"Dialogue System: SetSuspicionDecayAmount 找不到 ID 為 '{heroineID}' 的女主角。");
            }

            Stop();
        }
    }
}
