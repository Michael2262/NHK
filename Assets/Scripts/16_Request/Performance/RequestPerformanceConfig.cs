using System;
using UnityEngine;

/// <summary>
/// 請求表演的抽象基底。每種表演都是它的子類 ScriptableObject，
/// 放在 Resources/RequestPerformance/，檔名即 Request(...) 呼叫用的表演名。
///
/// 由 RequestPerformanceManager 依序播放；每段演完務必呼叫 onDone。
/// </summary>
public abstract class RequestPerformanceConfig : ScriptableObject
{
    /// <summary>
    /// 播放這段表演。
    /// </summary>
    /// <param name="host">用來跑協程的 MonoBehaviour（通常是 RequestPerformanceManager）。</param>
    /// <param name="heroineID">對象女主角 ID。</param>
    /// <param name="pass">本次請求成敗（由指令讀 Flag_RequestPass 得來）。不需要的表演可忽略。</param>
    /// <param name="args">呼叫端的額外參數（第 3 個起），例如情緒名。各表演自行解讀。</param>
    /// <param name="onDone">演完務必呼叫，通知管理器接續下一個。</param>
    public abstract void Play(MonoBehaviour host, string heroineID, bool pass, string[] args, Action onDone);
}
