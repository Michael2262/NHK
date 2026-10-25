using System;

/// <summary>女主角現役數值與統計。舊 JSON 的卡池、舊能力等多餘欄位不再讀取。</summary>
[Serializable]
public class HeroineSaveData
{
    public HeroineEmotionCardType Emotion = HeroineStatusModel.InitialEmotion;
    public int Trust;
    public int Affinity;
    // 舊存檔未包含 Mood 時使用初始值；明確保存的 0 仍維持 0。
    public int Mood = HeroineStatusModel.InitialMood;
    public int Libido;
    public bool LibidoIncreasedToday;
    public int Excitement;
    public int Orgasm;
    public int HCount;
    public bool IsInHeat;
    public HeroineStatisticsSaveData StatisticsData;
}
