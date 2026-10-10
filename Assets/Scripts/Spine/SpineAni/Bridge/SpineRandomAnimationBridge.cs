using UnityEngine;

/// <summary>
/// 隨機動畫的 UnityEvent 入口；群組抽選與播放規則交由 SpineRandomAnimationPlayer 處理。
/// </summary>
[AddComponentMenu("Spine/Bridge/Spine Random Animation Bridge")]
public class SpineRandomAnimationBridge : MonoBehaviour
{
    [Header("動畫目標（引用優先）")]
    [Tooltip("指定要使用的隨機動畫播放器。")]
    [SerializeField] private SpineRandomAnimationPlayer player;

    [Tooltip("未指定 Player 時使用。尋找此 Controller ID 所在物件上的 SpineRandomAnimationPlayer；兩者皆留空時尋找 Bridge 同物件上的播放器。")]
    [SerializeField] private string controllerID;

    [Header("播放設定")]
    [Tooltip("填入播放器 Groups 中的群組 ID，區分大小寫；不是 Controller ID 或動畫名稱。")]
    [SerializeField] private string groupID;

    /// <summary>依 Inspector 設定的群組 ID，隨機選取一個動畫播放。</summary>
    public void PlayConfiguredGroup()
    {
        PlayByID(groupID);
    }

    /// <summary>供 UnityEvent 或 PlayMaker Call Method 傳入群組 ID，當次隨機播放一個動畫。</summary>
    public void PlayByID(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning("[SpineRandomAnimationBridge] 請填寫動畫群組 ID。", this);
            return;
        }

        var target = ResolvePlayer();
        if (target != null) target.PlayByID(id);
    }

    private SpineRandomAnimationPlayer ResolvePlayer()
    {
        if (player != null) return player;

        SpineRandomAnimationPlayer target;
        if (!string.IsNullOrWhiteSpace(controllerID))
        {
            // 每次呼叫時解析，避免場景切換後保留舊目標。
            var controller = SpineAnimationController.GetByID(controllerID);
            if (controller == null) return null;
            target = controller.GetComponent<SpineRandomAnimationPlayer>();
            if (target == null)
                Debug.LogWarning($"[SpineRandomAnimationBridge] Controller ID '{controllerID}' 所在物件沒有 SpineRandomAnimationPlayer。", this);
        }
        else
        {
            target = GetComponent<SpineRandomAnimationPlayer>();
            if (target == null)
                Debug.LogWarning("[SpineRandomAnimationBridge] 請指定 Player、填寫 Controller ID，或在同物件掛上 SpineRandomAnimationPlayer。", this);
        }

        return target;
    }
}
