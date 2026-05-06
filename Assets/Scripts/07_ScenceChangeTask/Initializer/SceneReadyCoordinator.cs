using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 場景準備工作的總指揮官（v2）。
/// 
/// 【改動重點】
/// - 現在是場景中「唯一的 ISceneReadyHandler 入口」，SceneController 只需找到它並執行。
/// - 所有準備工作（包括原本硬編碼在 SceneController 裡的 HubController、ProgressStateController）
///   都統一以 SceneReadyTaskBase 的形式，在 Inspector 的列表中排序執行。
/// - 支援 Prefab 化：做成一個公用 Prefab，每個場景拖入後在 Inspector 調整 Task 列表即可。
/// 
/// 【使用方式】
/// 1. 建立一個 Prefab，掛上此腳本
/// 2. 在 Prefab 上（或子物件上）掛載需要的 SceneReadyTaskBase 衍生類別
/// 3. 在 Inspector 的 sceneTasks 列表中，按照想要的執行順序排列
/// 4. 每個場景放一個此 Prefab 的實例，視需求增減 / 覆寫 Task
/// </summary>
public class SceneReadyCoordinator : MonoBehaviour, ISceneReadyHandler
{
    [Header("場景準備任務列表（依序執行）")]
    [Tooltip("Inspector 中的排列順序 = 執行順序。所有準備工作都在這裡管理。")]
    [SerializeField] private List<SceneReadyTaskBase> sceneTasks = new List<SceneReadyTaskBase>();

    public IEnumerator OnSceneReady()
    {
        string entryID = GameDataManager.Instance?.SceneEntryID ?? "Unknown";
        Debug.Log($"--- Coordinator: 開始準備場景，入口ID: [{entryID}] ---");

        if (sceneTasks.Count == 0)
        {
            Debug.Log("--- Coordinator: 無任務，直接完成 ---");
            yield break;
        }

        for (int i = 0; i < sceneTasks.Count; i++)
        {
            var task = sceneTasks[i];
            if (task == null)
            {
                Debug.LogWarning($"--- Coordinator: 任務 [{i}] 為 null，已跳過 ---");
                continue;
            }

            if (!task.enabled)
            {
                Debug.Log($"--- Coordinator: 任務 [{i}] {task.GetType().Name} 已停用，跳過 ---");
                continue;
            }

            Debug.Log($"--- Coordinator: 執行任務 [{i}] {task.GetType().Name} ---");
            yield return task.ExecuteTask(entryID);
        }

        Debug.Log("--- Coordinator: 所有任務執行完畢 ---");
    }

    // ============================================================
    // 編輯器輔助
    // ============================================================

    [ContextMenu("Auto-Find Tasks on this GameObject")]
    private void AutoFindTasks()
    {
        sceneTasks.Clear();
        GetComponents(sceneTasks);
        Debug.Log($"自動找到了 {sceneTasks.Count} 個任務。");
    }

    [ContextMenu("Auto-Find Tasks in Children")]
    private void AutoFindTasksInChildren()
    {
        sceneTasks.Clear();
        GetComponentsInChildren(true, sceneTasks);
        // 排除自己身上沒有的（如果 Coordinator 本身也繼承了 TaskBase 的話）
        Debug.Log($"自動找到了 {sceneTasks.Count} 個任務（含子物件）。");
    }
}