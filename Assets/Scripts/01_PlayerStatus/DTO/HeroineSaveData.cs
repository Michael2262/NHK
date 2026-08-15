using System;
using System.Collections.Generic;

/// <summary>
/// NHK 版女主角存檔資料。
/// 核心只保存情緒卡池、目前主導情緒、HCount，以及暫留統計/狀態效果資料。
/// </summary>
[Serializable]
public class HeroineSaveData
{
    public HeroineEmotionCardType Emotion;
    public HeroineEmotionCardType DominantEmotion;
    public int Libido;
    public int Trust;
    public int HCount;
    public bool IsInHeat;

    public int NextEmotionCardOrder;
    public List<HeroineEmotionCardSaveData> EmotionDeckData = new List<HeroineEmotionCardSaveData>();

    // 暫留相容資料。若後續確認無引用，可以再移除。
    public HeroineStatisticsSaveData StatisticsData;
    public ProtagonistStatusEffectSaveData StatusEffectData;
}
