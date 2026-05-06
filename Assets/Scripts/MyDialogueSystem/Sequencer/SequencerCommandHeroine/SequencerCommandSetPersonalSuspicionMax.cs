using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 用法: SetPersonalSuspicionMax(heroineID, newMax)
    /// 範例: SetPersonalSuspicionMax(Heroine_A, 1500)
    /// 若當前可疑度超過新上限，會自動 clamp。
    /// </summary>
    public class SequencerCommandSetPersonalSuspicionMax : SequencerCommand
    {
        public void Awake()
        {
            string heroineID = GetParameter(0);
            int newMax = GetParameterAsInt(1);

            if (GameStatusService.Instance != null && GameStatusService.Instance.Heroines.ContainsKey(heroineID))
            {
                GameStatusService.Instance.Heroines[heroineID].SetPersonalSuspicionMax(newMax);
            }
            else
            {
                Debug.LogWarning($"Dialogue System: SetPersonalSuspicionMax 找不到 ID 為 '{heroineID}' 的女主角。");
            }

            Stop();
        }
    }
}
