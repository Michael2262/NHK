using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>場景計時行為編輯、事件管理與 Play Mode 觀察入口。</summary>
public sealed class SceneTimerControllerWindow : EditorWindow
{
    [SerializeField] private SceneTimerController controller, editController;
    [SerializeField] private string editID, runtimeID;
    [SerializeField] private int selected = -1, editSelected = -1;
    [SerializeField] private int page;
    [SerializeField] private string search = "";
    [SerializeField] private Vector2 listScroll, detailScroll;
    [SerializeField] private bool showUnityFunctions;
    private bool transitioning;
    private SerializedObject data;
    private readonly Dictionary<string, FilteredUnityEventDrawer> drawers = new Dictionary<string, FilteredUnityEventDrawer>();
    private readonly Dictionary<TimerId, SceneTimerModel.TimerSnapshot> snapshots = new Dictionary<TimerId, SceneTimerModel.TimerSnapshot>();
    private static Snapshot allClipboard, singleClipboard;
    private bool Locked => EditorApplication.isPlayingOrWillChangePlaymode;
    private bool CanTest => Application.isPlaying && !transitioning && controller != null
        && controller.isActiveAndEnabled && controller.IsInitialized && !EditorUtility.IsPersistent(controller)
        && controller.gameObject.scene.IsValid() && controller.gameObject.scene.isLoaded
        && !UnityEditor.SceneManagement.EditorSceneManager.IsPreviewSceneObject(controller.gameObject);

    [MenuItem("Tools/NHK/SceneTimerController編輯器")]
    public static void Open()
    {
        var window = GetWindow<SceneTimerControllerWindow>("SceneTimerController編輯器");
        var target = SelectedController();
        if (target != null) window.SetController(target);
        window.Show();
    }

    [MenuItem("CONTEXT/SceneTimerController/開啟 SceneTimerController編輯器")]
    private static void OpenContext(MenuCommand command)
    {
        var window = GetWindow<SceneTimerControllerWindow>("SceneTimerController編輯器");
        window.SetController(command.context as SceneTimerController);
        window.Show();
    }

    private static SceneTimerController SelectedController() => Selection.activeGameObject != null
        ? Selection.activeGameObject.GetComponent<SceneTimerController>() : null;

    private void OnEnable()
    {
        minSize = new Vector2(940, 600);
        ReleaseData();
        transitioning = EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying;
        EditorApplication.playModeStateChanged += OnModeChanged;
        Undo.undoRedoPerformed += OnUndo;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnModeChanged;
        Undo.undoRedoPerformed -= OnUndo;
        ReleaseData();
    }

    private void ReleaseData() { drawers.Clear(); data?.Dispose(); data = null; snapshots.Clear(); }
    private void OnUndo() { ReleaseData(); selected = -1; Repaint(); }
    private void OnInspectorUpdate() { ResolveController(); Repaint(); }

    private static SceneTimerController ResolveID(string id) => !string.IsNullOrEmpty(id)
        && GlobalObjectId.TryParse(id, out var global)
        ? GlobalObjectId.GlobalObjectIdentifierToObjectSlow(global) as SceneTimerController : null;

    private void ResolveController()
    {
        if (transitioning || controller != null) return;
        var next = ResolveID(Application.isPlaying ? runtimeID : editID);
        if (!Locked && next == null) next = editController;
        if (next != null) { ReleaseData(); controller = next; }
    }

    private void OnModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            data?.ApplyModifiedProperties();
            editController = controller;
            if (controller != null) editID = GlobalObjectId.GetGlobalObjectIdSlow(controller).ToString();
            runtimeID = editID;
            editSelected = selected;
        }
        transitioning = state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode;
        ReleaseData();
        if (state == PlayModeStateChange.EnteredEditMode) { controller = null; selected = editSelected; }
        ResolveController();
        Repaint();
    }

    private void SetController(SceneTimerController target)
    {
        if (transitioning) return;
        data?.ApplyModifiedProperties();
        ReleaseData();
        controller = target;
        string id = target != null ? GlobalObjectId.GetGlobalObjectIdSlow(target).ToString() : "";
        if (Application.isPlaying) runtimeID = id;
        else { editController = target; editID = id; }
        selected = -1;
        search = "";
        listScroll = detailScroll = Vector2.zero;
    }

    private void OnGUI()
    {
        if (transitioning) { EditorGUILayout.HelpBox("正在切換試玩模式，完成後會重新連接原控制器。", MessageType.Info); return; }
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            var next = (SceneTimerController)EditorGUILayout.ObjectField(controller, typeof(SceneTimerController), true);
            if (next != controller) { SetController(next); GUIUtility.ExitGUI(); }
            if (GUILayout.Button("使用選取物件", EditorStyles.toolbarButton, GUILayout.Width(105)))
            { SetController(SelectedController()); GUIUtility.ExitGUI(); }
            using (new EditorGUI.DisabledScope(controller == null))
                if (GUILayout.Button("定位", EditorStyles.toolbarButton, GUILayout.Width(45))) EditorGUIUtility.PingObject(controller.gameObject);
        }
        int nextPage = GUILayout.Toolbar(page, new[] { "行為編輯", "判斷事件", "共用設定", "執行狀態／測試" });
        if (nextPage != page) { page = nextPage; detailScroll = Vector2.zero; drawers.Clear(); GUI.FocusControl(null); GUIUtility.ExitGUI(); }
        if (controller == null)
        { EditorGUILayout.HelpBox("請指定 SceneTimerController。已記住的目標恢復後會自動重連，不會改接其他同名物件。", MessageType.Info); return; }
        if (data == null || data.targetObject != controller) { ReleaseData(); data = new SerializedObject(controller); }
        data.Update();
        snapshots.Clear();
        if (Application.isPlaying)
            foreach (TimerId timer in Enum.GetValues(typeof(TimerId)))
                if (controller.TryGetTimerSnapshot(timer, out var snapshot)) snapshots[timer] = snapshot;
        var behaviors = data.FindProperty("behaviors");
        if (Locked) EditorGUILayout.HelpBox("試玩期間設定唯讀；測試操作會觸發真正的遊戲事件。", MessageType.Info);
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(260))) DrawList(behaviors);
            using (var scroll = new EditorGUILayout.ScrollViewScope(detailScroll))
            {
                detailScroll = scroll.scrollPosition;
                if (page == 0) DrawBehavior(behaviors);
                else if (page == 1)
                {
                    EditorGUILayout.HelpBox("EvaluateTimer：閒置時成立，暫停仍算佔用。EvaluateID：執行中或佇列內有此 ID 時成立。", MessageType.None);
                    DrawEvent(data.FindProperty("onCheckTrue"), "判斷成立事件");
                    DrawEvent(data.FindProperty("onCheckFalse"), "判斷不成立事件");
                }
                else if (page == 2)
                {
                    EditorGUILayout.LabelField("事件選單顯示", EditorStyles.boldLabel);
                    showUnityFunctions = EditorGUILayout.ToggleLeft("顯示 Unity 內建功能", showUnityFunctions);
                    EditorGUILayout.HelpBox("統一套用此視窗的所有事件，預設關閉；既有綁定不受影響。", MessageType.None);
                }
                else DrawRuntime(behaviors);
            }
        }
        data.ApplyModifiedProperties();
    }

    private void DrawList(SerializedProperty behaviors)
    {
        search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
        using (new EditorGUI.DisabledScope(Locked))
            if (GUILayout.Button("＋ 新增行為"))
            {
                string id = UniqueID(behaviors, "新行為");
                selected = behaviors.arraySize++;
                var item = behaviors.GetArrayElementAtIndex(selected);
                item.FindPropertyRelative("ID").stringValue = id;
                item.FindPropertyRelative("Timer").intValue = (int)TimerId.Main;
                item.FindPropertyRelative("Duration").floatValue = 10f;
                item.FindPropertyRelative("StartMode").intValue = (int)TimerStartMode.Interrupt;
                item.FindPropertyRelative("OnCompleted.m_PersistentCalls.m_Calls").ClearArray();
                page = 0; search = ""; Commit();
            }
        using (var scroll = new EditorGUILayout.ScrollViewScope(listScroll))
        {
            listScroll = scroll.scrollPosition;
            var timers = new List<int>();
            foreach (TimerId timer in Enum.GetValues(typeof(TimerId))) if (!timers.Contains((int)timer)) timers.Add((int)timer);
            for (int i = 0; i < behaviors.arraySize; i++)
            {
                int timer = behaviors.GetArrayElementAtIndex(i).FindPropertyRelative("Timer").intValue;
                if (!timers.Contains(timer)) timers.Add(timer);
            }
            foreach (int value in timers)
            {
                var timer = (TimerId)value;
                EditorGUILayout.LabelField(Enum.IsDefined(typeof(TimerId), timer) ? timer.ToString() : "未知 Timer：" + value, EditorStyles.boldLabel);
                snapshots.TryGetValue(timer, out var state);
                for (int i = 0; i < behaviors.arraySize; i++)
                {
                    var item = behaviors.GetArrayElementAtIndex(i);
                    if (item.FindPropertyRelative("Timer").intValue != value) continue;
                    string id = item.FindPropertyRelative("ID").stringValue;
                    if (!string.IsNullOrEmpty(search) && id.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0
                        && timer.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    bool current = state != null && state.CurrentID == id;
                    Color old = GUI.backgroundColor;
                    GUI.backgroundColor = current ? (state.Paused ? new Color(1f, 0.75f, 0.3f) : new Color(0.35f, 0.9f, 0.45f))
                        : selected == i ? new Color(0.4f, 0.7f, 1f) : old;
                    bool click = GUILayout.Button((selected == i ? "▶ " : "") + id + (current ? (state.Paused ? "｜暫停" : "｜倒數中") : ""), GUILayout.Height(28));
                    GUI.backgroundColor = old;
                    if (click) { selected = i; page = 0; detailScroll = Vector2.zero; Commit(); }
                }
            }
        }
    }

    private void DrawBehavior(SerializedProperty behaviors)
    {
        if (selected < 0 || selected >= behaviors.arraySize)
        { EditorGUILayout.HelpBox("從左側選擇計時行為，或新增行為。", MessageType.Info); return; }
        var item = behaviors.GetArrayElementAtIndex(selected);
        using (new EditorGUI.DisabledScope(Locked))
        {
            EditorGUILayout.PropertyField(item.FindPropertyRelative("ID"), new GUIContent("行為 ID"));
            EditorGUILayout.PropertyField(item.FindPropertyRelative("Timer"), new GUIContent("所屬 Timer"));
            EditorGUILayout.PropertyField(item.FindPropertyRelative("Duration"), new GUIContent("倒數秒數"));
            EditorGUILayout.PropertyField(item.FindPropertyRelative("StartMode"), new GUIContent("啟動模式"));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("複製行為"))
                {
                    string id = UniqueID(behaviors, item.FindPropertyRelative("ID").stringValue + "_副本");
                    if (item.DuplicateCommand()) { selected++; behaviors.GetArrayElementAtIndex(selected).FindPropertyRelative("ID").stringValue = id; Commit(); }
                }
                if (GUILayout.Button("刪除行為")) { behaviors.DeleteArrayElementAtIndex(selected); selected = -1; Commit(); }
            }
        }
        string name = item.FindPropertyRelative("ID").stringValue;
        int matches = 0;
        for (int i = 0; i < behaviors.arraySize; i++) if (behaviors.GetArrayElementAtIndex(i).FindPropertyRelative("ID").stringValue == name) matches++;
        if (string.IsNullOrWhiteSpace(name) || name != name.Trim() || matches > 1)
            EditorGUILayout.HelpBox("行為 ID 必須唯一、非空白且不能含前後空格。", MessageType.Error);
        float duration = item.FindPropertyRelative("Duration").floatValue;
        if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
            EditorGUILayout.HelpBox("倒數秒數必須為有限的正數。", MessageType.Error);
        if (!Enum.IsDefined(typeof(TimerId), item.FindPropertyRelative("Timer").intValue)
            || !Enum.IsDefined(typeof(TimerStartMode), item.FindPropertyRelative("StartMode").intValue))
            EditorGUILayout.HelpBox("Timer 或啟動模式無效，請重新選擇。", MessageType.Error);
        EditorGUILayout.HelpBox("Interrupt：取代目前工作；Skip：忙碌時忽略；Queue：排隊；Priority：插至隊首。\n有效的 StartTimer 會恢復所屬 Timer；相同 ID 不重複啟動或排隊。", MessageType.None);
        DrawEvent(item.FindPropertyRelative("OnCompleted"), "倒數完成事件");
        if (Application.isPlaying) DrawSelectedTest(behaviors);
    }

    private void DrawSelectedTest(SerializedProperty behaviors)
    {
        if (selected < 0 || selected >= behaviors.arraySize) return;
        string id = behaviors.GetArrayElementAtIndex(selected).FindPropertyRelative("ID").stringValue;
        EditorGUILayout.LabelField("所選行為：" + id, EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(!CanTest))
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("開始／接續此行為")) Run(() => controller.StartTimer(id));
            if (GUILayout.Button("EvaluateID（觸發判斷事件）")) Run(() => controller.EvaluateID(id));
        }
    }

    private void DrawRuntime(SerializedProperty behaviors)
    {
        if (!Application.isPlaying) { EditorGUILayout.HelpBox("進入 Play Mode 後可查看倒數、佇列與測試。編輯模式不推進計時，也不觸發遊戲事件。", MessageType.Info); return; }
        if (!controller.IsInitialized)
        { EditorGUILayout.HelpBox(controller.InitializationFailed ? "初始化失敗，請依 Console 錯誤修正設定後重新進入 Play Mode。" : "控制器尚未初始化，請先啟用場景物件。", MessageType.Warning); return; }
        DrawSelectedTest(behaviors);
        using (new EditorGUI.DisabledScope(!CanTest))
            if (GUILayout.Button("取消全部 Timer 與佇列")) Run(() => controller.CancelAllTimers());
        foreach (var pair in snapshots)
        {
            TimerId timer = pair.Key;
            var state = pair.Value;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(timer + "｜" + (state.Paused ? "已暫停" : state.IsIdle ? "閒置" : "執行中"), EditorStyles.boldLabel);
                EditorGUILayout.LabelField("目前行為：" + (state.CurrentID ?? "無"));
                Rect progress = EditorGUILayout.GetControlRect(false, 22);
                EditorGUI.ProgressBar(progress, state.Duration > 0f ? Mathf.Clamp01((float)(1d - state.Remaining / state.Duration)) : 0f,
                    "剩餘 " + state.Remaining.ToString("0.00") + " 秒");
                EditorGUILayout.LabelField("佇列（先 → 後）：" + (state.QueuedIDs.Count == 0 ? "無" : string.Join(" → ", state.QueuedIDs)), EditorStyles.wordWrappedLabel);
                using (new EditorGUI.DisabledScope(!CanTest))
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("暫停")) Run(() => controller.PauseTimer(timer));
                    string resumeID = state.CurrentID ?? (state.QueuedIDs.Count > 0 ? state.QueuedIDs[0] : null);
                    using (new EditorGUI.DisabledScope(!state.Paused || resumeID == null))
                        if (GUILayout.Button("接續")) Run(() => controller.StartTimer(resumeID));
                    if (GUILayout.Button("取消此 Timer")) Run(() => controller.CancelTimer(timer));
                    if (GUILayout.Button("EvaluateTimer")) Run(() => controller.EvaluateTimer(timer.ToString()));
                }
            }
        }
        EditorGUILayout.HelpBox("倒數使用 Time.deltaTime，受遊戲 Time Scale 影響。暫停仍佔用 Timer；取消會清除該 Timer 的工作與佇列。", MessageType.None);
    }

    private void Run(Action action)
    {
        if (!CanTest) return;
        action();
        Repaint();
        GUIUtility.ExitGUI();
    }

    private static string UniqueID(SerializedProperty behaviors, string prefix)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < behaviors.arraySize; i++) ids.Add(behaviors.GetArrayElementAtIndex(i).FindPropertyRelative("ID").stringValue);
        string id = prefix; int suffix = 2;
        while (ids.Contains(id)) id = prefix + suffix++;
        return id;
    }

    private void DrawEvent(SerializedProperty property, string title)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("複製全部事件")) { allClipboard = Capture(property, controller.name + " / " + title); Commit(); }
                using (new EditorGUI.DisabledScope(!CanPaste(allClipboard)))
                    if (GUILayout.Button("貼上全部（覆蓋）")) Paste(property, allClipboard, false);
            }
            if (!drawers.TryGetValue(property.propertyPath, out var drawer))
            {
                drawer = new FilteredUnityEventDrawer(this, () => controller, () => showUnityFunctions,
                    () => !Locked && !transitioning, item => drawers.ContainsValue(item));
                drawers.Add(property.propertyPath, drawer);
            }
            var label = new GUIContent(title);
            Rect rect = EditorGUILayout.GetControlRect(false, drawer.GetPropertyHeight(property, label));
            using (new EditorGUI.DisabledScope(Locked)) drawer.OnGUI(rect, property, label);
            var calls = property.FindPropertyRelative("m_PersistentCalls.m_Calls");
            int index = drawer.SelectedIndex;
            bool valid = index >= 0 && index < calls.arraySize;
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!valid))
                    if (GUILayout.Button("複製所選事件")) { singleClipboard = Capture(calls.GetArrayElementAtIndex(index), title + " / 第 " + (index + 1) + " 項"); Commit(); }
                using (new EditorGUI.DisabledScope(!CanPaste(singleClipboard)))
                    if (GUILayout.Button("貼上單項（新增）")) Paste(calls, singleClipboard, true);
                using (new EditorGUI.DisabledScope(Locked || !valid))
                    if (GUILayout.Button("刪除所選事件")) { calls.DeleteArrayElementAtIndex(index); Commit(); }
            }
            if (allClipboard != null) EditorGUILayout.LabelField("全部事件剪貼簿：" + allClipboard.Label, EditorStyles.wordWrappedMiniLabel);
            if (singleClipboard != null) EditorGUILayout.LabelField("單項剪貼簿：" + singleClipboard.Label, EditorStyles.wordWrappedMiniLabel);
        }
    }

    private bool CanPaste(Snapshot snapshot) => !Locked && snapshot != null && !snapshot.FromPlay;

    private void Paste(SerializedProperty destination, Snapshot snapshot, bool append)
    {
        if (!CanPaste(snapshot)) return;
        foreach (var value in snapshot.Values)
        {
            if (value.Type != SerializedPropertyType.ObjectReference || ReferenceEquals(value.Data, null)) continue;
            var reference = (UnityEngine.Object)value.Data;
            if (reference == null) { ShowNotification(new GUIContent("來源物件已失效，請重新複製事件。")); return; }
            if (EditorUtility.IsPersistent(reference)) continue;
            GameObject go = reference as GameObject;
            if (reference is Component component) go = component.gameObject;
            if (EditorUtility.IsPersistent(controller) || go == null || go.scene != controller.gameObject.scene)
            { ShowNotification(new GUIContent("事件含其他場景或 Prefab Stage 參照，無法貼入目前目標。")); return; }
        }
        if (append) { int index = destination.arraySize++; destination = destination.GetArrayElementAtIndex(index); }
        Apply(destination, snapshot);
        Commit();
    }

    private void Commit()
    {
        data.ApplyModifiedProperties();
        drawers.Clear();
        GUI.FocusControl(null);
        Repaint();
        GUIUtility.ExitGUI();
    }

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

}
