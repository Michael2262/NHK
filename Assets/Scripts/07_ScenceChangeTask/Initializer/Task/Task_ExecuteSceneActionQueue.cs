using UnityEngine;
using System.Collections;

/// <summary>
/// 執行場景動作佇列（SceneActionQueueModel）中所有待執行命令的任務。
///
/// 【時機】場景載入後、淡入前（壓幕期間），時間推進等變化不會被玩家看到過程。
/// 【建議】放在 Coordinator 任務列表的前段，讓後續任務（如 Task_InitProgressState）
///         看到的是命令執行後的世界狀態。
/// 【注意】小遊戲串關期間佇列處於暫停狀態，本任務會自動跳過。
/// </summary>
public class Task_ExecuteSceneActionQueue : SceneReadyTaskBase
{
    public override IEnumerator ExecuteTask(string entryID)
    {
        var queue = GameStatusService.Instance?.SceneActionQueue;
        if (queue == null)
        {
            Debug.LogWarning("[SceneTask] 找不到 GameStatusService.SceneActionQueue，跳過佇列執行。");
            yield break;
        }

        queue.ExecuteAll();
        yield return null;
    }
}
