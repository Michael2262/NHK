#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Progress 系統總覽視窗。
/// 選單位置：Tools → Progress → Progress Inspector
///
/// 功能：
/// ─── 編輯模式（Edit Mode） ───
///   • 掃描並列出專案內所有 ProgressFlagDefinition / ProgressValueDefinition
///   • 表格內直接編輯 DefaultValue / description / 檔名(= FlagID)
///   • 篩選（類型、關鍵字、資料夾）
///   • 一鍵建立新 Flag / Value 到指定資料夾
///   • 一鍵查找引用：點擊後列出所有 asset 引用 + 代碼字串引用
///   • 孤兒檢測：找出完全沒人引用的 Definition
///
/// ─── Runtime 模式（Play Mode） ───
///   • 即時監看所有 Flag 的啟用狀態（依 Lifetime 分組）
///   • 即時監看所有 Variable 的當前值
///   • 手動 Add / Remove Flag（可指定 Lifetime）
///   • 手動改 Value
///   • 一鍵觸發 Clear Scene/Slot/Phase/Day Flags
///   • Dump 當前狀態到 Console
///
/// 放在 Editor/ 資料夾，不進 build。
/// </summary>
public class ProgressInspectorWindow : EditorWindow
{
    // ───── Tab ─────
    private enum Tab { Edit, Runtime }
    private Tab _tab = Tab.Edit;

    // ───── 資料（編輯模式） ─────
    private List<ProgressBaseDefinition> _allDefs = new List<ProgressBaseDefinition>();
    private Dictionary<ProgressBaseDefinition, bool> _foldouts = new Dictionary<ProgressBaseDefinition, bool>();
    private Vector2 _scrollEdit;

    // 篩選
    private enum TypeFilter { All, FlagOnly, ValueOnly }
    private TypeFilter _typeFilter = TypeFilter.All;
    private string _searchText = "";

    // 排序
    private enum SortMode { ByName, ByType, ByFolder, ByFeature }
    private SortMode _sortMode = SortMode.ByType;

    // 功能篩選（依檔名第二段，例如 Scenario / Notice / Event / Conditional）
    // 空字串 = 不篩選
    private string _featureFilter = "";
    private const string FEATURE_FILTER_ALL = "(全部)";
    private const string FEATURE_UNCATEGORIZED = "(未分類)";

    // 新增 Definition
    private string _newDefFolder = "Assets/Resources/Progress";

    // 引用查找快取（按需啟動，避免每次重繪都掃）
    private Dictionary<ProgressBaseDefinition, ReferenceResult> _refCache
        = new Dictionary<ProgressBaseDefinition, ReferenceResult>();

    // 孤兒檢測結果
    private HashSet<ProgressBaseDefinition> _orphans = null; // null = 尚未檢測
    private bool _orphanCheckRunning = false;

    // ───── 資料（Runtime 模式） ─────
    private Vector2 _scrollRuntime;
    private string _runtimeSearch = "";
    private FlagLifetime _newFlagLifetime = FlagLifetime.Scene;
    private string _runtimeNewFlagName = "";
    private string _runtimeNewVarName = "";
    private int _runtimeNewVarValue = 0;

    // 欄位寬度（編輯模式表格）
    private const float COL_FOLDOUT = 16f;
    private const float COL_TYPE = 55f;
    private const float COL_NAME = 220f;
    private const float COL_DEFAULT = 80f;
    private const float COL_FOLDER = 180f;
    private const float COL_ACTION = 180f;

    [MenuItem("Tools/Progress/Progress Inspector")]
    public static void ShowWindow()
    {
        var win = GetWindow<ProgressInspectorWindow>("Progress Inspector");
        win.minSize = new Vector2(920, 420);
    }

    private void OnEnable()
    {
        RefreshDefList();
    }

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
    // 核心：掃描所有 Definition
    // ==========================================================
    private void RefreshDefList()
    {
        _allDefs.Clear();
        var guids = AssetDatabase.FindAssets("t:ProgressBaseDefinition");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<ProgressBaseDefinition>(path);
            if (asset != null) _allDefs.Add(asset);
        }
        ApplySort();
        // 定義列表變動後，引用快取和孤兒結果都作廢
        _refCache.Clear();
        _orphans = null;
    }

    private void ApplySort()
    {
        switch (_sortMode)
        {
            case SortMode.ByName:
                _allDefs = _allDefs.OrderBy(d => d.name).ToList();
                break;
            case SortMode.ByFolder:
                _allDefs = _allDefs
                    .OrderBy(d => Path.GetDirectoryName(AssetDatabase.GetAssetPath(d)) ?? "")
                    .ThenBy(d => d.name)
                    .ToList();
                break;
            case SortMode.ByFeature:
                // Flag/Value 先分開 → 功能字母序 → 名字
                // 這樣 Flag_Scenario_* 聚一起，Value_Scenario_* 另外聚一起
                _allDefs = _allDefs
                    .OrderBy(d => d is ProgressFlagDefinition ? 0 : 1)
                    .ThenBy(d => GetFeatureName(d), System.StringComparer.OrdinalIgnoreCase)
                    .ThenBy(d => d.name)
                    .ToList();
                break;
            case SortMode.ByType:
            default:
                // Flag 先 Value 後，同類型按名字
                _allDefs = _allDefs
                    .OrderBy(d => d is ProgressFlagDefinition ? 0 : 1)
                    .ThenBy(d => d.name)
                    .ToList();
                break;
        }
    }

    /// <summary>
    /// 從命名中抽出「功能」分類。
    /// 規則：用 '_' 分割後取第二段。
    ///   Flag_Scenario_Mom_LivingRoom → "Scenario"
    ///   Value_Conditional_Talk1     → "Conditional"
    ///   Flag_Event                  → "Event"    (剛好兩段)
    ///   Flag                        → "(未分類)" (只有一段)
    ///   MyWeirdName                 → "(未分類)" (沒有底線)
    /// </summary>
    private static string GetFeatureName(ProgressBaseDefinition def)
    {
        if (def == null || string.IsNullOrEmpty(def.name)) return FEATURE_UNCATEGORIZED;
        var parts = def.name.Split('_');
        if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1])) return FEATURE_UNCATEGORIZED;
        return parts[1];
    }

    /// <summary>
    /// 掃描目前所有 Def 的功能，回傳去重後的排序結果（供篩選下拉用）。
    /// 第一個元素永遠是 FEATURE_FILTER_ALL。
    /// </summary>
    private List<string> GetAllFeatureNames()
    {
        var features = _allDefs
            .Where(d => d != null)
            .Select(GetFeatureName)
            .Distinct(System.StringComparer.OrdinalIgnoreCase)
            .OrderBy(f => f, System.StringComparer.OrdinalIgnoreCase)
            .ToList();

        features.Insert(0, FEATURE_FILTER_ALL);
        return features;
    }

    private IEnumerable<ProgressBaseDefinition> GetFilteredDefs()
    {
        foreach (var def in _allDefs)
        {
            if (def == null) continue;

            // 類型篩選
            if (_typeFilter == TypeFilter.FlagOnly && !(def is ProgressFlagDefinition)) continue;
            if (_typeFilter == TypeFilter.ValueOnly && !(def is ProgressValueDefinition)) continue;

            // 功能篩選（空字串 = 不篩）
            if (!string.IsNullOrEmpty(_featureFilter) && _featureFilter != FEATURE_FILTER_ALL)
            {
                var feature = GetFeatureName(def);
                if (!string.Equals(feature, _featureFilter, System.StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            // 關鍵字篩選（比對名字）
            if (!string.IsNullOrEmpty(_searchText) &&
                def.name.IndexOf(_searchText, System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            yield return def;
        }
    }

    // ==========================================================
    // 繪製主介面
    // ==========================================================
    private void OnGUI()
    {
        DrawTabBar();
        EditorGUILayout.Space(4);

        switch (_tab)
        {
            case Tab.Edit: DrawEditTab(); break;
            case Tab.Runtime: DrawRuntimeTab(); break;
        }
    }

    private void DrawTabBar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Toggle(_tab == Tab.Edit, "編輯 Definitions", EditorStyles.toolbarButton, GUILayout.Width(140)))
                _tab = Tab.Edit;
            if (GUILayout.Toggle(_tab == Tab.Runtime, "Runtime 監看", EditorStyles.toolbarButton, GUILayout.Width(140)))
                _tab = Tab.Runtime;

            GUILayout.FlexibleSpace();

            if (Application.isPlaying)
            {
                GUI.color = new Color(0.6f, 1f, 0.6f);
                GUILayout.Label("● Play Mode", EditorStyles.toolbarButton, GUILayout.Width(90));
                GUI.color = Color.white;
            }
            else
            {
                GUILayout.Label("○ Edit Mode", EditorStyles.toolbarButton, GUILayout.Width(90));
            }
        }
    }

    // ==========================================================
    // 編輯模式
    // ==========================================================
    private void DrawEditTab()
    {
        DrawEditToolbar();
        EditorGUILayout.Space(2);
        DrawEditTableHeader();
        DrawEditTable();
        EditorGUILayout.Space(6);
        DrawOrphanSection();
    }

    private void DrawEditToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            int flagCount = _allDefs.Count(d => d is ProgressFlagDefinition);
            int valCount = _allDefs.Count(d => d is ProgressValueDefinition);
            GUILayout.Label($"Flag: {flagCount}  Value: {valCount}  (共 {_allDefs.Count})",
                EditorStyles.toolbarButton, GUILayout.Width(180));

            if (GUILayout.Button("重新掃描", EditorStyles.toolbarButton, GUILayout.Width(70)))
                RefreshDefList();

            GUILayout.Label("類型:", GUILayout.Width(36));
            _typeFilter = (TypeFilter)EditorGUILayout.EnumPopup(_typeFilter, EditorStyles.toolbarDropDown, GUILayout.Width(90));

            // 功能篩選（依檔名第二段）
            GUILayout.Label("功能:", GUILayout.Width(36));
            var features = GetAllFeatureNames();
            string currentFeature = string.IsNullOrEmpty(_featureFilter) ? FEATURE_FILTER_ALL : _featureFilter;
            int curIdx = Mathf.Max(0, features.IndexOf(currentFeature));
            int newIdx = EditorGUILayout.Popup(curIdx, features.ToArray(), EditorStyles.toolbarDropDown, GUILayout.Width(110));
            if (newIdx != curIdx)
            {
                _featureFilter = (newIdx == 0) ? "" : features[newIdx];
            }

            GUILayout.Label("排序:", GUILayout.Width(36));
            SortMode newSort = (SortMode)EditorGUILayout.EnumPopup(_sortMode, EditorStyles.toolbarDropDown, GUILayout.Width(90));
            if (newSort != _sortMode) { _sortMode = newSort; ApplySort(); }

            GUILayout.Label("搜尋:", GUILayout.Width(36));
            _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarTextField, GUILayout.Width(150));

            GUILayout.FlexibleSpace();

            GUILayout.Label("新 Def 資料夾:", GUILayout.Width(90));
            _newDefFolder = EditorGUILayout.TextField(_newDefFolder, GUILayout.Width(200));
            if (GUILayout.Button("瀏覽", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                var selected = EditorUtility.OpenFolderPanel("選擇儲存資料夾", "Assets", "");
                if (!string.IsNullOrEmpty(selected) && selected.StartsWith(Application.dataPath))
                    _newDefFolder = "Assets" + selected.Substring(Application.dataPath.Length);
            }

            if (GUILayout.Button("+ Flag", EditorStyles.toolbarButton, GUILayout.Width(60)))
                CreateNewDefinition(typeof(ProgressFlagDefinition));
            if (GUILayout.Button("+ Value", EditorStyles.toolbarButton, GUILayout.Width(60)))
                CreateNewDefinition(typeof(ProgressValueDefinition));
        }
    }

    private void DrawEditTableHeader()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("", GUILayout.Width(COL_FOLDOUT));
            GUILayout.Label("類型", EditorStyles.toolbarButton, GUILayout.Width(COL_TYPE));
            GUILayout.Label("名稱 (FlagID)", EditorStyles.toolbarButton, GUILayout.Width(COL_NAME));
            GUILayout.Label("預設值", EditorStyles.toolbarButton, GUILayout.Width(COL_DEFAULT));
            GUILayout.Label("資料夾", EditorStyles.toolbarButton, GUILayout.Width(COL_FOLDER));
            GUILayout.FlexibleSpace();
            GUILayout.Label("動作", EditorStyles.toolbarButton, GUILayout.Width(COL_ACTION));
        }
    }

    private void DrawEditTable()
    {
        _scrollEdit = EditorGUILayout.BeginScrollView(_scrollEdit);

        bool showFeatureDividers = (_sortMode == SortMode.ByFeature);
        string lastFeatureKey = null; // 組 key = "Flag|Scenario" 之類，避免 Flag/Value 同名混淆

        foreach (var def in GetFilteredDefs())
        {
            // ByFeature 模式下：跨組時顯示標題列
            if (showFeatureDividers)
            {
                string typeLabel = (def is ProgressFlagDefinition) ? "Flag" : "Value";
                string featureName = GetFeatureName(def);
                string featureKey = typeLabel + "|" + featureName;

                if (featureKey != lastFeatureKey)
                {
                    lastFeatureKey = featureKey;
                    EditorGUILayout.Space(2);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // Flag 標題用偏藍，Value 標題用偏橙，跟類型色塊呼應
                        GUI.color = (def is ProgressFlagDefinition)
                            ? new Color(0.7f, 0.9f, 1f)
                            : new Color(1f, 0.9f, 0.6f);
                        EditorGUILayout.LabelField($"── [{typeLabel}] {featureName} ──", EditorStyles.boldLabel);
                        GUI.color = Color.white;
                    }
                }
            }

            DrawDefRow(def);
            if (_foldouts.TryGetValue(def, out bool expanded) && expanded)
                DrawDefDetails(def);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawDefRow(ProgressBaseDefinition def)
    {
        EditorGUI.BeginChangeCheck();

        bool isOrphan = _orphans != null && _orphans.Contains(def);

        using (new EditorGUILayout.HorizontalScope())
        {
            // Foldout
            if (!_foldouts.ContainsKey(def)) _foldouts[def] = false;
            _foldouts[def] = GUILayout.Toggle(_foldouts[def], GUIContent.none, EditorStyles.foldout, GUILayout.Width(COL_FOLDOUT));

            // 類型標籤（孤兒的話整條標紅）
            if (isOrphan) GUI.color = new Color(1f, 0.6f, 0.6f);

            if (def is ProgressFlagDefinition)
            {
                GUI.color *= new Color(0.7f, 0.9f, 1f);
                GUILayout.Label("Flag", EditorStyles.miniButton, GUILayout.Width(COL_TYPE));
            }
            else
            {
                GUI.color *= new Color(1f, 0.9f, 0.6f);
                GUILayout.Label("Value", EditorStyles.miniButton, GUILayout.Width(COL_TYPE));
            }
            GUI.color = Color.white;

            // 名稱（可改檔名）+ Ping 按鈕
            const float PING_BTN_W = 24f;
            string newName = EditorGUILayout.DelayedTextField(def.name, GUILayout.Width(COL_NAME - PING_BTN_W - 2));
            if (newName != def.name && !string.IsNullOrWhiteSpace(newName))
            {
                RenameDefinition(def, newName);
                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button("→", EditorStyles.miniButton, GUILayout.Width(PING_BTN_W)))
            {
                EditorGUIUtility.PingObject(def);
                Selection.activeObject = def;
            }

            // 預設值
            if (def is ProgressFlagDefinition flagDef)
            {
                flagDef.DefaultValue = EditorGUILayout.Toggle(flagDef.DefaultValue, GUILayout.Width(COL_DEFAULT));
            }
            else if (def is ProgressValueDefinition valDef)
            {
                valDef.DefaultValue = EditorGUILayout.IntField(valDef.DefaultValue, GUILayout.Width(COL_DEFAULT));
            }

            // 資料夾（唯讀顯示）
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(def))?.Replace('\\', '/') ?? "";
            // 縮短顯示
            string folderShort = folder.Length > 30 ? "..." + folder.Substring(folder.Length - 27) : folder;
            GUILayout.Label(new GUIContent(folderShort, folder), EditorStyles.miniLabel, GUILayout.Width(COL_FOLDER));

            GUILayout.FlexibleSpace();

            // 查找引用
            if (GUILayout.Button("查找引用", GUILayout.Width(70)))
            {
                _refCache[def] = FindReferences(def);
                _foldouts[def] = true;
            }

            // 複製 ID 到剪貼簿
            if (GUILayout.Button("複製ID", GUILayout.Width(55)))
            {
                EditorGUIUtility.systemCopyBuffer = def.FlagID;
                Debug.Log($"[Progress Inspector] 已複製 FlagID：{def.FlagID}");
            }

            // 刪除
            if (GUILayout.Button("刪除", GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("刪除 Definition",
                    $"確定要刪除 '{def.name}'？\n建議先按「查找引用」確認沒有代碼在用。",
                    "刪除", "取消"))
                {
                    var path = AssetDatabase.GetAssetPath(def);
                    AssetDatabase.DeleteAsset(path);
                    RefreshDefList();
                    GUIUtility.ExitGUI();
                }
            }
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(def, "Edit Progress Definition");
            EditorUtility.SetDirty(def);
        }
    }

    private void DrawDefDetails(ProgressBaseDefinition def)
    {
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            EditorGUI.BeginChangeCheck();

            // 描述（用 SerializedObject 比較保險，因為 description 是 protected）
            var so = new SerializedObject(def);
            var descProp = so.FindProperty("description");
            if (descProp != null)
            {
                EditorGUILayout.PropertyField(descProp, new GUIContent("描述"));
                if (EditorGUI.EndChangeCheck()) so.ApplyModifiedProperties();
            }

            // 引用結果
            if (_refCache.TryGetValue(def, out var refResult))
            {
                EditorGUILayout.Space(4);
                DrawReferenceResult(def, refResult);
            }
            else
            {
                EditorGUILayout.HelpBox("尚未查找引用。點表格右邊的「查找引用」按鈕來掃描。", MessageType.None);
            }
        }
    }

    // ==========================================================
    // 引用查找
    // ==========================================================
    private class ReferenceResult
    {
        public List<string> AssetReferences = new List<string>();   // 引用此 Def 的 asset 路徑
        public List<CodeHit> CodeReferences = new List<CodeHit>();  // 代碼中字串引用
        public class CodeHit
        {
            public string FilePath;
            public int LineNumber;
            public string LineText;
        }
    }

    /// <summary>
    /// 查找所有引用該 Definition 的 asset 和代碼中的字串引用。
    /// Asset 引用用 AssetDatabase.GetDependencies（精準）。
    /// 代碼引用用全文搜尋 *.cs 和 *.asset（會有誤判但範圍可控）。
    /// </summary>
    private ReferenceResult FindReferences(ProgressBaseDefinition def)
    {
        var result = new ReferenceResult();
        if (def == null) return result;

        string targetPath = AssetDatabase.GetAssetPath(def);
        string targetID = def.FlagID;

        // ── 1. 硬引用：其他 asset 引用此 ScriptableObject ──
        // 掃所有 .asset 看誰依賴它
        try
        {
            EditorUtility.DisplayProgressBar("查找引用", "掃描 asset 引用中...", 0.1f);
            var allAssetGuids = AssetDatabase.FindAssets("t:ScriptableObject");
            int total = allAssetGuids.Length;
            for (int i = 0; i < total; i++)
            {
                if (i % 50 == 0)
                    EditorUtility.DisplayProgressBar("查找引用", $"掃描 asset... ({i}/{total})", (float)i / total * 0.5f);

                var path = AssetDatabase.GUIDToAssetPath(allAssetGuids[i]);
                if (path == targetPath) continue;
                var deps = AssetDatabase.GetDependencies(path, false);
                foreach (var dep in deps)
                {
                    if (dep == targetPath)
                    {
                        result.AssetReferences.Add(path);
                        break;
                    }
                }
            }

            // ── 2. 軟引用：全文搜尋 .cs 檔和 .asset 檔中的字串 ──
            EditorUtility.DisplayProgressBar("查找引用", "掃描代碼字串引用中...", 0.6f);
            SearchCodeReferences(targetID, result);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        return result;
    }

    private void SearchCodeReferences(string id, ReferenceResult result)
    {
        if (string.IsNullOrEmpty(id)) return;

        // 搜尋範圍：Assets 下所有 .cs
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string assetsDir = Application.dataPath;

        // 收集要搜尋的檔案
        var files = new List<string>();
        files.AddRange(Directory.GetFiles(assetsDir, "*.cs", SearchOption.AllDirectories));
        // Dialogue System 的對話檔通常是 .asset，但也可能是 .json；先只掃 .cs 避免誤傷
        // 如果要掃更多，可以加上：
        // files.AddRange(Directory.GetFiles(assetsDir, "*.json", SearchOption.AllDirectories));

        // 為避免正則誤判，我們用簡單的字串 Contains 比對，
        // 但會過濾掉明顯的誤判（例如 id 恰好是別人的 substring）：
        // 用前後各加一個「非字母非數字非底線」的判斷。
        int total = files.Count;
        for (int i = 0; i < total; i++)
        {
            if (i % 100 == 0)
                EditorUtility.DisplayProgressBar("查找引用", $"掃描代碼... ({i}/{total})",
                    0.6f + (float)i / total * 0.4f);

            var file = files[i];
            string[] lines;
            try { lines = File.ReadAllLines(file); }
            catch { continue; }

            for (int ln = 0; ln < lines.Length; ln++)
            {
                var line = lines[ln];
                int idx = 0;
                while ((idx = line.IndexOf(id, idx, System.StringComparison.Ordinal)) >= 0)
                {
                    // 邊界檢查：id 前後不能是識別字元（避免 "MyFlagFoo" 誤中 "MyFlag"）
                    bool leftOk = (idx == 0) || !IsIdentChar(line[idx - 1]);
                    bool rightOk = (idx + id.Length >= line.Length) || !IsIdentChar(line[idx + id.Length]);
                    if (leftOk && rightOk)
                    {
                        // 進一步篩：這行至少要看起來像個字串（含雙引號）才納入，
                        // 避免把變數名 "MyFlag" 的定義本身也算進去（那不是引用）。
                        // 寬鬆判斷：這行有 "id" 樣式就收
                        if (line.Contains("\"" + id + "\"") || line.Contains("\"" + id) || line.Contains(id + "\""))
                        {
                            result.CodeReferences.Add(new ReferenceResult.CodeHit
                            {
                                FilePath = "Assets" + file.Substring(assetsDir.Length).Replace('\\', '/'),
                                LineNumber = ln + 1,
                                LineText = line.Trim()
                            });
                            break; // 這行已經命中，不再重複找同一行
                        }
                    }
                    idx += id.Length;
                }
            }
        }
    }

    private static bool IsIdentChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    private void DrawReferenceResult(ProgressBaseDefinition def, ReferenceResult result)
    {
        int totalRefs = result.AssetReferences.Count + result.CodeReferences.Count;

        if (totalRefs == 0)
        {
            EditorGUILayout.HelpBox(
                $"⚠ 找不到任何引用：'{def.FlagID}' 可能是孤兒。\n" +
                "（代碼引用只掃 .cs 且需要加雙引號，字串用變數組出來的抓不到。）",
                MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField($"── 引用結果（共 {totalRefs} 處） ──", EditorStyles.miniBoldLabel);

        // Asset 引用
        if (result.AssetReferences.Count > 0)
        {
            EditorGUILayout.LabelField($"Asset 引用（精準，{result.AssetReferences.Count} 個）：", EditorStyles.boldLabel);
            foreach (var path in result.AssetReferences)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(12);
                    if (GUILayout.Button("→", EditorStyles.miniButton, GUILayout.Width(24)))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                        if (asset != null)
                        {
                            EditorGUIUtility.PingObject(asset);
                            Selection.activeObject = asset;
                        }
                    }
                    EditorGUILayout.LabelField(path, EditorStyles.miniLabel);
                }
            }
        }

        // 代碼引用
        if (result.CodeReferences.Count > 0)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField($"代碼字串引用（可能有誤判，{result.CodeReferences.Count} 處）：", EditorStyles.boldLabel);
            foreach (var hit in result.CodeReferences)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(12);
                    if (GUILayout.Button("開啟", EditorStyles.miniButton, GUILayout.Width(40)))
                    {
                        // 打開 IDE 並跳到該行
                        var fullPath = Path.GetFullPath(hit.FilePath);
                        UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(fullPath, hit.LineNumber);
                    }
                    EditorGUILayout.LabelField(
                        $"{hit.FilePath}:{hit.LineNumber}  |  {hit.LineText}",
                        EditorStyles.miniLabel);
                }
            }
        }
    }

    // ==========================================================
    // 孤兒檢測
    // ==========================================================
    private void DrawOrphanSection()
    {
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("孤兒檢測（專案內沒人引用的 Definition）", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (_orphans != null)
                {
                    GUILayout.Label($"已找到 {_orphans.Count} 個孤兒", EditorStyles.miniLabel);
                    if (GUILayout.Button("清除結果", GUILayout.Width(70)))
                    {
                        _orphans = null;
                        Repaint();
                    }
                }

                using (new EditorGUI.DisabledScope(_orphanCheckRunning))
                {
                    if (GUILayout.Button("掃描全部孤兒", GUILayout.Width(110)))
                        RunOrphanCheck();
                }
            }

            if (_orphans == null)
            {
                EditorGUILayout.HelpBox(
                    "點「掃描全部孤兒」會對所有 Definition 跑一次引用查找。\n" +
                    "專案大時會花幾秒到幾十秒，請耐心等候。",
                    MessageType.Info);
            }
            else if (_orphans.Count == 0)
            {
                EditorGUILayout.HelpBox("🎉 沒有孤兒，所有 Definition 都被引用了！", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"找到 {_orphans.Count} 個孤兒。它們在表格中會以紅色標示。\n" +
                    "注意：代碼字串引用可能有漏網之魚（用變數組出來的 ID 抓不到），刪除前請再次確認。",
                    MessageType.Warning);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("在表格頂部列出孤兒", GUILayout.Width(160)))
                    {
                        // 把孤兒排到最上面
                        _allDefs = _allDefs
                            .OrderBy(d => _orphans.Contains(d) ? 0 : 1)
                            .ThenBy(d => d.name)
                            .ToList();
                    }
                    if (GUILayout.Button("選取所有孤兒", GUILayout.Width(120)))
                    {
                        Selection.objects = _orphans.Cast<Object>().ToArray();
                    }
                }
            }
        }
    }

    private void RunOrphanCheck()
    {
        _orphanCheckRunning = true;
        _orphans = new HashSet<ProgressBaseDefinition>();
        try
        {
            int total = _allDefs.Count;
            for (int i = 0; i < total; i++)
            {
                var def = _allDefs[i];
                if (def == null) continue;

                EditorUtility.DisplayProgressBar("孤兒檢測", $"檢查 {def.name}... ({i + 1}/{total})", (float)i / total);

                var refResult = FindReferences(def);
                _refCache[def] = refResult;

                if (refResult.AssetReferences.Count == 0 && refResult.CodeReferences.Count == 0)
                {
                    _orphans.Add(def);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            _orphanCheckRunning = false;
        }

        Debug.Log($"[Progress Inspector] 孤兒檢測完成，找到 {_orphans.Count} 個沒人引用的 Definition。");
    }

    // ==========================================================
    // 改名 / 新增 / 刪除
    // ==========================================================
    private void RenameDefinition(ProgressBaseDefinition def, string newName)
    {
        if (def == null || string.IsNullOrWhiteSpace(newName)) return;
        var oldPath = AssetDatabase.GetAssetPath(def);
        if (string.IsNullOrEmpty(oldPath)) return;

        string cleanName = newName.Trim();
        foreach (char c in Path.GetInvalidFileNameChars())
            cleanName = cleanName.Replace(c, '_');

        if (string.IsNullOrWhiteSpace(cleanName) || cleanName == def.name) return;

        // 改名前提醒：Progress 系統是靠 FlagID（= 檔名）來識別的，
        // 代碼裡的字串引用不會自動跟著改，必須手動查找替換。
        bool confirm = EditorUtility.DisplayDialog(
            "改名警告",
            $"即將把 '{def.name}' 改名為 '{cleanName}'。\n\n" +
            $"⚠ Progress 系統的 FlagID 就是檔名。改名後，代碼和 Dialogue 裡用舊名字的字串引用不會自動更新！\n" +
            $"建議先用「查找引用」確認有哪些地方在用，手動替換後再改名。\n\n" +
            "確定要改名嗎？",
            "改名", "取消");

        if (!confirm) return;

        string error = AssetDatabase.RenameAsset(oldPath, cleanName);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError($"[Progress Inspector] 改名失敗：{error}");
            EditorUtility.DisplayDialog("改名失敗", error, "確定");
            return;
        }
        AssetDatabase.SaveAssets();
        ApplySort();
    }

    private void CreateNewDefinition(System.Type type)
    {
        if (!AssetDatabase.IsValidFolder(_newDefFolder))
        {
            if (EditorUtility.DisplayDialog("資料夾不存在",
                $"資料夾 '{_newDefFolder}' 不存在，要建立嗎？", "建立", "取消"))
                CreateFolderRecursive(_newDefFolder);
            else return;
        }

        var newDef = ScriptableObject.CreateInstance(type) as ProgressBaseDefinition;
        string prefix = (type == typeof(ProgressFlagDefinition)) ? "Flag_NewFlag" : "Val_NewVariable";
        var path = AssetDatabase.GenerateUniqueAssetPath($"{_newDefFolder}/{prefix}.asset");
        AssetDatabase.CreateAsset(newDef, path);
        AssetDatabase.SaveAssets();

        RefreshDefList();
        _foldouts[newDef] = true;
        EditorGUIUtility.PingObject(newDef);
        Selection.activeObject = newDef;

        Debug.Log($"[Progress Inspector] 已建立 {type.Name}：{path}");
    }

    private void CreateFolderRecursive(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var folderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent)) CreateFolderRecursive(parent);
        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
            AssetDatabase.CreateFolder(parent, folderName);
    }

    // ==========================================================
    // Runtime 模式
    // ==========================================================
    private void DrawRuntimeTab()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Runtime 模式需要在 Play Mode 才能使用。\n" +
                "進入遊戲後這裡會顯示所有 Flag 的即時狀態、所有 Variable 的當前值，\n" +
                "並可以手動增減、觸發各種 Clear 操作來測試時效性標記。",
                MessageType.Info);
            return;
        }

        var svc = GameStatusService.Instance;
        if (svc == null || svc.ProgressFlags == null)
        {
            EditorGUILayout.HelpBox(
                "找不到 GameStatusService.Instance 或 ProgressFlags。\n" +
                "確定場景中有 GameStatusService 且已 Awake？",
                MessageType.Warning);
            return;
        }

        var model = svc.ProgressFlags;

        // 用反射拿到內部的 5 個 HashSet 和 Dictionary。
        // 這裡為了不動到你的 Model，用反射做唯讀窺視；寫入則走公開 API。
        var persistentFlags = GetPrivateField<HashSet<string>>(model, "_persistentFlags");
        var sceneFlags = GetPrivateField<HashSet<string>>(model, "_sceneFlags");
        var slotFlags = GetPrivateField<HashSet<string>>(model, "_slotFlags");
        var phaseFlags = GetPrivateField<HashSet<string>>(model, "_phaseFlags");
        var dayFlags = GetPrivateField<HashSet<string>>(model, "_dayFlags");
        var variables = GetPrivateField<Dictionary<string, int>>(model, "_variables");

        DrawRuntimeToolbar(model);
        EditorGUILayout.Space(4);

        _scrollRuntime = EditorGUILayout.BeginScrollView(_scrollRuntime);

        // ── Flag 區塊 ──
        DrawFlagBucket("🔒 Persistent（永久）", persistentFlags, model, FlagLifetime.Persistent);
        DrawFlagBucket("🏞 Scene（場景）", sceneFlags, model, FlagLifetime.Scene);
        DrawFlagBucket("⏱ Slot（時段）", slotFlags, model, FlagLifetime.UntilNextSlot);
        DrawFlagBucket("🌅 Phase（階段）", phaseFlags, model, FlagLifetime.UntilNextPhase);
        DrawFlagBucket("📅 Day（每日）", dayFlags, model, FlagLifetime.UntilNextDay);

        // ── Variable 區塊 ──
        DrawVariableSection(variables, model);

        EditorGUILayout.Space(6);

        // ── 手動操作區 ──
        DrawManualOperations(model);

        EditorGUILayout.EndScrollView();
    }

    private void DrawRuntimeToolbar(ProgressFlagModel model)
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("搜尋:", GUILayout.Width(36));
            _runtimeSearch = EditorGUILayout.TextField(_runtimeSearch, EditorStyles.toolbarTextField, GUILayout.Width(180));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Clear Scene", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                model.ClearSceneFlags();
                Debug.Log("[Progress Inspector] ClearSceneFlags 已觸發");
            }
            if (GUILayout.Button("Clear Slot", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                model.ClearSlotFlags();
                Debug.Log("[Progress Inspector] ClearSlotFlags 已觸發");
            }
            if (GUILayout.Button("Clear Phase", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                model.ClearPhaseFlags();
                Debug.Log("[Progress Inspector] ClearPhaseFlags 已觸發");
            }
            if (GUILayout.Button("Clear Day", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                model.ClearDayFlags();
                Debug.Log("[Progress Inspector] ClearDayFlags 已觸發");
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Dump 到 Console", EditorStyles.toolbarButton, GUILayout.Width(110)))
                DumpRuntimeState(model);
        }
    }

    private void DrawFlagBucket(string title, HashSet<string> bucket, ProgressFlagModel model, FlagLifetime lifetime)
    {
        if (bucket == null) return;

        var filtered = string.IsNullOrEmpty(_runtimeSearch)
            ? bucket
            : bucket.Where(f => f.IndexOf(_runtimeSearch, System.StringComparison.OrdinalIgnoreCase) >= 0).ToArray()
                as IEnumerable<string>;

        int count = bucket.Count;
        int shown = filtered.Count();

        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            EditorGUILayout.LabelField(
                $"{title}    ({(shown == count ? count.ToString() : $"{shown}/{count}")})",
                EditorStyles.boldLabel);

            if (count == 0)
            {
                EditorGUILayout.LabelField("  （空）", EditorStyles.miniLabel);
                return;
            }

            foreach (var flag in filtered.OrderBy(f => f))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(12);
                    GUILayout.Label("●", GUILayout.Width(12));
                    EditorGUILayout.LabelField(flag);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("移除", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        model.RemoveFlag(flag);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }
    }

    private void DrawVariableSection(Dictionary<string, int> variables, ProgressFlagModel model)
    {
        if (variables == null) return;

        var filtered = string.IsNullOrEmpty(_runtimeSearch)
            ? variables
            : variables.Where(kv => kv.Key.IndexOf(_runtimeSearch, System.StringComparison.OrdinalIgnoreCase) >= 0)
                       .ToDictionary(kv => kv.Key, kv => kv.Value);

        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            EditorGUILayout.LabelField(
                $"🔢 Variables    ({(filtered.Count == variables.Count ? variables.Count.ToString() : $"{filtered.Count}/{variables.Count}")})",
                EditorStyles.boldLabel);

            if (variables.Count == 0)
            {
                EditorGUILayout.LabelField("  （空）", EditorStyles.miniLabel);
                return;
            }

            foreach (var kv in filtered.OrderBy(x => x.Key))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(12);
                    EditorGUILayout.LabelField(kv.Key, GUILayout.Width(240));

                    int newVal = EditorGUILayout.IntField(kv.Value, GUILayout.Width(80));
                    if (newVal != kv.Value)
                    {
                        model.SetValue(kv.Key, newVal);
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("-1", EditorStyles.miniButtonLeft, GUILayout.Width(30)))
                    { model.AddValue(kv.Key, -1); GUIUtility.ExitGUI(); }
                    if (GUILayout.Button("+1", EditorStyles.miniButtonRight, GUILayout.Width(30)))
                    { model.AddValue(kv.Key, 1); GUIUtility.ExitGUI(); }

                    GUILayout.Space(6);

                    if (GUILayout.Button("移除", EditorStyles.miniButton, GUILayout.Width(50)))
                    { model.RemoveFlag(kv.Key); GUIUtility.ExitGUI(); }
                }
            }
        }
    }

    private void DrawManualOperations(ProgressFlagModel model)
    {
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            EditorGUILayout.LabelField("手動操作", EditorStyles.boldLabel);

            // 新增 Flag
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("新 Flag:", GUILayout.Width(60));
                _runtimeNewFlagName = EditorGUILayout.TextField(_runtimeNewFlagName, GUILayout.Width(220));
                _newFlagLifetime = (FlagLifetime)EditorGUILayout.EnumPopup(_newFlagLifetime, GUILayout.Width(130));

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_runtimeNewFlagName)))
                {
                    if (GUILayout.Button("加入 Flag", GUILayout.Width(80)))
                    {
                        model.AddFlag(_runtimeNewFlagName.Trim(), _newFlagLifetime);
                        _runtimeNewFlagName = "";
                        GUI.FocusControl(null);
                    }
                }

                // 從 Definition 選擇
                if (GUILayout.Button("從 Definition 選…", GUILayout.Width(130)))
                {
                    ShowFlagDefinitionPicker((picked) =>
                    {
                        _runtimeNewFlagName = picked;
                        Repaint();
                    });
                }
            }

            // 新增 Variable
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("新 Value:", GUILayout.Width(60));
                _runtimeNewVarName = EditorGUILayout.TextField(_runtimeNewVarName, GUILayout.Width(220));
                GUILayout.Label("=", GUILayout.Width(14));
                _runtimeNewVarValue = EditorGUILayout.IntField(_runtimeNewVarValue, GUILayout.Width(80));

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_runtimeNewVarName)))
                {
                    if (GUILayout.Button("設定 Value", GUILayout.Width(90)))
                    {
                        model.SetValue(_runtimeNewVarName.Trim(), _runtimeNewVarValue);
                        _runtimeNewVarName = "";
                        _runtimeNewVarValue = 0;
                        GUI.FocusControl(null);
                    }
                }

                if (GUILayout.Button("從 Definition 選…", GUILayout.Width(130)))
                {
                    ShowValueDefinitionPicker((picked) =>
                    {
                        _runtimeNewVarName = picked;
                        Repaint();
                    });
                }
            }
        }
    }

    private void ShowFlagDefinitionPicker(System.Action<string> onPicked)
    {
        var menu = new GenericMenu();
        var flags = _allDefs.OfType<ProgressFlagDefinition>().OrderBy(d => d.name).ToArray();
        if (flags.Length == 0)
        {
            menu.AddDisabledItem(new GUIContent("（專案中沒有 FlagDefinition）"));
        }
        else
        {
            foreach (var f in flags)
            {
                string captured = f.FlagID;
                menu.AddItem(new GUIContent(captured), false, () => onPicked(captured));
            }
        }
        menu.ShowAsContext();
    }

    private void ShowValueDefinitionPicker(System.Action<string> onPicked)
    {
        var menu = new GenericMenu();
        var vals = _allDefs.OfType<ProgressValueDefinition>().OrderBy(d => d.name).ToArray();
        if (vals.Length == 0)
        {
            menu.AddDisabledItem(new GUIContent("（專案中沒有 ValueDefinition）"));
        }
        else
        {
            foreach (var v in vals)
            {
                string captured = v.FlagID;
                menu.AddItem(new GUIContent(captured), false, () => onPicked(captured));
            }
        }
        menu.ShowAsContext();
    }

    private void DumpRuntimeState(ProgressFlagModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("========== Progress Runtime Dump ==========");

        var buckets = new (string name, HashSet<string> set)[]
        {
            ("Persistent", GetPrivateField<HashSet<string>>(model, "_persistentFlags")),
            ("Scene",      GetPrivateField<HashSet<string>>(model, "_sceneFlags")),
            ("Slot",       GetPrivateField<HashSet<string>>(model, "_slotFlags")),
            ("Phase",      GetPrivateField<HashSet<string>>(model, "_phaseFlags")),
            ("Day",        GetPrivateField<HashSet<string>>(model, "_dayFlags")),
        };

        foreach (var (name, set) in buckets)
        {
            sb.AppendLine($"--- {name} Flags ({set?.Count ?? 0}) ---");
            if (set != null)
                foreach (var f in set.OrderBy(x => x)) sb.AppendLine("  " + f);
        }

        var vars = GetPrivateField<Dictionary<string, int>>(model, "_variables");
        sb.AppendLine($"--- Variables ({vars?.Count ?? 0}) ---");
        if (vars != null)
            foreach (var kv in vars.OrderBy(x => x.Key))
                sb.AppendLine($"  {kv.Key} = {kv.Value}");

        sb.AppendLine("===========================================");
        Debug.Log(sb.ToString());
    }

    // ==========================================================
    // 反射工具
    // ==========================================================
    private static T GetPrivateField<T>(object obj, string fieldName) where T : class
    {
        if (obj == null) return null;
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(obj) as T;
    }
}
#endif