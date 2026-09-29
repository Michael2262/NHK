using UnityEngine;

/// <summary>
/// Spine 動畫清單的 UnityEvent 入口。優先使用拖入的播放器，否則在呼叫時依 Controller ID 查找。
/// 群組的軌道、循環、串接及條件轉場均由 SpinePlayByList 管理。
/// </summary>
[AddComponentMenu("Spine/Bridge/Spine List Bridge")]
public class SpineListBridge : MonoBehaviour
{
    [Header("清單目標（引用優先）")]
    [SerializeField] private SpinePlayByList playByList;
    [Tooltip("未指定清單引用時使用。目標 Controller 必須已啟用並註冊此 ID，且同物件上有 SpinePlayByList。")]
    [SerializeField] private string controllerID;

    [Header("預設群組")]
    [SerializeField] private string groupName;

    /// <summary>播放 Inspector 設定的群組。</summary>
    public void PlayConfiguredGroup()
    {
        PlayGroup(groupName);
    }

    /// <summary>UnityEvent 傳入群組名稱。</summary>
    public void PlayGroup(string name)
    {
        if (!ValidateGroupName(name)) return;
        var target = ResolvePlayByList();
        if (target != null) target.PlayGroup(name);
    }

    /// <summary>插播 Inspector 設定的群組。</summary>
    public void PlayConfiguredGroupAndGoBack()
    {
        PlayGroupAndGoBack(groupName);
    }

    /// <summary>
    /// 插播後從原群組開頭重播。插播目標不能有 Loop 或 NextGroup，沿用原播放器的檢查。
    /// </summary>
    public void PlayGroupAndGoBack(string name)
    {
        if (!ValidateGroupName(name)) return;
        var target = ResolvePlayByList();
        if (target != null) target.PlayGroupAndGoBack(name);
    }

    /// <summary>
    /// 停止清單並清除目前群組軌道；自然完成後的殘留軌道沿用原播放器行為，不額外清除。
    /// </summary>
    public void StopList()
    {
        var target = ResolvePlayByList();
        if (target != null) target.StopPlaying();
    }

    private SpinePlayByList ResolvePlayByList()
    {
        if (playByList != null) return playByList;
        if (!string.IsNullOrWhiteSpace(controllerID))
            return SpinePlayByList.GetByControllerID(controllerID);

        Debug.LogWarning($"[{nameof(SpineListBridge)}] 請拖入 SpinePlayByList 或填寫 Controller ID。", this);
        return null;
    }

    private bool ValidateGroupName(string name)
    {
        if (!string.IsNullOrWhiteSpace(name)) return true;
        Debug.LogWarning($"[{nameof(SpineListBridge)}] 尚未指定群組名稱。", this);
        return false;
    }
}
