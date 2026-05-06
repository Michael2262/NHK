using UnityEngine;
using HutongGames.PlayMaker;

/// <summary>
/// 作為一個物件上所有邏輯組件的共用上下文，存放 FSM 引用與預設事件名稱。
/// </summary>
public class FsmContext : MonoBehaviour
{
    [Header("共享的 FSM 配置")]
    [UnityEngine.Tooltip("請拖入負責處理此物件邏輯的 FSM (通常在不同的 GameObject 上)")]
    public PlayMakerFSM sharedFSM;

    [UnityEngine.Tooltip("預設發送給 FSM 的事件名稱")]
    public string defaultEvent = "CSHARP_TRIGGERED_TOUCH";
}