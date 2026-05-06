using UnityEngine;

/// <summary>
/// 定義手勢的滑動方向 (可依需求擴充)
/// (來自舊版)
/// </summary>
public enum SwipeDir
{
    None,
    Up,
    Down,
    Left,
    Right
}

/// <summary>
/// C# 輸入管理器 (例如 TouchCursorController) 在偵測到輸入時，
/// 應建立此結構並傳遞給過濾器。
/// 
/// (此結構是為了支援新版 Unity Input 而設計的，
///  它本身與輸入系統無關，但「建立它」的 C# 腳本應使用新版 Input System)
/// (基於舊版 簡化)
/// </summary>
public struct TouchGesture
{
    public bool isClick; //
    public SwipeDir swipe; //
}

/// <summary>
/// 介面：定義一個「可被觸發反應」的物件
/// (來自舊版)
/// </summary>
public interface ITouchReaction
{
    /// <summary>
    /// 觸發此反應 (FSM 將透過此方法溝通)
    /// (來自舊版)
    /// </summary>
    void OnTouched();
}

/// <summary>
/// 介面：定義一個「手勢過濾器」
/// (來自舊版)
/// </summary>
public interface IGestureFilter
{
    /// <summary>
    /// 檢查傳入的手勢是否滿足此過濾器的條件
    /// (來自舊版)
    /// </summary>
    bool Match(TouchGesture g);
}