using System;

/// <summary>
/// NHK 情緒卡種類。
/// 獨立成檔案，供 HeroineStatusModel / SaveData / 抽選機 / View 共用。
/// </summary>
public enum HeroineEmotionCardType
{
    Angry,
    Shy,
    Worried,
    Maternal,
    Relaxed,
    Disappointed
}

/// <summary>
/// 存檔用：一張情緒卡的資料。
/// AddedOrder 用於判斷「最舊卡」與平手時的「最近加入情緒」。
/// </summary>
[Serializable]
public class HeroineEmotionCardSaveData
{
    public HeroineEmotionCardType Type;
    public int AddedOrder;
}
