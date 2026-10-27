using System;

/// <summary>女主角現役數值與統計。舊 JSON 的卡池、舊能力等多餘欄位不再讀取。</summary>
[Serializable]
public class HeroineSaveData
{
    public HeroineEmotionCardType Emotion = HeroineStatusModel.InitialEmotion;
    // 舊存檔缺少 Awake 時為 100；明確保存的 0 仍維持 0。
    public float Awake = HeroineStatusModel.InitialAwake;
    public int Trust;
    public int Affinity;
    // 舊存檔未包含 Mood 時使用初始值；明確保存的 0 仍維持 0。
    public int Mood = HeroineStatusModel.InitialMood;
    // 舊存檔未包含 Interest 時使用初始值。
    public int Interest = HeroineStatusModel.InitialInterest;
    public int Libido;
    public bool LibidoIncreasedToday;
    public int Excitement;
    public int Orgasm;
    public int HCount;
    public bool IsInHeat;
    public HeroineStatisticsSaveData StatisticsData;
    // null 表示舊存檔尚無週期資料，依各角色的初始週期設定建立。
    public HeroineCycleSaveData CycleData;
}

/// <summary>每位女主角獨立保存；懷孕時仍保留暫停的自然週期位置。</summary>
[Serializable]
public class HeroineCycleSaveData
{
    public int CurrentCycleDay;
    public int AfterPeriodDays;
    public int NextAfterPeriodDays;
    public PregnancyState Pregnancy;
    public int EarlyPregnancyDaysElapsed;
    // null 表示舊檔無此欄位；0 則是明確設定的 0%，不能當成缺值。
    public float? PregnancyProbability;
}
