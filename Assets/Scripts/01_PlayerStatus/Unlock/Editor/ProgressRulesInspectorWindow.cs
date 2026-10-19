#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>編輯單一 Config 內的多條件規則；每列不再建立獨立資產。</summary>
public class ProgressRulesInspectorWindow : EditorWindow
{
    private const string FlagRulesFolder = "Assets/Resources/Progress/Flag/Rules";
    [SerializeField] private ProgressUnlockConfig _config;
    [SerializeField] private string _newConfigFolder = "Assets/Resources/UnlockRules";
    private SerializedObject _serializedConfig;
    private Vector2 _scroll;
    private bool _showMirrors;
    private enum SortMode { Original, ByHeroine, StatAscending, StatDescending }
    private SortMode _sortMode;
    private UnlockStatType _sortStat = UnlockStatType.Trust;
    private UnlockStatType _batchStat = UnlockStatType.Trust;
    private int _batchDelta;
    private bool _batchFilterByHeroine;
    private string _batchHeroineID = "";

    [MenuItem("Tools/Progress/Rules Inspector")]
    public static void ShowWindow()
    {
        var window = GetWindow<ProgressRulesInspectorWindow>("Rules Inspector");
        window.minSize = new Vector2(960, 400);
    }

    private void OnEnable()
    {
        Undo.undoRedoPerformed += OnUndoRedo;
        FindConfig();
    }

    private void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;
    private void OnFocus() { FindConfig(); Repaint(); }
    private void OnUndoRedo() { _serializedConfig = null; Repaint(); }

    private void FindConfig()
    {
        if (_config != null) return;
        if (Selection.activeObject is ProgressUnlockConfig selected)
            _config = selected;
        else
        {
            var guids = AssetDatabase.FindAssets("t:ProgressUnlockConfig");
            if (guids.Length == 1)
                _config = AssetDatabase.LoadAssetAtPath<ProgressUnlockConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        _serializedConfig = null;
    }

    private void OnGUI()
    {
        DrawToolbar();
        if (_config == null)
        {
            EditorGUILayout.HelpBox("請選擇或建立一份 Config 設定表。所有規則都直接存在該表內。", MessageType.Info);
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            EditorGUILayout.HelpBox("執行期間僅供查看；請離開 Play Mode 後編輯設定。", MessageType.Info);

        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (_serializedConfig == null || _serializedConfig.targetObject != _config)
                _serializedConfig = new SerializedObject(_config);
            _serializedConfig.Update();
            var entries = _serializedConfig.FindProperty("entries");
            int duplicateIndex = -1;
            int deleteIndex = -1;
            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;
                foreach (int index in GetSortedIndices())
                {
                    var entry = entries.GetArrayElementAtIndex(index);
                    DrawEntry(entry, index, ref duplicateIndex, ref deleteIndex);
                }
                if (entries.arraySize == 0)
                    EditorGUILayout.HelpBox("按「新增一列」，填入條件並拖入目標 Flag。每列可加入多個條件。", MessageType.Info);

                _showMirrors = EditorGUILayout.Foldout(_showMirrors, "數值與布林映射（同一份設定表）", true);
                if (_showMirrors)
                {
                    EditorGUILayout.PropertyField(_serializedConfig.FindProperty("mirrors"), new GUIContent("數值映射"), true);
                    EditorGUILayout.PropertyField(_serializedConfig.FindProperty("flagMirrors"), new GUIContent("布林 Flag 映射"), true);
                }
            }
            // SerializedProperty 在寫入前註冊 Undo，並將修改標記到所屬 Config。
            _serializedConfig.ApplyModifiedProperties();
            if (deleteIndex >= 0) DeleteEntry(deleteIndex);
            else if (duplicateIndex >= 0) DuplicateEntry(duplicateIndex);
            DrawBatchTools();
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("設定表", GUILayout.Width(45));
            var selected = (ProgressUnlockConfig)EditorGUILayout.ObjectField(
                _config, typeof(ProgressUnlockConfig), false, GUILayout.MinWidth(170));
            if (selected != _config) { _config = selected; _serializedConfig = null; }
            using (new EditorGUI.DisabledScope(_config == null || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("新增一列", EditorStyles.toolbarButton, GUILayout.Width(80))) AddEntry();
                if (GUILayout.Button("儲存設定表", EditorStyles.toolbarButton, GUILayout.Width(90))) AssetDatabase.SaveAssets();
            }
            if (GUILayout.Button(new GUIContent("開啟 Rules Flag 資料夾", FlagRulesFolder),
                EditorStyles.toolbarButton, GUILayout.Width(165))) OpenFlagFolder();
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("設定表資料夾", GUILayout.Width(90));
            _newConfigFolder = EditorGUILayout.TextField(_newConfigFolder, GUILayout.MinWidth(160));
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("建立 Config…", GUILayout.Width(115))) CreateConfig();
            }
            GUILayout.Label("排序", GUILayout.Width(35));
            _sortMode = (SortMode)EditorGUILayout.EnumPopup(_sortMode, GUILayout.Width(120));
            if (_sortMode == SortMode.StatAscending || _sortMode == SortMode.StatDescending)
                _sortStat = (UnlockStatType)EditorGUILayout.EnumPopup(_sortStat, GUILayout.Width(110));
            GUILayout.Label($"共 {_config?.entries?.Count ?? 0} 列", GUILayout.Width(75));
        }
    }

    private void DrawEntry(SerializedProperty entry, int index, ref int duplicateIndex, ref int deleteIndex)
    {
        var rule = _config.entries[index];
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                entry.isExpanded = GUILayout.Toggle(entry.isExpanded, $"第 {index + 1} 列", EditorStyles.foldout, GUILayout.Width(85));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("name"), GUIContent.none, GUILayout.Width(165));
                EditorGUILayout.LabelField(rule?.GetConditionsSummary() ?? "(無條件)", GUILayout.MinWidth(150));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("target"), GUIContent.none, GUILayout.Width(180));
                if (GUILayout.Button("複製", GUILayout.Width(45))) duplicateIndex = index;
                if (GUILayout.Button("刪除", GUILayout.Width(45))) deleteIndex = index;
            }
            if (!entry.isExpanded) return;
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("heroineID"), new GUIContent("女主角 ID（主角條件可留空）"));
            EditorGUILayout.LabelField("條件：全部成立才執行", EditorStyles.boldLabel);
            var conditions = entry.FindPropertyRelative("conditions");
            int removeCondition = -1;
            for (int i = 0; i < conditions.arraySize; i++)
            {
                var condition = conditions.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(condition.FindPropertyRelative("stat"), GUIContent.none, GUILayout.Width(140));
                    EditorGUILayout.PropertyField(condition.FindPropertyRelative("op"), GUIContent.none, GUILayout.Width(140));
                    EditorGUILayout.PropertyField(condition.FindPropertyRelative("threshold"), GUIContent.none, GUILayout.Width(90));
                    if (GUILayout.Button("移除條件", GUILayout.Width(80))) removeCondition = i;
                }
            }
            if (removeCondition >= 0) conditions.DeleteArrayElementAtIndex(removeCondition);
            if (GUILayout.Button("新增條件", GUILayout.Width(100)))
            {
                int i = conditions.arraySize;
                conditions.InsertArrayElementAtIndex(i);
                var added = conditions.GetArrayElementAtIndex(i);
                added.FindPropertyRelative("stat").intValue = (int)UnlockStatType.Trust;
                added.FindPropertyRelative("op").intValue = (int)ComparisonOp.GreaterOrEqual;
                added.FindPropertyRelative("threshold").intValue = 0;
            }
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("action"), new GUIContent("達成時動作"));
            if ((ProgressActionType)entry.FindPropertyRelative("action").intValue == ProgressActionType.SetValue)
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("valueToSet"), new GUIContent("寫入數值"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("revertWhenConditionFails"),
                new GUIContent("條件失去時撤銷", "不勾選：達成一次後保持；勾選：失去條件時撤銷。"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("unlockAlertKey"), new GUIContent("解鎖公告 Key（選填）"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("ruleName"), new GUIContent("備註"));
            if (rule == null || rule.conditions == null || !rule.conditions.Any(c => c != null))
                EditorGUILayout.HelpBox("尚未設定條件，此列不會執行。", MessageType.Warning);
            else if (rule.HasHeroineCondition && string.IsNullOrWhiteSpace(rule.heroineID))
                EditorGUILayout.HelpBox("包含女主角條件，請填入女主角 ID。", MessageType.Warning);
            if (rule != null && rule.action != ProgressActionType.OnlyCondition && rule.target == null)
                EditorGUILayout.HelpBox("請拖入目標 Flag 或 Value。", MessageType.Warning);
            if (rule != null && rule.target != null && _config.entries.Count(r => r != null && r.target == rule.target && r.action != ProgressActionType.OnlyCondition) > 1)
                EditorGUILayout.HelpBox("多列會寫入同一個目標；各列分別套用及撤銷，並非自動合併成任一條件成立。", MessageType.Warning);
            if (rule != null && rule.action == ProgressActionType.OnlyCondition)
                EditorGUILayout.HelpBox("OnlyCondition 不會開關 Flag；若要觸發 Flag，請選擇 SetFlagOn 或 SetFlagOff。", MessageType.Info);
        }
    }

    private IEnumerable<int> GetSortedIndices()
    {
        var indices = Enumerable.Range(0, _config.entries?.Count ?? 0);
        if (_sortMode == SortMode.ByHeroine)
            return indices.OrderBy(i => string.IsNullOrEmpty(_config.entries[i]?.heroineID))
                .ThenBy(i => _config.entries[i]?.heroineID).ThenBy(i => _config.entries[i]?.name).ToArray();
        if (_sortMode == SortMode.StatAscending || _sortMode == SortMode.StatDescending)
        {
            var ordered = indices.OrderBy(i => !GetThreshold(_config.entries[i], _sortStat).HasValue);
            return (_sortMode == SortMode.StatAscending
                ? ordered.ThenBy(i => GetThreshold(_config.entries[i], _sortStat))
                : ordered.ThenByDescending(i => GetThreshold(_config.entries[i], _sortStat))).ToArray();
        }
        return indices.ToArray();
    }

    private static int? GetThreshold(ProgressUnlockRule rule, UnlockStatType stat)
    {
        return rule?.conditions?.Where(c => c != null && c.stat == stat).Select(c => (int?)c.threshold).Min();
    }

    private void AddEntry()
    {
        Undo.RecordObject(_config, "新增規則列");
        if (_config.entries == null) _config.entries = new List<ProgressUnlockRule>();
        _config.entries.Add(new ProgressUnlockRule());
        FinishListEdit(_config.entries.Count - 1);
    }

    private void DuplicateEntry(int index)
    {
        Undo.RecordObject(_config, "複製規則列");
        var copy = _config.entries[index]?.Copy() ?? new ProgressUnlockRule();
        copy.name += "（副本）";
        _config.entries.Insert(index + 1, copy);
        FinishListEdit(index + 1);
    }

    private void DeleteEntry(int index)
    {
        if (!EditorUtility.DisplayDialog("刪除規則列", "只移除設定表內此列，目標 Flag 不受影響。可用 Ctrl+Z 復原。", "刪除", "取消")) return;
        Undo.RecordObject(_config, "刪除規則列");
        _config.entries.RemoveAt(index);
        FinishListEdit(-1);
    }

    private void FinishListEdit(int expandedIndex)
    {
        EditorUtility.SetDirty(_config);
        _serializedConfig = new SerializedObject(_config);
        if (expandedIndex >= 0)
            _serializedConfig.FindProperty("entries").GetArrayElementAtIndex(expandedIndex).isExpanded = true;
        Repaint();
    }

    private void DrawBatchTools()
    {
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            EditorGUILayout.LabelField("批次調整此設定表的條件閾值", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _batchFilterByHeroine = EditorGUILayout.ToggleLeft("限定女主角 ID", _batchFilterByHeroine, GUILayout.Width(120));
                using (new EditorGUI.DisabledScope(!_batchFilterByHeroine))
                    _batchHeroineID = EditorGUILayout.TextField(_batchHeroineID, GUILayout.Width(110));
                _batchStat = (UnlockStatType)EditorGUILayout.EnumPopup(_batchStat, GUILayout.Width(130));
                GUILayout.Label("增減", GUILayout.Width(35));
                _batchDelta = EditorGUILayout.IntField(_batchDelta, GUILayout.Width(60));
                var affected = (_config.entries ?? new List<ProgressUnlockRule>()).Where(r => r != null
                    && (!_batchFilterByHeroine || r.heroineID == _batchHeroineID)
                    && GetThreshold(r, _batchStat).HasValue).ToList();
                GUILayout.Label($"影響 {affected.Count} 列");
                using (new EditorGUI.DisabledScope(affected.Count == 0 || _batchDelta == 0))
                {
                    if (GUILayout.Button("套用", GUILayout.Width(60)))
                    {
                        Undo.RecordObject(_config, "批次調整規則閾值");
                        foreach (var rule in affected)
                            foreach (var condition in rule.conditions)
                                if (condition != null && condition.stat == _batchStat)
                                    condition.threshold = Mathf.Max(0, condition.threshold + _batchDelta);
                        FinishListEdit(-1);
                    }
                }
            }
        }
    }

    private static void OpenFlagFolder()
    {
        var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(FlagRulesFolder);
        if (folder == null)
        {
            EditorUtility.DisplayDialog("找不到資料夾", FlagRulesFolder, "確定");
            return;
        }
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = folder;
        EditorGUIUtility.PingObject(folder);
    }

    private void CreateConfig()
    {
        string folder = AssetDatabase.IsValidFolder(_newConfigFolder) ? _newConfigFolder : "Assets";
        string path = EditorUtility.SaveFilePanelInProject("建立規則設定表", "ProgressUnlock_Rules", "asset", "選擇設定表位置", folder);
        if (string.IsNullOrEmpty(path)) return;
        var config = CreateInstance<ProgressUnlockConfig>();
        AssetDatabase.CreateAsset(config, AssetDatabase.GenerateUniqueAssetPath(path));
        AssetDatabase.SaveAssets();
        _config = config;
        _serializedConfig = null;
        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
        EditorUtility.DisplayDialog("設定表已建立", "請將此 Config 加入 GameStatusService 的 Progress Unlock Configs，遊戲才會執行。使用既有已掛載的 Config 則不需重新設定。", "確定");
    }
}
#endif
