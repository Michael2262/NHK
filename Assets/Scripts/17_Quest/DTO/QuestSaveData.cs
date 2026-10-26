using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>單一存檔槽的 Quest 進度；使用 ID List + Data List 配對。</summary>
[Serializable]
public sealed class QuestSaveData
{
    public const int CurrentVersion = 2;
    public int Version = CurrentVersion;
    public List<string> QuestIDs = new List<string>();
    public List<QuestProgressSaveData> QuestDataList = new List<QuestProgressSaveData>();

    // 僅用來讀取第一版啟用清單；成功轉換後清空，不寫入第二版存檔。
    [JsonProperty("ActiveQuestIDs", NullValueHandling = NullValueHandling.Ignore)]
    internal List<string> LegacyActiveQuestIDs;

    public QuestSaveData Copy()
    {
        var copy = (QuestSaveData)MemberwiseClone();
        copy.QuestIDs = QuestIDs == null ? null : new List<string>(QuestIDs);
        copy.QuestDataList = QuestDataList == null ? null : QuestDataList.ConvertAll(item => item?.Copy());
        copy.LegacyActiveQuestIDs = LegacyActiveQuestIDs == null ? null : new List<string>(LegacyActiveQuestIDs);
        return copy;
    }
}

[Serializable]
public sealed class QuestProgressSaveData
{
    public bool IsActive;
    public int CurrentCount;
    public bool IsCompleted;
    public bool IsRewarded;
    public int LastPerformedDay = -1;

    public QuestProgressSaveData Copy() => (QuestProgressSaveData)MemberwiseClone();
}
