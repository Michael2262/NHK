/// <summary>
/// Animator 內已綁好的所有 Trigger。
///
/// 注意:列舉的「名稱」必須與 Animator Controller 裡的 Trigger 參數名稱
/// 完全一致 (含大小寫),因為程式會用 ToString() 去對應實際的 Trigger。
/// </summary>
public enum EmotionAnimatorTrigger
{
    // 情緒 (觸發時會先清掉其他情緒 Trigger)
    Angry,
    Shy,
    Worried,
    Maternal,
    Relaxed,
    Disappointed,

    // 播放控制
    Think,
    Stop,
}
