using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 根據場景入口ID (Entry ID) 來決定要觸發哪一個 UnityEvent 的任務。
/// </summary>
public class Task_InvokeUnityEventByEntry : SceneReadyTaskBase
{
    // 內部類別，用於在 Inspector 中建立對應列表
    [System.Serializable]
    public class EntryEvent
    {
        public string entryID;
        public UnityEvent action;
    }

    [Header("預設事件 (Fallback)")]
    [Tooltip("如果下方列表中找不到符合的 Entry ID，或列表為空，則觸發此事件。")]
    [SerializeField] private UnityEvent defaultAction;

    [Header("根據入口ID觸發的事件列表")]
    [Tooltip("將根據傳入的 Entry ID 在此尋找要觸發的事件。")]
    [SerializeField] private List<EntryEvent> entryActions;

    public override IEnumerator ExecuteTask(string entryID)
    {
        // --- 決定要觸發哪個事件 ---
        UnityEvent eventToInvoke = defaultAction;
        bool entryFound = false;

        // 1. 優先檢查覆寫列表
        if (entryActions != null && entryActions.Count > 0)
        {
            foreach (var entryEvent in entryActions)
            {
                if (entryEvent.entryID == entryID)
                {
                    eventToInvoke = entryEvent.action;
                    entryFound = true;
                    break;
                }
            }
        }

        Debug.Log(entryFound
            ? $"[SceneTask] 根據入口 '{entryID}'，觸發特定 UnityEvent。"
            : $"[SceneTask] 未找到入口對應，觸發預設 UnityEvent。");

        // --- 觸發事件 ---
        eventToInvoke?.Invoke();
        yield return null;
    }
}