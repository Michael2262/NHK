using UnityEngine;

/// <summary>
/// 一個最簡單的實作：
/// 當手勢條件符合 (Match) 時，僅發送 FSM Event。
/// </summary>

public class NotifyFSMOnTouch : ConditionalTouchReactionBase
{
    public override void OnTouched()
    {
        // 呼叫基底類別內建的 FSM 通知方法
        NotifyFSM();

        // 可以在這裡加一行 Log 方便測試確認
        // Debug.Log($"[SimpleFSMTouchReaction] {name} Touched! Event Sent.");
    }
}