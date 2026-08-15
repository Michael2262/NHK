using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

// 這個腳本放在每個場景中 (例如跟 Main Camera 放在一起)
public class SceneCursorSettings : MonoBehaviour
{
    [Header("此場景的預設鼠標")]
    [Tooltip("只換圖案；hotspot（對位點/縮放軸心）沿用 GlobalCursorManager 的 default 設定。")]
    public Texture2D sceneNormalTexture;
    public Texture2D sceneClickTexture;

    [Header("此場景的 Raycaster (可選)")]
    [Tooltip("如果留空，會自動嘗試尋找 Camera.main 上的 Physics2DRaycaster")]
    public Physics2DRaycaster scenePhysicsRaycaster;

    [Header("此場景的 UI System (可選)")]
    [Tooltip("如果留空，會自動嘗試尋找場景中的 EventSystem")]
    public EventSystem sceneEventSystem; // 新增欄位

    void Start()
    {
        StartCoroutine(InitializeSceneSettings());
    }

    private IEnumerator InitializeSceneSettings()
    {
        // 持續等待，直到 GlobalCursorManager.Instance 不是 null
        while (GlobalCursorManager.Instance == null)
        {
            yield return null;
        }

        // --- 1. 設定預設鼠標（只換圖，hotspot 沿用 GlobalCursorManager 既有設定）---
        GlobalCursorManager.Instance.SetDefaultCursors(
            sceneNormalTexture,
            sceneClickTexture
        );

        // --- 2. 註冊 Physics Raycaster (Collider) ---
        if (scenePhysicsRaycaster == null)
        {
            if (Camera.main != null)
            {
                scenePhysicsRaycaster = Camera.main.GetComponent<Physics2DRaycaster>();
            }
        }

        if (scenePhysicsRaycaster != null)
        {
            GlobalCursorManager.Instance.RegisterPhysicsRaycaster(scenePhysicsRaycaster);
        }
        else
        {
            Debug.LogError("SceneCursorSettings: 在此場景中找不到 Physics2DRaycaster！");
        }

        // --- 3. 新增：註冊 Event System (UI) ---
        if (sceneEventSystem == null)
        {
            // 嘗試抓取當前場景的 EventSystem (通常場景裡只有一個)
            sceneEventSystem = EventSystem.current;
        }

        if (sceneEventSystem != null)
        {
            GlobalCursorManager.Instance.RegisterEventSystem(sceneEventSystem);
        }
        else
        {
            // 如果場景完全沒有 UI，這也可能是正常的，用 Warning 即可
            // Debug.LogWarning("SceneCursorSettings: 在此場景中找不到 EventSystem，無法控制 UI 開關。");
        }
    }

    /// <summary>
    /// (橋接函式) 呼叫全局管理器，禁用 2D Collider 點擊。
    /// </summary>
    [ContextMenu("手動禁用 2D Collider 點擊")]
    public void DisableSceneColliderClicks()
    {
        if (GlobalCursorManager.Instance != null)
        {
            GlobalCursorManager.Instance.DisableColliderClicks();
        }
    }

    /// <summary>
    /// (橋接函式) 呼叫全局管理器，重新啟用 2D Collider 點擊。
    /// </summary>
    [ContextMenu("手動啟用 2D Collider 點擊")]
    public void EnableSceneColliderClicks()
    {
        if (GlobalCursorManager.Instance != null)
        {
            GlobalCursorManager.Instance.EnableColliderClicks();
        }
    }

    // --- 新增 UI 控制的 ContextMenu ---

    [ContextMenu("手動禁用 UI 點擊")]
    public void DisableSceneUIClicks()
    {
        if (GlobalCursorManager.Instance != null)
        {
            GlobalCursorManager.Instance.DisableUIClicks();
        }
    }

    [ContextMenu("手動啟用 UI 點擊")]
    public void EnableSceneUIClicks()
    {
        if (GlobalCursorManager.Instance != null)
        {
            GlobalCursorManager.Instance.EnableUIClicks();
        }
    }
}