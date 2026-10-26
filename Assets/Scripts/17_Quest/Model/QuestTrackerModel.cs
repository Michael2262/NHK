using System;
using System.Collections.Generic;

/// <summary>
/// 單一追蹤面板的暫存名單，不掛場景、不存檔、不修改 Quest 進度。
/// 重新挑選才加入任務；平常只移除已領獎或失效的任務，停用與完成仍保留。
/// </summary>
public sealed class QuestTrackerModel
{
    private readonly QuestModel _quests;
    private readonly List<string> _visibleIDs = new List<string>();

    public int DisplayDay { get; private set; } = -1;
    public int MaxCount { get; private set; }

    public QuestTrackerModel(QuestModel quests) => _quests = quests;

    /// <summary>依資料庫順序重新選取啟動中且未領獎的前 N 筆，包含已完成任務。</summary>
    public void RefreshTasks(int day, int maxCount)
    {
        DisplayDay = day;
        MaxCount = Math.Max(0, maxCount);
        _visibleIDs.Clear();
        if (!CanReadProgress || day < 0 || MaxCount == 0) return;
        foreach (string id in _quests.GetActiveQuestIDs())
        {
            var progress = _quests.GetProgress(id);
            if (progress == null || progress.IsRewarded) continue;
            _visibleIDs.Add(id);
            if (_visibleIDs.Count == MaxCount) break;
        }
    }

    /// <summary>同一天不補位；換日或調整顯示上限時重新挑選。</summary>
    public void UpdateProgress(int day, int maxCount)
    {
        if (DisplayDay != day || MaxCount != Math.Max(0, maxCount))
        {
            RefreshTasks(day, maxCount);
            return;
        }
        if (!CanReadProgress)
        {
            _visibleIDs.Clear();
            return;
        }
        _visibleIDs.RemoveAll(id =>
        {
            var progress = _quests.GetProgress(id);
            return progress == null || progress.IsRewarded;
        });
    }

    /// <summary>目前項目的唯讀顯示資料；順序不隨任務啟停或完成狀態改變。</summary>
    public QuestTrackerItemState[] GetItems()
    {
        var items = new List<QuestTrackerItemState>();
        if (CanReadProgress)
            foreach (string id in _visibleIDs)
            {
                var info = _quests.GetDefinition(id);
                var progress = _quests.GetProgress(id);
                if (info != null && progress != null && !progress.IsRewarded)
                    items.Add(new QuestTrackerItemState(info, progress, DisplayDay));
            }
        return items.ToArray();
    }

    private bool CanReadProgress
        => _quests != null && _quests.ConfigurationError == null && _quests.LoadError == null;
}

/// <summary>單筆追蹤 UI 所需的顯示狀態，避免 View 自行推斷任務規則。</summary>
public sealed class QuestTrackerItemState
{
    public string QuestID { get; }
    public string TitleTextKey { get; }
    public int StickerCount { get; }
    public bool ShowStickerCount => StickerCount > 0;
    public bool ShowSticker => StickerCount != 0;
    public bool ShowDailyProgress { get; }
    public bool PerformedToday { get; }
    public int CurrentCount { get; }
    public int RequiredCount { get; }
    public bool IsCompleted { get; }

    internal QuestTrackerItemState(QuestInfo info, QuestProgressSaveData progress, int day)
    {
        QuestID = info.QuestID;
        TitleTextKey = info.TitleTextKey;
        StickerCount = info.Rewards.StickerDelta;
        ShowDailyProgress = info.OncePerDay;
        PerformedToday = info.OncePerDay && day >= 0 && progress.LastPerformedDay == day;
        CurrentCount = progress.CurrentCount;
        RequiredCount = info.RequiredCount;
        IsCompleted = progress.IsCompleted;
    }
}
