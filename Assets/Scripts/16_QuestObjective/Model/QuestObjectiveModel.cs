using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary> 任務目標的三態 </summary>
public enum QuestObjectiveState
{
    Hidden = 0,    // 未顯示（預設，不存檔）
    Revealed = 1,  // 已顯示但未完成
    Completed = 2  // 已完成
}

/// <summary>
/// 任務目標 Model（純 C#，由 GameStatusService 建立並持有）。
/// - 目錄（catalog）來自 Resources/Progress/Objective/ 下的 QuestObjectiveDefinition。
/// - 只記錄「非 Hidden」的目標狀態；查不到的 ID 一律視為 Hidden。
/// - 完成時若該目標的 MirrorFlagOnComplete 為 true，
///   會同步加一顆 Persistent Flag（ObjDone_目標ID）給既有條件判斷管線用。
/// </summary>
public class QuestObjectiveModel
{
    /// <summary> 完成映射 Flag 的前綴：ObjDone_ + ObjectiveID </summary>
    public const string MIRROR_FLAG_PREFIX = "ObjDone_";

    // ───── 目錄與狀態 ─────
    private readonly Dictionary<string, QuestObjectiveDefinition> _catalog =
        new Dictionary<string, QuestObjectiveDefinition>();

    // 只存非 Hidden 的狀態
    private readonly Dictionary<string, QuestObjectiveState> _states =
        new Dictionary<string, QuestObjectiveState>();

    private readonly ProgressFlagModel _progressFlags;

    // ───── 事件 ─────
    public event Action<string, QuestObjectiveState> OnObjectiveChanged;

    public QuestObjectiveModel(ProgressFlagModel progressFlags, QuestObjectiveDefinition[] definitions)
    {
        _progressFlags = progressFlags;

        if (definitions == null) return;
        foreach (var def in definitions)
        {
            if (def == null || string.IsNullOrEmpty(def.ObjectiveID)) continue;
            if (_catalog.ContainsKey(def.ObjectiveID))
            {
                Debug.LogWarning($"[QuestObjective] 發現重複的目標 ID：{def.ObjectiveID}，已略過後者。");
                continue;
            }
            _catalog[def.ObjectiveID] = def;
        }
    }

    // ───── 查詢 API ─────

    public QuestObjectiveState GetState(string id)
    {
        if (string.IsNullOrEmpty(id)) return QuestObjectiveState.Hidden;
        return _states.TryGetValue(id, out var state) ? state : QuestObjectiveState.Hidden;
    }

    public bool IsRevealed(string id) => GetState(id) == QuestObjectiveState.Revealed;
    public bool IsCompleted(string id) => GetState(id) == QuestObjectiveState.Completed;

    /// <summary>
    /// 取得指定狀態的所有目標定義（依 SortOrder → ID 排序），給 UI 列表用。
    /// 注意：只回傳目錄中有定義的目標；沒有對應 SO 的 ID 無法顯示（Reveal 時會警告）。
    /// </summary>
    public List<QuestObjectiveDefinition> GetObjectives(QuestObjectiveState state)
    {
        var result = new List<QuestObjectiveDefinition>();
        foreach (var def in _catalog.Values)
        {
            if (GetState(def.ObjectiveID) == state)
                result.Add(def);
        }
        result.Sort((a, b) =>
        {
            int cmp = a.SortOrder.CompareTo(b.SortOrder);
            return cmp != 0 ? cmp : string.CompareOrdinal(a.ObjectiveID, b.ObjectiveID);
        });
        return result;
    }

    /// <summary> 依 ID 取定義（可能為 null） </summary>
    public QuestObjectiveDefinition GetDefinition(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _catalog.TryGetValue(id, out var def) ? def : null;
    }

    // ───── 狀態變更 API ─────

    /// <summary> 顯示目標（Hidden → Revealed）。已完成的目標不會被降級。 </summary>
    public void Reveal(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        WarnIfUnknown(id);

        var current = GetState(id);
        if (current != QuestObjectiveState.Hidden) return; // 已顯示或已完成，不動作

        _states[id] = QuestObjectiveState.Revealed;
        OnObjectiveChanged?.Invoke(id, QuestObjectiveState.Revealed);
    }

    /// <summary> 完成目標（任何狀態 → Completed），未顯示的目標會直接跳到完成。 </summary>
    public void Complete(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        WarnIfUnknown(id);

        if (GetState(id) == QuestObjectiveState.Completed) return;

        _states[id] = QuestObjectiveState.Completed;

        // 完成映射：由 SO 上的 bool 決定，預設不映射
        var def = GetDefinition(id);
        if (def != null && def.MirrorFlagOnComplete)
            _progressFlags?.AddPersistentFlag(MIRROR_FLAG_PREFIX + id);

        OnObjectiveChanged?.Invoke(id, QuestObjectiveState.Completed);
    }

    /// <summary>
    /// 隱藏目標（回到 Hidden）。主要給除錯 / 特殊劇情用。
    /// 注意：不會移除已映射的 ObjDone_ Flag，需要的話請自行 RemoveFlag。
    /// </summary>
    public void Hide(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (_states.Remove(id))
            OnObjectiveChanged?.Invoke(id, QuestObjectiveState.Hidden);
    }

    private void WarnIfUnknown(string id)
    {
        // 允許目錄外的 ID 照常記錄（劇本先行的工作流程），但提醒補建 SO，否則 UI 顯示不出來
        if (!_catalog.ContainsKey(id))
            Debug.LogWarning($"[QuestObjective] 目標 ID「{id}」在 Resources/Progress/Objective 中沒有對應的 QuestObjectiveDefinition，UI 將無法顯示它。");
    }

    // ───── 生命週期與存檔 ─────

    /// <summary> 新遊戲：清空所有狀態，並為原本非 Hidden 的目標廣播歸零事件。 </summary>
    public void NewGame()
    {
        var snapshot = new List<string>(_states.Keys);
        _states.Clear();
        foreach (var id in snapshot)
            OnObjectiveChanged?.Invoke(id, QuestObjectiveState.Hidden);
    }

    public QuestObjectiveSaveData ToSaveData()
    {
        var data = new QuestObjectiveSaveData();
        foreach (var kv in _states)
        {
            if (kv.Value == QuestObjectiveState.Completed) data.CompletedIDs.Add(kv.Key);
            else if (kv.Value == QuestObjectiveState.Revealed) data.RevealedIDs.Add(kv.Key);
        }
        return data;
    }

    public void LoadFromSaveData(QuestObjectiveSaveData data)
    {
        NewGame();
        if (data == null) return; // 舊存檔沒有此欄位 → 全部維持 Hidden

        // 注意：載檔時刻意「不」逐項廣播，等 GameStatusService.NotifyGameStatusLoaded()
        // 統一觸發 OnGameStatusLoaded，各系統整批 refresh（與其他 Model 的慣例一致）。
        if (data.RevealedIDs != null)
            foreach (var id in data.RevealedIDs)
                if (!string.IsNullOrEmpty(id)) _states[id] = QuestObjectiveState.Revealed;

        if (data.CompletedIDs != null)
            foreach (var id in data.CompletedIDs)
                if (!string.IsNullOrEmpty(id)) _states[id] = QuestObjectiveState.Completed;
    }
}
