// 檔案：HeroineDebugManager.cs (新檔案)
using UnityEngine;

/// <summary>
/// 職責：在遊戲啟動時，自動為所有存在於 GameStatusService 的女主角
/// 創建對應的 HeroineDebugUI 實例。
/// </summary>
public class HeroineDebugManager : MonoBehaviour
{
    [Header("UI Prefab")]
    [Tooltip("請拖曳您製作的『單一女主角顯示UI』的 Prefab")]
    [SerializeField] private GameObject heroineUIPrefab;

    [Header("UI 容器")]
    [Tooltip("所有生成出的 UI Prefab 都會放在這個物件底下")]
    [SerializeField] private Transform contentParent;

    void Start()
    {
        // ★ 核心改造 1：在 Start() 中不再直接建立 UI，而是去「訂閱」全局刷新事件
        if (GameStatusService.Instance != null)
        {
            GameStatusService.Instance.OnGameStatusLoaded += RebuildAllHeroineUI;

            // 為了處理第一次啟動，手動呼叫一次來建立初始 UI
            RebuildAllHeroineUI();
        }
        else
        {
            Debug.LogError("HeroineDebugManager 初始化失敗！GameStatusService 尚未存在。");
        }
    }
    /// <summary>
    /// 當物件被銷毀時，務必取消訂閱。
    /// </summary>
    private void OnDestroy()
    {
        if (GameStatusService.Instance != null)
        {
            GameStatusService.Instance.OnGameStatusLoaded -= RebuildAllHeroineUI;
        }
    }
    /// <summary>
    /// ★ 核心改造 2：創建一個獨立的、可重複呼叫的「重建UI」方法
    /// </summary>
    private void RebuildAllHeroineUI()
    {
        Debug.Log("[HeroineDebugManager] 收到刷新信號，開始重建所有女主角 UI...");

        // 步驟 1：銷毀所有舊的 UI 實例，清除殘留的連結
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 步驟 2：重新遍歷 GameStatusService 中「當前最新」的女主角模型
        foreach (var heroineModel in GameStatusService.Instance.Heroines.Values)
        {
            // 實例化 UI Prefab
            GameObject uiInstance = Instantiate(heroineUIPrefab, contentParent);

            // 獲取 UI 腳本
            HeroineDebugUI debugUI = uiInstance.GetComponent<HeroineDebugUI>();

            // 初始化 UI，將它與「最新的」Model 建立新的連結
            if (debugUI != null)
            {
                debugUI.Initialize(heroineModel);
            }
        }
    }
}
