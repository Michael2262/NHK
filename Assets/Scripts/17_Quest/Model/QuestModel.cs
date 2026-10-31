using System;
using System.Collections.Generic;

/// <summary>
/// 每筆任務的唯一進度來源。只管理啟停、成功計次、每日限制、完成與已發獎勵紀錄。
/// 遊戲層自行判定成功與消耗資源；固定獎勵由 QuestRewardService 套用。
/// </summary>
public sealed class QuestModel
{
    private readonly Dictionary<string, QuestInfo> _catalog = new Dictionary<string, QuestInfo>(StringComparer.Ordinal);
    private readonly List<string> _order = new List<string>();
    private readonly Dictionary<string, QuestProgressSaveData> _progress = new Dictionary<string, QuestProgressSaveData>(StringComparer.Ordinal);
    private readonly Func<int> _getDay;
    private readonly Action<string> _reportWarning;
    private readonly bool _hasDatabase;
    private QuestSaveData _state;
    private bool _busy;

    public string ConfigurationError { get; }
    public string LoadError { get; private set; }
    public bool IsBusy => _busy;

    /// <summary>操作完成才通知；讀檔及新遊戲保持靜默，由 Service 統一刷新。</summary>
    public event Action OnChanged;

    public QuestModel(QuestDatabase database, Func<int> getDay, Action<string> reportWarning = null)
    {
        _getDay = getDay;
        _reportWarning = reportWarning;
        _hasDatabase = database != null;
        ConfigurationError = database == null ? null : database.GetConfigurationError();
        if (_hasDatabase && ConfigurationError == null)
        {
            foreach (var definition in database.Tasks)
            {
                var info = new QuestInfo(definition);
                _catalog.Add(info.QuestID, info);
                _order.Add(info.QuestID);
            }
        }
        NewGame();
    }

    public QuestInfo GetDefinition(string questID)
        => questID != null && _catalog.TryGetValue(questID, out var info) ? info : null;

    /// <summary>回傳複本；外部不可繞過 Model 改寫存檔狀態。</summary>
    public QuestProgressSaveData GetProgress(string questID)
        => LoadError == null && questID != null && _progress.TryGetValue(questID, out var record) ? record.Copy() : null;

    // 供持有 Inspector SO 引用的遊戲程式使用；所有規則仍走同一套 ID 操作。
    public QuestProgressSaveData GetProgress(QuestDefinition quest) => GetProgress(GetQuestID(quest));
    public QuestResult SetActive(QuestDefinition quest, bool active) => SetActive(GetQuestID(quest), active);
    public QuestResult CanRecordAction(QuestDefinition quest) => CanRecordAction(GetQuestID(quest));
    public QuestResult TryRecordAction(QuestDefinition quest) => TryRecordAction(GetQuestID(quest));
    public QuestResult TryRecordAction(QuestDefinition quest, out bool completedNow)
        => TryRecordAction(GetQuestID(quest), out completedNow);
    public QuestResult TryDecreaseSuccessCount(QuestDefinition quest) => TryDecreaseSuccessCount(GetQuestID(quest));
    public QuestResult CanMarkRewarded(QuestDefinition quest) => CanMarkRewarded(GetQuestID(quest));
    public QuestResult TryMarkRewarded(QuestDefinition quest) => TryMarkRewarded(GetQuestID(quest));

    private static string GetQuestID(QuestDefinition quest) => quest != null ? quest.QuestID : null;

    public string[] GetActiveQuestIDs()
    {
        var active = new List<string>();
        if (LoadError == null)
            foreach (string id in _order)
                if (_progress[id].IsActive) active.Add(id);
        return active.ToArray();
    }

    /// <summary>只計入目前已啟動且尚未完成的任務；無有效資料時回傳 false。</summary>
    public bool HasActiveIncompleteQuests
    {
        get
        {
            if (!_hasDatabase || ConfigurationError != null || LoadError != null) return false;
            foreach (string id in _order)
                if (_progress[id].IsActive && !_progress[id].IsCompleted) return true;
            return false;
        }
    }

    private QuestResult CheckQuest(string questID)
    {
        if (_busy) return QuestResult.Busy;
        if (LoadError != null) return QuestResult.InvalidSaveData;
        if (!_hasDatabase) return QuestResult.NotConfigured;
        if (ConfigurationError != null) return QuestResult.InvalidConfiguration;
        return GetDefinition(questID) == null ? QuestResult.UnknownQuest : QuestResult.Success;
    }

    /// <summary>停用或再次啟動不重設次數、日期、完成及已發獎勵紀錄。</summary>
    public QuestResult SetActive(string questID, bool active)
    {
        var result = CheckQuest(questID);
        if (result != QuestResult.Success || _progress[questID].IsActive == active) return result;
        return Mutate(() => _progress[questID].IsActive = active);
    }

    public QuestResult CanRecordAction(string questID) => CheckRecordAction(questID, out _);

    private QuestResult CheckRecordAction(string questID, out int day)
    {
        day = -1;
        var result = CheckQuest(questID);
        if (result != QuestResult.Success) return result;
        var progress = _progress[questID];
        if (!progress.IsActive) return QuestResult.NotActive;
        if (progress.IsCompleted) return QuestResult.AlreadyCompleted;
        if (_getDay == null || (day = _getDay()) < 0) return QuestResult.NotConfigured;
        if (_catalog[questID].OncePerDay && progress.LastPerformedDay >= day) return QuestResult.DailyLimit;
        return QuestResult.Success;
    }

    public QuestResult TryRecordAction(string questID) => TryRecordAction(questID, out _);

    /// <summary>遊戲層確認成功後呼叫；只加一。completedNow 表示本次是否剛好達標。</summary>
    public QuestResult TryRecordAction(string questID, out bool completedNow)
    {
        completedNow = false;
        var result = CheckRecordAction(questID, out int day);
        if (result != QuestResult.Success) return result;
        bool completes = _progress[questID].CurrentCount + 1 == _catalog[questID].RequiredCount;
        result = Mutate(() =>
        {
            var progress = _progress[questID];
            progress.CurrentCount++;
            progress.LastPerformedDay = day;
            progress.IsCompleted = completes;
        });
        completedNow = completes;
        return result;
    }

    /// <summary>
    /// 扣回一次成功紀錄，最低為零；停用或完成的任務也可修正。
    /// 保留最後計次日與已發獎紀錄，扣回次數不解除每日限制、不回收或重新開放獎勵。
    /// </summary>
    public QuestResult TryDecreaseSuccessCount(string questID)
    {
        var result = CheckQuest(questID);
        if (result != QuestResult.Success) return result;
        var progress = _progress[questID];
        if (progress.CurrentCount == 0) return QuestResult.Success;
        return Mutate(() =>
        {
            progress.CurrentCount--;
            progress.IsCompleted = progress.CurrentCount == _catalog[questID].RequiredCount;
        });
    }

    /// <summary>僅供遊戲層查詢是否尚待記錄發獎；不會發放任何獎勵。</summary>
    public QuestResult CanMarkRewarded(string questID)
    {
        var result = CheckQuest(questID);
        if (result != QuestResult.Success) return result;
        var progress = _progress[questID];
        if (!progress.IsCompleted) return QuestResult.NotCompleted;
        return progress.IsRewarded ? QuestResult.AlreadyRewarded : QuestResult.Success;
    }

    /// <summary>外部完成發獎後記錄；不要求仍啟用，不呼叫獎勵程式或補發效果。</summary>
    public QuestResult TryMarkRewarded(string questID)
    {
        var result = CanMarkRewarded(questID);
        return result == QuestResult.Success ? Mutate(() => _progress[questID].IsRewarded = true) : result;
    }

    /// <summary>只判斷完成及未領取；停用的任務仍可領取。順序沿用資料庫清單。</summary>
    public bool HasUnclaimedRewards => GetNextUnclaimedQuestID() != null;

    public string GetNextUnclaimedQuestID()
    {
        if (LoadError != null || ConfigurationError != null || !_hasDatabase) return null;
        foreach (string id in _order)
            if (_progress[id].IsCompleted && !_progress[id].IsRewarded) return id;
        return null;
    }

    /// <summary>
    /// 服務已驗證全部獎勵後才進入此處。先記錄領取，再同步套用，最後通知任務變更。
    /// 整段期間禁止重入操作，避免數值通知內再次發放同一筆或下一筆。
    /// </summary>
    internal QuestResult ClaimReward(string questID, Action applyRewards, bool deactivate)
    {
        var result = CanMarkRewarded(questID);
        if (result != QuestResult.Success) return result;
        if (applyRewards == null) return QuestResult.InvalidReward;
        var progress = _progress[questID];
        return Mutate(() =>
        {
            progress.IsRewarded = true;
            if (deactivate) progress.IsActive = false;
            applyRewards();
        });
    }

    private QuestResult Mutate(Action action)
    {
        _busy = true;
        try
        {
            action();
            OnChanged?.Invoke();
        }
        finally { _busy = false; }
        return QuestResult.Success;
    }

    /// <summary>靜默套用 SO 初始狀態；新遊戲不沿用前一存檔的紀錄。</summary>
    public void NewGame()
    {
        if (_busy) throw new InvalidOperationException("Quest 操作通知期間不可重設或載入，請於操作結束後執行。");
        LoadError = null;
        _state = new QuestSaveData();
        _progress.Clear();
        AddMissingDefaults();
    }

    private void AddMissingDefaults()
    {
        foreach (string id in _order)
        {
            if (_progress.ContainsKey(id)) continue;
            var record = new QuestProgressSaveData { IsActive = _catalog[id].InitiallyActive };
            _state.QuestIDs.Add(id);
            _state.QuestDataList.Add(record);
            _progress.Add(id, record);
        }
    }

    public QuestSaveData ToSaveData() => _state.Copy();

    /// <summary>
    /// 靜默載入；第一版只沿用進度及啟用清單，舊門檻領獎無法對應每筆任務，不推測為已發獎。
    /// 缺少定義的舊任務保留於存檔，不加入可操作進度；新增 ID 套用初始狀態。
    /// 其餘無效資料保留原進度並阻擋操作，避免部分載入後覆寫存檔。
    /// </summary>
    public void LoadFromSaveData(QuestSaveData data)
    {
        NewGame();
        if (data == null) return;
        var candidate = data.Copy();
        LoadError = ValidateSaveData(candidate);
        if (LoadError != null)
        {
            _state = candidate;
            _progress.Clear();
            _reportWarning?.Invoke($"[QuestModel] 任務資料暫停使用：{LoadError} 請檢查設定或存檔。");
            return;
        }

        if (candidate.Version == 1)
        {
            var active = new HashSet<string>(candidate.LegacyActiveQuestIDs, StringComparer.Ordinal);
            for (int i = 0; i < candidate.QuestIDs.Count; i++)
            {
                candidate.QuestDataList[i].IsActive = active.Contains(candidate.QuestIDs[i]);
                candidate.QuestDataList[i].IsRewarded = false;
            }
        }
        candidate.Version = QuestSaveData.CurrentVersion;
        candidate.LegacyActiveQuestIDs = null;
        _state = candidate;
        _progress.Clear();
        var missingIDs = new List<string>();
        for (int i = 0; i < _state.QuestIDs.Count; i++)
        {
            string id = _state.QuestIDs[i];
            if (_catalog.ContainsKey(id))
                _progress.Add(id, _state.QuestDataList[i]);
            else
                missingIDs.Add(id);
        }
        AddMissingDefaults();
        if (missingIDs.Count > 0)
            _reportWarning?.Invoke($"[QuestModel] 下列舊任務找不到定義，已保留存檔紀錄並略過使用，不影響其他任務：{string.Join(", ", missingIDs)}。");
    }

    private string ValidateSaveData(QuestSaveData data)
    {
        if (data.Version != 1 && data.Version != QuestSaveData.CurrentVersion)
            return $"不支援的資料版本 {data.Version}。";
        if (data.QuestIDs == null || data.QuestDataList == null || data.QuestIDs.Count != data.QuestDataList.Count)
            return "任務 ID 與資料清單缺失或長度不一致。";
        if (data.QuestIDs.Count > 0 && (!_hasDatabase || ConfigurationError != null))
            return "尚未指定有效的 QuestDatabase。";

        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < data.QuestIDs.Count; i++)
        {
            string id = data.QuestIDs[i];
            var info = GetDefinition(id);
            var record = data.QuestDataList[i];
            if (string.IsNullOrWhiteSpace(id)) return "任務紀錄 ID 不可為空。";
            if (!ids.Add(id)) return $"任務紀錄 ID 重複：{id}。";
            if (record == null || record.CurrentCount < 0
                || record.LastPerformedDay < -1 || (record.CurrentCount > 0 && record.LastPerformedDay < 0)
                // 曾發獎後可以扣回次數，因此已發獎不代表目前仍達標；仍須有成功計次日期。
                || (data.Version >= 2 && record.IsRewarded && record.LastPerformedDay < 0))
                return $"任務 {id} 的紀錄與目前設定不一致。";
            // 舊定義已移除時無法核對目標次數，保留紀錄；仍驗證 ID 與通用資料完整性。
            if (info != null && (record.CurrentCount > info.RequiredCount
                || record.IsCompleted != (record.CurrentCount == info.RequiredCount)))
                return $"任務 {id} 的紀錄與目前設定不一致。";
        }

        if (data.Version == 1)
        {
            if (data.LegacyActiveQuestIDs == null) return "第一版存檔缺少啟用任務清單。";
            var active = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in data.LegacyActiveQuestIDs)
                if (id == null || !ids.Contains(id) || !active.Add(id))
                    return "第一版啟用清單沒有對應的唯一任務紀錄。";
        }
        return null;
    }
}
