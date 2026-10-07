using UnityEngine;

/// <summary>
/// ToolButtonGroupDisplayControl 的 UnityEvent 入口；每次呼叫時取得目前的場景控制器。
/// 群組導覽、返回歷史、顯示淡入淡出與條件判斷均由控制器處理。
/// </summary>
[AddComponentMenu("NHK/ToolButton/Tool Button Group Bridge")]
public class ToolButtonGroupBridge : MonoBehaviour
{
    [Header("預設組名")]
    [Tooltip("供 ShowConfiguredGroup 使用，區分大小寫。留空或找不到組名時，控制器會警告並保持目前群組。")]
    [SerializeField] private string groupName;

    /// <summary>切換至 Inspector 設定的群組，不自動顯示面板。</summary>
    public void ShowConfiguredGroup() => ShowGroup(groupName);

    /// <summary>切換至指定群組，不自動顯示面板；沿用控制器的返回歷史規則。</summary>
    public void ShowGroup(string name)
    {
        var target = ResolveTarget();
        if (target != null) target.ShowGroup(name);
    }

    /// <summary>依目前群組設定返回；未指定返回組名時使用歷史。</summary>
    public void BackGroup()
    {
        var target = ResolveTarget();
        if (target != null) target.BackGroup();
    }

    /// <summary>依歷史返回上一組；沒有歷史時保持目前群組。</summary>
    public void LastGroup()
    {
        var target = ResolveTarget();
        if (target != null) target.LastGroup();
    }

    /// <summary>返回 Main 群組。</summary>
    public void MainGroup()
    {
        var target = ResolveTarget();
        if (target != null) target.MainGroup();
    }

    /// <summary>淡入顯示面板，保留目前群組與返回歷史。</summary>
    public void Show()
    {
        var target = ResolveTarget();
        if (target != null) target.Show();
    }

    /// <summary>淡出隱藏面板並停用輸入，保留目前群組。</summary>
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

    /// <summary>重新依目前群組、Flag 與語系刷新按鈕。</summary>
    public void Refresh()
    {
        var target = ResolveTarget();
        if (target != null) target.Refresh();
    }

    private ToolButtonGroupDisplayControl ResolveTarget()
    {
        var target = ToolButtonGroupDisplayControl.Instance;
        if (target == null)
            Debug.LogWarning("[ToolButtonGroupBridge] 找不到已初始化的 ToolButtonGroupDisplayControl，請確認場景中的控制器已執行 Awake。", this);
        return target;
    }
}
