using UnityEngine;

/// <summary>
/// 全局遊戲數據管理器，以單例模式常駐於遊戲中。
/// 負責存放需要跨場景傳遞的臨時狀態或全局數據。
/// </summary>
[DefaultExecutionOrder(-900)] // 確保它比 SceneController 更早 Awake
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    /// <summary>
    /// 紀錄上一個場景是從哪個入口點進入下一個場景的。
    /// </summary>
    public string SceneEntryID { get; private set; }

    // --- Bootstrap 與單例初始化 ---
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoBootstrap()
    {
        if (Instance == null)
        {
            var go = new GameObject("GameDataManager_Auto");
            go.AddComponent<GameDataManager>();
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 設定下一個場景的入口ID。通常由 SceneChangeButton 在切換場景前呼叫。
    /// </summary>
    /// <param name="entryID">入口的唯一識別符，例如 "FromWorldMap" 或 "PlayerDied"。</param>
    public void SetNextSceneEntry(string entryID)
    {
        Debug.Log($"[GameDataManager] 下一個場景入口被設定為: {entryID}");
        SceneEntryID = entryID;
    }
}