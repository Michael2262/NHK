using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>任務目錄；清單順序決定待領獎勵的優先順序。設定在 Model 建立時取快照。</summary>
[CreateAssetMenu(menuName = "Game/Quest/任務資料庫", fileName = "QuestDatabase")]
public sealed class QuestDatabase : ScriptableObject
{
    public List<QuestDefinition> Tasks = new List<QuestDefinition>();

    public string GetConfigurationError()
    {
        if (Tasks == null) return "任務清單不可為 null。";
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < Tasks.Count; i++)
        {
            var task = Tasks[i];
            if (task == null) return $"任務第 {i + 1} 筆未指定定義。";
            if (string.IsNullOrWhiteSpace(task.QuestID) || task.QuestID != task.QuestID.Trim())
                return $"任務第 {i + 1} 筆需要有效的 SO 名稱，且頭尾不可有空白。";
            if (!ids.Add(task.QuestID)) return $"任務 SO 名稱（ID）重複：{task.QuestID}。";
            if (task.RequiredCount < 1) return $"任務 {task.QuestID} 的目標次數必須至少為 1。";
        }
        return null;
    }
}
