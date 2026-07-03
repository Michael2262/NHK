using System;

/// <summary>
/// NHK 情緒卡種類。
/// 獨立成檔案，供 HeroineStatusModel / SaveData / 抽選機 / View 共用。
///
/// Normal 放在最前面（= 0），為 default 值。
/// Normal 是「無特殊情緒」狀態，與其他情緒一樣可以進入卡池、被抽選。
/// </summary>
public enum HeroineEmotionCardType
{
    /// <summary>普通（無特殊情緒）。正式情緒之一，可進卡池、可作為 CurrentEmotion 值。</summary>
    Normal,

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
