#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class HeroineFlagScheduleEditorWindow : EditorWindow
{
    [SerializeField] private HeroineFlagScheduleConfig _config;
    private Editor _editor;
    private Vector2 _scroll;
    private HeroineFlagScheduleConfig[] _configs = Array.Empty<HeroineFlagScheduleConfig>();

    [MenuItem("Tools/Heroine Flag Schedule Editor")]
    public static void Open()
    {
        var window = GetWindow<HeroineFlagScheduleEditorWindow>("Heroine Flag Schedule");
        window.minSize = new Vector2(560, 420);
        window.Show();
    }

    private void OnEnable() => Scan();
    private void OnFocus() => Scan();
    private void OnDisable() { if (_editor != null) DestroyImmediate(_editor); }
    private void OnInspectorUpdate() { if (Application.isPlaying) Repaint(); }

    private void Scan()
    {
        _configs = AssetDatabase.FindAssets("t:HeroineFlagScheduleConfig")
            .Select(g => AssetDatabase.LoadAssetAtPath<HeroineFlagScheduleConfig>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(c => c != null).OrderBy(c => c.heroineID).ToArray();
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            string[] labels = new[] { "選擇規則表…" }.Concat(_configs.Select(c => $"{c.heroineID} — {c.name}")).ToArray();
            int current = Array.IndexOf(_configs, _config) + 1;
            int next = EditorGUILayout.Popup(current, labels, EditorStyles.toolbarPopup);
            if (next != current) SelectConfig(next > 0 ? _configs[next - 1] : null);
            if (GUILayout.Button("重新掃描", EditorStyles.toolbarButton, GUILayout.Width(75))) Scan();
        }
        var selected = (HeroineFlagScheduleConfig)EditorGUILayout.ObjectField("規則表", _config, typeof(HeroineFlagScheduleConfig), false);
        if (selected != _config) SelectConfig(selected);
        if (_config == null)
        {
            EditorGUILayout.HelpBox("在 Project 右鍵 → Create → Game → Config → Heroine Flag Schedule 建立規則表，\n再到 Heroine Status Config 選擇 Flag 規則模式，並加入規則表清單。", MessageType.Info);
            return;
        }
        if (_editor == null || _editor.target != _config) Editor.CreateCachedEditor(_config, null, ref _editor);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _editor.OnInspectorGUI();
        EditorGUILayout.EndScrollView();
    }

    private void SelectConfig(HeroineFlagScheduleConfig config)
    {
        GUI.FocusControl(null);
        EditorGUIUtility.editingTextField = false;
        _config = config;
        _scroll = Vector2.zero;
        Repaint();
        GUIUtility.ExitGUI();
    }
}

[CustomEditor(typeof(HeroineFlagScheduleConfig))]
public class HeroineFlagScheduleConfigEditor : Editor
{
    private HeroineStatusConfig[] _statusConfigs;

    private void OnEnable()
    {
        _statusConfigs = AssetDatabase.FindAssets("t:HeroineStatusConfig")
            .Select(g => AssetDatabase.LoadAssetAtPath<HeroineStatusConfig>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(c => c != null).ToArray();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var config = (HeroineFlagScheduleConfig)target;
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("heroineID"), new GUIContent("女主角 ID"));
            EditorGUILayout.HelpBox("每個行程必須有一個專用狀態 Flag。指定活動會同步切換同角色的互斥 Flag；新遊戲、讀檔或依 Flag 刷新時，從該行程的活動清單等機率抽選。", MessageType.Info);
            var rules = serializedObject.FindProperty("rules");
            var defaultID = serializedObject.FindProperty("defaultRuleID");
            var labels = new List<string> { "（請選擇新遊戲預設行程）" };
            int currentDefault = 0;
            for (int i = 0; i < rules.arraySize; i++)
            {
                var rule = rules.GetArrayElementAtIndex(i);
                labels.Add(rule.FindPropertyRelative("ruleName").stringValue);
                if (!string.IsNullOrEmpty(defaultID.stringValue) && rule.FindPropertyRelative("ruleID").stringValue == defaultID.stringValue)
                    currentDefault = i + 1;
            }
            int nextDefault = EditorGUILayout.Popup("新遊戲預設行程", currentDefault, labels.ToArray());
            if (nextDefault != currentDefault)
                defaultID.stringValue = nextDefault == 0 ? "" : rules.GetArrayElementAtIndex(nextDefault - 1).FindPropertyRelative("ruleID").stringValue;
            for (int i = 0; i < rules.arraySize; i++)
            {
                var rule = rules.GetArrayElementAtIndex(i);
                bool remove;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(rule.FindPropertyRelative("ruleName"), new GUIContent($"行程 {i + 1}"));
                        remove = GUILayout.Button("刪除", GUILayout.Width(45));
                    }
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(rule.FindPropertyRelative("ruleID"), new GUIContent("呼叫用 ID"));
                    EditorGUILayout.PropertyField(rule.FindPropertyRelative("stateFlag"), new GUIContent("狀態 Flag（必填）"));
                    EditorGUILayout.PropertyField(rule.FindPropertyRelative("locationID"), new GUIContent("地點 ID（必填）"));
                    DrawActivities(rule);
                    var legacy = rule.FindPropertyRelative("conditions");
                    if (legacy != null && legacy.arraySize > 0)
                        EditorGUILayout.HelpBox("此行程留有第一版條件資料，已不參與運算。請直接指定上方唯一的狀態 Flag。", MessageType.None);
                }
                if (remove)
                {
                    if (rule.FindPropertyRelative("ruleID").stringValue == defaultID.stringValue) defaultID.stringValue = "";
                    rules.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
            if (GUILayout.Button("＋ 新增行程"))
            {
                int index = rules.arraySize;
                rules.InsertArrayElementAtIndex(index);
                var rule = rules.GetArrayElementAtIndex(index);
                rule.FindPropertyRelative("ruleID").stringValue = Guid.NewGuid().ToString("N");
                rule.FindPropertyRelative("ruleName").stringValue = "新行程";
                rule.FindPropertyRelative("stateFlag").objectReferenceValue = null;
                rule.FindPropertyRelative("conditions").ClearArray();
                rule.FindPropertyRelative("locationID").stringValue = "";
                rule.FindPropertyRelative("activityState").stringValue = "";
                rule.FindPropertyRelative("activityIDs").ClearArray();
            }
            if (GUILayout.Button("修復缺少／重複的行程 ID"))
            {
                var seen = new HashSet<string>();
                for (int i = 0; i < rules.arraySize; i++)
                {
                    var id = rules.GetArrayElementAtIndex(i).FindPropertyRelative("ruleID");
                    if (string.IsNullOrWhiteSpace(id.stringValue) || !seen.Add(id.stringValue))
                    {
                        id.stringValue = Guid.NewGuid().ToString("N");
                        seen.Add(id.stringValue);
                    }
                }
            }
        }
        serializedObject.ApplyModifiedProperties();
        DrawDiagnostics(config);
        DrawRuntime(config);
    }

    private static void DrawActivities(SerializedProperty rule)
    {
        var activities = rule.FindPropertyRelative("activityIDs");
        var legacy = rule.FindPropertyRelative("activityState");
        if (activities.arraySize == 0 && !string.IsNullOrWhiteSpace(legacy.stringValue))
        {
            EditorGUILayout.HelpBox($"目前沿用舊活動：{legacy.stringValue}。可轉成清單後加入更多活動。", MessageType.Info);
            if (GUILayout.Button("將舊活動轉為清單"))
            {
                activities.arraySize = 1;
                activities.GetArrayElementAtIndex(0).stringValue = legacy.stringValue;
                legacy.stringValue = "";
                activities.isExpanded = true;
            }
        }
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(activities, new GUIContent("活動 ID 清單（至少一個）"), true);
        // 使用者開始維護清單後，清空舊欄位，避免刪光清單時舊活動再次生效。
        if (EditorGUI.EndChangeCheck() && activities.arraySize > 0) legacy.stringValue = "";
    }

    private void DrawDiagnostics(HeroineFlagScheduleConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.heroineID)) EditorGUILayout.HelpBox("尚未指定女主角 ID。", MessageType.Warning);
        var rules = config.rules ?? new List<HeroineFlagScheduleRule>();
        if (string.IsNullOrEmpty(config.defaultRuleID) || !rules.Any(r => r != null && r.ruleID == config.defaultRuleID))
            EditorGUILayout.HelpBox("請選擇一個新遊戲預設行程；讀檔若所有行程 Flag 皆為 OFF，會維持離場。", MessageType.Warning);
        var ids = new HashSet<string>();
        var flags = new HashSet<string>();
        var activities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rule in rules)
        {
            if (rule == null) continue;
            if (string.IsNullOrWhiteSpace(rule.ruleID) || !ids.Add(rule.ruleID))
                EditorGUILayout.HelpBox($"「{rule.ruleName}」缺少 ID 或與其他行程重複。", MessageType.Error);
            if (rule.stateFlag == null || !flags.Add(rule.stateFlag.FlagID))
                EditorGUILayout.HelpBox($"「{rule.ruleName}」未指定狀態 Flag，或共用了其他行程的 Flag。", MessageType.Error);
            if (string.IsNullOrWhiteSpace(rule.locationID))
                EditorGUILayout.HelpBox($"「{rule.ruleName}」未填地點。", MessageType.Error);
            var activityIDs = rule.GetActivityIDs();
            if (activityIDs.Length == 0)
                EditorGUILayout.HelpBox($"「{rule.ruleName}」至少需要一個活動 ID。", MessageType.Error);
            foreach (string activity in activityIDs)
                if (string.IsNullOrWhiteSpace(activity) || !activities.Add(activity))
                    EditorGUILayout.HelpBox($"「{rule.ruleName}」的活動 ID 為空或在同角色內重複：{activity}", MessageType.Error);
        }
        foreach (var status in _statusConfigs ?? Array.Empty<HeroineStatusConfig>())
        {
            if (status == null || status.FlagSchedules == null || !status.FlagSchedules.Contains(config)) continue;
            int sameHeroine = status.FlagSchedules.Count(c => c != null && string.Equals(c.heroineID, config.heroineID, StringComparison.OrdinalIgnoreCase));
            if (sameHeroine > 1) EditorGUILayout.HelpBox($"「{status.name}」對此女主角指定了多份行程表。", MessageType.Error);
            foreach (var other in status.FlagSchedules)
            {
                if (other == null || other == config || other.rules == null) continue;
                if (other.rules.Any(r => r?.stateFlag != null && flags.Contains(r.stateFlag.FlagID)))
                    EditorGUILayout.HelpBox($"狀態 Flag 與「{other.name}」重複；不同角色也必須使用專用 Flag。", MessageType.Error);
            }
        }
    }

    private static void DrawRuntime(HeroineFlagScheduleConfig config)
    {
        if (!Application.isPlaying) return;
        var service = GameStatusService.Instance;
        if (service?.HeroineFlagSchedule == null)
        {
            EditorGUILayout.HelpBox("目前未啟用 Flag 行程模式。", MessageType.Info);
            return;
        }
        if (service.HeroineConfig.FlagSchedules == null || !service.HeroineConfig.FlagSchedules.Contains(config))
        {
            EditorGUILayout.HelpBox("這份行程表未加入目前的 Heroine Status Config。", MessageType.Warning);
            return;
        }
        EditorGUILayout.HelpBox(service.HeroineFlagSchedule.Describe(config.heroineID), MessageType.Info);
        foreach (var pair in service.Scenario.AllLocationStates)
        {
            var heroine = pair.Value?.Heroines?.FirstOrDefault(h => h != null
                && string.Equals(h.HeroineID, config.heroineID, StringComparison.OrdinalIgnoreCase));
            if (heroine != null) EditorGUILayout.LabelField("實際位置／活動", $"{pair.Key} / {heroine.Activity}");
        }
        foreach (var rule in config.rules ?? new List<HeroineFlagScheduleRule>())
        {
            if (rule == null) continue;
            if (GUILayout.Button("依 Flag 抽選活動並切換至：" + rule.ruleName)) service.HeroineFlagSchedule.TryActivateFlag(config.heroineID, rule.stateFlag?.FlagID);
            foreach (string activity in rule.GetActivityIDs())
                if (GUILayout.Button("指定活動：" + activity)) service.HeroineFlagSchedule.TryActivateActivity(config.heroineID, activity);
        }
        if (GUILayout.Button("此角色離場（關閉全部行程 Flag）")) service.HeroineFlagSchedule.TryDeactivate(config.heroineID);
        if (GUILayout.Button("依 Flag 刷新全部女主角")) service.HeroineFlagSchedule.Refresh();
    }
}
#endif
