using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>左側 ID／選項導覽，右側條件與事件編輯；場景綁定由 Inspector 設定。</summary>
public sealed class ShootButtonGroupEditorWindow : EditorWindow
{
    [SerializeField] private ShootButtonGroup controller;
    [SerializeField] private ShootButtonGroup editController;
    [SerializeField] private string editControllerId;
    private bool changingPlayMode;
    [SerializeField] private int selectedGroup = -1;
    [SerializeField] private int selectedOption = -1;
    private readonly HashSet<int> collapsed = new HashSet<int>();
    [SerializeField] private Vector2 groupScroll, detailScroll;
    [SerializeField] private bool showUnityFunctions;
    private SerializedObject data;
    private readonly Dictionary<string, FilteredUnityEventDrawer> drawers = new Dictionary<string, FilteredUnityEventDrawer>();
    private static readonly Color ActiveColor = new Color(0.18f, 0.65f, 0.34f, 0.6f);
    private static readonly Color SelectedColor = new Color(0.25f, 0.65f, 1f);
    private static Snapshot eventClipboard, singleClipboard;
    private bool Locked => EditorApplication.isPlayingOrWillChangePlaymode;

    private sealed class Value
    {
        public string Path;
        public SerializedPropertyType Type;
        public object Data;
        public bool IsArray;
    }

    private sealed class Snapshot
    {
        public readonly List<Value> Values = new List<Value>();
        public string Label;
        public bool FromPlay;
    }

    [MenuItem("Tools/NHK/ShootButtonGroup編輯器")]
    public static void Open()
    {
        var window = GetWindow<ShootButtonGroupEditorWindow>("ShootButtonGroup編輯器");
        var selected = SelectedController();
        if (selected != null) window.SetController(selected);
        window.Show();
    }

    [MenuItem("CONTEXT/ShootButtonGroup/開啟 ShootButtonGroup編輯器")]
    private static void OpenFromComponent(MenuCommand command)
    {
        var window = GetWindow<ShootButtonGroupEditorWindow>("ShootButtonGroup編輯器");
        window.SetController(command.context as ShootButtonGroup);
        window.Show();
    }

    private void OnEnable()
    {
        minSize = new Vector2(960, 540);
        ReleaseData();
        Undo.undoRedoPerformed += OnUndo;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        changingPlayMode = EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying;
        if (!Locked && editController == null && string.IsNullOrEmpty(editControllerId) && controller != null)
            RememberEditController(controller);
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndo;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        ReleaseData();
    }

    private void ReleaseData() { drawers.Clear(); data?.Dispose(); data = null; }
    private void OnUndo() { ReleaseData(); selectedGroup = selectedOption = -1; collapsed.Clear(); Repaint(); }
    private void OnInspectorUpdate()
    {
        ResolveController();
        Repaint();
    }
    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            data?.ApplyModifiedProperties();
            if (controller != null) RememberEditController(controller);
        }
        changingPlayMode = state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode;
        ReleaseData();
        if (state == PlayModeStateChange.EnteredEditMode) controller = null;
        ResolveController();
        Repaint();
    }

    private void RememberEditController(ShootButtonGroup target)
    {
        editController = target;
        editControllerId = target != null ? GlobalObjectId.GetGlobalObjectIdSlow(target).ToString() : string.Empty;
    }

    private void ResolveController()
    {
        if (changingPlayMode) return;
        if (Application.isPlaying)
        {
            // 跟隨場景中的執行期單例，保留試玩前的編輯目標。
            BindController(ShootButtonGroup.Instance);
            return;
        }
        if (Locked) return;
        if (editController == null && !string.IsNullOrEmpty(editControllerId)
            && GlobalObjectId.TryParse(editControllerId, out var id))
            editController = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as ShootButtonGroup;
        // 已指定的目標暫時不存在時等待它恢復，不改接其他控制器。
        if (editController == null && string.IsNullOrEmpty(editControllerId))
        {
            ShootButtonGroup candidate = null;
            foreach (var item in Resources.FindObjectsOfTypeAll<ShootButtonGroup>())
            {
                if (EditorUtility.IsPersistent(item) || !item.gameObject.scene.IsValid() || !item.gameObject.scene.isLoaded
                    || UnityEditor.SceneManagement.EditorSceneManager.IsPreviewSceneObject(item.gameObject)) continue;
                if (candidate != null) { candidate = null; break; }
                candidate = item;
            }
            if (candidate != null) RememberEditController(candidate);
        }
        BindController(editController);
    }

    private void BindController(ShootButtonGroup target)
    {
        if (ReferenceEquals(controller, target)) return;
        ReleaseData();
        controller = target;
    }

    private static ShootButtonGroup SelectedController() => Selection.activeGameObject == null
        ? null : Selection.activeGameObject.GetComponent<ShootButtonGroup>();

    private void OnSelectionChange()
    {
        // 編輯目標保持固定，拖入事件物件時不會跟著跳走。
        if (!Locked && !changingPlayMode && controller == null && string.IsNullOrEmpty(editControllerId))
            SetController(SelectedController());
        Repaint();
    }

    private void SetController(ShootButtonGroup next)
    {
        if (Locked || changingPlayMode) { ResolveController(); return; }
        if (controller != null) data?.ApplyModifiedProperties();
        RememberEditController(next);
        ReleaseData();
        controller = next;
        selectedGroup = selectedOption = -1;
        collapsed.Clear();
        groupScroll = detailScroll = Vector2.zero;
        Repaint();
    }

    private void OnGUI()
    {
        if (changingPlayMode)
        {
            EditorGUILayout.HelpBox("正在切換試玩模式，完成後會自動連接控制器。", MessageType.Info);
            return;
        }
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label(Application.isPlaying ? "執行中單例" : "編輯目標", GUILayout.Width(75));
            using (new EditorGUI.DisabledScope(Locked))
            {
                var next = (ShootButtonGroup)EditorGUILayout.ObjectField(controller, typeof(ShootButtonGroup), true);
                if (next != controller) { SetController(next); GUIUtility.ExitGUI(); }
                if (GUILayout.Button("使用選取物件", EditorStyles.toolbarButton, GUILayout.Width(100)))
                { SetController(SelectedController()); GUIUtility.ExitGUI(); }
            }
            using (new EditorGUI.DisabledScope(controller == null))
                if (GUILayout.Button("定位", EditorStyles.toolbarButton, GUILayout.Width(45)))
                    EditorGUIUtility.PingObject(controller.gameObject);
            showUnityFunctions = GUILayout.Toggle(showUnityFunctions, new GUIContent("顯示 Unity 內建功能", "勾選後使用完整事件選單，既有綁定不受影響。"), EditorStyles.toolbarButton, GUILayout.Width(145));
        }
        if (controller == null)
        {
            ReleaseData();
            EditorGUILayout.HelpBox(Application.isPlaying
                ? "等待 ShootButtonGroup 單例初始化，視窗會自動連接。"
                : "等待原編輯目標恢復。也可拖入 ShootButtonGroup，或按「使用選取物件」指定目標。", MessageType.Info);
            return;
        }
        if (data == null || data.targetObject != controller) { ReleaseData(); data = new SerializedObject(controller); }
        data.Update();
        var groups = data.FindProperty("groups");
        EditorGUILayout.LabelField("左側選擇 ID／選項，右側編輯。綠底＝目前使用的 ID；藍框＝目前選取。", EditorStyles.miniLabel);
        string defaultID = data.FindProperty("defaultID").stringValue;
        if (!string.IsNullOrWhiteSpace(defaultID))
        {
            bool found = false;
            for (int i = 0; i < groups.arraySize; i++)
                if (string.Equals(groups.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue,
                    defaultID, StringComparison.Ordinal)) { found = true; break; }
            if (!found) EditorGUILayout.HelpBox("預設 ID 找不到對應分組，初始化後按鈕將保持不可用。", MessageType.Warning);
        }
        if (Locked)
            EditorGUILayout.HelpBox("目前使用的組：" + (controller == ShootButtonGroup.Instance ? controller.CurrentID ?? "無" : "此元件不是執行中的實例")
                + "。綠底＝遊戲目前使用；藍框＝編輯器選取。試玩期間設定唯讀。", MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(Mathf.Clamp(position.width * 0.31f, 270, 400))))
                DrawGroups(groups);
            GUILayout.Space(8);
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            using (var scroll = new EditorGUILayout.ScrollViewScope(detailScroll))
            {
                detailScroll = scroll.scrollPosition;
                float oldWidth = EditorGUIUtility.labelWidth;
                bool oldWide = EditorGUIUtility.wideMode;
                try
                {
                    EditorGUIUtility.labelWidth = 135;
                    EditorGUIUtility.wideMode = true;
                    if (selectedGroup >= 0 && selectedGroup < groups.arraySize) DrawGroup(groups);
                    else EditorGUILayout.HelpBox("請從左側選擇 ID 或選項，或新增分組。", MessageType.Info);
                }
                finally { EditorGUIUtility.labelWidth = oldWidth; EditorGUIUtility.wideMode = oldWide; }
            }
        }
        data.ApplyModifiedProperties();
    }

    private void DrawGroups(SerializedProperty groups)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(Locked))
                if (GUILayout.Button("＋ 新增 ID")) AddGroup(groups);
            if (GUILayout.Button("全部展開")) { collapsed.Clear(); Commit(); }
            if (GUILayout.Button("收合"))
            {
                for (int i = 0; i < groups.arraySize; i++) collapsed.Add(i);
                Commit();
            }
        }
        using (var scroll = new EditorGUILayout.ScrollViewScope(groupScroll))
        {
            groupScroll = scroll.scrollPosition;
            bool activeDrawn = false;
            for (int i = 0; i < groups.arraySize; i++)
            {
                var group = groups.GetArrayElementAtIndex(i);
                var options = group.FindPropertyRelative("options");
                string id = group.FindPropertyRelative("id").stringValue;
                bool active = !activeDrawn && Application.isPlaying && controller == ShootButtonGroup.Instance
                    && !string.IsNullOrWhiteSpace(id) && string.Equals(id, controller.CurrentID, StringComparison.Ordinal);
                if (active) activeDrawn = true;
                string name = string.IsNullOrWhiteSpace(id) ? "未命名 ID " + (i + 1) : id;
                Rect row = EditorGUILayout.GetControlRect(false, 32);
                PaintRow(row, selectedGroup == i && selectedOption < 0, active);
                bool expanded = !collapsed.Contains(i);
                bool next = EditorGUI.Foldout(new Rect(row.x + 4, row.y + 7, 16, 18), expanded, GUIContent.none);
                if (expanded != next)
                {
                    if (next) collapsed.Remove(i); else collapsed.Add(i);
                    Commit();
                }
                if (GUI.Button(new Rect(row.x + 24, row.y, row.width - 28, row.height),
                    new GUIContent(name + "  (" + (options.arraySize + 1) + ")" + (active ? " [目前]" : ""), name),
                    EditorStyles.boldLabel)) Select(i, -1);
                if (!expanded) continue;
                for (int b = 0; b <= options.arraySize; b++)
                {
                    var option = b == 0 ? group : options.GetArrayElementAtIndex(b - 1);
                    Rect item = EditorGUILayout.GetControlRect(false, 44);
                    item.xMin += 18;
                    PaintRow(item, selectedGroup == i && selectedOption == b, false);
                    string caption = (b + 1) + ". " + OptionName(option, b) + (b == 0 ? " [預設]" : "");
                    GUI.Label(new Rect(item.x + 8, item.y + 3, item.width - 16, 20), new GUIContent(caption, caption));
                    string description = "Icon: " + option.FindPropertyRelative("iconName").stringValue
                        + (option.FindPropertyRelative("useNgClickEvent").boolValue ? " · NG 分流開啟" : " · NG 分流關閉");
                    GUI.Label(new Rect(item.x + 8, item.y + 24, item.width - 16, 18), description, EditorStyles.miniLabel);
                    if (GUI.Button(item, GUIContent.none, GUIStyle.none)) Select(i, b);
                }
                using (new EditorGUI.DisabledScope(Locked))
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("＋ 新增選項", EditorStyles.miniButton)) AddOption(group, i);
                    var source = GetSelectedButton();
                    using (new EditorGUI.DisabledScope(source == null))
                        if (GUILayout.Button(new GUIContent("＋ 從選中按鈕複製", source == null
                            ? "請先選中一個來源選項。"
                            : "將「" + OptionName(source, selectedOption) + "」的完整設定與事件複製到此 ID 末尾。"),
                            EditorStyles.miniButton)) CopySelectedButton(options, i);
                }
            }
        }
    }

    private static string OptionName(SerializedProperty option, int index)
    {
        string label = option.FindPropertyRelative("label").stringValue;
        if (!string.IsNullOrWhiteSpace(label)) return label;
        string key = option.FindPropertyRelative("textKey").stringValue;
        return string.IsNullOrWhiteSpace(key) ? "選項 " + (index + 1) : key;
    }

    private static void PaintRow(Rect rect, bool selected, bool active)
    {
        EditorGUI.DrawRect(rect, active ? ActiveColor : new Color(0.4f, 0.5f, 0.6f, 0.09f));
        if (!selected) return;
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2), SelectedColor);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2, rect.width, 2), SelectedColor);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2, rect.height), SelectedColor);
        EditorGUI.DrawRect(new Rect(rect.xMax - 2, rect.y, 2, rect.height), SelectedColor);
    }

    private void Select(int group, int option)
    {
        selectedGroup = group;
        selectedOption = option;
        detailScroll = Vector2.zero;
        Commit();
    }

    private void AddOption(SerializedProperty group, int groupIndex)
    {
        var options = group.FindPropertyRelative("options");
        int index = options.arraySize++;
        ResetOption(options.GetArrayElementAtIndex(index));
        collapsed.Remove(groupIndex);
        Select(groupIndex, index + 1);
    }

    private SerializedProperty GetSelectedButton()
    {
        if (data == null || selectedGroup < 0 || selectedOption < 0) return null;
        var groups = data.FindProperty("groups");
        if (selectedGroup >= groups.arraySize) return null;
        var group = groups.GetArrayElementAtIndex(selectedGroup);
        if (selectedOption == 0) return group;
        var options = group.FindPropertyRelative("options");
        return selectedOption <= options.arraySize ? options.GetArrayElementAtIndex(selectedOption - 1) : null;
    }

    private void CopySelectedButton(SerializedProperty options, int groupIndex)
    {
        if (Locked || changingPlayMode) return;
        var source = GetSelectedButton();
        if (source == null) return;
        // 先取快照再擴充陣列，支援同組與跨 ID 複製，保留完整 UnityEvent 參數及物件參照。
        // 第一項與 ID 共用資料容器，只複製選項內容，不帶入 ID 或其他選項。
        var snapshot = Capture(source, OptionName(source, selectedOption), true);
        int index = options.arraySize++;
        Apply(options.GetArrayElementAtIndex(index), snapshot);
        collapsed.Remove(groupIndex);
        Select(groupIndex, index + 1);
    }

    private void DrawGroup(SerializedProperty groups)
    {
        var group = groups.GetArrayElementAtIndex(selectedGroup);
        var options = group.FindPropertyRelative("options");
        string id = group.FindPropertyRelative("id").stringValue;
        if (selectedOption < 0 || selectedOption > options.arraySize)
        {
            EditorGUILayout.LabelField("ID / " + id, EditorStyles.largeLabel);
            using (new EditorGUI.DisabledScope(Locked))
            {
                EditorGUILayout.PropertyField(group.FindPropertyRelative("id"), new GUIContent("組名／ID"));
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("複製此組")) DuplicateGroup(groups, group);
                    if (GUILayout.Button("刪除此組"))
                    {
                        groups.DeleteArrayElementAtIndex(selectedGroup);
                        selectedGroup = selectedOption = -1;
                        collapsed.Clear();
                        Commit();
                    }
                    if (GUILayout.Button("新增選項")) AddOption(group, selectedGroup);
                }
            }
            if (string.IsNullOrWhiteSpace(id)) EditorGUILayout.HelpBox("組名／ID 不可空白。", MessageType.Warning);
            for (int i = 0; i < groups.arraySize; i++)
                if (i != selectedGroup && groups.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue == id)
                { EditorGUILayout.HelpBox("ID 重複，遊戲切換時只會使用第一組。", MessageType.Warning); break; }
            EditorGUILayout.HelpBox("共有 " + (options.arraySize + 1)
                + " 個選項。請從左側選取要編輯的選項。第一項是預設自動播放；其他選項可調整順位。", MessageType.None);
            EditorGUILayout.HelpBox("中央按鈕依顯示條件動態生成；Prefab 與 Content 請在 Inspector 指定。", MessageType.None);
            return;
        }

        var selected = selectedOption == 0 ? group : options.GetArrayElementAtIndex(selectedOption - 1);
        EditorGUILayout.LabelField(id + " / " + OptionName(selected, selectedOption), EditorStyles.largeLabel);
        if (selectedOption == 0)
            EditorGUILayout.HelpBox("第一項為預設自動播放選項，沿用原 ID 的事件。", MessageType.None);
        else
        {
            int index = selectedOption - 1;
            using (new EditorGUI.DisabledScope(Locked))
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(index == 0))
                    if (GUILayout.Button("上移"))
                    { options.MoveArrayElement(index, index - 1); selectedOption--; Commit(); }
                using (new EditorGUI.DisabledScope(index + 1 >= options.arraySize))
                    if (GUILayout.Button("下移"))
                    { options.MoveArrayElement(index, index + 1); selectedOption++; Commit(); }
                if (GUILayout.Button("刪除此選項"))
                { options.DeleteArrayElementAtIndex(index); selectedOption = -1; Commit(); }
            }
        }
        DrawOption(selected, id + " / " + (selectedOption + 1), "選項設定");
    }

    private void DrawOption(SerializedProperty option, string id, string title)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        string[] fields = { "label", "textKey", "iconName", "visibilityFlag", "invertVisibility",
            "interactableFlag", "invertInteractable", "ngFlag", "invertNgFlag", "useNgClickEvent" };
        string[] labels = { "備註", "文字 Key", "Icon", "顯示條件", "反轉顯示條件",
            "可點條件", "反轉可點條件", "NG 條件", "反轉 NG 條件", "啟用 NG 點擊事件" };
        using (new EditorGUI.DisabledScope(Locked))
        {
            for (int i = 0; i < fields.Length; i++)
            {
                if (i == 0 || i == 3 || i == 7 || i == 9)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(i == 0 ? "名稱與 Icon" : i == 3 ? "顯示與可點條件"
                        : i == 7 ? "NG 條件" : "事件分流", EditorStyles.boldLabel);
                }
                EditorGUILayout.PropertyField(option.FindPropertyRelative(fields[i]), new GUIContent(labels[i]));
            }
        }
        DrawAction(option, id, "onClick", "一般 Shoot 事件");
        if (option.FindPropertyRelative("useNgClickEvent").boolValue)
            DrawAction(option, id, "onNgClick", "NG Shoot 事件");
    }

    private static void ResetOption(SerializedProperty option)
    {
        option.FindPropertyRelative("label").stringValue = string.Empty;
        option.FindPropertyRelative("textKey").stringValue = string.Empty;
        option.FindPropertyRelative("iconName").stringValue = "Heart";
        foreach (string name in new[] { "visibilityFlag", "interactableFlag", "ngFlag" })
            option.FindPropertyRelative(name).objectReferenceValue = null;
        foreach (string name in new[] { "invertVisibility", "invertInteractable", "useNgClickEvent" })
            option.FindPropertyRelative(name).boolValue = false;
        option.FindPropertyRelative("invertNgFlag").boolValue = true;
        option.FindPropertyRelative("onClick.m_PersistentCalls.m_Calls").ClearArray();
        option.FindPropertyRelative("onNgClick.m_PersistentCalls.m_Calls").ClearArray();
    }

    private void DrawAction(SerializedProperty action, string id, string field, string title)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(title, EditorStyles.largeLabel);
            var property = action.FindPropertyRelative(field);
            string source = controller.name + " / " + id;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("複製全部事件"))
                {
                    eventClipboard = Capture(property, source);
                    Commit();
                }
                using (new EditorGUI.DisabledScope(!CanPaste(eventClipboard)))
                    if (GUILayout.Button("貼上全部（覆蓋）")) Paste(property, eventClipboard, false);
            }
            if (!drawers.TryGetValue(property.propertyPath, out FilteredUnityEventDrawer drawer))
            {
                drawer = new FilteredUnityEventDrawer(this, () => controller, () => showUnityFunctions, () => !Locked && !changingPlayMode, item => drawers.ContainsValue(item));
                drawers.Add(property.propertyPath, drawer);
            }
            var label = new GUIContent("Shoot 事件");
            Rect eventRect = EditorGUILayout.GetControlRect(false, drawer.GetPropertyHeight(property, label));
            using (new EditorGUI.DisabledScope(Locked)) drawer.OnGUI(eventRect, property, label);
            var calls = property.FindPropertyRelative("m_PersistentCalls.m_Calls");
            int selected = drawer.SelectedIndex;
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(selected < 0 || selected >= calls.arraySize))
                    if (GUILayout.Button("複製所選事件"))
                    {
                        singleClipboard = Capture(calls.GetArrayElementAtIndex(selected), source + " / 第 " + (selected + 1) + " 項");
                        Commit();
                    }
                using (new EditorGUI.DisabledScope(!CanPaste(singleClipboard)))
                    if (GUILayout.Button("貼上單項（新增）")) Paste(calls, singleClipboard, true);
            }
            using (new EditorGUI.DisabledScope(Locked || selected < 0 || selected >= calls.arraySize))
                if (GUILayout.Button("刪除所選事件"))
                {
                    calls.DeleteArrayElementAtIndex(selected);
                    Commit();
                }
            EditorGUILayout.LabelField("點選事件列後，可複製或刪除單一項目。", EditorStyles.wordWrappedMiniLabel);
            if (eventClipboard != null) EditorGUILayout.LabelField("全部事件剪貼簿：" + eventClipboard.Label, EditorStyles.wordWrappedMiniLabel);
            if (singleClipboard != null) EditorGUILayout.LabelField("單項剪貼簿：" + singleClipboard.Label, EditorStyles.wordWrappedMiniLabel);
        }
    }

    private void AddGroup(SerializedProperty groups)
    {
        if (Locked) return;
        string id = UniqueName(groups, "新分組");
        int index = groups.arraySize++;
        var group = groups.GetArrayElementAtIndex(index);
        group.FindPropertyRelative("id").stringValue = id;
        // Unity 擴充清單可能複製上一組，新增 ID 時明確清空事件。
        ResetOption(group);
        group.FindPropertyRelative("options").ClearArray();
        collapsed.Remove(index);
        Select(index, -1);
    }

    private void DuplicateGroup(SerializedProperty groups, SerializedProperty source)
    {
        if (Locked) return;
        string id = UniqueName(groups, source.FindPropertyRelative("id").stringValue + "_副本");
        var snapshot = Capture(source, id);
        int index = groups.arraySize++;
        var copy = groups.GetArrayElementAtIndex(index);
        Apply(copy, snapshot);
        copy.FindPropertyRelative("id").stringValue = id;
        collapsed.Remove(index);
        Select(index, -1);
    }

    private static string UniqueName(SerializedProperty groups, string prefix)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < groups.arraySize; i++) names.Add(groups.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue);
        string name = prefix;
        int suffix = 2;
        while (names.Contains(name)) name = prefix + suffix++;
        return name;
    }

    private bool CanPaste(Snapshot snapshot) => !Locked && snapshot != null && !snapshot.FromPlay;

    private void Paste(SerializedProperty destination, Snapshot snapshot, bool append)
    {
        if (!CanPaste(snapshot)) return;
        // 貼上前完整檢查物件參照，避免跨場景或已卸載的物件留下無效事件。
        foreach (var value in snapshot.Values)
        {
            if (value.Type != SerializedPropertyType.ObjectReference || ReferenceEquals(value.Data, null)) continue;
            var reference = (UnityEngine.Object)value.Data;
            if (reference == null) { ShowNotification(new GUIContent("來源物件已失效，請重新複製事件。")); return; }
            if (EditorUtility.IsPersistent(reference)) continue;
            var referencedObject = reference as GameObject;
            if (reference is Component component) referencedObject = component.gameObject;
            if (EditorUtility.IsPersistent(controller) || referencedObject == null || referencedObject.scene != controller.gameObject.scene)
            { ShowNotification(new GUIContent("事件含其他場景或 Prefab Stage 參照，無法貼入目前目標。")); return; }
        }
        if (append)
        {
            int index = destination.arraySize++;
            destination = destination.GetArrayElementAtIndex(index);
        }
        Apply(destination, snapshot);
        Commit();
    }

    // 擷取值與 Unity 物件參照，避免來源變動或陣列擴充使剪貼簿失效。
    private static Snapshot Capture(SerializedProperty property, string label, bool optionOnly = false)
    {
        var result = new Snapshot { Label = label, FromPlay = EditorApplication.isPlayingOrWillChangePlaymode };
        var iterator = property.Copy();
        var end = property.GetEndProperty();
        bool children = true;
        while (iterator.Next(children) && !SerializedProperty.EqualContents(iterator, end))
        {
            if (optionOnly && iterator.depth == property.depth + 1
                && (iterator.name == "id" || iterator.name == "options"))
            {
                children = false;
                continue;
            }
            children = iterator.propertyType == SerializedPropertyType.Generic;
            var value = new Value { Path = iterator.propertyPath.Substring(property.propertyPath.Length + 1),
                Type = iterator.propertyType, IsArray = iterator.isArray && iterator.propertyType != SerializedPropertyType.String };
            if (value.IsArray) value.Data = iterator.arraySize;
            else switch (value.Type)
            {
                case SerializedPropertyType.Generic:
                case SerializedPropertyType.ArraySize: continue;
                case SerializedPropertyType.Integer: value.Data = iterator.longValue; break;
                case SerializedPropertyType.Boolean: value.Data = iterator.boolValue; break;
                case SerializedPropertyType.Float: value.Data = iterator.doubleValue; break;
                case SerializedPropertyType.String: value.Data = iterator.stringValue; break;
                case SerializedPropertyType.Enum: value.Data = iterator.intValue; break;
                case SerializedPropertyType.ObjectReference: value.Data = iterator.objectReferenceValue; break;
                default: throw new NotSupportedException("不支援的事件欄位：" + value.Type);
            }
            result.Values.Add(value);
        }
        return result;
    }

    private static void Apply(SerializedProperty property, Snapshot snapshot)
    {
        foreach (var value in snapshot.Values)
        {
            var destination = property.FindPropertyRelative(value.Path);
            if (value.IsArray) destination.arraySize = (int)value.Data;
            else switch (value.Type)
            {
                case SerializedPropertyType.Integer: destination.longValue = (long)value.Data; break;
                case SerializedPropertyType.Boolean: destination.boolValue = (bool)value.Data; break;
                case SerializedPropertyType.Float: destination.doubleValue = (double)value.Data; break;
                case SerializedPropertyType.String: destination.stringValue = (string)value.Data; break;
                case SerializedPropertyType.Enum: destination.intValue = (int)value.Data; break;
                case SerializedPropertyType.ObjectReference: destination.objectReferenceValue = (UnityEngine.Object)value.Data; break;
            }
        }
    }

    private void Commit()
    {
        // SerializedObject 負責 Undo、場景 dirty 與 Prefab override。
        data.ApplyModifiedProperties();
        drawers.Clear();
        GUI.FocusControl(null);
        Repaint();
        GUIUtility.ExitGUI();
    }
}
