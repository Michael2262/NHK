/// <summary>
/// NHK 當前情緒種類；保留型別名稱與列舉索引以維持既有綁定。
/// 獨立成檔案，供 HeroineStatusModel / SaveData / 抽選機 / View 共用。
///
/// Normal 放在最前面（= 0），為 default 值。
/// Normal 是「無特殊情緒」狀態；新角色初始值由 Model 指定為 Angry。
/// </summary>
public enum HeroineEmotionCardType
{
    /// <summary>普通（無特殊情緒）。正式情緒之一，可作為 CurrentEmotion 值。</summary>
    Normal,

    Angry,
    Shy,
    Worried,
    Maternal,
    Relaxed,
    Disappointed
}
