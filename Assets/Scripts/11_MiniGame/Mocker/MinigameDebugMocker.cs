using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 職責：在測試環境下手動偽造數據並注入 Adapter。
/// 已完全移除 UnityEngine.Input 相關代碼，避免與 New Input System 衝突。
/// </summary>
public class MinigameDebugMocker : MonoBehaviour
{
    [Header("測試設定")]
    public bool enableMock = true;
    public bool autoMockOnStart = false; // 新增：是否在啟動時自動注入
    public string mockOriginScene = "MainMapScene"; // 新增：預計跳轉回去的場景名
    public PlayMakerMinigameAdapter_Multi2 adapter;

    [Header("偽造數據")]
    public List<string> mockHeroineIDs = new List<string> { "Heroine_Sister", "Heroine_Cousin" };
    public HeroineStatusConfig mockConfig;

    private bool _isInitialized = false;

    // --- 新增：啟動時的邏輯 ---
    private void Start()
    {
        if (enableMock && autoMockOnStart)
        {
            // 在 1 秒後執行 PerformMock
            Invoke(nameof(PerformMock), 1.0f);
        }
    }
    /// <summary>
    /// 執行手動注入邏輯
    /// </summary>
    [ContextMenu("Execute Mock Now")]
    public void PerformMock()
    {
        // --- 0. 基礎安全檢查 ---
        if (_isInitialized) return;

        if (MinigameManager.Instance == null)
        {
            Debug.LogError("[Mocker] 找不到 MinigameManager 實例！請確認場景中有 MGM。");
            return;
        }

        if (adapter == null) adapter = GetComponent<PlayMakerMinigameAdapter_Multi2>();
        if (adapter == null || mockConfig == null)
        {
            Debug.LogError("[Mocker] 缺少必要的 Adapter 或 MockConfig 設定。");
            return;
        }

        // --- 1. 注入回歸場景  ---
        MinigameManager.Instance.DebugSetOriginScene(mockOriginScene);

        // --- 2. 偽造女主角 Model 數據 ---
        List<HeroineStatusModel> mockHeroines = new List<HeroineStatusModel>();
        foreach (string id in mockHeroineIDs)
        {
            // 建立原始數據結構 (Stat)
            var stat = new HeroineStat
            {
                ID = id,
                Name = "測試對象_" + id
            };
            // 封裝成邏輯模型 (Model)
            mockHeroines.Add(new HeroineStatusModel(stat, mockConfig));
        }

        // --- 3. 打包 Context (模擬 MGM 的打包行為) ---
        // 測試環境下，不需要的組件 (如 TimeManager, Scenario) 可以傳 null
        var context = new MinigameContext(
            protagonist: null,
            activeHeroines: mockHeroines,
            skills: null,
            time: null,
            riskAgents: null,
            statusEffect: null,
            progressFlags: null,
            scenario: null,
            timeManager: null
        );

        // --- 4. 啟動小遊戲生命週期 ---
        // 這一步模擬了 MGM 調用 IMinigameController 的過程
        Debug.Log("<color=cyan>[Mocker] 正在初始化 Adapter...</color>");
        adapter.Initialize(context);

        Debug.Log("<color=cyan>[Mocker] 正在啟動小遊戲...</color>");
        adapter.StartGame();

        _isInitialized = true;
        Debug.Log($"<color=green>[Mocker] 注入成功！回歸場景設定為: {mockOriginScene}</color>");
    }

    /// <summary>
    /// 在畫面上繪製除錯按鈕
    /// </summary>
    void OnGUI()
    {
        if (enableMock && !_isInitialized)
        {
            // 設定一個較大的按鈕方便點擊
            if (GUI.Button(new Rect(20, 20, 250, 80), "【測試】注入 Mock 數據"))
            {
                PerformMock();
            }
        }
    }
}