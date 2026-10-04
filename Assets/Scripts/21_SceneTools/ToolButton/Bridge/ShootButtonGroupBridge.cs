using UnityEngine;

/// <summary>
/// ShootButtonGroup 的 UnityEvent 入口；每次呼叫時取得目前的場景控制器。
/// 組名切換、顯示淡入淡出與 Semen 滿值判斷均由 ShootButtonGroup 處理。
/// </summary>
[AddComponentMenu("NHK/ToolButton/Shoot Button Group Bridge")]
public class ShootButtonGroupBridge : MonoBehaviour
{
    [Header("預設組名")]
    [Tooltip("供 SetConfiguredID 使用，區分大小寫。留空會清除目前組名。")]
    [SerializeField] private string groupID;

    /// <summary>使用 Inspector 設定的組名，不自動顯示操作列。</summary>
    public void SetConfiguredID() => SetID(groupID);

    /// <summary>切換組名，不自動顯示；空白或不存在的組名沿用控制器的清除行為。</summary>
    public void SetID(string id)
    {
        var target = ResolveTarget();
        if (target != null) target.SetID(id);
    }

    /// <summary>清除目前組名，讓按鈕無法觸發組內事件。</summary>
    public void RemoveID()
    {
        var target = ResolveTarget();
        if (target != null) target.RemoveID();
    }

    /// <summary>依控制器的淡入設定顯示操作列。</summary>
    public void Show()
    {
        var target = ResolveTarget();
        if (target != null) target.Show();
    }

    /// <summary>依控制器的淡出設定隱藏操作列，保留目前組名。</summary>
    public void Hide()
    {
        var target = ResolveTarget();
        if (target != null) target.Hide();
    }

    /// <summary>提供 Toggle 等 UnityEvent 的 bool 入口。</summary>
    public void SetVisible(bool visible)
    {
        if (visible) Show(); else Hide();
    }

    /// <summary>重新依目前組名與 Semen 滿值判斷按鈕是否可點。</summary>
    public void Refresh()
    {
        var target = ResolveTarget();
        if (target != null) target.Refresh();
    }

    private ShootButtonGroup ResolveTarget()
    {
        var target = ShootButtonGroup.Instance;
        if (target == null)
            Debug.LogWarning("[ShootButtonGroupBridge] 找不到已初始化的 ShootButtonGroup，請確認場景中的控制器已執行 Awake。", this);
        return target;
    }
}
