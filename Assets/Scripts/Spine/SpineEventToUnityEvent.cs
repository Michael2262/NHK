using UnityEngine;
using UnityEngine.Events; // 引用 UnityEvent 必要的命名空間
using Spine.Unity;
using System.Collections.Generic;

// 建立一個可序列化的類別，用來將 Spine Event 名稱與一個 UnityEvent 連結起來
[System.Serializable]
public class SpineUnityEventMapping
{
    // 對應 Spine 中的 Event 名稱
    [Tooltip("在 Spine 中設定的 Event 名稱 (大小寫需完全相符)")]
    public string spineEventName;

    // 當事件觸發時，要執行的 UnityEvent
    [Tooltip("當上方指定的事件被觸發時，要執行哪些函式")]
    public UnityEvent onEventTriggered;
}

public class SpineEventToUnityEvent : MonoBehaviour
{
    [Tooltip("共用的事件 ID 目錄，供 Inspector 選取；不指定時沿用既有手填字串。")]
    public SpineEventDatabase eventDatabase;

    // 在 Inspector 中設定所有的事件連結
    public List<SpineUnityEventMapping> eventMappings;

    [Tooltip("是否顯示 Spine 事件觸發的 Log；關閉時仍會正常執行 UnityEvent。")]
    public bool enableEventLog = false;

    private SkeletonAnimation skeletonAnimation;
    private Dictionary<string, UnityEvent> eventMap;

    void Awake()
    {
        // 將 List 轉換為 Dictionary 以便快速查找，提升效能
        eventMap = new Dictionary<string, UnityEvent>();
        if (eventMappings == null) return;
        foreach (var mapping in eventMappings)
        {
            if (mapping == null || string.IsNullOrWhiteSpace(mapping.spineEventName))
            {
                Debug.LogWarning("[SpineEventToUnityEvent] 映射未填寫事件 ID，已跳過。", this);
                continue;
            }
            if (eventDatabase != null && eventDatabase.Find(mapping.spineEventName) == null)
                Debug.LogWarning($"[SpineEventToUnityEvent] ID「{mapping.spineEventName}」未收錄於共用資料庫，沿用既有映射。", this);
            if (eventMap.ContainsKey(mapping.spineEventName))
                Debug.LogWarning($"[SpineEventToUnityEvent] ID「{mapping.spineEventName}」重複，只使用第一筆映射。", this);
            if (!string.IsNullOrEmpty(mapping.spineEventName) && !eventMap.ContainsKey(mapping.spineEventName))
            {
                eventMap.Add(mapping.spineEventName, mapping.onEventTriggered);
            }
        }
    }

    void Start()
    {
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        if (skeletonAnimation == null)
        {
            Debug.LogError("物件上找不到 SkeletonAnimation 組件！");
            return;
        }

        // 訂閱 Spine 動畫事件
        skeletonAnimation.AnimationState.Event += HandleAnimationEvent;
    }

    void OnDestroy()
    {
        if (skeletonAnimation != null)
        {
            skeletonAnimation.AnimationState.Event -= HandleAnimationEvent;
        }
    }

    // Spine 事件的處理函式
    private void HandleAnimationEvent(Spine.TrackEntry trackEntry, Spine.Event e)
    {
        // 嘗試從字典中尋找對應的 UnityEvent
        if (eventMap.TryGetValue(e.Data.Name, out UnityEvent unityEventToInvoke))
        {
            // 如果找到了，就觸發 (Invoke) 它
            if (enableEventLog)
                Debug.Log($"觸發 Spine Event: '{e.Data.Name}', 執行對應的 UnityEvent。");
            unityEventToInvoke?.Invoke(); // ?. 是一個安全檢查，確保 unityEventToInvoke 不是 null
        }
    }
}
