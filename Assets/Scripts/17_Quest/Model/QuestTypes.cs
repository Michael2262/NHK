/// <summary>任務進度及領取獎勵的操作結果。</summary>
public enum QuestResult
{
    Success,
    Busy,
    NotConfigured,
    InvalidConfiguration,
    InvalidSaveData,
    UnknownQuest,
    NotActive,
    AlreadyCompleted,
    DailyLimit,
    NotCompleted,
    AlreadyRewarded,
    NoUnclaimedReward,
    InvalidReward
}

/// <summary>任務設定的唯讀執行期快照；SO 修改於下一次建立 Model 時生效。</summary>
public sealed class QuestInfo
{
    public string QuestID { get; }
    public string TitleTextKey { get; }
    public string DescriptionTextKey { get; }
    public int RequiredCount { get; }
    public bool OncePerDay { get; }
    public bool InitiallyActive { get; }
    internal QuestRewardSnapshot Rewards { get; }

    public QuestInfo(QuestDefinition definition)
    {
        QuestID = definition.QuestID;
        TitleTextKey = definition.TitleTextKey;
        DescriptionTextKey = definition.DescriptionTextKey;
        RequiredCount = definition.RequiredCount;
        OncePerDay = definition.OncePerDay;
        InitiallyActive = definition.InitiallyActive;
        Rewards = new QuestRewardSnapshot(definition.Rewards);
    }
}

/// <summary>複製數值與 Flag ID，避免執行中改動 SO 影響本次遊戲的獎勵。</summary>
internal sealed class QuestRewardSnapshot
{
    internal readonly int StickerDelta;
    internal readonly QuestHeroineReward[] Heroines;
    internal readonly QuestFlagRewardSnapshot[] Flags;
    internal readonly string ConfigurationError;

    internal QuestRewardSnapshot(QuestRewardDefinition definition)
    {
        // 舊 SO 沒有獎勵欄位時視為空獎勵，仍可正常領取並記錄。
        StickerDelta = definition?.StickerDelta ?? 0;
        var heroines = new System.Collections.Generic.List<QuestHeroineReward>();
        var flags = new System.Collections.Generic.List<QuestFlagRewardSnapshot>();
        if (definition?.Heroines != null)
            foreach (var entry in definition.Heroines)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.HeroineID))
                {
                    ConfigurationError = "女主角獎勵缺少有效的角色 ID。";
                    continue;
                }
                heroines.Add(new QuestHeroineReward { HeroineID = entry.HeroineID,
                    Mood = entry.Mood, Trust = entry.Trust, Libido = entry.Libido });
            }
        if (definition?.Flags != null)
            foreach (var entry in definition.Flags)
            {
                if (entry == null || entry.Flag == null || string.IsNullOrWhiteSpace(entry.Flag.FlagID))
                {
                    ConfigurationError = "Flag 獎勵尚未指定有效的 Flag SO。";
                    continue;
                }
                if (entry.Enabled && !System.Enum.IsDefined(typeof(FlagLifetime), entry.Lifetime))
                {
                    ConfigurationError = $"Flag {entry.Flag.FlagID} 的存活週期無效。";
                    continue;
                }
                flags.Add(new QuestFlagRewardSnapshot(entry.Flag.FlagID, entry.Enabled, entry.Lifetime));
            }
        Heroines = heroines.ToArray();
        Flags = flags.ToArray();
    }
}

internal sealed class QuestFlagRewardSnapshot
{
    internal readonly string FlagID;
    internal readonly bool Enabled;
    internal readonly FlagLifetime Lifetime;

    internal QuestFlagRewardSnapshot(string flagID, bool enabled, FlagLifetime lifetime)
    {
        FlagID = flagID;
        Enabled = enabled;
        Lifetime = lifetime;
    }
}
