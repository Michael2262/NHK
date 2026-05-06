using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 用法: AddPersonalSuspicionMax(heroineID, amount)
    /// 範例: AddPersonalSuspicionMax(Heroine_A, 200)
    /// 負數可減少上限。若當前可疑度超過新上限，會自動 clamp。
    /// </summary>
    public class SequencerCommandAddPersonalSuspicionMax : SequencerCommand
    {
        public void Awake()
        {
            string heroineID = GetParameter(0);
            int amount = GetParameterAsInt(1);

            if (GameStatusService.Instance != null && GameStatusService.Instance.Heroines.ContainsKey(heroineID))
            {
                GameStatusService.Instance.Heroines[heroineID].AddPersonalSuspicionMax(amount);
            }
            else
            {
                Debug.LogWarning($"Dialogue System: AddPersonalSuspicionMax 找不到 ID 為 '{heroineID}' 的女主角。");
            }

            Stop();
        }
    }
}
