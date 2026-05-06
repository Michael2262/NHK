using UnityEngine;
using System.Collections;

/// <summary>
/// 薄包裝任務:在 Coordinator 管線中觸發 SceneMusicController 的初始化。
/// 
/// 為什麼需要 Task?
/// SceneMusicController 在 Start() 裡只訂閱事件,不立刻播放音樂。
/// 因為讀檔時,Start() 執行的時機點 CurrentPhaseIndex 還是預設值(0),
/// 此時播放會撥到錯誤的時段 BGM。
/// 透過 Task 讓初始播放延後到 ApplySaveData 之後,確保讀到正確的 phase。
/// </summary>
public class Task_InitSceneMusic : SceneReadyTaskBase
{
    [Header("目標 SceneMusicController")]
    [Tooltip("拖入場景中的 SceneMusicController。如果留空,會自動在場景中搜尋。")]
    [SerializeField] private SceneMusicController targetController;

    public override IEnumerator ExecuteTask(string entryID)
    {
        if (targetController == null)
        {
            targetController = FindObjectOfType<SceneMusicController>();
        }

        if (targetController == null)
        {
            Debug.LogWarning($"[Task_InitSceneMusic] 場景中找不到 SceneMusicController,跳過。");
            yield break;
        }

        Debug.Log($"[Task_InitSceneMusic] 觸發 SceneMusicController 初始化");
        yield return targetController.Initialize();
    }
}