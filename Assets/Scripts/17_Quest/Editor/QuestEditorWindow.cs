#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>編輯任務資產、唯讀查看目前遊戲進度；不執行任務或獎勵邏輯。</summary>
public sealed class QuestEditorWindow : EditorWindow
{
    private const string QuestFolder = "Assets/Resources/Quest";
    private QuestDatabase _database;
    private string _databaseError;
    [SerializeField] private QuestDefinition _selected;
    private QuestDefinition _pendingSelection;
    private bool _hasPendingSelection;
    [SerializeField] private string _search = "";
    [SerializeField] private bool _showDatabase;
    private readonly List<QuestDefinition> _allTasks = new List<QuestDefinition>();
    private Vector2 _listScroll;
    private Vector2 _detailScroll;
    private Vector2 _databaseScroll;
    private GameStatusService _service;
    private QuestModel _model;
    private string _membershipMessage;
    private MessageType _membershipMessageType;
    private string _renameError;
    private QuestDefinition _renameErrorTarget;

    [MenuItem("Tools/Quest/任務編輯器")]
    public static void Open()
    {
        var window = GetWindow<QuestEditorWindow>("Quest 任務編輯器");
        window.minSize = new Vector2(820, 600);
        window.Show();
    }

    private void OnEnable()
    {
        minSize = new Vector2(820, 600);
        RefreshAssets();
        Undo.undoRedoPerformed += OnUndoRedo;
        FollowSelection();
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
        UnbindRuntime();
    }

    private void OnProjectChange() => RefreshAssets();
    private void OnSelectionChange() => FollowSelection();

    private void FollowSelection()
    {
        if (Selection.activeObject is QuestDefinition definition)
            RequestSelection(definition);
        Repaint();
    }

    private void RequestSelection(QuestDefinition definition)
    {
        if (!_hasPendingSelection && _selected == definition) return;
        // Project 選取事件也會走這裡；焦點處理留到此視窗的 OnGUI，避免在 GUI 外操作控制項。
        _pendingSelection = definition;
        _hasPendingSelection = true;
        Repaint();
    }

    private void ApplyPendingSelection()
    {
        if (!_hasPendingSelection) return;
        var next = _pendingSelection;
        _pendingSelection = null;
        _hasPendingSelection = false;
        if (_selected == next) return;

        // IMGUI 相同位置的輸入欄位會重用 control ID；先解除舊欄位，不能讓其編輯緩衝套到新 SO。
        GUI.FocusControl(null);
        GUIUtility.keyboardControl = 0;
        GUIUtility.hotControl = 0;
        EditorGUIUtility.editingTextField = false;
        _selected = next;
        _detailScroll = Vector2.zero;
        _renameError = null;
        _renameErrorTarget = null;
        Repaint();
        // 兩筆任務的獎勵清單長度可能不同；若已在 Layout 就直接建立新版面，其他事件則中止舊版面。
        if (Event.current.type != EventType.Layout) GUIUtility.ExitGUI();
    }

    private void RefreshAssets()
    {
        _allTasks.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:QuestDefinition"))
        {
            var task = AssetDatabase.LoadAssetAtPath<QuestDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (task != null) _allTasks.Add(task);
        }
        _allTasks.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
        var databases = AssetDatabase.FindAssets("t:QuestDatabase");
        _database = databases.Length == 1
            ? AssetDatabase.LoadAssetAtPath<QuestDatabase>(AssetDatabase.GUIDToAssetPath(databases[0]))
            : null;
        _databaseError = databases.Length > 1 ? "專案中有多份 Quest Database，請只保留遊戲使用的那一份。" : null;
        Repaint();
    }

    private void OnUndoRedo()
    {
        RefreshAssets();
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        // Undo / Redo 後也將 Quest 設定寫回磁碟，不需要另一個儲存步驟。
        if (_database != null) AssetDatabase.SaveAssetIfDirty(_database);
        foreach (var task in _allTasks) AssetDatabase.SaveAssetIfDirty(task);
        _membershipMessage = null;
    }

    private void OnInspectorUpdate()
    {
        var service = EditorApplication.isPlaying ? GameStatusService.Instance : null;
        var model = service != null ? service.Quests : null;
        if (_service != service || !ReferenceEquals(_model, model))
        {
            UnbindRuntime();
            _service = service;
            _model = model;
            if (_service != null) _service.OnGameStatusLoaded += Repaint;
            if (_model != null) _model.OnChanged += Repaint;
        }
        // 同時刷新遊戲日與 Play Mode 切換，不寫入 SO 或遊戲資料。
        Repaint();
    }

    private void UnbindRuntime()
    {
        if (_service != null) _service.OnGameStatusLoaded -= Repaint;
        if (_model != null) _model.OnChanged -= Repaint;
        _service = null;
        _model = null;
    }

    private void OnGUI()
    {
        ApplyPendingSelection();
        DrawToolbar();
        DrawDatabase();
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawTaskList();
            using (new EditorGUILayout.VerticalScope())
            using (var scroll = new EditorGUILayout.ScrollViewScope(_detailScroll))
            {
                _detailScroll = scroll.scrollPosition;
                DrawDefinition();
                EditorGUILayout.Space(16);
                DrawProgress();
            }
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || _databaseError != null))
            {
                if (GUILayout.Button("新增 Quest SO", EditorStyles.toolbarButton))
                {
                    CreateTask();
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button(new GUIContent("加入未加入的 Quest", "將專案中所有尚未加入的 Quest 收錄到唯一資料庫，不受搜尋文字影響。"), EditorStyles.toolbarButton))
                {
                    if (EnsureDatabase()) AddToDatabase(_allTasks);
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label("修改即時保存", EditorStyles.miniLabel);
        }
    }

    private void DrawDatabase()
    {
        if (_databaseError != null)
        {
            EditorGUILayout.HelpBox(_databaseError, MessageType.Error);
            return;
        }
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField("任務資料庫（自動載入）", _database, typeof(QuestDatabase), false);
        if (_database == null)
        {
            EditorGUILayout.HelpBox("新增或加入任務時，會自動在 Assets/Resources/Quest 建立資料庫。", MessageType.Info);
            return;
        }

        string error = _database.GetConfigurationError();
        if (error != null) EditorGUILayout.HelpBox(error, MessageType.Warning);
        if (!string.IsNullOrEmpty(_membershipMessage))
            EditorGUILayout.HelpBox(_membershipMessage, _membershipMessageType);
        _showDatabase = EditorGUILayout.Foldout(_showDatabase, "資料庫任務清單（領獎順序／加入／移出 SO）", true);
        if (!_showDatabase) return;
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        using (var serialized = new SerializedObject(_database))
        {
            serialized.Update();
            _databaseScroll = EditorGUILayout.BeginScrollView(_databaseScroll, GUILayout.MaxHeight(150));
            EditorGUILayout.PropertyField(serialized.FindProperty("Tasks"), new GUIContent("任務清單"), true);
            EditorGUILayout.EndScrollView();
            if (serialized.ApplyModifiedProperties()) AssetDatabase.SaveAssetIfDirty(_database);
        }
        EditorGUILayout.LabelField("領獎依此清單由上往下找第一筆已完成、未領取的任務。移出只解除引用；資料庫需指定到 GameStatusService。", EditorStyles.wordWrappedMiniLabel);
    }

    private void DrawTaskList()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(250)))
        {
            EditorGUILayout.LabelField("任務清單", EditorStyles.boldLabel);
            _search = EditorGUILayout.TextField("搜尋", _search);
            var tasks = _allTasks;
            using (var scroll = new EditorGUILayout.ScrollViewScope(_listScroll))
            {
                _listScroll = scroll.scrollPosition;
                int shown = 0;
                if (tasks != null)
                {
                    foreach (var task in tasks)
                    {
                        if (task == null || !MatchesSearch(task)) continue;
                        shown++;
                        bool included = IsInDatabase(task);
                        string membership = _database == null ? "" : included ? "  [已加入]" : "  [未加入]";
                        var label = new GUIContent(task.name + membership, AssetDatabase.GetAssetPath(task));
                        if (GUILayout.Toggle(_selected == task, label, "Button", GUILayout.Height(30)) && _selected != task)
                        {
                            RequestSelection(task);
                            GUIUtility.ExitGUI();
                        }
                    }
                }
                if (shown == 0) EditorGUILayout.HelpBox("尚無符合條件的任務。可按「新增 Quest SO」。", MessageType.Info);
            }
        }
    }

    private bool IsInDatabase(QuestDefinition task)
        => _database != null && _database.Tasks != null && _database.Tasks.Contains(task);

    private void AddToDatabase(IEnumerable<QuestDefinition> candidates)
    {
        if (_database == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var pending = new List<QuestDefinition>();
        var seen = new HashSet<QuestDefinition>();
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var existingIDs = new HashSet<string>(StringComparer.Ordinal);
        if (_database.Tasks != null)
            foreach (var task in _database.Tasks)
                if (task != null && task.QuestID != null) existingIDs.Add(task.QuestID);

        foreach (var task in candidates)
        {
            if (task == null || IsInDatabase(task) || !seen.Add(task)) continue;
            pending.Add(task);
            if (task.QuestID != null)
            {
                counts.TryGetValue(task.QuestID, out int count);
                counts[task.QuestID] = count + 1;
            }
        }

        var additions = new List<QuestDefinition>();
        var issues = new List<string>();
        foreach (var task in pending)
        {
            string id = task.QuestID;
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim())
                issues.Add($"{task.name}：SO 名稱為空或頭尾含空白。");
            else if (existingIDs.Contains(id) || counts[id] > 1)
                issues.Add($"{task.name}：ID「{id}」與資料庫或本次其他任務重複。");
            else if (task.RequiredCount < 1)
                issues.Add($"{task.name}：目標成功次數必須至少為 1。");
            else additions.Add(task);
        }

        // 批次中的重複 ID 全部略過，讓使用者選擇正確資產，不依掃描順序擅自挑選。
        if (additions.Count > 0)
        {
            Undo.RecordObject(_database, "加入 Quest 至資料庫");
            if (_database.Tasks == null) _database.Tasks = new List<QuestDefinition>();
            _database.Tasks.AddRange(additions);
            EditorUtility.SetDirty(_database);
            AssetDatabase.SaveAssetIfDirty(_database);
        }
        _membershipMessage = $"資料庫「{_database.name}」：已加入 {additions.Count} 筆，略過 {issues.Count} 筆有問題的任務。";
        if (additions.Count > 0) _membershipMessage += " 已自動保存。";
        int shownIssues = Math.Min(issues.Count, 5);
        for (int i = 0; i < shownIssues; i++) _membershipMessage += "\n" + issues[i];
        if (issues.Count > shownIssues) _membershipMessage += $"\n另有 {issues.Count - shownIssues} 筆，修正後可再次按「加入未加入的 Quest」。";
        _membershipMessageType = issues.Count > 0 ? MessageType.Warning : MessageType.Info;
        Repaint();
    }

    private bool MatchesSearch(QuestDefinition task)
    {
        if (string.IsNullOrWhiteSpace(_search)) return true;
        return (task.name + " " + task.QuestID + " " + task.TitleTextKey)
            .IndexOf(_search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void DrawDefinition()
    {
        EditorGUILayout.LabelField("任務設定 · SO", EditorStyles.boldLabel);
        var selected = (QuestDefinition)EditorGUILayout.ObjectField("任務 SO", _selected, typeof(QuestDefinition), false);
        if (selected != _selected)
        {
            RequestSelection(selected);
            GUIUtility.ExitGUI();
        }
        if (_selected == null)
        {
            EditorGUILayout.HelpBox("從左側選取任務，或建立新的 Quest SO。", MessageType.Info);
            return;
        }
        if (GUILayout.Button("在 Project 中定位", GUILayout.Width(150))) EditorGUIUtility.PingObject(_selected);
        EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(_selected), EditorStyles.wordWrappedMiniLabel);
        bool playing = EditorApplication.isPlayingOrWillChangePlaymode;
        if (playing) EditorGUILayout.HelpBox("遊戲執行中只供查看；停止 Play Mode 後可編輯設定。", MessageType.Info);
        using (new EditorGUI.DisabledScope(playing))
        {
            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.DelayedTextField(
                new GUIContent("任務名稱／ID", "就是 SO 檔名。按 Enter 或離開欄位後更名並保存，資產引用會保留。"),
                _selected.name);
            if (EditorGUI.EndChangeCheck()) RenameTask(newName);
        }
        if (_renameErrorTarget == _selected && !string.IsNullOrEmpty(_renameError))
            EditorGUILayout.HelpBox(_renameError, MessageType.Warning);
        EditorGUILayout.LabelField("名稱就是 ID；改名不會更新舊存檔或程式中的 ID 字串。", EditorStyles.wordWrappedMiniLabel);
        using (new EditorGUI.DisabledScope(playing))
        using (var serialized = new SerializedObject(_selected))
        {
            serialized.Update();
            EditorGUILayout.PropertyField(serialized.FindProperty("TitleTextKey"), new GUIContent("標題文字 Key"));
            EditorGUILayout.PropertyField(serialized.FindProperty("DescriptionTextKey"), new GUIContent("說明文字 Key"));
            EditorGUILayout.PropertyField(serialized.FindProperty("RequiredCount"), new GUIContent("目標成功次數"));
            EditorGUILayout.PropertyField(serialized.FindProperty("OncePerDay"), new GUIContent("每天最多計次一次"));
            EditorGUILayout.PropertyField(serialized.FindProperty("InitiallyActive"), new GUIContent("新遊戲時啟動"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serialized.FindProperty("Rewards"), new GUIContent("完成後領取的獎勵"), true);
            if (serialized.ApplyModifiedProperties()) AssetDatabase.SaveAssetIfDirty(_selected);
        }
        EditorGUILayout.LabelField("修改即時保存，支援 Undo。獎勵數值填正數增加、負數扣除；貼紙直接改實際持有量。Flag 開啟時可設定存活週期，關閉時直接移除。", EditorStyles.wordWrappedMiniLabel);
        if (string.IsNullOrWhiteSpace(_selected.QuestID) || _selected.QuestID != _selected.QuestID.Trim())
            EditorGUILayout.HelpBox("請設定有效的 SO 名稱，且頭尾不可有空白。", MessageType.Warning);
        if (_selected.RequiredCount < 1)
            EditorGUILayout.HelpBox("目標成功次數必須至少為 1。", MessageType.Warning);
    }

    private void RenameTask(string newName)
    {
        if (_selected == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
        _renameErrorTarget = _selected;
        _renameError = null;
        if (newName == _selected.name) return;
        if (string.IsNullOrWhiteSpace(newName) || newName != newName.Trim()
            || newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || newName.EndsWith(".", StringComparison.Ordinal)
            || newName.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
        {
            _renameError = "請輸入有效檔名，不含副檔名、頭尾空白或路徑符號。";
            return;
        }
        RefreshAssets();
        if (_allTasks.Exists(task => task != _selected && task.QuestID == newName))
        {
            _renameError = $"已有同名 Quest：{newName}。不同資料夾的任務也不能使用相同 ID。";
            return;
        }
        string path = AssetDatabase.GetAssetPath(_selected);
        if (string.IsNullOrEmpty(path))
        {
            _renameError = "請先將任務儲存為 SO 資產。";
            return;
        }
        string error = AssetDatabase.RenameAsset(path, newName);
        if (!string.IsNullOrEmpty(error))
        {
            _renameError = error;
            return;
        }
        AssetDatabase.SaveAssetIfDirty(_selected);
        _membershipMessage = null;
        RefreshAssets();
    }

    private void DrawProgress()
    {
        EditorGUILayout.LabelField("目前遊戲進度 · 存檔資料（唯讀）", EditorStyles.boldLabel);
        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("進入 Play Mode 後顯示目前遊戲的進度。尚未執行時不顯示虛構的完成或發獎狀態。", MessageType.Info);
            return;
        }
        if (_service == null || _model == null)
        {
            EditorGUILayout.HelpBox("等待 GameStatusService 初始化 Quest。", MessageType.Info);
            return;
        }
        if (_model.ConfigurationError != null || _model.LoadError != null)
        {
            EditorGUILayout.HelpBox(_model.LoadError ?? _model.ConfigurationError, MessageType.Error);
            return;
        }

        QuestDatabase runtimeDatabase;
        using (var serialized = new SerializedObject(_service))
            runtimeDatabase = serialized.FindProperty("questDatabase").objectReferenceValue as QuestDatabase;
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField("遊戲使用的資料庫", runtimeDatabase, typeof(QuestDatabase), false);
        if (runtimeDatabase == null || _selected == null || runtimeDatabase.Tasks == null || !runtimeDatabase.Tasks.Contains(_selected))
        {
            EditorGUILayout.HelpBox("請確認 GameStatusService 已指定這份資料庫，且任務已加入清單。", MessageType.Info);
            return;
        }
        var info = _model.GetDefinition(_selected.QuestID);
        var progress = _model.GetProgress(_selected.QuestID);
        if (info == null || progress == null)
        {
            EditorGUILayout.HelpBox("執行中的 Model 沒有這筆任務；若剛修改設定，請重新進入 Play Mode。", MessageType.Warning);
            return;
        }
        int day = _service.Time.DayIndex;
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.Toggle("目前已啟動", progress.IsActive);
            EditorGUILayout.IntField("目前成功次數", progress.CurrentCount);
            EditorGUILayout.IntField("執行中的目標次數", info.RequiredCount);
            EditorGUILayout.Toggle("已完成", progress.IsCompleted);
            EditorGUILayout.Toggle("已發獎勵", progress.IsRewarded);
            EditorGUILayout.Toggle("清單中還有待領獎勵", _model.HasUnclaimedRewards);
            EditorGUILayout.IntField("目前遊戲日", day);
            EditorGUILayout.TextField("上次成功計次日", progress.LastPerformedDay < 0 ? "尚未計次" : progress.LastPerformedDay.ToString());
            EditorGUILayout.Toggle("每日限制生效中", info.OncePerDay && progress.LastPerformedDay >= day);
        }
        var rect = GUILayoutUtility.GetRect(1, 22, GUILayout.ExpandWidth(true));
        EditorGUI.ProgressBar(rect, (float)progress.CurrentCount / info.RequiredCount, $"{progress.CurrentCount} / {info.RequiredCount}");
        EditorGUILayout.HelpBox("完成不會自動發獎。呼叫 QuestRewards.ClaimNextReward() 才領取第一筆，並記錄「已發獎勵」；進度與獎勵數值隨原有存檔流程保存。", MessageType.Info);
    }

    private bool EnsureDatabase()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return false;
        RefreshAssets();
        if (_databaseError != null) return false;
        if (_database != null) return true;
        EnsureQuestFolder();
        _database = CreateInstance<QuestDatabase>();
        AssetDatabase.CreateAsset(_database, AssetDatabase.GenerateUniqueAssetPath(QuestFolder + "/QuestDatabase.asset"));
        AssetDatabase.SaveAssetIfDirty(_database);
        return true;
    }

    private static void EnsureQuestFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(QuestFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "Quest");
    }

    private void CreateTask()
    {
        if (!EnsureDatabase()) return;
        EnsureQuestFolder();
        // 名稱同時是 ID；新名稱也避開其他資料夾內已存在的 Quest。
        string path = QuestFolder + "/Quest_New.asset";
        int suffix = 1;
        while (AssetDatabase.LoadMainAssetAtPath(path) != null
            || _allTasks.Exists(task => task.QuestID == Path.GetFileNameWithoutExtension(path)))
            path = QuestFolder + "/Quest_New_" + suffix++ + ".asset";
        var definition = CreateInstance<QuestDefinition>();
        definition.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(definition, path);
        AssetDatabase.SaveAssetIfDirty(definition);
        AddToDatabase(new[] { definition });
        RequestSelection(definition);
        _search = "";
        RefreshAssets();
        EditorGUIUtility.PingObject(definition);
    }

}
#endif
