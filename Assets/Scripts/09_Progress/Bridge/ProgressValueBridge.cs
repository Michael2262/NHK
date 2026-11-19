using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 數值操作接口：由呼叫端傳入 Value 資產，不保存預設目標或事件。
/// 所有數值狀態與變更通知均交由 GameStatusService 的 ProgressFlagModel 管理。
/// </summary>
public class ProgressValueBridge : MonoBehaviour
{
    [Header("UnityEvent 操作參數")]
    [Tooltip("呼叫單參數 SetValue 時，要設定的數值。")]
    [SerializeField] private int valueToSet = 1;

    [Tooltip("呼叫單參數 AddValue 時，要增加的量；負數代表減少。")]
    [SerializeField] private int amountToAdd = 1;

    /// <summary>使用 Inspector 的設定值，適用於 UnityEvent 傳入 Value 資產。</summary>
    public void SetValue(ProgressValueDefinition valueDef)
    {
        SetValue(valueDef, valueToSet);
    }

    /// <summary>設定指定數值，供程式直接傳入資產與整數。</summary>
    public void SetValue(ProgressValueDefinition valueDef, int value)
    {
        if (!TryGetModel(valueDef, out var model)) return;
        model.SetValue(valueDef.FlagID, value);
    }

    /// <summary>使用 Inspector 的增減量，適用於 UnityEvent 傳入 Value 資產。</summary>
    public void AddValue(ProgressValueDefinition valueDef)
    {
        AddValue(valueDef, amountToAdd);
    }

    /// <summary>增加指定數值；傳入負數可減少，供程式直接呼叫。</summary>
    public void AddValue(ProgressValueDefinition valueDef, int amount)
    {
        if (!TryGetModel(valueDef, out var model)) return;
        model.AddValue(valueDef.FlagID, amount);
    }

    /// <summary>將數值設為零，保留數值記錄與同 ID 的布林旗標、隔日預約。</summary>
    public void ClearValue(ProgressValueDefinition valueDef)
    {
        SetValue(valueDef, 0);
    }

    /// <summary>將數值還原為 Value 資產的 DefaultValue。</summary>
    public void ResetToDefault(ProgressValueDefinition valueDef)
    {
        if (!TryGetModel(valueDef, out var model)) return;
        model.SetValue(valueDef.FlagID, valueDef.DefaultValue);
    }

    /// <summary>供程式讀取目前數值；未建立的數值或無效呼叫回傳零。</summary>
    public int GetValue(ProgressValueDefinition valueDef)
    {
        if (!TryGetModel(valueDef, out var model)) return 0;
        return model.GetValue(valueDef.FlagID);
    }

    private bool TryGetModel(ProgressValueDefinition valueDef, out ProgressFlagModel model)
    {
        model = null;
        if (valueDef == null)
        {
            Debug.LogWarning("[ProgressValueBridge] 請在呼叫參數中指定 Value 資產。", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(valueDef.FlagID))
        {
            Debug.LogWarning($"[ProgressValueBridge] Value 資產「{valueDef.name}」的 FlagID 為空。", this);
            return false;
        }

        var service = GameStatusService.Instance;
        model = service != null ? service.ProgressFlags : null;
        if (model == null)
        {
            Debug.LogWarning("[ProgressValueBridge] 進度旗標服務尚未初始化，無法操作 Value。", this);
            return false;
        }

        return true;
    }
}

#if UNITY_EDITOR
/// <summary>僅在 Editor 顯示使用說明與 Value 資料夾捷徑。</summary>
[CustomEditor(typeof(ProgressValueBridge))]
[CanEditMultipleObjects]
public class ProgressValueBridgeEditor : Editor
{
    private const string ValueFolderPath = "Assets/Resources/Progress/Value";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox(
            "在 UnityEvent 選擇單參數的 SetValue、AddValue、ClearValue 或 ResetToDefault，並在呼叫參數中拖入 Value 資產。\n" +
            "SetValue 使用 Value To Set；AddValue 使用 Amount To Add，負數代表減少。這兩個設定由此元件的所有呼叫共用。\n" +
            "ClearValue 設為 0；ResetToDefault 還原資產的 DefaultValue。數值隨目前存檔保存，不會因切場景或跨日自動清除。\n" +
            "程式可呼叫雙參數的 SetValue / AddValue，或使用 GetValue 讀取數值。",
            MessageType.Info);

        if (GUILayout.Button("在 Project 開啟 Value 資料夾"))
        {
            var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(ValueFolderPath);
            if (folder == null || !AssetDatabase.IsValidFolder(ValueFolderPath))
            {
                Debug.LogWarning($"[ProgressValueBridge] 找不到 Value 資料夾：{ValueFolderPath}", target);
                return;
            }

            EditorUtility.FocusProjectWindow();
            // 延後到 Inspector 繪製結束，再讓 Project 視窗直接顯示資料夾內容。
            EditorApplication.delayCall += () => OpenValueFolder(folder);
        }
    }

    private static void OpenValueFolder(DefaultAsset folder)
    {
        if (folder == null) return;

        // 沿用 Flag Bridge 的資料夾捷徑，透過 ProjectBrowser 的內部方法開啟。
        var browserType = typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
        const System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var showFolder = browserType?.GetMethod("ShowFolderContents", flags, null,
            new[] { typeof(int), typeof(bool) }, null);
        var setViewMode = browserType?.GetMethod("SetViewMode", flags);
        if (showFolder == null || setViewMode == null)
        {
            Debug.LogWarning("[ProgressValueBridge] 此 Unity 版本不支援直接開啟 Project 資料夾。");
            return;
        }

        try
        {
            var browser = EditorWindow.GetWindow(browserType);
            var viewModeType = setViewMode.GetParameters()[0].ParameterType;
            setViewMode.Invoke(browser, new[] { System.Enum.Parse(viewModeType, "TwoColumns") });
            showFolder.Invoke(browser, new object[] { folder.GetInstanceID(), true });
            browser.Focus();
            browser.Repaint();
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"[ProgressValueBridge] 無法開啟 Value 資料夾：{exception.GetBaseException().Message}");
        }
    }
}
#endif
