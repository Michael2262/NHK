using UnityEngine;

/// <summary>已退役的送禮面板入口。僅保留舊場景引用，請在 Editor 移除元件與面板。</summary>
[AddComponentMenu("")]
public class GiftUI : MonoBehaviour
{
    public static GiftUI Instance { get; private set; }
    public bool IsOpen => false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        Hide();
    }

    private void OnEnable() => Hide();
    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void SetTargetHeroine(string heroineID) { }
    public void ShowForHeroine(string heroineID) => Show();

    public void Show()
    {
        Hide();
        Debug.LogWarning("[GiftUI] 道具送禮功能已移除，請清理場景中的送禮入口與面板。", this);
    }

    public void Hide()
    {
        var group = GetComponent<CanvasGroup>();
        if (group == null) return;
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }
}
