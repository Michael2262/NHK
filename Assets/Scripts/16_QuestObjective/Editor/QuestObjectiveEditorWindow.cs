#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 任務目標管理視窗。
/// 選單位置：Tools → Progress → Quest Objective Manager
///
/// 功能：
/// ─── 編輯模式（Edit Mode） ───
///   • 掃描並列出專案內所有 QuestObjectiveDefinition
///   • 表格內直接編輯 TextTableKey / SortOrder / MirrorFlagOnComplete / 描述 / 檔名(= ObjectiveID)
///   • 關鍵字篩選
///   • 一鍵建立新目標到 Resources/Progress/Objective（Model 只會從這裡載入）
///   • 刪除目標（有確認對話框）
///
/// ─── Runtime 模式（Play Mode） ───
///   • 即時監看所有目標的三態（未顯示 / 已顯示 / 已完成）
///   • 手動 Reveal / Complete / Hide，方便測試 UI 與劇情
///
/// 放在 Editor/ 資料夾，不進 build。
/// </summary>
public class QuestObjectiveEditorWindow : EditorWindow
{
    // Model 讀取路徑固定為 Resources/Progress/Objective，新目標一律建在這裡
    private const string DEFAULT_FOLDER = "Assets/Resources/Progress/Objective";

    // ───── Tab ─────
    private enum Tab { Edit, Runtime }
    private Tab _tab = Tab.Edit;

    // ───── 資料（編輯模式） ─────
    private List<QuestObjectiveDefinition> _allDefs = new List<QuestObjectiveDefinition>();
    private Dictionary<QuestObjectiveDefinition, bool> _foldouts = new Dictionary<QuestObjectiveDefinition, bool>();
    private Vector2 _scrollEdit;
    private string _searchText = "";
    private string _newObjName = "";

    // ───── 資料（Runtime 模式） ─────
    private Vector2 _scrollRuntime;
    private string _runtimeSearch = "";

    // 欄位寬度（編輯模式表格）
    private const float COL_FOLDOUT = 16f;
    private const float COL_NAME = 200f;
    private const float COL_TEXTKEY = 220f;
    private const float COL_SORT = 50f;
    private const float COL_MIRROR = 60f;
    private const float COL_ACTION = 110f;

    [MenuItem("Tools/Progress/Quest Objective Manager")]
    public static void ShowWindow()
    {
        var win = GetWindow<QuestObjectiveEditorWindow>("Quest Objectives");
        win.minSize = new Vector2(760, 400);
    }

    private void OnEnable() => RefreshDefList();

    private void OnFocus()
    {
        if (_tab == Tab.Edit) RefreshDefList();
    }

    // Play Mode 時讓 Runtime 頁持續刷新
    private void OnInspectorUpdate()
    {
        if (_tab == Tab.Runtime && Application.isPlaying) Repaint();
    }

    // ==========================================================
    // 掃描所有 Definition
    // ==========================================================
    private void RefreshDefList()
    {
        _allDefs.Clear();
        var guids = AssetDatabase.FindAssets("t:QuestObjectiveDefinition");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<QuestObjectiveDefinition>(path);
            if (asset != null) _allDefs.Add(asset);
        }
        _allDefs = _allDefs
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.name, System.StringComparer.Ordinal)
            .ToList();
    }

    // ==========================================================
    // GUI 入口
    // ==========================================================
    private void OnGUI()
    {
        EditorGUILayout.Space(4);
        _tab = (Tab)GUILayout.Toolbar((int)_tab, new[] { "編輯 (Edit)", "監看 (Runtime)" }, GUILayout.Height(24));
        EditorGUILayout.Space(4);

        if (_tab == Tab.Edit) DrawEditTab();
        else DrawRuntimeTab();
    }

    // ==========================================================
    // 編輯模式
    // ==========================================================
    private void DrawEditTab()
    {
        // ── 建立新目標 ──
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("新目標 ID：", GUILayout.Width(70));
            _newObjName = EditorGUILayout.TextField(_newObjName);

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_newObjName)))
            {
                if (GUILayout.Button("建立", GUILayout.Width(60)))
                    CreateNewObjective(_newObjName.Trim());
            }
        }
        EditorGUILayout.HelpBox($"新目標會建立在 {DEFAULT_FOLDER}（Model 只從此路徑載入）。ID = 檔名，建議用 Obj_ 前綴。", MessageType.None);

        // ── 篩選列 ──
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("搜尋：", GUILayout.Width(40));
            _searchText = EditorGUILayout.TextField(_searchText);
            if (GUILayout.Button("重新掃描", GUILayout.Width(80))) RefreshDefList();
        }

        // ── 表頭 ──
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Space(COL_FOLDOUT);
            EditorGUILayout.LabelField("ID (檔名)", EditorStyles.miniBoldLabel, GUILayout.Width(COL_NAME));
            EditorGUILayout.LabelField("TextTable Key", EditorStyles.miniBoldLabel, GUILayout.Width(COL_TEXTKEY));
            EditorGUILayout.LabelField("排序", EditorStyles.miniBoldLabel, GUILayout.Width(COL_SORT));
            EditorGUILayout.LabelField("映射Flag", EditorStyles.miniBoldLabel, GUILayout.Width(COL_MIRROR));
            EditorGUILayout.LabelField("操作", EditorStyles.miniBoldLabel, GUILayout.Width(COL_ACTION));
        }

        // ── 清單 ──
        _scrollEdit = EditorGUILayout.BeginScrollView(_scrollEdit);

        var filtered = _allDefs.Where(d => d != null &&
            (string.IsNullOrEmpty(_searchText) ||
             d.name.IndexOf(_searchText, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             (d.TextTableKey ?? "").IndexOf(_searchText, System.StringComparison.OrdinalIgnoreCase) >= 0)).ToList();

        if (filtered.Count == 0)
            EditorGUILayout.HelpBox("沒有符合的 QuestObjectiveDefinition。", MessageType.Info);

        QuestObjectiveDefinition toDelete = null;

        foreach (var def in filtered)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (!_foldouts.ContainsKey(def)) _foldouts[def] = false;
                _foldouts[def] = EditorGUILayout.Toggle(_foldouts[def], EditorStyles.foldout, GUILayout.Width(COL_FOLDOUT));

                // ID = 檔名，用 DelayedTextField 改名（避免打字中途觸發 Rename）
                string newName = EditorGUILayout.DelayedTextField(def.name, GUILayout.Width(COL_NAME));
                if (newName != def.name && !string.IsNullOrWhiteSpace(newName))
                {
                    string error = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(def), newName.Trim());
                    if (!string.IsNullOrEmpty(error))
                        EditorUtility.DisplayDialog("改名失敗", error, "OK");
                }

                EditorGUI.BeginChangeCheck();
                string newKey = EditorGUILayout.TextField(def.TextTableKey, GUILayout.Width(COL_TEXTKEY));
                int newSort = EditorGUILayout.IntField(def.SortOrder, GUILayout.Width(COL_SORT));
                bool newMirror = EditorGUILayout.Toggle(def.MirrorFlagOnComplete, GUILayout.Width(COL_MIRROR));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(def, "Edit Quest Objective");
                    def.TextTableKey = newKey;
                    def.SortOrder = newSort;
                    def.MirrorFlagOnComplete = newMirror;
                    EditorUtility.SetDirty(def);
                }

                if (GUILayout.Button("選取", GUILayout.Width(50)))
                {
                    Selection.activeObject = def;
                    EditorGUIUtility.PingObject(def);
                }
                if (GUILayout.Button("刪除", GUILayout.Width(50)))
                    toDelete = def;
            }

            // 展開列：描述 + 映射 Flag ID 提示
            if (_foldouts[def])
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUI.BeginChangeCheck();
                    string newDesc = EditorGUILayout.TextArea(def.Description ?? "", GUILayout.MinHeight(36));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(def, "Edit Quest Objective Description");
                        def.Description = newDesc;
                        EditorUtility.SetDirty(def);
                    }

                    if (def.MirrorFlagOnComplete)
                        EditorGUILayout.LabelField($"完成時映射 Persistent Flag：{QuestObjectiveModel.MIRROR_FLAG_PREFIX}{def.ObjectiveID}", EditorStyles.miniLabel);

                    string path = AssetDatabase.GetAssetPath(def);
                    EditorGUILayout.LabelField(path, EditorStyles.miniLabel);
                    if (!path.Replace('\\', '/').StartsWith(DEFAULT_FOLDER))
                        EditorGUILayout.HelpBox($"此 asset 不在 {DEFAULT_FOLDER} 之下，Model 不會載入它！請搬移到正確資料夾。", MessageType.Warning);
                }
            }
        }

        EditorGUILayout.EndScrollView();

        // 刪除（在迭代結束後執行，避免 layout 錯誤）
        if (toDelete != null)
        {
            if (EditorUtility.DisplayDialog("刪除任務目標",
                $"確定要刪除「{toDelete.name}」嗎？\n\n注意：已引用此目標的對話腳本 / FSM / 存檔不會被連動修改。",
                "刪除", "取消"))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(toDelete));
                AssetDatabase.SaveAssets();
                RefreshDefList();
            }
        }
    }

    private void CreateNewObjective(string objName)
    {
        if (!Directory.Exists(DEFAULT_FOLDER))
        {
            Directory.CreateDirectory(DEFAULT_FOLDER);
            AssetDatabase.Refresh();
        }

        string path = $"{DEFAULT_FOLDER}/{objName}.asset";
        if (AssetDatabase.LoadAssetAtPath<QuestObjectiveDefinition>(path) != null)
        {
            EditorUtility.DisplayDialog("建立失敗", $"已存在同名目標：{objName}", "OK");
            return;
        }

        var asset = ScriptableObject.CreateInstance<QuestObjectiveDefinition>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        _newObjName = "";
        RefreshDefList();
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }

    // ==========================================================
    // Runtime 模式
    // ==========================================================
    private void DrawRuntimeTab()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("進入 Play Mode 後可即時監看與操作任務目標狀態。", MessageType.Info);
            return;
        }

        var service = GameStatusService.Instance;
        if (service == null || service.QuestObjectives == null)
        {
            EditorGUILayout.HelpBox("找不到 GameStatusService.Instance，請確認場景中有掛載。", MessageType.Warning);
            return;
        }

        var model = service.QuestObjectives;

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("搜尋：", GUILayout.Width(40));
            _runtimeSearch = EditorGUILayout.TextField(_runtimeSearch);
        }

        _scrollRuntime = EditorGUILayout.BeginScrollView(_scrollRuntime);

        DrawRuntimeGroup(model, QuestObjectiveState.Revealed, "已顯示未完成 (Revealed)");
        DrawRuntimeGroup(model, QuestObjectiveState.Completed, "已完成 (Completed)");
        DrawRuntimeGroup(model, QuestObjectiveState.Hidden, "未顯示 (Hidden)");

        EditorGUILayout.EndScrollView();
    }

    private void DrawRuntimeGroup(QuestObjectiveModel model, QuestObjectiveState state, string title)
    {
        var defs = model.GetObjectives(state)
            .Where(d => string.IsNullOrEmpty(_runtimeSearch) ||
                        d.name.IndexOf(_runtimeSearch, System.StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();

        EditorGUILayout.LabelField($"{title} — {defs.Count} 筆", EditorStyles.boldLabel);

        foreach (var def in defs)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(def.ObjectiveID, GUILayout.Width(220));
                EditorGUILayout.LabelField(def.TextTableKey ?? "", EditorStyles.miniLabel);

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(state == QuestObjectiveState.Revealed))
                    if (GUILayout.Button("Reveal", GUILayout.Width(60))) model.Reveal(def.ObjectiveID);

                using (new EditorGUI.DisabledScope(state == QuestObjectiveState.Completed))
                    if (GUILayout.Button("Complete", GUILayout.Width(70))) model.Complete(def.ObjectiveID);

                using (new EditorGUI.DisabledScope(state == QuestObjectiveState.Hidden))
                    if (GUILayout.Button("Hide", GUILayout.Width(50))) model.Hide(def.ObjectiveID);
            }
        }

        EditorGUILayout.Space(6);
    }
}
#endif
