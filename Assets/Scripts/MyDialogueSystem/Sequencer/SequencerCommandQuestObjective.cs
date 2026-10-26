using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>舊 QuestObjective 指令已退役；保留入口提示並立即結束，不修改遊戲狀態。</summary>
    public class SequencerCommandQuestObjective : SequencerCommand
    {
        public void Awake()
        {
            Debug.LogWarning("[QuestObjective] 舊任務指令已退役，請移除對話中的 QuestObjective(...) 呼叫。", this);
            Stop();
        }
    }
}
