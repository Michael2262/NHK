using UnityEngine;

/// <summary>轉接任務啟停、成功次數與已發獎紀錄；不判定成功或執行獎勵效果。</summary>
[AddComponentMenu("NHK/Quest/Quest Bridge")]
public sealed class QuestBridge : MonoBehaviour
{
    [Header("目標任務")]
    [Tooltip("拖入要操作的 Quest SO；ID 就是 SO 檔名。所有無參數操作都使用這筆任務。")]
    [SerializeField] private QuestDefinition targetQuest;

    [ContextMenu("啟動任務")]
    public void ActivateQuest()
        => ChangeActive(targetQuest != null ? targetQuest.QuestID : null, true);

    [ContextMenu("停用任務")]
    public void DeactivateQuest()
        => ChangeActive(targetQuest != null ? targetQuest.QuestID : null, false);

    public void ActivateQuestByID(string questID) => ChangeActive(questID, true);

    public void DeactivateQuestByID(string questID) => ChangeActive(questID, false);

    [ContextMenu("標記已發獎勵")]
    public void MarkRewarded()
        => MarkRewardedByID(targetQuest != null ? targetQuest.QuestID : null);

    /// <summary>給外部獎勵流程結束後手動標記；此方法本身不發獎。</summary>
    public void MarkRewardedByID(string questID)
    {
        if (!TryGetModel(questID, out var model)) return;
        var result = model.TryMarkRewarded(questID);
        if (result != QuestResult.Success && result != QuestResult.AlreadyRewarded)
            Debug.LogWarning($"[QuestBridge] 任務「{questID}」標記已發獎勵未執行：{Describe(result)}", this);
    }

    [ContextMenu("成功次數 +1")]
    public void IncreaseSuccessCount()
        => ChangeCount(targetQuest != null ? targetQuest.QuestID : null, true);

    [ContextMenu("成功次數 -1")]
    public void DecreaseSuccessCount()
        => ChangeCount(targetQuest != null ? targetQuest.QuestID : null, false);

    /// <summary>給需要直接傳 ID 的 UnityEvent 或其他程式使用。</summary>
    public void IncreaseSuccessCountByID(string questID) => ChangeCount(questID, true);

    /// <summary>給需要直接傳 ID 的 UnityEvent 或其他程式使用。</summary>
    public void DecreaseSuccessCountByID(string questID) => ChangeCount(questID, false);

    private void ChangeCount(string questID, bool increase)
    {
        if (!TryGetModel(questID, out var model)) return;
        var result = increase ? model.TryRecordAction(questID) : model.TryDecreaseSuccessCount(questID);
        if (result != QuestResult.Success)
            Debug.LogWarning($"[QuestBridge] 任務「{questID}」{(increase ? "加一" : "減一")}未執行：{Describe(result)}", this);
    }

    private void ChangeActive(string questID, bool active)
    {
        if (!TryGetModel(questID, out var model)) return;
        var result = model.SetActive(questID, active);
        if (result != QuestResult.Success)
            Debug.LogWarning($"[QuestBridge] 任務「{questID}」{(active ? "啟動" : "停用")}未執行：{Describe(result)}", this);
    }

    private bool TryGetModel(string questID, out QuestModel model)
    {
        model = null;
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[QuestBridge] 請在 Play Mode 中操作任務進度。", this);
            return false;
        }
        if (string.IsNullOrWhiteSpace(questID))
        {
            Debug.LogWarning("[QuestBridge] 請指定目標 Quest SO，或傳入有效的任務 ID。", this);
            return false;
        }
        var service = GameStatusService.Instance;
        model = service != null ? service.Quests : null;
        if (model == null)
        {
            Debug.LogWarning("[QuestBridge] GameStatusService 尚未初始化 Quest。", this);
            return false;
        }
        return true;
    }

    private static string Describe(QuestResult result)
    {
        switch (result)
        {
            case QuestResult.Busy: return "正在處理另一項任務操作。";
            case QuestResult.NotConfigured: return "任務資料庫或遊戲日期尚未就緒。";
            case QuestResult.InvalidConfiguration: return "任務資料庫設定無效。";
            case QuestResult.InvalidSaveData: return "任務存檔資料無效。";
            case QuestResult.UnknownQuest: return "資料庫中找不到這個任務 ID。";
            case QuestResult.NotActive: return "任務尚未啟動。";
            case QuestResult.AlreadyCompleted: return "任務已完成，不能再增加次數。";
            case QuestResult.DailyLimit: return "今天已成功計次，已達每日限制。";
            case QuestResult.NotCompleted: return "任務尚未完成。";
            case QuestResult.AlreadyRewarded: return "任務已記錄為發過獎勵。";
            default: return result.ToString();
        }
    }
}
