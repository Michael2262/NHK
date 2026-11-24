using UnityEngine;

/// <summary>已退役；保留舊 UnityEvent 入口以提示場景清理，不再開啟送禮面板。</summary>
[AddComponentMenu("")]
public class GiftUIOpener : MonoBehaviour
{
    public void Open()
    {
        Debug.LogWarning("[GiftUIOpener] 道具送禮功能已移除，請在 Editor 移除按鈕事件與此元件。", this);
    }

    public void OpenFor(string heroineID) => Open();
}
