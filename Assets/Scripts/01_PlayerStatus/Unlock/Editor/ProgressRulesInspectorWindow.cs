#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 解鎖規則總覽視窗。
/// 選單位置：Tools → Progress → Rules Inspector
///
/// 功能：
/// - 一個表格列出專案內所有 ProgressUnlockRuleAsset
/// - 基礎欄位直接在表格編輯（改完自動存檔），條件列表在展開區增刪改
/// - 排序支援「依女主角」與「依指定數值的閾值」
/// - 批次調整工具（指定數值類型的閾值整批 +1 等）
/// - 一鍵建立新 Rule 到指定資料夾
///
/// 注意：這是純編輯器工具，放在 Editor/ 資料夾，不進 build。
/// </summary>
public class ProgressRulesInspectorWindow : EditorWindow
{
    // ───── 資料 ─────
    private List<ProgressUnlockRuleAsset> _allRules = new List<ProgressUnlockRuleAsset>();
    private Dictionary<ProgressUnlockRuleAsset, bool> _foldouts
        = new Dictionary<ProgressUnlockRuleAsset, bool>();
    private Vector2 _scroll;

    // 專案內所有 ProgressUnlockConfig，以及「已被任一 Config 收錄」的 Rule 集合。
    // Manager 只評估 Config 內的 Rule，沒掛進 Config 的 Rule 不會生效 → 用來標警示。
    private List<ProgressUnlockConfig> _allConfigs = new List<ProgressUnlockConfig>();
    private HashSet<ProgressUnlockRuleAsset> _rulesInConfigs
        = new HashSet<ProgressUnlockRuleAsset>();

    // ───── 排序模式 ─────
    private enum SortMode
    {
        ByHeroine,      // 依女主角分組 (預設)
        StatAscending,  // 指定數值的閾值：低 → 高
        StatDescending, // 指定數值的閾值：高 → 低
    }
    private SortMode _sortMode = SortMode.ByHeroine;
    private UnlockStatType _sortStat = UnlockStatType.Libido; // 閾值排序時看哪個數值

    // ───── 批次調整工具 ─────
    private bool _batchFilterByHeroine = false;
    private string _batchFilterHeroineID = "";
    private UnlockStatType _batchStat = UnlockStatType.Libido;
    private int _batchDelta = 0;

    // ───── 新增 Rule ─────
    private string _newRuleFolder = "Assets/Resources/UnlockRules";

    // 欄位寬度
    private const float COL_FOLDOUT = 16f;
    private const float COL_NAME = 180f;
    private const float COL_HEROINE = 80f;
    private const float COL_CONDITIONS = 240f;
    private const float COL_ACTION = 110f;
    private const float COL_TARGET = 160f;

    [MenuItem("Tools/Progress/Rules Inspector")]
    public static void ShowWindow()
    {
        var win = GetWindow<ProgressRulesInspectorWindow>("Rules Inspector");
        win.minSize = new Vector2(960, 400);
    }

    private void OnEnable()
    {
        RefreshRuleList();
    }

    private void OnFocus()
    {
        // 視窗被重新聚焦時刷新，確保新建的 asset 會被抓到
        RefreshRuleList();
    }

    // ==========================================================
    // 核心：掃描專案中所有 Rule Asset
    // ==========================================================
    private void RefreshRuleList()
    {
        _allRules.Clear();
        var guids = AssetDatabase.FindAssets("t:ProgressUnlockRuleAsset");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<ProgressUnlockRuleAsset>(path);
            if (asset != null) _allRules.Add(asset);
        }

        RefreshConfigList();
        ApplySort();
    }

    /// <summary>
    /// 掃描專案內所有 ProgressUnlockConfig，記錄哪些 Rule 已被收錄。
    /// </summary>
    private void RefreshConfigList()
    {
        _allConfigs.Clear();
        _rulesInConfigs.Clear();

        var guids = AssetDatabase.FindAssets("t:ProgressUnlockConfig");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var cfg = AssetDatabase.LoadAssetAtPath<ProgressUnlockConfig>(path);
            if (cfg == null) continue;

            _allConfigs.Add(cfg);

            if (cfg.rules == null) continue;
            foreach (var rule in cfg.rules)
            {
                if (rule != null) _rulesInConfigs.Add(rule);
            }
        }
    }

    /// <summary>
    /// 取得規則中指定數值類型的最小閾值；該規則沒有此數值的條件時 found = false。
    /// </summary>
    private static int GetStatThreshold(ProgressUnlockRuleAsset rule, UnlockStatType stat, out bool found)
    {
        found = false;
        int min = int.MaxValue;
        if (rule.conditions == null) return 0;

        foreach (var c in rule.conditions)
        {
            if (c == null || c.stat != stat) continue;
            found = true;
            if (c.threshold < min) min = c.threshold;
        }
        return found ? min : 0;
    }

    /// <summary>
    /// 依目前的 _sortMode 重新排序 _allRules。
    /// 閾值排序時，沒有該數值條件的規則排在最後。
    /// </summary>
    private void ApplySort()
    {
        switch (_sortMode)
        {
            case SortMode.StatAscending:
                _allRules = _allRules
                    .OrderBy(r => { GetStatThreshold(r, _sortStat, out bool has); return !has; })
                    .ThenBy(r => { int t = GetStatThreshold(r, _sortStat, out _); return t; })
                    .ThenBy(r => r.name)
                    .ToList();
                break;
            case SortMode.StatDescending:
                _allRules = _allRules
                    .OrderBy(r => { GetStatThreshold(r, _sortStat, out bool has); return !has; })
                    .ThenByDescending(r => { int t = GetStatThreshold(r, _sortStat, out _); return t; })
                    .ThenBy(r => r.name)
                    .ToList();
                break;
            case SortMode.ByHeroine:
            default:
                // 依 heroineID 再依名稱排序 (純主角條件的規則 heroineID 為空，排最後)
                _allRules = _allRules
                    .OrderBy(r => string.IsNullOrEmpty(r.heroineID) ? "zzz" : r.heroineID)
                    .ThenBy(r => r.name)
                    .ToList();
                break;
        }
    }

    // ==========================================================
    // 繪製介面
    // ==========================================================
    private void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.Space(4);
        DrawTableHeader();
        DrawTable();
        EditorGUILayout.Space(8);
        DrawBatchTools();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label($"共 {_allRules.Count} 條規則", EditorStyles.toolbarButton, GUILayout.Width(120));

            if (GUILayout.Button("重新掃描", EditorStyles.toolbarButton, GUILayout.Width(80)))
                RefreshRuleList();

            // 排序模式
            GUILayout.Label("排序:", GUILayout.Width(36));
            SortMode newSort = (SortMode)EditorGUILayout.EnumPopup(
                _sortMode, EditorStyles.toolbarDropDown, GUILayout.Width(120));

            // 閾值排序時多一個「看哪個數值」的下拉
            UnlockStatType newSortStat = _sortStat;
            if (newSort != SortMode.ByHeroine)
            {
                newSortStat = (UnlockStatType)EditorGUILayout.EnumPopup(
                    _sortStat, EditorStyles.toolbarDropDown, GUILayout.Width(100));
            }

            if (newSort != _sortMode || newSortStat != _sortStat)
            {
                _sortMode = newSort;
                _sortStat = newSortStat;
                ApplySort();
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label("新規則資料夾:", GUILayout.Width(90));
            _newRuleFolder = EditorGUILayout.TextField(_newRuleFolder, GUILayout.Width(220));

            if (GUILayout.Button("瀏覽...", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                var selected = EditorUtility.OpenFolderPanel("選擇儲存資料夾", "Assets", "");
                if (!string.IsNullOrEmpty(selected))
                {
                    // 轉成相對路徑
                    if (selected.StartsWith(Application.dataPath))
                        _newRuleFolder = "Assets" + selected.Substring(Application.dataPath.Length);
                }
            }

            if (GUILayout.Button("+ 新增 Rule", EditorStyles.toolbarButton, GUILayout.Width(90)))
                CreateNewRule();
        }
    }

    private void DrawTableHeader()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("", GUILayout.Width(COL_FOLDOUT));
            GUILayout.Label("規則名稱", EditorStyles.toolbarButton, GUILayout.Width(COL_NAME));
            GUILayout.Label("女主角 ID", EditorStyles.toolbarButton, GUILayout.Width(COL_HEROINE));
            GUILayout.Label("條件 (展開列編輯)", EditorStyles.toolbarButton, GUILayout.Width(COL_CONDITIONS));
            GUILayout.Label("動作", EditorStyles.toolbarButton, GUILayout.Width(COL_ACTION));
            GUILayout.Label("目標 Flag/Value", EditorStyles.toolbarButton, GUILayout.Width(COL_TARGET));
            GUILayout.FlexibleSpace();
            GUILayout.Label("", EditorStyles.toolbarButton, GUILayout.Width(60));
        }
    }

    private void DrawTable()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        string lastHeroine = null;
        bool showHeroineDividers = (_sortMode == SortMode.ByHeroine);

        foreach (var rule in _allRules)
        {
            if (rule == null) continue;

            // 只在依女主角排序時顯示分隔列 (其他排序模式顯示分隔會很錯亂)
            if (showHeroineDividers)
            {
                string thisHeroine = string.IsNullOrEmpty(rule.heroineID) ? "(主角/未指定)" : rule.heroineID;
                if (thisHeroine != lastHeroine)
                {
                    lastHeroine = thisHeroine;
                    EditorGUILayout.Space(2);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUI.color = new Color(1, 1, 0.6f);
                        EditorGUILayout.LabelField($"── {thisHeroine} ──", EditorStyles.boldLabel);
                        GUI.color = Color.white;
                    }
                }
            }

            DrawRuleRow(rule);

            if (_foldouts.TryGetValue(rule, out bool expanded) && expanded)
                DrawRuleDetails(rule);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawRuleRow(ProgressUnlockRuleAsset rule)
    {
        EditorGUI.BeginChangeCheck();

        using (new EditorGUILayout.HorizontalScope())
        {
            // 展開箭頭 (用 GUILayout.Toggle 搭配 foldout 樣式，比 EditorGUILayout.Foldout 更容易控制寬度)
            if (!_foldouts.ContainsKey(rule)) _foldouts[rule] = false;
            _foldouts[rule] = GUILayout.Toggle(_foldouts[rule], GUIContent.none, EditorStyles.foldout, GUILayout.Width(COL_FOLDOUT));

            // 規則名稱 (可編輯 asset 檔名) + 跳轉按鈕
            // 把寬度分成：檔名輸入框 + 小按鈕
            // 未被任何 Config 收錄的規則 (OnlyCondition 除外) 名稱標紅提醒
            bool notInConfig = rule.action != ProgressActionType.OnlyCondition
                && !_rulesInConfigs.Contains(rule);
            const float JUMP_BTN_WIDTH = 24f;
            if (notInConfig) GUI.color = new Color(1f, 0.5f, 0.5f);
            string newName = EditorGUILayout.DelayedTextField(
                rule.name,
                GUILayout.Width(COL_NAME - JUMP_BTN_WIDTH - 2));
            if (notInConfig) GUI.color = Color.white;

            if (newName != rule.name && !string.IsNullOrWhiteSpace(newName))
            {
                RenameRuleAsset(rule, newName);
                GUIUtility.ExitGUI(); // 改名後字典 key 仍然有效 (Unity ScriptableObject 引用不變)，但佈局可能已變
            }

            // 「→」按鈕：點一下高亮 asset
            if (GUILayout.Button("→", EditorStyles.miniButton, GUILayout.Width(JUMP_BTN_WIDTH)))
            {
                EditorGUIUtility.PingObject(rule);
                Selection.activeObject = rule;
            }

            // 女主角 ID (可編輯)。含女主角條件卻沒填 ID 時標紅提醒
            bool missingHeroineID = rule.HasHeroineCondition && string.IsNullOrEmpty(rule.heroineID);
            if (missingHeroineID) GUI.color = new Color(1f, 0.5f, 0.5f);
            rule.heroineID = EditorGUILayout.TextField(rule.heroineID, GUILayout.Width(COL_HEROINE));
            if (missingHeroineID) GUI.color = Color.white;

            // 條件摘要 (唯讀；編輯請展開該列)
            int condCount = rule.conditions != null ? rule.conditions.Count : 0;
            string summary = rule.GetConditionsSummary();
            EditorGUILayout.LabelField(
                new GUIContent(summary, $"{condCount} 個條件，全部達成才算符合。點左側箭頭展開編輯。"),
                EditorStyles.miniLabel, GUILayout.Width(COL_CONDITIONS));

            // 動作
            rule.action = (ProgressActionType)EditorGUILayout.EnumPopup(
                rule.action, GUILayout.Width(COL_ACTION));

            // 目標
            using (new EditorGUI.DisabledScope(rule.action == ProgressActionType.OnlyCondition))
            {
                rule.target = (ProgressBaseDefinition)EditorGUILayout.ObjectField(
                    rule.target, typeof(ProgressBaseDefinition), false, GUILayout.Width(COL_TARGET));
            }

            GUILayout.FlexibleSpace();

            // 刪除按鈕
            if (GUILayout.Button("刪除", GUILayout.Width(55)))
            {
                if (EditorUtility.DisplayDialog("刪除 Rule",
                    $"確定要刪除 '{rule.name}'？\n此操作無法撤銷（可從 Unity 的 Undo 復原）。",
                    "刪除", "取消"))
                {
                    var path = AssetDatabase.GetAssetPath(rule);
                    AssetDatabase.DeleteAsset(path);
                    RefreshRuleList();
                    GUIUtility.ExitGUI();
                }
            }
        }

        if (EditorGUI.EndChangeCheck())
        {
            // 使用 Undo 註冊變更 + 標記 dirty，Unity 會自動儲存
            Undo.RecordObject(rule, "Edit Rule in Inspector Window");
            EditorUtility.SetDirty(rule);
        }
    }

    private void DrawRuleDetails(ProgressUnlockRuleAsset rule)
    {
        EditorGUI.BeginChangeCheck();

        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            DrawConfigMembership(rule);

            EditorGUILayout.LabelField("── 條件 (全部達成才算符合) ──", EditorStyles.miniBoldLabel);

            if (rule.conditions == null)
                rule.conditions = new List<UnlockStatCondition>();

            int removeIndex = -1;
            for (int i = 0; i < rule.conditions.Count; i++)
            {
                var cond = rule.conditions[i];
                if (cond == null)
                {
                    cond = new UnlockStatCondition();
                    rule.conditions[i] = cond;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(16);

                    cond.stat = (UnlockStatType)EditorGUILayout.EnumPopup(cond.stat, GUILayout.Width(100));
                    cond.op = (ComparisonOp)EditorGUILayout.EnumPopup(cond.op, GUILayout.Width(110));
                    cond.threshold = EditorGUILayout.IntField(cond.threshold, GUILayout.Width(60));

                    GUILayout.Label(
                        cond.IsHeroineStat ? "(女主角)" : "(主角)",
                        EditorStyles.miniLabel, GUILayout.Width(50));

                    if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                        removeIndex = i;

                    GUILayout.FlexibleSpace();
                }
            }

            if (removeIndex >= 0)
                rule.conditions.RemoveAt(removeIndex);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(16);
                if (GUILayout.Button("+ 新增條件", GUILayout.Width(100)))
                    rule.conditions.Add(new UnlockStatCondition());
                GUILayout.FlexibleSpace();
            }

            if (rule.HasHeroineCondition && string.IsNullOrEmpty(rule.heroineID))
            {
                EditorGUILayout.HelpBox(
                    "含女主角條件 (Libido / Trust / HCount) 但未填 heroineID，此規則會被 Manager 跳過。",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("── 進階設定 ──", EditorStyles.miniBoldLabel);

            rule.ruleName = EditorGUILayout.TextField("備註名稱", rule.ruleName);

            // 撤銷行為
            rule.revertWhenConditionFails = EditorGUILayout.Toggle(
                new GUIContent("條件失去時撤銷",
                    "勾選：條件不成立時自動撤銷；不勾：達成一次即永久啟用"),
                rule.revertWhenConditionFails);

            // SetValue 專用
            if (rule.action == ProgressActionType.SetValue)
            {
                rule.valueToSet = EditorGUILayout.IntField("寫入數值 (SetValue 用)", rule.valueToSet);
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("── 達成公告 (Alert，選填) ──", EditorStyles.miniBoldLabel);

            rule.unlockAlertKey = EditorGUILayout.TextField(
                new GUIContent("公告 Key",
                    "達成時用 StoryManager 顯示的系統公告 (無顏色)。\n" +
                    "填 Localization Key；留空則不顯示。撤銷 (revert) 時不會顯示。"),
                rule.unlockAlertKey);

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("── UI 提示覆蓋 ──", EditorStyles.miniBoldLabel);

            rule.uiHintTypeKeyOverride = EditorGUILayout.TextField(
                new GUIContent("類型 Key 覆蓋", "留空則自動用第一個條件的數值名稱 (Libido / Trust / Stress …)"),
                rule.uiHintTypeKeyOverride);

            rule.uiHintLevelOverride = EditorGUILayout.IntField(
                new GUIContent("顯示等級覆蓋", "留 0 則自動用第一個條件的閾值"),
                rule.uiHintLevelOverride);

            EditorGUILayout.HelpBox(
                $"UI 會顯示：[{rule.GetUIDisplayTypeKey() ?? "(無類型)"}] LV.{rule.GetUIDisplayLevel()}",
                MessageType.None);
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(rule, "Edit Rule Details");
            EditorUtility.SetDirty(rule);
        }
    }

    // ==========================================================
    // Config 收錄狀態 (Manager 只評估 Config 內的 Rule)
    // ==========================================================

    /// <summary>
    /// 顯示此規則的 Config 收錄狀態。
    /// 未被任何 Config 收錄的規則 Manager 不會評估 (等於不生效)，
    /// 提供一鍵加入按鈕；OnlyCondition 類型本來就不需要掛 Config，不警告。
    /// </summary>
    private void DrawConfigMembership(ProgressUnlockRuleAsset rule)
    {
        if (_rulesInConfigs.Contains(rule))
        {
            // 找出收錄它的 Config 名稱 (通常只有一個)
            var owners = _allConfigs
                .Where(c => c.rules != null && c.rules.Contains(rule))
                .Select(c => c.name);
            EditorGUILayout.LabelField(
                $"已收錄於 Config：{string.Join(", ", owners)}", EditorStyles.miniLabel);
            return;
        }

        if (rule.action == ProgressActionType.OnlyCondition)
            return; // 純 UI 條件用，不需要掛 Config

        EditorGUILayout.HelpBox(
            "此規則未被任何 ProgressUnlockConfig 收錄，Manager 不會評估它 (規則不生效)！\n" +
            "請把它加入 Config 的 rules 列表。",
            MessageType.Error);

        if (_allConfigs.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "專案內找不到任何 ProgressUnlockConfig，請先建立 (Create → Game → Progress → Progress Unlock Config)。",
                MessageType.Warning);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(16);
            foreach (var cfg in _allConfigs)
            {
                if (GUILayout.Button($"加入 {cfg.name}", GUILayout.Width(220)))
                {
                    Undo.RecordObject(cfg, "Add Rule To Config");
                    if (cfg.rules == null) cfg.rules = new List<ProgressUnlockRuleAsset>();
                    cfg.rules.Add(rule);
                    EditorUtility.SetDirty(cfg);
                    AssetDatabase.SaveAssets();
                    _rulesInConfigs.Add(rule);
                    Debug.Log($"[Rules Inspector] 已把 '{rule.name}' 加入 Config '{cfg.name}'。");
                }
            }
            GUILayout.FlexibleSpace();
        }
    }

    // ==========================================================
    // 批次調整工具
    // ==========================================================
    private void DrawBatchTools()
    {
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            EditorGUILayout.LabelField("批次調整工具", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "選擇要調整的數值類型 → 設定 delta 值 → 按套用。\n" +
                "例如：數值類型=Libido + delta=1 → 所有規則中 Libido 條件的閾值 +1",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                _batchFilterByHeroine = EditorGUILayout.ToggleLeft(
                    "限定女主角 ID:", _batchFilterByHeroine, GUILayout.Width(120));
                using (new EditorGUI.DisabledScope(!_batchFilterByHeroine))
                {
                    _batchFilterHeroineID = EditorGUILayout.TextField(_batchFilterHeroineID, GUILayout.Width(150));
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("數值類型:", GUILayout.Width(70));
                _batchStat = (UnlockStatType)EditorGUILayout.EnumPopup(_batchStat, GUILayout.Width(110));

                GUILayout.Space(20);

                GUILayout.Label("閾值 delta:", GUILayout.Width(70));
                _batchDelta = EditorGUILayout.IntField(_batchDelta, GUILayout.Width(60));

                GUILayout.FlexibleSpace();

                int affected = CountBatchAffected();
                GUILayout.Label($"將影響 {affected} 條規則", EditorStyles.miniLabel);

                using (new EditorGUI.DisabledScope(affected == 0 || _batchDelta == 0))
                {
                    if (GUILayout.Button("套用", GUILayout.Width(60)))
                    {
                        if (EditorUtility.DisplayDialog("批次調整",
                            $"將對 {affected} 條規則套用：\n" +
                            $"  {_batchStat} 條件的閾值 {(_batchDelta >= 0 ? "+" : "")}{_batchDelta}\n\n" +
                            "確定套用？（可用 Ctrl+Z 復原）",
                            "套用", "取消"))
                        {
                            ApplyBatch();
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 取出批次調整會影響的規則：通過女主角篩選、且含有 _batchStat 條件的規則。
    /// </summary>
    private IEnumerable<ProgressUnlockRuleAsset> GetBatchAffectedRules()
    {
        foreach (var rule in _allRules)
        {
            if (rule == null) continue;
            if (_batchFilterByHeroine && rule.heroineID != _batchFilterHeroineID) continue;

            GetStatThreshold(rule, _batchStat, out bool has);
            if (!has) continue;

            yield return rule;
        }
    }

    private int CountBatchAffected() => GetBatchAffectedRules().Count();

    private void ApplyBatch()
    {
        var affected = GetBatchAffectedRules().ToList();
        Undo.RecordObjects(affected.ToArray(), "Batch Adjust Rules");

        foreach (var rule in affected)
        {
            foreach (var cond in rule.conditions)
            {
                if (cond == null || cond.stat != _batchStat) continue;
                cond.threshold = Mathf.Max(0, cond.threshold + _batchDelta);
            }
            EditorUtility.SetDirty(rule);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Rules Inspector] 已對 {affected.Count} 條規則的 {_batchStat} 條件套用批次調整。");
    }

    // ==========================================================
    // 重新命名 Rule Asset
    // ==========================================================
    private void RenameRuleAsset(ProgressUnlockRuleAsset rule, string newName)
    {
        if (rule == null || string.IsNullOrWhiteSpace(newName)) return;

        var oldPath = AssetDatabase.GetAssetPath(rule);
        if (string.IsNullOrEmpty(oldPath)) return;

        // 清理非法字元 (Windows 檔名禁止字元 + 兩端空白)
        string cleanName = newName.Trim();
        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
        {
            cleanName = cleanName.Replace(c, '_');
        }
        if (string.IsNullOrWhiteSpace(cleanName)) return;

        // 如果名字沒變化，不用做事 (避免誤觸)
        if (cleanName == rule.name) return;

        // AssetDatabase.RenameAsset 接受的 newName 不含副檔名
        string error = AssetDatabase.RenameAsset(oldPath, cleanName);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError($"[Rules Inspector] 重新命名失敗：{error}");
            EditorUtility.DisplayDialog("重新命名失敗", error, "確定");
            return;
        }

        AssetDatabase.SaveAssets();

        // 重新排序 (名字變了排序可能跟著變)
        ApplySort();
    }

    // ==========================================================
    // 新增 Rule
    // ==========================================================
    private void CreateNewRule()
    {
        // 確保資料夾存在
        if (!AssetDatabase.IsValidFolder(_newRuleFolder))
        {
            if (EditorUtility.DisplayDialog("資料夾不存在",
                $"資料夾 '{_newRuleFolder}' 不存在，要建立嗎？", "建立", "取消"))
            {
                CreateFolderRecursive(_newRuleFolder);
            }
            else return;
        }

        var newRule = CreateInstance<ProgressUnlockRuleAsset>();
        var path = AssetDatabase.GenerateUniqueAssetPath($"{_newRuleFolder}/Rule_NewUnlockRule.asset");
        AssetDatabase.CreateAsset(newRule, path);
        AssetDatabase.SaveAssets();

        RefreshRuleList();

        // 自動捲到新建的 rule 並展開
        _foldouts[newRule] = true;
        EditorGUIUtility.PingObject(newRule);
        Selection.activeObject = newRule;

        Debug.Log($"[Rules Inspector] 已建立新 Rule：{path}");
    }

    private void CreateFolderRecursive(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        var folderName = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent)) CreateFolderRecursive(parent);
        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
            AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
