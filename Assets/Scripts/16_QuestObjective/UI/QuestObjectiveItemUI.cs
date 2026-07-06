using TMPro;
using UnityEngine;

/// <summary>
/// 任務目標清單的單一項目（掛在 item prefab 上）。
/// 由 QuestObjectiveListUI 生成並呼叫 Setup，本身不持有任何邏輯。
/// </summary>
public class QuestObjectiveItemUI : MonoBehaviour
{
    [Tooltip("目標文字")]
    [SerializeField] private TextMeshProUGUI label;

    [Tooltip("已完成標記（勾勾圖示等），可留空。顯示已完成清單時開啟。")]
    [SerializeField] private GameObject completedMark;

    public void Setup(string text, bool completed)
    {
        if (label != null) label.text = text;
        if (completedMark != null) completedMark.SetActive(completed);
    }
}
