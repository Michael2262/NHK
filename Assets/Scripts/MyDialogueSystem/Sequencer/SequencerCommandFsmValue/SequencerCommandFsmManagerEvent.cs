using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    // 語法: FsmManagerEvent(eventName, targetIDOrGroup, [id/group])
    // 預設模式為 id
    public class SequencerCommandFsmManagerEvent : SequencerCommand
    {
        public void Awake()
        {
            string eventName = GetParameter(0);
            string target = GetParameter(1);
            string mode = GetParameter(2, "id").ToLower();

            if (FsmValueManager.Instance == null)
            {
                Debug.LogWarning("Dialogue System: SequencerCommand FsmManagerEvent 找不到 FsmValueManager 實例。");
                Stop();
                return;
            }

            if (mode == "group")
            {
                FsmValueManager.Instance.SendEventByGroup(target, eventName);
            }
            else
            {
                FsmValueManager.Instance.SendEventById(target, eventName);
            }

            Stop(); // 執行完畢後立即停止指令
        }
    }
}