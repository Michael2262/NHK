using UnityEngine;

/// <summary>
/// 分組狀態的 UnityEvent 入口；每次呼叫重新取得目標，狀態切換由 Controller 處理。
/// </summary>
[AddComponentMenu("NHK/Scene Tools/Grouped State Bridge")]
public sealed class GroupedStateBridge : MonoBehaviour
{
    [Header("目標（選填）")]
    [Tooltip("留空時自動尋找已載入場景中唯一啟用的 GroupedStateController；有多個時請指定。")]
    [SerializeField] private GroupedStateController target;

    [Header("預設組名與狀態名")]
    [Tooltip("供無參數入口使用，區分大小寫，不可空白、包含 / 或帶有頭尾空白。")]
    [SerializeField] private string groupName;

    [Tooltip("供 SetConfiguredState 使用，區分大小寫，不可空白、包含 / 或帶有頭尾空白。")]
    [SerializeField] private string stateName;

    /// <summary>切換至 Inspector 設定的組名與狀態名。</summary>
    public void SetConfiguredState()
    {
        var controller = ResolveTarget();
        if (controller != null) controller.SetState(groupName, stateName);
    }

    /// <summary>供 UnityEvent 傳入「組名/狀態名」，例如「房門/上鎖」。</summary>
    public void SetState(string statePath)
    {
        var controller = ResolveTarget();
        if (controller != null) controller.SetState(statePath);
    }

    /// <summary>清除 Inspector 設定分組的目前狀態，執行其 OnExit。</summary>
    public void ClearConfiguredState() => ClearState(groupName);

    /// <summary>清除指定分組的目前狀態，執行其 OnExit。</summary>
    public void ClearState(string groupId)
    {
        var controller = ResolveTarget();
        if (controller != null) controller.ClearState(groupId);
    }

    private GroupedStateController ResolveTarget()
    {
        if (target != null)
        {
            if (IsAvailable(target)) return target;
            Debug.LogWarning("[GroupedStateBridge] 指定目標必須位於已載入場景，且物件與元件皆已啟用。", this);
            return null;
        }

        var controllers = Object.FindObjectsByType<GroupedStateController>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        GroupedStateController result = null;
        int count = 0;
        foreach (var candidate in controllers)
        {
            if (!IsAvailable(candidate)) continue;
            result = candidate;
            count++;
        }

        if (count == 1) return result;
        Debug.LogWarning(count == 0
            ? "[GroupedStateBridge] 找不到可用的 GroupedStateController，請確認已載入場景中有物件與元件皆啟用的 Controller。"
            : "[GroupedStateBridge] 找到 " + count + " 個可用的 GroupedStateController，請在 Target 指定目標，本次不執行。", this);
        return null;
    }

    private static bool IsAvailable(GroupedStateController controller)
    {
        if (controller == null || !controller.isActiveAndEnabled) return false;
        var scene = controller.gameObject.scene;
        return scene.IsValid() && scene.isLoaded;
    }
}
