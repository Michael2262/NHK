using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// 語法：QuestObjective(動作, ObjectiveID)
    ///
    /// 支援動作：
    /// 1. Reveal/Show:     QuestObjective(Reveal, ObjectiveID)   -> 顯示目標（未顯示 → 已顯示未完成）
    /// 2. Complete/Done:   QuestObjective(Complete, ObjectiveID) -> 完成目標（任何狀態 → 已完成；
    ///                                                              若 SO 勾了 MirrorFlagOnComplete，
    ///                                                              同步加 Persistent Flag：ObjDone_目標ID）
    /// 3. Hide/Remove:     QuestObjective(Hide, ObjectiveID)     -> 隱藏目標（回到未顯示，除錯用）
    /// </summary>
    public class SequencerCommandQuestObjective : SequencerCommand
    {
        public void Awake()
        {
            var model = GameStatusService.Instance?.QuestObjectives;

            if (model == null)
            {
                Debug.LogWarning("[QuestObjective] 找不到 QuestObjectives 實例。請確認 GameStatusService 已正確初始化。");
                Stop();
                return;
            }

            string action = GetParameter(0);
            string objectiveID = GetParameter(1);

            if (string.IsNullOrEmpty(objectiveID))
            {
                Debug.LogWarning("[QuestObjective] 缺少 ObjectiveID。用法：QuestObjective(動作, ObjectiveID)");
                Stop();
                return;
            }

            if (IsAction(action, "Reveal", "Show", "Add"))
            {
                model.Reveal(objectiveID);
            }
            else if (IsAction(action, "Complete", "Done", "Finish"))
            {
                model.Complete(objectiveID);
            }
            else if (IsAction(action, "Hide", "Remove", "Clear"))
            {
                model.Hide(objectiveID);
            }
            else
            {
                Debug.LogWarning($"[QuestObjective] 未知的動作類型: {action}。可用選項: Reveal, Complete, Hide。");
            }

            Stop();
        }

        private bool IsAction(string input, params string[] targets)
        {
            foreach (var target in targets)
            {
                if (string.Equals(input, target, System.StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}
