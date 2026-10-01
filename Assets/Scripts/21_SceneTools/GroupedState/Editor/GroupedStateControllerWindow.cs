using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>分組方塊總覽與選取項目編輯面板。</summary>
public sealed class GroupedStateControllerWindow : EditorWindow
{
    [SerializeField] private GroupedStateController controller;
    [SerializeField] private GroupedStateController editController;
    [SerializeField] private string editControllerId;
    [SerializeField] private GroupedStateController runtimeController;
    [SerializeField] private string runtimeControllerId;
    private bool changingPlayMode;
    [SerializeField] private Vector2 boardScroll, detailScroll;
    [SerializeField] private int selectedGroup = -1, selectedState = -1;
    [SerializeField] private string search = "";
    [SerializeField] private bool showChecks;
    private SerializedObject data;
    private static List<EventValue> clipboard;
    private static string clipboardLabel;
    private static bool clipboardFromPlay;
    private static List<EventValue> selectedEventClipboard;
    private static string selectedEventClipboardLabel;
    private static bool selectedEventClipboardFromPlay;
    private readonly Dictionary<string, SelectableEventDrawer> eventDrawers = new Dictionary<string, SelectableEventDrawer>();
    private static readonly Color Active = new Color(0.18f, 0.65f, 0.34f, 0.55f);
    private static readonly Color Exited = new Color(0.9f, 0.5f, 0.12f, 0.5f);
    private static readonly Color Selected = new Color(0.25f, 0.65f, 1f);
    private bool Locked => EditorApplication.isPlayingOrWillChangePlaymode;

    private sealed class EventValue
    {
        public string Path;
        public SerializedPropertyType Type;
        public object Value;
        public bool IsArray;
    }

    // 沿用原生事件欄位與函式選單，只追蹤使用者實際選中的清單項目。
    private sealed class SelectableEventDrawer : UnityEventDrawer
    {
        private ReorderableList eventList;
        private bool hasSelection;
        public int SelectedIndex => hasSelection && eventList != null ? eventList.index : -1;

        protected override void SetupReorderableList(ReorderableList list)
        {
            base.SetupReorderableList(list);
            eventList = list;
            hasSelection = false;
        }

        protected override void OnSelectEvent(ReorderableList list)
        {
            base.OnSelectEvent(list);
            hasSelection = true;
        }

        protected override void OnReorderEvent(ReorderableList list)
        {
            base.OnReorderEvent(list);
            hasSelection = true;
        }
    }

    [MenuItem("Tools/NHK/分組狀態編輯器")]
    public static void Open()
    {
        var window = GetWindow<GroupedStateControllerWindow>("分組狀態編輯器");
        var selected = SelectedController();
        if (selected != null) window.SetController(selected);
        window.Show();
    }

    [MenuItem("CONTEXT/GroupedStateController/開啟分組狀態編輯視窗")]
    private static void OpenFromComponent(MenuCommand command)
    {
        var window = GetWindow<GroupedStateControllerWindow>("分組狀態編輯器");
        window.SetController(command.context as GroupedStateController);
        window.Show();
    }

    private void OnEnable()
    {
        minSize = new Vector2(900, 620);
        ReleaseData();
        Undo.undoRedoPerformed += OnUndo;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        changingPlayMode = EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying;
        if (!Locked && editController == null && string.IsNullOrEmpty(editControllerId) && controller != null)
            RememberEditController(controller);
        if (Application.isPlaying && !changingPlayMode && runtimeController == null && IsSceneController(controller))
            runtimeController = controller;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndo;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        ReleaseData();
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            data?.ApplyModifiedProperties();
            if (controller != null) RememberEditController(controller);
            // 讓 Unity 重映射場景參照；若重映射失效，再依穩定 ID 找回原元件。
            runtimeController = IsSceneController(controller) ? controller : null;
            runtimeControllerId = runtimeController != null ? editControllerId : string.Empty;
        }
        changingPlayMode = state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode;
        ReleaseData();
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            controller = null;
            runtimeController = null;
            runtimeControllerId = string.Empty;
        }
        ResolveController();
        Repaint();
    }

    private void OnSelectionChange()
    {
        // 有效目標維持固定；失效時才接上目前選取的元件。
        if (!Locked && !changingPlayMode && controller == null && string.IsNullOrEmpty(editControllerId))
            SetController(SelectedController());
        Repaint();
    }

    private void OnUndo()
    {
        eventDrawers.Clear();
        // 結構 Undo 可能改變索引，避免繼續編輯另一項。
        selectedGroup = selectedState = -1;
        Repaint();
    }

    private void OnInspectorUpdate()
    {
        ResolveController();
        Repaint();
    }

    private static bool IsSceneController(GroupedStateController target) => target != null
        && !EditorUtility.IsPersistent(target) && target.gameObject.scene.IsValid() && target.gameObject.scene.isLoaded
        && !UnityEditor.SceneManagement.EditorSceneManager.IsPreviewSceneObject(target.gameObject);

    private void RememberEditController(GroupedStateController target)
    {
        editController = target;
        editControllerId = target != null ? GlobalObjectId.GetGlobalObjectIdSlow(target).ToString() : string.Empty;
    }

    private static GroupedStateController FindController(string objectId)
    {
        if (string.IsNullOrEmpty(objectId) || !GlobalObjectId.TryParse(objectId, out var id)) return null;
        return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GroupedStateController;
    }

    private void ResolveController()
    {
        if (changingPlayMode) return;
        if (Application.isPlaying)
        {
            if (!IsSceneController(runtimeController)) runtimeController = FindController(runtimeControllerId);
            // 分組狀態允許多個控制器，只重連同一物件，不用名稱或第一個搜尋結果猜測。
            BindController(IsSceneController(runtimeController) ? runtimeController : null);
            return;
        }
        if (Locked) return;
        if (editController == null) editController = FindController(editControllerId);
        BindController(editController);
    }

    private void BindController(GroupedStateController target)
    {
        if (ReferenceEquals(controller, target)) return;
        ReleaseData();
        controller = target;
    }
    private static GroupedStateController SelectedController() => Selection.activeGameObject == null
        ? null : Selection.activeGameObject.GetComponent<GroupedStateController>();

    private void ReleaseData() { eventDrawers.Clear(); data?.Dispose(); data = null; }

    private void SetController(GroupedStateController value)
    {
        if (changingPlayMode || (Locked && !Application.isPlaying)) return;
        if (Application.isPlaying)
        {
            // 試玩中手動查看其他元件，不覆寫回到編輯模式時要還原的目標。
            if (value != null && !IsSceneController(value)) return;
            runtimeController = value;
            runtimeControllerId = value != null ? GlobalObjectId.GetGlobalObjectIdSlow(value).ToString() : string.Empty;
        }
        else
        {
            if (controller != null) data?.ApplyModifiedProperties();
            RememberEditController(value);
        }
        // Unity 已銷毀的物件也會等於 null，仍須清掉其舊參照與快取。
        if (controller == value && (controller != null || ReferenceEquals(controller, null))) return;
        ReleaseData();
        controller = value;
        selectedGroup = selectedState = -1;
        search = "";
        boardScroll = detailScroll = Vector2.zero;
        Repaint();
    }

    private void OnGUI()
    {
        if (changingPlayMode)
        {
            EditorGUILayout.HelpBox("正在切換試玩模式，完成後會自動連接原控制器。", MessageType.Info);
            return;
        }
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label(Application.isPlaying ? "試玩目標" : "編輯目標", GUILayout.Width(56));
            var next = (GroupedStateController)EditorGUILayout.ObjectField(controller, typeof(GroupedStateController), true);
            if (next != controller) { SetController(next); GUIUtility.ExitGUI(); }
            if (GUILayout.Button("使用選取物件", EditorStyles.toolbarButton, GUILayout.Width(95)))
            { SetController(SelectedController()); GUIUtility.ExitGUI(); }
            using (new EditorGUI.DisabledScope(controller == null))
                if (GUILayout.Button("定位", EditorStyles.toolbarButton, GUILayout.Width(40)))
                    EditorGUIUtility.PingObject(controller.gameObject);
        }
        if (controller == null)
        {
            ReleaseData();
            EditorGUILayout.HelpBox("等待原控制器恢復，視窗會自動重連。也可拖入 GroupedStateController，或按「使用選取物件」。\n試玩中指定其他元件不會改變原編輯目標。", MessageType.Info);
            return;
        }
        if (data == null || data.targetObject != controller) { ReleaseData(); data = new SerializedObject(controller); }
        data.Update();
        SerializedProperty groups = data.FindProperty("groups");
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("分組總覽", EditorStyles.boldLabel, GUILayout.Width(75));
            search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.Width(200));
            if (GUILayout.Button("清除搜尋", EditorStyles.toolbarButton, GUILayout.Width(70))) search = "";
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(Locked))
                if (GUILayout.Button("＋ 分組", EditorStyles.toolbarButton, GUILayout.Width(65))) AddGroup(groups);
            bool checks = GUILayout.Toggle(showChecks, "判斷事件", EditorStyles.toolbarButton, GUILayout.Width(70));
            if (checks != showChecks) { showChecks = checks; detailScroll = Vector2.zero; GUIUtility.ExitGUI(); }
        }
        EditorGUILayout.LabelField("單擊方塊：編輯　｜　右鍵：測試切換　｜　藍框：選取　綠底：成立　橘底：最後退出", EditorStyles.miniLabel);
        using (var scroll = new EditorGUILayout.ScrollViewScope(boardScroll, GUILayout.Height(Mathf.Max(180, position.height * 0.43f))))
        {
            boardScroll = scroll.scrollPosition;
            bool any = false;
            for (int g = 0; g < groups.arraySize; g++)
            {
                SerializedProperty group = groups.GetArrayElementAtIndex(g);
                if (!Matches(group)) continue;
                any = true;
                DrawGroupRow(group, g);
            }
            if (!any) EditorGUILayout.HelpBox(groups.arraySize == 0 ? "尚無分組，按右上角「＋ 分組」開始。" : "沒有符合搜尋的分組或狀態。", MessageType.Info);
        }
        Rect divider = EditorGUILayout.GetControlRect(false, 3);
        EditorGUI.DrawRect(divider, new Color(0.25f, 0.65f, 1f, 0.5f));
        using (var scroll = new EditorGUILayout.ScrollViewScope(detailScroll))
        {
            detailScroll = scroll.scrollPosition;
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            bool oldWide = EditorGUIUtility.wideMode;
            try
            {
                EditorGUIUtility.labelWidth = 75;
                EditorGUIUtility.wideMode = true;
                DrawDetails(groups);
            }
            finally { EditorGUIUtility.labelWidth = oldLabelWidth; EditorGUIUtility.wideMode = oldWide; }
        }
        if (data.ApplyModifiedProperties()) Repaint();
    }

    private bool Matches(SerializedProperty group)
    {
        if (string.IsNullOrEmpty(search) || group.FindPropertyRelative("GroupName").stringValue.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        SerializedProperty states = group.FindPropertyRelative("States");
        for (int i = 0; i < states.arraySize; i++)
            if (states.GetArrayElementAtIndex(i).FindPropertyRelative("StateName").stringValue.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }

    private void DrawGroupRow(SerializedProperty group, int g)
    {
        string groupName = group.FindPropertyRelative("GroupName").stringValue;
        SerializedProperty states = group.FindPropertyRelative("States");
        string current = "", exited = "";
        bool ready = Application.isPlaying && controller.TryGetStateSnapshot(groupName, out current, out exited);
        const float labelWidth = 174, gap = 7, tileWidth = 146, tileHeight = 86;
        int columns = Mathf.Max(1, Mathf.FloorToInt((position.width - labelWidth - 45) / (tileWidth + gap)));
        int rows = Mathf.Max(1, Mathf.CeilToInt((states.arraySize + 1f) / columns));
        Rect row = GUILayoutUtility.GetRect(0, rows * (tileHeight + gap) + 16, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(row, new Color(0.45f, 0.5f, 0.6f, g % 2 == 0 ? 0.13f : 0.07f));
        var header = new Rect(row.x + 7, row.y + 8, labelWidth - 14, tileHeight);
        string summary = ready ? (current.Length == 0 ? "皆不成立" : "成立：" + current) : states.arraySize + " 個狀態";
        DrawTile(header, groupName, summary, selectedGroup == g && selectedState < 0 && !showChecks,
            new Color(0.3f, 0.45f, 0.65f, 0.2f), () => Select(g, -1), () => ShowTestMenu(groupName, null, states, ready));
        for (int s = 0; s <= states.arraySize; s++)
        {
            var tile = new Rect(row.x + labelWidth + (s % columns) * (tileWidth + gap),
                row.y + 8 + (s / columns) * (tileHeight + gap), tileWidth, tileHeight);
            if (s == states.arraySize)
            {
                float buttonHeight = (tile.height - 4) / 2;
                var addRect = new Rect(tile.x, tile.y, tile.width, buttonHeight);
                var copyRect = new Rect(tile.x, tile.y + buttonHeight + 4, tile.width, buttonHeight);
                using (new EditorGUI.DisabledScope(Locked))
                    if (GUI.Button(addRect, "＋ 狀態")) AddState(states, g);
                using (new EditorGUI.DisabledScope(Locked || GetSelectedState() == null))
                    if (GUI.Button(copyRect, new GUIContent("＋ 從選中狀態複製", "將選中的狀態、Flag 與進入／解除事件複製到此組，支援 Undo")))
                        CopySelectedState(states, g);
                continue;
            }
            SerializedProperty state = states.GetArrayElementAtIndex(s);
            string stateName = state.FindPropertyRelative("StateName").stringValue;
            bool active = ready && stateName.Length > 0 && current == stateName;
            bool last = ready && stateName.Length > 0 && exited == stateName;
            string counts = $"進入 {Calls(state, "OnEnter")}　解除 {Calls(state, "OnExit")}";
            string badge = active ? "● 成立" : last ? "● 最後退出" : "未成立";
            var flag = state.FindPropertyRelative("StateFlag").objectReferenceValue;
            string flagLabel = flag != null ? "Flag：" + flag.name : "Flag：未設定";
            int index = s;
            DrawTile(tile, stateName, badge + "\n" + counts + "\n" + flagLabel, selectedGroup == g && selectedState == s && !showChecks,
                active ? Active : last ? Exited : new Color(0.5f, 0.5f, 0.5f, 0.18f),
                () => Select(g, index), () => ShowTestMenu(groupName, stateName, states, ready));
        }
        GUILayout.Space(5);
    }

    private static int Calls(SerializedProperty state, string field) => state.FindPropertyRelative(field + ".m_PersistentCalls.m_Calls").arraySize;

    private static void DrawTile(Rect rect, string title, string subtitle, bool selected, Color color, Action click, Action context)
    {
        EditorGUI.DrawRect(rect, color);
        if (selected)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2), Selected);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2, rect.width, 2), Selected);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2, rect.height), Selected);
            EditorGUI.DrawRect(new Rect(rect.xMax - 2, rect.y, 2, rect.height), Selected);
        }
        GUI.Label(new Rect(rect.x + 6, rect.y + 5, rect.width - 12, 20), new GUIContent(title, title), EditorStyles.boldLabel);
        GUI.Label(new Rect(rect.x + 6, rect.y + 27, rect.width - 12, rect.height - 29), new GUIContent(subtitle, subtitle), EditorStyles.miniLabel);
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
        Event e = Event.current;
        if (!rect.Contains(e.mousePosition)) return;
        if (e.type == EventType.ContextClick) { context(); e.Use(); }
        else if (e.type == EventType.MouseDown && e.button == 0) { click(); e.Use(); GUIUtility.ExitGUI(); }
    }

    private void Select(int group, int state)
    {
        selectedGroup = group; selectedState = state; showChecks = false;
        detailScroll = Vector2.zero;
        GUI.FocusControl(null);
        Repaint();
    }

    private void DrawDetails(SerializedProperty groups)
    {
        if (showChecks)
        {
            EditorGUILayout.LabelField("CheckState 判斷結果", EditorStyles.boldLabel);
            DrawEventPair(data.FindProperty("onCheckTrue"), data.FindProperty("onCheckFalse"), "成立時", "不成立時", "CheckState");
            return;
        }
        if (selectedGroup < 0 || selectedGroup >= groups.arraySize)
        { EditorGUILayout.HelpBox("點選上方狀態方塊編輯事件，或點選組名編輯分組。", MessageType.Info); return; }
        SerializedProperty group = groups.GetArrayElementAtIndex(selectedGroup);
        SerializedProperty states = group.FindPropertyRelative("States");
        string groupName = group.FindPropertyRelative("GroupName").stringValue;
        EditorGUILayout.LabelField("分組：" + groupName, EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(Locked))
        {
            EditorGUILayout.PropertyField(group.FindPropertyRelative("GroupName"), new GUIContent("組名"));
            if (selectedState < 0) DrawManagement(groups, selectedGroup, true);
        }
        ValidateName(groups, selectedGroup, "GroupName");
        if (selectedState < 0 || selectedState >= states.arraySize)
        { EditorGUILayout.HelpBox("此組共有 " + states.arraySize + " 個狀態。點選上方方塊編輯其進入與解除事件。", MessageType.None); return; }
        SerializedProperty state = states.GetArrayElementAtIndex(selectedState);
        using (new EditorGUI.DisabledScope(Locked))
        {
            EditorGUILayout.PropertyField(state.FindPropertyRelative("StateName"), new GUIContent("狀態名"));
            EditorGUILayout.PropertyField(state.FindPropertyRelative("StateFlag"),
                new GUIContent("狀態 Flag（選填）", "拖入 ProgressFlagDefinition 資產；留空不處理 Flag。"));
            DrawManagement(states, selectedState, false);
        }
        ValidateName(states, selectedState, "StateName");
        EditorGUILayout.LabelField("Flag：進入時開啟 → OnEnter；退出時移除 → OnExit。使用 Scene 生命週期；可同時成立的狀態請使用不同 Flag。",
            EditorStyles.wordWrappedMiniLabel);
        if (Locked) EditorGUILayout.LabelField("執行中設定唯讀；在上方狀態方塊按右鍵測試。", EditorStyles.miniLabel);
        DrawEventPair(state.FindPropertyRelative("OnEnter"), state.FindPropertyRelative("OnExit"),
            "OnEnter · 進入事件", "OnExit · 解除事件", groupName + "/" + state.FindPropertyRelative("StateName").stringValue);
    }

    private void DrawManagement(SerializedProperty array, int index, bool isGroup)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (isGroup && GUILayout.Button(new GUIContent("複製所選 Group",
                "建立此組的完整副本，包含所有狀態、Flag 與進入／解除事件，支援 Undo"), GUILayout.Width(125)))
                CopySelectedGroup(array);
            using (new EditorGUI.DisabledScope(index == 0))
                if (GUILayout.Button("前移", GUILayout.Width(55)))
                { array.MoveArrayElement(index, index - 1); if (isGroup) selectedGroup--; else selectedState--; Commit(); }
            using (new EditorGUI.DisabledScope(index == array.arraySize - 1))
                if (GUILayout.Button("後移", GUILayout.Width(55)))
                { array.MoveArrayElement(index, index + 1); if (isGroup) selectedGroup++; else selectedState++; Commit(); }
            if (GUILayout.Button(isGroup ? "刪除此組" : "刪除此狀態", GUILayout.Width(90)))
            {
                array.DeleteArrayElementAtIndex(index);
                if (isGroup) selectedGroup = -1;
                selectedState = -1;
                Commit();
            }
        }
    }

    private void DrawEventPair(SerializedProperty left, SerializedProperty right, string leftTitle, string rightTitle, string source)
    {
        if (clipboard != null) EditorGUILayout.LabelField("已複製：" + clipboardLabel, EditorStyles.wordWrappedMiniLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawEvent(left, leftTitle, source);
            GUILayout.Space(10);
            DrawEvent(right, rightTitle, source);
        }
    }

    private void DrawEvent(SerializedProperty property, string title, string source)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(Mathf.Max(300, (position.width - 45) / 2))))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(title, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("複製", GUILayout.Width(45)))
                { clipboard = Capture(property); clipboardLabel = source + " · " + title; clipboardFromPlay = Application.isPlaying; }
                using (new EditorGUI.DisabledScope(Locked || clipboard == null || clipboardFromPlay))
                    if (GUILayout.Button(new GUIContent("貼上", "覆蓋此事件，支援 Undo"), GUILayout.Width(45))) Paste(property);
            }
            if (!eventDrawers.TryGetValue(property.propertyPath, out SelectableEventDrawer drawer))
            {
                drawer = new SelectableEventDrawer();
                eventDrawers.Add(property.propertyPath, drawer);
            }
            var label = new GUIContent(title);
            Rect eventRect = EditorGUILayout.GetControlRect(false, drawer.GetPropertyHeight(property, label));
            using (new EditorGUI.DisabledScope(Locked))
                drawer.OnGUI(eventRect, property, label);
            DrawSelectedEventActions(property, drawer.SelectedIndex, source + " · " + title);
        }
    }

    private void DrawSelectedEventActions(SerializedProperty property, int index, string source)
    {
        SerializedProperty calls = property.FindPropertyRelative("m_PersistentCalls.m_Calls");
        bool selected = index >= 0 && index < calls.arraySize;
        // 固定保留按鈕列高度，選取改變時不影響 IMGUI 的 Layout／Repaint 配對。
        Rect row = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
        float width = (row.width - 8) / 3;
        if (selected)
        {
            if (GUI.Button(new Rect(row.x, row.y, width, row.height), "複製所選事件"))
            {
                selectedEventClipboard = Capture(calls.GetArrayElementAtIndex(index));
                selectedEventClipboardLabel = source + " · 第 " + (index + 1) + " 項";
                selectedEventClipboardFromPlay = Locked;
            }
            using (new EditorGUI.DisabledScope(Locked))
                if (GUI.Button(new Rect(row.x + width + 4, row.y, width, row.height), "刪除所選事件"))
                {
                    calls.DeleteArrayElementAtIndex(index);
                    Commit();
                }
        }
        using (new EditorGUI.DisabledScope(Locked || selectedEventClipboard == null || selectedEventClipboardFromPlay))
            if (GUI.Button(new Rect(row.x + (width + 4) * 2, row.y, width, row.height),
                new GUIContent("貼上所選事件", "新增到清單末尾，保留原有項目，支援 Undo")))
                AppendSelectedEvent(calls);
        EditorGUILayout.LabelField(selectedEventClipboard == null ? "點選事件列可複製或刪除單一項目。"
            : "單項剪貼簿：" + selectedEventClipboardLabel, EditorStyles.wordWrappedMiniLabel);
    }

    private void AppendSelectedEvent(SerializedProperty calls)
    {
        if (Locked || selectedEventClipboard == null || selectedEventClipboardFromPlay) return;
        foreach (EventValue value in selectedEventClipboard)
        {
            if (value.Type != SerializedPropertyType.ObjectReference || ReferenceEquals(value.Value, null)) continue;
            var reference = (UnityEngine.Object)value.Value;
            if (reference == null)
            { ShowNotification(new GUIContent("複製的物件已失效，請重新複製事件")); return; }
            if (EditorUtility.IsPersistent(controller) && !EditorUtility.IsPersistent(reference))
            { ShowNotification(new GUIContent("場景物件參照無法貼入 Prefab 資產")); return; }
        }
        int index = calls.arraySize++;
        ApplyValues(calls.GetArrayElementAtIndex(index), selectedEventClipboard);
        Commit();
    }

    private void ShowTestMenu(string group, string state, SerializedProperty states, bool ready)
    {
        var menu = new GenericMenu();
        var target = controller;
        bool canTest = ready && !EditorUtility.IsPersistent(target);
        if (state != null)
        {
            if (canTest) menu.AddItem(new GUIContent("切換至此狀態"), false, () => { if (Application.isPlaying && target != null) target.SetState(group, state); });
            else menu.AddDisabledItem(new GUIContent("切換至此狀態（需 Play Mode 且已初始化）"));
        }
        for (int i = 0; i < states.arraySize; i++)
        {
            string id = states.GetArrayElementAtIndex(i).FindPropertyRelative("StateName").stringValue;
            if (canTest) menu.AddItem(new GUIContent("切換到/" + id), target.IsState(group, id), () => { if (Application.isPlaying && target != null) target.SetState(group, id); });
            else menu.AddDisabledItem(new GUIContent("切換到/" + id));
        }
        menu.AddSeparator("");
        if (canTest) menu.AddItem(new GUIContent("解除此組目前狀態"), false, () => { if (Application.isPlaying && target != null) target.ClearState(group); });
        else menu.AddDisabledItem(new GUIContent("解除此組目前狀態"));
        menu.ShowAsContext();
    }

    private void AddGroup(SerializedProperty groups)
    {
        int index = groups.arraySize++;
        SerializedProperty group = groups.GetArrayElementAtIndex(index);
        group.FindPropertyRelative("GroupName").stringValue = UniqueName(groups, "GroupName", "新分組", index);
        group.FindPropertyRelative("States").ClearArray();
        search = "";
        Select(index, -1);
        Commit();
    }

    private void CopySelectedGroup(SerializedProperty groups)
    {
        if (Locked || showChecks || selectedGroup < 0 || selectedGroup >= groups.arraySize) return;
        SerializedProperty source = groups.GetArrayElementAtIndex(selectedGroup);
        // 先擷取完整內容，再擴充分組清單，避免來源屬性因陣列變動而失效。
        string prefix = source.FindPropertyRelative("GroupName").stringValue + "_副本";
        List<EventValue> values = Capture(source);
        int index = groups.arraySize++;
        SerializedProperty copy = groups.GetArrayElementAtIndex(index);
        ApplyValues(copy, values);
        copy.FindPropertyRelative("GroupName").stringValue = UniqueName(groups, "GroupName", prefix, index);
        search = "";
        Select(index, -1);
        Commit();
    }

    private void AddState(SerializedProperty states, int group)
    {
        int index = states.arraySize++;
        SerializedProperty state = states.GetArrayElementAtIndex(index);
        state.FindPropertyRelative("StateName").stringValue = UniqueName(states, "StateName", "新狀態", index);
        state.FindPropertyRelative("StateFlag").objectReferenceValue = null;
        state.FindPropertyRelative("OnEnter.m_PersistentCalls.m_Calls").ClearArray();
        state.FindPropertyRelative("OnExit.m_PersistentCalls.m_Calls").ClearArray();
        Select(group, index);
        Commit();
    }

    private SerializedProperty GetSelectedState()
    {
        if (showChecks || selectedGroup < 0 || selectedState < 0) return null;
        SerializedProperty groups = data.FindProperty("groups");
        if (selectedGroup >= groups.arraySize) return null;
        SerializedProperty states = groups.GetArrayElementAtIndex(selectedGroup).FindPropertyRelative("States");
        return selectedState < states.arraySize ? states.GetArrayElementAtIndex(selectedState) : null;
    }

    private void CopySelectedState(SerializedProperty states, int group)
    {
        SerializedProperty source = GetSelectedState();
        if (Locked || source == null) return;
        // 先擷取完整內容，避免同組新增時使來源的序列化屬性失效。
        string prefix = source.FindPropertyRelative("StateName").stringValue + "_副本";
        List<EventValue> values = Capture(source);
        int index = states.arraySize++;
        SerializedProperty copy = states.GetArrayElementAtIndex(index);
        ApplyValues(copy, values);
        copy.FindPropertyRelative("StateName").stringValue = UniqueName(states, "StateName", prefix, index);
        Select(group, index);
        Commit();
    }

    private static string UniqueName(SerializedProperty array, string field, string prefix, int ignore)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < array.arraySize; i++) if (i != ignore) names.Add(array.GetArrayElementAtIndex(i).FindPropertyRelative(field).stringValue);
        int suffix = 1;
        while (names.Contains(prefix + suffix)) suffix++;
        return prefix + suffix;
    }

    private static void ValidateName(SerializedProperty array, int index, string field)
    {
        string name = array.GetArrayElementAtIndex(index).FindPropertyRelative(field).stringValue;
        if (string.IsNullOrWhiteSpace(name) || name.Contains("/") || name != name.Trim())
            EditorGUILayout.HelpBox("名稱不可空白、包含 / 或帶有頭尾空白。", MessageType.Error);
        for (int i = 0; i < array.arraySize; i++)
            if (i != index && array.GetArrayElementAtIndex(i).FindPropertyRelative(field).stringValue == name)
            { EditorGUILayout.HelpBox("名稱重複，請修改為唯一名稱。", MessageType.Error); break; }
    }

    // 複製序列化欄位的值與 Unity 物件參照；來源修改不影響剪貼簿。
    private static List<EventValue> Capture(SerializedProperty property)
    {
        var result = new List<EventValue>();
        SerializedProperty iterator = property.Copy(), end = property.GetEndProperty();
        bool children = true;
        while (iterator.Next(children) && !SerializedProperty.EqualContents(iterator, end))
        {
            children = iterator.propertyType == SerializedPropertyType.Generic;
            var value = new EventValue { Path = iterator.propertyPath.Substring(property.propertyPath.Length + 1), Type = iterator.propertyType,
                IsArray = iterator.isArray && iterator.propertyType != SerializedPropertyType.String };
            if (value.IsArray) value.Value = iterator.arraySize;
            else switch (value.Type)
            {
                case SerializedPropertyType.Generic:
                case SerializedPropertyType.ArraySize: continue;
                case SerializedPropertyType.Integer: value.Value = iterator.longValue; break;
                case SerializedPropertyType.Boolean: value.Value = iterator.boolValue; break;
                case SerializedPropertyType.Float: value.Value = iterator.doubleValue; break;
                case SerializedPropertyType.String: value.Value = iterator.stringValue; break;
                case SerializedPropertyType.Enum: value.Value = iterator.intValue; break;
                case SerializedPropertyType.ObjectReference: value.Value = iterator.objectReferenceValue; break;
                default: throw new NotSupportedException("不支援的事件欄位：" + value.Type);
            }
            result.Add(value);
        }
        return result;
    }

    private void Paste(SerializedProperty property)
    {
        if (EditorUtility.IsPersistent(controller) && clipboard.Exists(value => value.Type == SerializedPropertyType.ObjectReference
            && value.Value is UnityEngine.Object reference && reference != null && !EditorUtility.IsPersistent(reference)))
        { ShowNotification(new GUIContent("場景物件參照無法貼入 Prefab 資產")); return; }
        ApplyValues(property, clipboard);
        Commit();
    }

    private static void ApplyValues(SerializedProperty property, List<EventValue> values)
    {
        foreach (EventValue value in values)
        {
            SerializedProperty destination = property.FindPropertyRelative(value.Path);
            if (value.IsArray) destination.arraySize = (int)value.Value;
            else switch (value.Type)
            {
                case SerializedPropertyType.Integer: destination.longValue = (long)value.Value; break;
                case SerializedPropertyType.Boolean: destination.boolValue = (bool)value.Value; break;
                case SerializedPropertyType.Float: destination.doubleValue = (double)value.Value; break;
                case SerializedPropertyType.String: destination.stringValue = (string)value.Value; break;
                case SerializedPropertyType.Enum: destination.intValue = (int)value.Value; break;
                case SerializedPropertyType.ObjectReference: destination.objectReferenceValue = (UnityEngine.Object)value.Value; break;
            }
        }
    }

    private void Commit()
    {
        // 結束本次 GUI，避免結構改動後使用失效的 SerializedProperty。
        data.ApplyModifiedProperties();
        eventDrawers.Clear();
        Repaint();
        GUIUtility.ExitGUI();
    }
}
