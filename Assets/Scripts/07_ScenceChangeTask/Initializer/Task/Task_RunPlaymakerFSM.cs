using UnityEngine;
using System.Collections;
using System.Linq;

/// <summary>
/// 在 SceneReady 管線中，對指定的 Playmaker FSM 發送一個事件（類似 FsmTrigger 的 Task 版）。
///
/// 【運作方式】
/// Coordinator 執行到此任務時，對 targetObject 上名稱符合 fsmName 的 FSM
/// 發送 eventName（預設 "TRIGGER"），發完即完成，不等待 FSM 跑完。
///
/// 【Playmaker 端設定】
/// - 目標 FSM 保持啟用，起始 State 閒置等待，並掛上 eventName 的全域轉換（Global Transition）。
/// </summary>
public class Task_RunPlaymakerFSM : SceneReadyTaskBase
{
    [Header("目標 FSM")]
    [UnityEngine.Tooltip("目標 FSM 所在的 GameObject")]
    [SerializeField] private GameObject targetObject;

    [UnityEngine.Tooltip("目標 FSM 的名稱(在 PlayMaker 編輯器 FSM 頁籤最上面可以看到)")]
    [SerializeField] private string fsmName = "FSM";

    [Header("事件")]
    [UnityEngine.Tooltip("要發送給 FSM 的事件名稱")]
    [SerializeField] private string eventName = "TRIGGER";

    public override IEnumerator ExecuteTask(string entryID)
    {
        if (targetObject == null)
        {
            Debug.LogWarning($"[Task_RunPlaymakerFSM] {gameObject.name}: targetObject 未設定！");
            yield break;
        }

        var fsm = targetObject.GetComponents<PlayMakerFSM>()
                              .FirstOrDefault(f => f.FsmName == fsmName);
        if (fsm == null)
        {
            Debug.LogWarning($"[Task_RunPlaymakerFSM] 在 {targetObject.name} 上找不到名稱為 '{fsmName}' 的 FSM！");
            yield break;
        }

        if (string.IsNullOrEmpty(eventName))
        {
            Debug.LogWarning($"[Task_RunPlaymakerFSM] {gameObject.name}: eventName 為空，未發送。");
            yield break;
        }

        Debug.Log($"[Task_RunPlaymakerFSM] 對 {targetObject.name}/{fsm.FsmName} 發送事件「{eventName}」");
        fsm.SendEvent(eventName);
    }
}
