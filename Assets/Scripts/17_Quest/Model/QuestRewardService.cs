using System;
using System.Collections.Generic;

/// <summary>依 QuestDatabase 順序或指定 ID 領取一筆固定獎勵；不依賴場景元件，不自行迴圈領完。</summary>
public sealed class QuestRewardService
{
    private readonly QuestModel _quests;
    private readonly ProtagonistStatusModel _protagonist;
    private readonly Func<string, HeroineStatusModel> _resolveHeroine;
    private readonly ProgressFlagModel _flags;
    private readonly Action<string> _reportWarning;

    public bool HasUnclaimedRewards => _quests != null && _quests.HasUnclaimedRewards;
    public string LastError { get; private set; }

    public QuestRewardService(QuestModel quests, ProtagonistStatusModel protagonist,
        Func<string, HeroineStatusModel> resolveHeroine, ProgressFlagModel flags, Action<string> reportWarning = null)
    {
        _quests = quests;
        _protagonist = protagonist;
        _resolveHeroine = resolveHeroine;
        _flags = flags;
        _reportWarning = reportWarning;
    }

    public QuestResult ClaimNextReward(bool deactivateQuest = true)
        => TryClaimNextReward(out _, deactivateQuest);

    /// <summary>
    /// 每次只領清單最前面的已完成、未領取任務。一定記錄已領取；可選擇不自動停用。
    /// 設定錯誤時不發任何獎勵、不標記，也不跳過這筆去領後面的任務。
    /// </summary>
    public QuestResult TryClaimNextReward(out string questID, bool deactivateQuest = true)
    {
        questID = null;
        if (_quests == null) return QuestResult.NotConfigured;
        if (_quests.IsBusy) return QuestResult.Busy;
        LastError = null;
        if (_quests.LoadError != null) return QuestResult.InvalidSaveData;
        if (_quests.ConfigurationError != null) return QuestResult.InvalidConfiguration;
        string nextID = _quests.GetNextUnclaimedQuestID();
        if (nextID == null) return QuestResult.NoUnclaimedReward;
        var result = ClaimReward(nextID, deactivateQuest);
        if (result == QuestResult.Success) questID = nextID;
        return result;
    }

    /// <summary>只領指定的已完成、未領取任務，不限制啟動狀態；失敗不改領其他任務。</summary>
    public QuestResult ClaimReward(string questID, bool deactivateQuest = true)
    {
        if (_quests == null) return QuestResult.NotConfigured;
        if (_quests.IsBusy) return QuestResult.Busy;
        LastError = null;
        var eligibility = _quests.CanMarkRewarded(questID);
        if (eligibility != QuestResult.Success) return eligibility;
        var reward = _quests.GetDefinition(questID).Rewards;
        if (!TryPrepare(reward, out var actions))
        {
            _reportWarning?.Invoke($"[QuestRewardService] 任務「{questID}」尚未領取：{LastError}");
            return QuestResult.InvalidReward;
        }

        var errors = new List<Exception>();
        var result = _quests.ClaimReward(questID, () =>
        {
            foreach (var apply in actions)
            {
                // 正式 Model 在修改數值後才通知。訂閱端拋錯不可中斷剩餘獎勵或造成再次領取。
                try { apply(); }
                catch (Exception exception) { errors.Add(exception); }
            }
        }, deactivateQuest);
        foreach (var error in errors)
            _reportWarning?.Invoke($"[QuestRewardService] 任務「{questID}」已領取，但數值通知發生錯誤：{error}");
        return result;
    }

    private bool TryPrepare(QuestRewardSnapshot reward, out List<Action> actions)
    {
        actions = new List<Action>();
        if (reward.ConfigurationError != null) return Fail(reward.ConfigurationError);
        if (reward.StickerDelta != 0)
        {
            if (_protagonist == null) return Fail("主角尚未初始化。");
            actions.Add(() => _protagonist.AddStickerCount(reward.StickerDelta));
        }
        foreach (var entry in reward.Heroines)
        {
            // 每次領取重新解析角色；讀檔／新遊戲重建的 Model 不會被舊引用取代。
            var heroine = _resolveHeroine?.Invoke(entry.HeroineID);
            if (heroine == null) return Fail($"找不到女主角 ID：{entry.HeroineID}。");
            if (entry.Mood != 0) actions.Add(() => heroine.AddMood(entry.Mood));
            if (entry.Trust != 0) actions.Add(() => heroine.AddTrust(entry.Trust));
            if (entry.Libido != 0) actions.Add(() => heroine.AddLibido(entry.Libido));
        }
        foreach (var entry in reward.Flags)
        {
            if (_flags == null) return Fail("Flag 系統尚未初始化。");
            if (entry.Enabled) actions.Add(() => _flags.AddFlag(entry.FlagID, entry.Lifetime));
            else actions.Add(() => _flags.RemoveFlag(entry.FlagID));
        }
        return true;
    }

    private bool Fail(string error)
    {
        LastError = error;
        return false;
    }
}
