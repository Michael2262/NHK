using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// 【簡化版】一個專門用於觸發「單一指定」UnityEvent 的任務。
/// 它不處理任何根據入口ID變化的邏輯。
/// </summary>
public class Task_InvokeUnityEvent : SceneReadyTaskBase
{
    [Header("要執行的事件")]
    [Tooltip("此處設定的事件會在輪到此任務時立即執行完畢。")]
    [SerializeField] private UnityEvent actionsToInvoke;

    // 接收 entryID 參數但完全不使用它
    public override IEnumerator ExecuteTask(string entryID)
    {
        Debug.Log($"[SceneTask] 正在執行固定 UnityEvent: {gameObject.name}");
        actionsToInvoke?.Invoke();
        yield return null;
    }
}