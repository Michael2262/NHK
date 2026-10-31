using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// QuestTrackerUI(ShowAndExpand)：允許顯示並展開，零筆任務仍保持隱藏。
    /// QuestTrackerUI(Hide)：隱藏整個任務面板與箭頭。
    /// QuestTrackerUI(Collapse)：收合面板；原本可見時保留箭頭，不解除隱藏。
    /// QuestTrackerUI(Refresh)：重新挑選顯示任務，保留顯示／隱藏與開合狀態。
    /// 透過共用 UI 單例呼叫，不需掛在場景上；發出要求後立即結束，不等待開合動畫。
    /// </summary>
    public class SequencerCommandQuestTrackerUI : SequencerCommand
    {
        public void Awake()
        {
            string action = GetParameter(0, "").Trim().ToLowerInvariant();
            var trackerUI = global::QuestTrackerUI.Instance;
            if (trackerUI == null)
            {
                Debug.LogWarning("[QuestTrackerUI Sequence] 找不到共用任務 UI，請確認共用 UI 場景已載入且 QuestTrackerUI 曾啟用。", this);
                Stop();
                return;
            }

            switch (action)
            {
                case "showandexpand":
                    trackerUI.ShowAndExpandUI();
                    break;
                case "hide":
                    trackerUI.HideUI();
                    break;
                case "collapse":
                    trackerUI.CollapsePanel();
                    break;
                case "refresh":
                    trackerUI.RefreshTasks();
                    break;
                default:
                    Debug.LogWarning("[QuestTrackerUI Sequence] 請指定 ShowAndExpand、Hide、Collapse 或 Refresh。", this);
                    break;
            }

            Stop();
        }
    }
}
