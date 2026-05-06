using UnityEngine;
using System.Collections;

/// <summary>
/// 薄包裝任務：在 Coordinator 管線中觸發 ProgressStateController 的初始化評估。
/// 
/// 【為什麼不直接把 ProgressStateController 改成 Task？】
/// 因為 ProgressStateController 有持續運作的職責（訂閱 Flag/Variable 變動事件），
/// 它需要一直活在場景上。這個 wrapper 只負責在正確時機觸發一次 Evaluate()。
/// </summary>
public class Task_InitProgressState : SceneReadyTaskBase
{
    [Header("目標 ProgressStateController")]
    [Tooltip("拖入場景中的 ProgressStateController。如果留空，會自動在場景中搜尋。")]
    [SerializeField] private ProgressStateController targetController;

    public override IEnumerator ExecuteTask(string entryID)
    {
        // 自動搜尋
        if (targetController == null)
        {
            targetController = FindObjectOfType<ProgressStateController>();
        }

        if (targetController == null)
        {
            Debug.LogWarning($"[Task_InitProgressState] 場景中找不到 ProgressStateController，跳過。");
            yield break;
        }

        Debug.Log($"[Task_InitProgressState] 觸發 ProgressStateController 評估");
        targetController.Evaluate();
        yield return null;
    }
}
