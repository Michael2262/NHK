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
/// - 一個表格列出專案內所有 HeroineUnlockRuleAsset
/// - 基礎欄位直接在表格編輯（改完自動存檔）
/// - 可展開每行看/改細節 (revertWhenConditionFails / UI 提示覆蓋)
/// - 批次調整工具 (全部 Lewd 閾值 +1 等)
/// - 一鍵建立新 Rule 到指定資料夾
///
/// 注意：這是純編輯器工具，放在 Editor/ 資料夾，不進 build。
/// </summary>
public class ProgressRulesInspectorWindow : EditorWindow
{
    // ───── 資料 ─────
    private List<HeroineUnlockRuleAsset> _allRules = new List<HeroineUnlockRuleAsset>();
    private Dictionary<HeroineUnlockRuleAsset, bool> _foldouts
        = new Dictionary<HeroineUnlockRuleAsset, bool>();
    private Vector2 _scroll;

    // ───── 排序模式 ─────
    private enum SortMode
    {
        ByHeroine,          // 依女主角分組 (預設)
        LewdAscending,      // Lewd 閾值：低 → 高
        LewdDescending,     // Lewd 閾值：高 → 低
        AffinityAscending,  // Affinity 閾值：低 → 高
        AffinityDescending, // Affinity 閾值：高 → 低
    }
    private SortMode _sortMode = SortMode.ByHeroine;

    // ───── 批次調整工具 ─────
    private HeroineUnlockConditionType _batchFilterType = HeroineUnlockConditionType.LewdnessOnly;
    private bool _batchFilterByType = false;
    private string _batchFilterHeroineID = "";
    private bool _batchFilterByHeroine = false;
    private int _batchLewdDelta = 0;
    private int _batchAffinityDelta = 0;

    // ───── 新增 Rule ─────
    private string _newRuleFolder = "Assets/GameData/UnlockRules";

    // 欄位寬度
    private const float COL_FOLDOUT = 16f;
    private const float COL_NAME = 180f;
    private const float COL_HEROINE = 80f;
    private const float COL_CONDTYPE = 100f;
    private const float COL_LEWD = 45f;
    private const float COL_AFFINITY = 45f;
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
        var guids = AssetDatabase.FindAssets("t:HeroineUnlockRuleAsset");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<HeroineUnlockRuleAsset>(path);
            if (asset != null) _allRules.Add(asset);
        }
        ApplySort();
    }

    /// <summary>
    /// 依目前的 _sortMode 重新排序 _allRules。
    /// </summary>
    private void ApplySort()
    {
        switch (_sortMode)
        {
            case SortMode.LewdAscending:
                _allRules = _allRules.OrderBy(r => r.requiredLewdnessLevel)
                                     .ThenBy(r => r.name).ToList();
                break;
            case SortMode.LewdDescending:
                _allRules = _allRules.OrderByDescending(r => r.requiredLewdnessLevel)
                                     .ThenBy(r => r.name).ToList();
                break;
            case SortMode.AffinityAscending:
                _allRules = _allRules.OrderBy(r => r.requiredAffinityLevel)
                                     .ThenBy(r => r.name).ToList();
                break;
            case SortMode.AffinityDescending:
                _allRules = _allRules.OrderByDescending(r => r.requiredAffinityLevel)
                                     .ThenBy(r => r.name).ToList();
                break;
            case SortMode.ByHeroine:
            default:
                // 依 heroineID 再依 ruleName 排序
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
                _sortMode, EditorStyles.toolbarDropDown, GUILayout.Width(140));
            if (newSort != _sortMode)
            {
                _sortMode = newSort;
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
            GUILayout.Label("條件類型", EditorStyles.toolbarButton, GUILayout.Width(COL_CONDTYPE));
            GUILayout.Label("Lewd", EditorStyles.toolbarButton, GUILayout.Width(COL_LEWD));
            GUILayout.Label("Affi", EditorStyles.toolbarButton, GUILayout.Width(COL_AFFINITY));
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
                string thisHeroine = string.IsNullOrEmpty(rule.heroineID) ? "(未指定)" : rule.heroineID;
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

    private void DrawRuleRow(HeroineUnlockRuleAsset rule)
    {
        EditorGUI.BeginChangeCheck();

        using (new EditorGUILayout.HorizontalScope())
        {
            // 展開箭頭 (用 GUILayout.Toggle 搭配 foldout 樣式，比 EditorGUILayout.Foldout 更容易控制寬度)
            if (!_foldouts.ContainsKey(rule)) _foldouts[rule] = false;
            _foldouts[rule] = GUILayout.Toggle(_foldouts[rule], GUIContent.none, EditorStyles.foldout, GUILayout.Width(COL_FOLDOUT));

            // 規則名稱 (可編輯 asset 檔名) + 跳轉按鈕
            // 把寬度分成：檔名輸入框 + 小按鈕
            const float JUMP_BTN_WIDTH = 24f;
            string newName = EditorGUILayout.DelayedTextField(
                rule.name,
                GUILayout.Width(COL_NAME - JUMP_BTN_WIDTH - 2));

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

            // 女主角 ID (可編輯)
            rule.heroineID = EditorGUILayout.TextField(rule.heroineID, GUILayout.Width(COL_HEROINE));

            // 條件類型
            rule.conditionType = (HeroineUnlockConditionType)EditorGUILayout.EnumPopup(
                rule.conditionType, GUILayout.Width(COL_CONDTYPE));

            // Lewd 閾值（只在用得到時亮起）
            using (new EditorGUI.DisabledScope(
                rule.conditionType == HeroineUnlockConditionType.AffinityOnly))
            {
                rule.requiredLewdnessLevel = EditorGUILayout.IntField(
                    rule.requiredLewdnessLevel, GUILayout.Width(COL_LEWD));
            }

            // Affinity 閾值
            using (new EditorGUI.DisabledScope(
                rule.conditionType == HeroineUnlockConditionType.LewdnessOnly))
            {
                rule.requiredAffinityLevel = EditorGUILayout.IntField(
                    rule.requiredAffinityLevel, GUILayout.Width(COL_AFFINITY));
            }

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

    private void DrawRuleDetails(HeroineUnlockRuleAsset rule)
    {
        EditorGUI.BeginChangeCheck();

        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
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
            EditorGUILayout.LabelField("── UI 提示覆蓋 ──", EditorStyles.miniBoldLabel);

            rule.uiHintTypeKeyOverride = EditorGUILayout.TextField(
                new GUIContent("類型 Key 覆蓋", "留空則依 conditionType 自動推斷 (Lewdness / Affinity)"),
                rule.uiHintTypeKeyOverride);

            rule.uiHintLevelOverride = EditorGUILayout.IntField(
                new GUIContent("顯示等級覆蓋", "留 0 則自動取條件閾值"),
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
    // 批次調整工具
    // ==========================================================
    private void DrawBatchTools()
    {
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            EditorGUILayout.LabelField("批次調整工具", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "勾選篩選條件 → 設定 delta 值 → 按套用。\n" +
                "例如：勾「限定條件類型=LewdnessOnly」+ Lewd delta=1 → 所有 LewdnessOnly 規則的 Lewd 閾值 +1",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                _batchFilterByHeroine = EditorGUILayout.ToggleLeft(
                    "限定女主角 ID:", _batchFilterByHeroine, GUILayout.Width(120));
                using (new EditorGUI.DisabledScope(!_batchFilterByHeroine))
                {
                    _batchFilterHeroineID = EditorGUILayout.TextField(_batchFilterHeroineID, GUILayout.Width(150));
                }

                GUILayout.Space(20);

                _batchFilterByType = EditorGUILayout.ToggleLeft(
                    "限定條件類型:", _batchFilterByType, GUILayout.Width(110));
                using (new EditorGUI.DisabledScope(!_batchFilterByType))
                {
                    _batchFilterType = (HeroineUnlockConditionType)EditorGUILayout.EnumPopup(
                        _batchFilterType, GUILayout.Width(120));
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Lewd 閾值 delta:", GUILayout.Width(110));
                _batchLewdDelta = EditorGUILayout.IntField(_batchLewdDelta, GUILayout.Width(60));

                GUILayout.Space(20);

                GUILayout.Label("Affi 閾值 delta:", GUILayout.Width(110));
                _batchAffinityDelta = EditorGUILayout.IntField(_batchAffinityDelta, GUILayout.Width(60));

                GUILayout.FlexibleSpace();

                int affected = CountBatchAffected();
                GUILayout.Label($"將影響 {affected} 條規則", EditorStyles.miniLabel);

                using (new EditorGUI.DisabledScope(affected == 0 || (_batchLewdDelta == 0 && _batchAffinityDelta == 0)))
                {
                    if (GUILayout.Button("套用", GUILayout.Width(60)))
                    {
                        if (EditorUtility.DisplayDialog("批次調整",
                            $"將對 {affected} 條規則套用：\n" +
                            $"  Lewd 閾值 {(_batchLewdDelta >= 0 ? "+" : "")}{_batchLewdDelta}\n" +
                            $"  Affinity 閾值 {(_batchAffinityDelta >= 0 ? "+" : "")}{_batchAffinityDelta}\n\n" +
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

    private IEnumerable<HeroineUnlockRuleAsset> GetBatchAffectedRules()
    {
        foreach (var rule in _allRules)
        {
            if (rule == null) continue;
            if (_batchFilterByHeroine && rule.heroineID != _batchFilterHeroineID) continue;
            if (_batchFilterByType && rule.conditionType != _batchFilterType) continue;
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
            // 依條件類型決定要不要動該欄位（避免動到無意義的值）
            if (rule.conditionType != HeroineUnlockConditionType.AffinityOnly && _batchLewdDelta != 0)
            {
                rule.requiredLewdnessLevel = Mathf.Max(0, rule.requiredLewdnessLevel + _batchLewdDelta);
            }
            if (rule.conditionType != HeroineUnlockConditionType.LewdnessOnly && _batchAffinityDelta != 0)
            {
                rule.requiredAffinityLevel = Mathf.Max(0, rule.requiredAffinityLevel + _batchAffinityDelta);
            }
            EditorUtility.SetDirty(rule);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Rules Inspector] 已套用批次調整到 {affected.Count} 條規則。");
    }

    // ==========================================================
    // 重新命名 Rule Asset
    // ==========================================================
    private void RenameRuleAsset(HeroineUnlockRuleAsset rule, string newName)
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

        var newRule = CreateInstance<HeroineUnlockRuleAsset>();
        var path = AssetDatabase.GenerateUniqueAssetPath($"{_newRuleFolder}/Rule_NewHeroineRule.asset");
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