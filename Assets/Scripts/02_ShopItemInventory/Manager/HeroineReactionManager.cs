using UnityEngine;

/// <summary>已退役；保留舊場景引用，不再播放送禮對話或呼叫 UnityEvent。</summary>
[AddComponentMenu("")]
public class HeroineReactionManager : MonoBehaviour
{
    public static HeroineReactionManager Instance { get; private set; }

    private void Awake() { if (Instance == null) Instance = this; }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void TriggerGiftReaction(string heroineID, string itemID)
    {
        Debug.LogWarning("[HeroineReactionManager] 道具送禮反應已移除，請在 Editor 清理舊元件與事件。", this);
    }
}
