using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemGuardian : MonoBehaviour
{
    private static EventSystemGuardian _instance;

    void Awake()
    {
        // 1. 確保自己是唯一的跨場景實體
        if (_instance == null)
        {
            _instance = this;
            transform.SetParent(null); // 確保它沒有父物件，避免跟著別人被卸載
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 如果這是一個重複產生的（通常是 False 那個），直接自殺
            Destroy(gameObject);
            return;
        }
    }

    void Update()
    {
        // 2. 主動出擊：每幀檢查是否有「冒名頂替者」
        // 這能解決您說的「瞬間補上」問題，只要它一出現，下一幀就會被清理
        var allSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (allSystems.Length > 1)
        {
            foreach (var es in allSystems)
            {
                // 如果這個系統不是我，且它不是 DontDestroyOnLoad，就殺掉它
                if (es.gameObject != this.gameObject && es.gameObject.scene.name != "DontDestroyOnLoad")
                {
                    Debug.Log($"<color=red>[Guardian]</color> 偵測到瞬間補位的 EventSystem ({es.gameObject.name})，已強制移除。");
                    Destroy(es.gameObject);
                }
            }
        }
    }
}