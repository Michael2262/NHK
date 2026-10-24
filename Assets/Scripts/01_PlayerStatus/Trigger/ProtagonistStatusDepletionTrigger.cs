using UnityEngine;

/// <summary>舊四維門檻觸發器已退役；保留元件型別，請在 Editor 移除舊掛載。</summary>
[AddComponentMenu("")]
public class ProtagonistStatusDepletionTrigger : MonoBehaviour
{
    private void OnEnable()
    {
        Debug.LogWarning("[ProtagonistStatusDepletionTrigger] 舊四維已退役，請移除這個元件。", this);
        enabled = false;
    }
}
