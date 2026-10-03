using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Flag 操作接口：由呼叫端傳入 Flag 資產，不保存預設 Flag 或事件。
/// 所有狀態與生命週期均交由 GameStatusService 的 ProgressFlagModel 管理。
/// </summary>
public class ProgressFlagBridge_V2 : MonoBehaviour
{
    /// <summary>永久標記：保留於目前存檔，直到手動移除。</summary>
    public void AddPersistentFlag(ProgressFlagDefinition flagDef)
    {
        AddFlag(flagDef, FlagLifetime.Persistent);
    }

    /// <summary>場景標記：切換場景時清除。</summary>
    public void AddSceneFlag(ProgressFlagDefinition flagDef)
    {
        AddFlag(flagDef, FlagLifetime.Scene);
    }

    /// <summary>時段標記：推進下一個 Slot 時清除。</summary>
    public void AddSlotFlag(ProgressFlagDefinition flagDef)
    {
        AddFlag(flagDef, FlagLifetime.UntilNextSlot);
    }

    /// <summary>階段標記：進入下一個 Phase 時清除。</summary>
    public void AddPhaseFlag(ProgressFlagDefinition flagDef)
    {
        AddFlag(flagDef, FlagLifetime.UntilNextPhase);
    }

    /// <summary>每日標記：跨日時清除。</summary>
    public void AddDailyFlag(ProgressFlagDefinition flagDef)
    {
        AddFlag(flagDef, FlagLifetime.UntilNextDay);
    }

    /// <summary>移除所有生命週期中的指定標記，以及同 ID 的數值。</summary>
    public void RemoveFlag(ProgressFlagDefinition flagDef)
    {
        if (!TryGetModel(flagDef, out var model)) return;
        model.RemoveFlag(flagDef.FlagID);
    }

    private void AddFlag(ProgressFlagDefinition flagDef, FlagLifetime lifetime)
    {
        if (!TryGetModel(flagDef, out var model)) return;
        model.AddFlag(flagDef.FlagID, lifetime);
    }

    private bool TryGetModel(ProgressFlagDefinition flagDef, out ProgressFlagModel model)
    {
        model = null;
        if (flagDef == null)
        {
            Debug.LogWarning("[ProgressFlagBridge_V2] 請在呼叫參數中指定 Flag 資產。", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(flagDef.FlagID))
        {
            Debug.LogWarning($"[ProgressFlagBridge_V2] Flag 資產「{flagDef.name}」的 FlagID 為空。", this);
            return false;
        }

        var service = GameStatusService.Instance;
        model = service != null ? service.ProgressFlags : null;
        if (model == null)
        {
            Debug.LogWarning("[ProgressFlagBridge_V2] 進度旗標服務尚未初始化，無法操作 Flag。", this);
            return false;
        }

        return true;
    }
}

#if UNITY_EDITOR
/// <summary>僅在 Editor 顯示使用說明與 Flag 資料夾捷徑。</summary>
[CustomEditor(typeof(ProgressFlagBridge_V2))]
[CanEditMultipleObjects]
public class ProgressFlagBridge_V2Editor : Editor
{
    private const string FlagFolderPath = "Assets/Resources/Progress/Flag";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox(
            "呼叫 AddPersistentFlag、AddSceneFlag、AddSlotFlag、AddPhaseFlag、AddDailyFlag 或 RemoveFlag，並在呼叫參數中拖入 Flag 資產。\n" +
            "此元件不需要預先設定 Flag 或事件。",
            MessageType.Info);

        if (GUILayout.Button("在 Project 開啟 Flag 資料夾"))
        {
            var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(FlagFolderPath);
            if (folder == null || !AssetDatabase.IsValidFolder(FlagFolderPath))
            {
                Debug.LogWarning($"[ProgressFlagBridge_V2] 找不到 Flag 資料夾：{FlagFolderPath}", target);
                return;
            }

            EditorUtility.FocusProjectWindow();
            // 延後到 Inspector 繪製結束，再讓 Project 視窗直接顯示資料夾內容。
            EditorApplication.delayCall += () => OpenFlagFolder(folder);
        }
    }

    private static void OpenFlagFolder(DefaultAsset folder)
    {
        if (folder == null) return;

        // Unity 未公開進入資料夾的 API，透過 ProjectBrowser 的內部方法開啟。
        var browserType = typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser");
        const System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var showFolder = browserType?.GetMethod("ShowFolderContents", flags, null,
            new[] { typeof(int), typeof(bool) }, null);
        var setViewMode = browserType?.GetMethod("SetViewMode", flags);
        if (showFolder == null || setViewMode == null)
        {
            Debug.LogWarning("[ProgressFlagBridge_V2] 此 Unity 版本不支援直接開啟 Project 資料夾。");
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
            Debug.LogWarning($"[ProgressFlagBridge_V2] 無法開啟 Flag 資料夾：{exception.GetBaseException().Message}");
        }
    }
}
#endif
