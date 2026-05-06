using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 用法: AddPersonalSuspicion(heroineID, amount)
    /// 範例: AddPersonalSuspicion(Heroine_A, 30)
    /// 負數可減少，受 PersonalSuspicionMax 上限限制。
    /// </summary>
    public class SequencerCommandAddPersonalSuspicion : SequencerCommand
    {
        public void Awake()
        {
            string heroineID = GetParameter(0);
            int amount = GetParameterAsInt(1);

            if (GameStatusService.Instance != null && GameStatusService.Instance.Heroines.ContainsKey(heroineID))
            {
                GameStatusService.Instance.Heroines[heroineID].AddPersonalSuspicion(amount);
            }
            else
            {
                Debug.LogWarning($"Dialogue System: AddPersonalSuspicion 找不到 ID 為 '{heroineID}' 的女主角。");
            }

            Stop();
        }
    }
}
