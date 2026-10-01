using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>固定三欄的播放按鈕分組編輯器。</summary>
public sealed class PlayerButtonGroupEditorWindow : EditorWindow
{
    [SerializeField] private PlayerButtonGroup controller;
    [SerializeField] private PlayerButtonGroup editController;
    [SerializeField] private string editControllerId;
    private bool changingPlayMode;
    [SerializeField] private int selectedGroup = -1;
    [SerializeField] private Vector2 groupScroll, detailScroll;
    [SerializeField] private bool commonSettings;
    private SerializedObject data;
    private readonly Dictionary<string, SelectableEventDrawer> drawers = new Dictionary<string, SelectableEventDrawer>();
    private static readonly string[] Actions = { "pause", "play", "fast" };
    private static readonly string[] Titles = { "暫停", "播放", "快速" };
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

    // 保留原生 UnityEvent 的函式選單、參數編輯與拖曳排序。
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

    [MenuItem("Tools/NHK/PlayerButtonGroup編輯器")]
    public static void Open()
    {
        var window = GetWindow<PlayerButtonGroupEditorWindow>("PlayerButtonGroup編輯器");
        var selected = SelectedController();
        if (selected != null) window.SetController(selected);
        window.Show();
    }

    [MenuItem("CONTEXT/PlayerButtonGroup/開啟 PlayerButtonGroup編輯器")]
    private static void OpenFromComponent(MenuCommand command)
    {
        var window = GetWindow<PlayerButtonGroupEditorWindow>("PlayerButtonGroup編輯器");
        window.SetController(command.context as PlayerButtonGroup);
        window.Show();
    }

    private void OnEnable()
    {
        minSize = new Vector2(1080, 580);
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
    private void OnUndo() { ReleaseData(); selectedGroup = -1; Repaint(); }
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

    private void RememberEditController(PlayerButtonGroup target)
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
            BindController(PlayerButtonGroup.Instance);
            return;
        }
        if (Locked) return;
        if (editController == null && !string.IsNullOrEmpty(editControllerId)
            && GlobalObjectId.TryParse(editControllerId, out var id))
            editController = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as PlayerButtonGroup;
        // 已指定的目標暫時不存在時等待它恢復，不改接其他控制器。
        if (editController == null && string.IsNullOrEmpty(editControllerId))
        {
            PlayerButtonGroup candidate = null;
            foreach (var item in Resources.FindObjectsOfTypeAll<PlayerButtonGroup>())
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

    private void BindController(PlayerButtonGroup target)
    {
        if (ReferenceEquals(controller, target)) return;
        ReleaseData();
        controller = target;
    }

    private static PlayerButtonGroup SelectedController() => Selection.activeGameObject == null
        ? null : Selection.activeGameObject.GetComponent<PlayerButtonGroup>();

    private void OnSelectionChange()
    {
        // 編輯目標保持固定，拖入事件物件時不會跟著跳走。
        if (!Locked && !changingPlayMode && controller == null && string.IsNullOrEmpty(editControllerId))
            SetController(SelectedController());
        Repaint();
    }

    private void SetController(PlayerButtonGroup next)
    {
        if (Locked || changingPlayMode) { ResolveController(); return; }
        if (controller != null) data?.ApplyModifiedProperties();
        RememberEditController(next);
        ReleaseData();
        controller = next;
        selectedGroup = -1;
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
                var next = (PlayerButtonGroup)EditorGUILayout.ObjectField(controller, typeof(PlayerButtonGroup), true);
                if (next != controller) { SetController(next); GUIUtility.ExitGUI(); }
                if (GUILayout.Button("使用選取物件", EditorStyles.toolbarButton, GUILayout.Width(100)))
                { SetController(SelectedController()); GUIUtility.ExitGUI(); }
            }
            using (new EditorGUI.DisabledScope(controller == null))
                if (GUILayout.Button("定位", EditorStyles.toolbarButton, GUILayout.Width(45)))
                    EditorGUIUtility.PingObject(controller.gameObject);
            bool nextCommon = GUILayout.Toggle(commonSettings, "共用設定", EditorStyles.toolbarButton, GUILayout.Width(80));
            if (nextCommon != commonSettings)
            {
                commonSettings = nextCommon;
                GUI.FocusControl(null);
                GUIUtility.ExitGUI();
            }
        }
        if (controller == null)
        {
            ReleaseData();
            EditorGUILayout.HelpBox(Application.isPlaying
                ? "等待 PlayerButtonGroup 單例初始化，視窗會自動連接。"
                : "等待原編輯目標恢復。也可拖入 PlayerButtonGroup，或按「使用選取物件」指定目標。", MessageType.Info);
            return;
        }
        if (data == null || data.targetObject != controller) { ReleaseData(); data = new SerializedObject(controller); }
        data.Update();
        var groups = data.FindProperty("groups");
        DrawGroups(groups);
        if (Locked)
            EditorGUILayout.HelpBox("目前使用的組：" + (controller == PlayerButtonGroup.Instance ? controller.CurrentID ?? "無" : "此元件不是執行中的實例")
                + "。綠底＝遊戲目前使用；藍框＝編輯器選取。試玩期間設定唯讀。", MessageType.Info);

        using (var scroll = new EditorGUILayout.ScrollViewScope(detailScroll))
        {
            detailScroll = scroll.scrollPosition;
            if (commonSettings) DrawCommonSettings();
            else if (selectedGroup >= 0 && selectedGroup < groups.arraySize) DrawGroup(groups);
            else EditorGUILayout.HelpBox("請從上方選擇一組，或新增分組。", MessageType.Info);
        }
        data.ApplyModifiedProperties();
    }

    private void DrawGroups(SerializedProperty groups)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("分組", EditorStyles.boldLabel, GUILayout.Width(40));
            using (new EditorGUI.DisabledScope(Locked))
                if (GUILayout.Button("＋ 新增組", GUILayout.Width(90))) AddGroup(groups);
            GUILayout.Label("上方選組，下方同時編輯三顆按鈕。", EditorStyles.miniLabel);
        }
        using (var scroll = new EditorGUILayout.ScrollViewScope(groupScroll, GUILayout.Height(72)))
        {
            groupScroll = scroll.scrollPosition;
            using (new EditorGUILayout.HorizontalScope())
            {
                bool activeDrawn = false;
                for (int i = 0; i < groups.arraySize; i++)
                {
                    string id = groups.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue;
                    bool active = !activeDrawn && Application.isPlaying && controller == PlayerButtonGroup.Instance
                        && !string.IsNullOrWhiteSpace(id) && string.Equals(id, controller.CurrentID, StringComparison.Ordinal);
                    if (active) activeDrawn = true; // ID 重複時執行端只使用第一組。
                    string label = (active ? "● " : "") + (string.IsNullOrWhiteSpace(id) ? "未命名組 " + (i + 1) : id);
                    float width = Mathf.Clamp(EditorStyles.boldLabel.CalcSize(new GUIContent(label)).x + 30, 130, 320);
                    Rect rect = GUILayoutUtility.GetRect(width, 40, GUILayout.Width(width), GUILayout.Height(40));
                    GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
                    if (active) EditorGUI.DrawRect(rect, ActiveColor);
                    if (!commonSettings && i == selectedGroup)
                    {
                        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2), SelectedColor);
                        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2, rect.width, 2), SelectedColor);
                        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2, rect.height), SelectedColor);
                        EditorGUI.DrawRect(new Rect(rect.xMax - 2, rect.y, 2, rect.height), SelectedColor);
                    }
                    GUI.Label(new Rect(rect.x + 8, rect.y + 10, rect.width - 16, 22), new GUIContent(label, id), EditorStyles.boldLabel);
                    if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                    {
                        selectedGroup = i;
                        commonSettings = false;
                        detailScroll = Vector2.zero;
                        Commit();
                    }
                }
            }
        }
    }

    private void DrawGroup(SerializedProperty groups)
    {
        var group = groups.GetArrayElementAtIndex(selectedGroup);
        using (new EditorGUI.DisabledScope(Locked))
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(group.FindPropertyRelative("id"), new GUIContent("組名／ID"));
            if (GUILayout.Button("複製選中的組", GUILayout.Width(110))) DuplicateGroup(groups, group);
            if (GUILayout.Button("刪除此組", GUILayout.Width(85)))
            {
                groups.DeleteArrayElementAtIndex(selectedGroup);
                selectedGroup = -1;
                Commit();
            }
        }
        string id = group.FindPropertyRelative("id").stringValue;
        if (string.IsNullOrWhiteSpace(id)) EditorGUILayout.HelpBox("組名／ID 不可空白。", MessageType.Warning);
        for (int i = 0; i < groups.arraySize; i++)
            if (i != selectedGroup && groups.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue == id)
            { EditorGUILayout.HelpBox("ID 重複，遊戲切換時只會使用第一組。", MessageType.Warning); break; }
        using (new EditorGUILayout.HorizontalScope())
            for (int i = 0; i < Actions.Length; i++) DrawAction(group.FindPropertyRelative(Actions[i]), Titles[i], id);
    }

    private void DrawAction(SerializedProperty action, string title, string id)
    {
        float width = Mathf.Max(330, (position.width - 46) / 3f);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(width)))
        {
            EditorGUILayout.LabelField(title, EditorStyles.largeLabel);
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 105;
            try
            {
                using (new EditorGUI.DisabledScope(Locked))
                {
                    EditorGUILayout.PropertyField(action.FindPropertyRelative("interactableFlag"), new GUIContent("可點 Flag"));
                    EditorGUILayout.PropertyField(action.FindPropertyRelative("invertInteractable"), new GUIContent("反轉可點條件"));
                }
            }
            finally { EditorGUIUtility.labelWidth = oldLabelWidth; }
            var property = action.FindPropertyRelative("onClick");
            string source = controller.name + " / " + id + " / " + title;
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
            if (!drawers.TryGetValue(property.propertyPath, out SelectableEventDrawer drawer))
            {
                drawer = new SelectableEventDrawer();
                drawers.Add(property.propertyPath, drawer);
            }
            var label = new GUIContent("點擊事件");
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
            EditorGUILayout.LabelField("點選事件列後，可複製或刪除單一項目。Flag 留空時永遠符合可點條件。", EditorStyles.wordWrappedMiniLabel);
            if (eventClipboard != null) EditorGUILayout.LabelField("全部事件剪貼簿：" + eventClipboard.Label, EditorStyles.wordWrappedMiniLabel);
            if (singleClipboard != null) EditorGUILayout.LabelField("單項剪貼簿：" + singleClipboard.Label, EditorStyles.wordWrappedMiniLabel);
        }
    }

    private void DrawCommonSettings()
    {
        EditorGUILayout.LabelField("固定按鈕與整體顯示", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(Locked))
            foreach (string field in new[] { "pauseButton", "playButton", "fastButton", "canvasGroup", "fadeInDuration", "fadeOutDuration" })
                EditorGUILayout.PropertyField(data.FindProperty(field), true);
    }

    private void AddGroup(SerializedProperty groups)
    {
        if (Locked) return;
        string id = UniqueName(groups, "新分組");
        int index = groups.arraySize++;
        var group = groups.GetArrayElementAtIndex(index);
        group.FindPropertyRelative("id").stringValue = id;
        // Unity 擴充清單可能複製上一組，新增時明確清空條件與事件。
        foreach (string field in Actions)
        {
            var action = group.FindPropertyRelative(field);
            action.FindPropertyRelative("interactableFlag").objectReferenceValue = null;
            action.FindPropertyRelative("invertInteractable").boolValue = false;
            action.FindPropertyRelative("onClick.m_PersistentCalls.m_Calls").ClearArray();
        }
        selectedGroup = index;
        commonSettings = false;
        Commit();
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
        selectedGroup = index;
        Commit();
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
    private static Snapshot Capture(SerializedProperty property, string label)
    {
        var result = new Snapshot { Label = label, FromPlay = EditorApplication.isPlayingOrWillChangePlaymode };
        var iterator = property.Copy();
        var end = property.GetEndProperty();
        bool children = true;
        while (iterator.Next(children) && !SerializedProperty.EqualContents(iterator, end))
        {
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
