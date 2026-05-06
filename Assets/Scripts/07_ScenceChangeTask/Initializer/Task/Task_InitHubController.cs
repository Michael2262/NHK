using UnityEngine;
using System.Collections;

/// <summary>
/// 薄包裝任務：在 Coordinator 管線中觸發 HubController 的初始化。
/// 
/// 【為什麼不直接把 HubController 改成 Task？】
/// 因為 HubController 有持續運作的職責（訂閱角色移動、時間變化等事件），
/// 它需要一直活在場景上。Task 的定位是「跑完就結束」的一次性工作。
/// 所以用這個 wrapper 讓 Coordinator 在正確時機觸發初始化，
/// HubController 本身保持原有的生命週期不變。
/// </summary>
public class Task_InitHubController : SceneReadyTaskBase
{
    [Header("目標 HubController")]
    [Tooltip("拖入場景中的 HubController。如果留空，會自動在場景中搜尋。")]
    [SerializeField] private HubController targetController;

    public override IEnumerator ExecuteTask(string entryID)
    {
        Debug.Log($"<color=magenta>[Task_InitHubController] ExecuteTask 進入,entryID={entryID}</color>");  

        // 自動搜尋
        if (targetController == null)
        {
            targetController = FindObjectOfType<HubController>();
        }

        if (targetController == null)
        {
            Debug.LogWarning($"[Task_InitHubController] 場景中找不到 HubController，跳過。");
            yield break;
        }

        Debug.Log($"[Task_InitHubController] 觸發 HubController 初始化");
        yield return targetController.Initialize();
    }
}
