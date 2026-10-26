using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>任務的固定設定；執行進度由 QuestModel 持有，不寫回此資產。</summary>
[CreateAssetMenu(menuName = "Game/Quest/任務定義", fileName = "Quest_New")]
public sealed class QuestDefinition : ScriptableObject
{
    /// <summary>與 Flag 相同，以 SO 名稱（檔名不含副檔名）識別任務。</summary>
    public string QuestID => name;
    public string TitleTextKey;
    public string DescriptionTextKey;

    [Min(1)] public int RequiredCount = 3;
    [Tooltip("同一任務每個遊戲日最多成功計次一次。")]
    public bool OncePerDay;
    [Tooltip("新遊戲時是否啟動；已有存檔時使用存檔中的狀態。")]
    public bool InitiallyActive;

    [Header("完成後領取的獎勵")]
    public QuestRewardDefinition Rewards = new QuestRewardDefinition();
}

/// <summary>固定獎勵；正數增加、負數扣除，零代表不變更。</summary>
[Serializable]
public sealed class QuestRewardDefinition
{
    [InspectorName("主角貼紙增減")]
    [Tooltip("直接增減實際持有的貼紙，不進入待領貼紙數值。")]
    public int StickerDelta;
    [InspectorName("女主角數值增減")]
    public List<QuestHeroineReward> Heroines = new List<QuestHeroineReward>();
    [InspectorName("Flag 開關")]
    public List<QuestFlagReward> Flags = new List<QuestFlagReward>();
}

[Serializable]
public sealed class QuestHeroineReward
{
    [InspectorName("女主角 ID")] public string HeroineID;
    [InspectorName("心情 Mood 增減")] public int Mood;
    [InspectorName("信任 Trust 增減")] public int Trust;
    [InspectorName("性慾 Libido 增減")] public int Libido;
}

[Serializable]
public sealed class QuestFlagReward
{
    [InspectorName("Flag SO")] public ProgressFlagDefinition Flag;
    [InspectorName("開啟")]
    [Tooltip("開啟時依存活週期啟用或預約 Flag；關閉時移除該 Flag，忽略存活週期。")]
    public bool Enabled = true;
    [InspectorName("存活週期")]
    public FlagLifetime Lifetime = FlagLifetime.Persistent;
}
