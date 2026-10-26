using UnityEngine;
using UnityEngine.UI;

/// <summary>舊任務清單已退役；暫留序列化欄位與事件入口，供 Unity Editor 清理。</summary>
public class QuestObjectiveListUI : MonoBehaviour
{
    // 保留既有欄位名稱與型別，不在程式退役階段改動場景或 Prefab 的參照。
    [SerializeField] private Transform contentRoot;
    [SerializeField] private QuestObjectiveItemUI itemPrefab;
    [SerializeField] private Toggle showCompletedToggle;
    [SerializeField] private bool showCompleted = false;

    private void OnEnable()
    {
        Debug.LogWarning("[QuestObjectiveListUI] 舊任務清單已退役，請在 Unity Editor 移除此元件。", this);
        enabled = false;
    }

    /// <summary>保留舊 UnityEvent 入口；不再讀取任務狀態、產生 UI 或訂閱事件。</summary>
    public void SetShowCompleted(bool value)
    {
        showCompleted = value;
    }
}
