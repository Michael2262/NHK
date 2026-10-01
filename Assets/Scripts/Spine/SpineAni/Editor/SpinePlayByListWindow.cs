using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>動畫組編輯與統一試播入口。</summary>
public sealed class SpinePlayByListWindow : EditorWindow
{
    [SerializeField] private SpinePlayByList target, editTarget;
    [SerializeField] private string editId, runtimeId;
    [SerializeField] private int selected = -1;
    [SerializeField] private string search = "";
    [SerializeField] private Vector2 listScroll, detailScroll;
    [SerializeField] private bool condition;
    private bool transitioning;
    private SerializedObject data;
    private SpineListPreview preview;
    private bool Locked => EditorApplication.isPlayingOrWillChangePlaymode;

    [MenuItem("Tools/NHK/SpinePlayByList編輯器")]
    public static void OpenMenu() => Open(Selection.activeGameObject != null
        ? Selection.activeGameObject.GetComponent<SpinePlayByList>() : null);
    public static void Open(SpinePlayByList component)
    {
        var window = GetWindow<SpinePlayByListWindow>("SpinePlayByList編輯器");
        if (component != null) window.SelectTarget(component);
        window.Show();
    }
    [MenuItem("CONTEXT/SpinePlayByList/開啟 SpinePlayByList編輯器")]
    private static void OpenContext(MenuCommand command) => Open(command.context as SpinePlayByList);

    private void OnEnable()
    {
        minSize = new Vector2(1050, 620);
        preview = new SpineListPreview();
        transitioning = EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += ModeChanged;
        AssemblyReloadEvents.beforeAssemblyReload += StopPreview;
        EditorSceneManager.sceneSaving += BeforeSceneSave;
        EditorSceneManager.sceneClosing += BeforeSceneClose;
        Undo.undoRedoPerformed += OnUndo;
    }
    private void OnDisable()
    {
        StopPreview();
        ReleaseData();
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= ModeChanged;
        AssemblyReloadEvents.beforeAssemblyReload -= StopPreview;
        EditorSceneManager.sceneSaving -= BeforeSceneSave;
        EditorSceneManager.sceneClosing -= BeforeSceneClose;
        Undo.undoRedoPerformed -= OnUndo;
    }
    private void StopPreview() => preview?.Dispose();
    private void BeforeSceneSave(Scene scene, string path) => StopPreview();
    private void BeforeSceneClose(Scene scene, bool removingScene) => StopPreview();
    private void OnUndo() { StopPreview(); ReleaseData(); selected = -1; Repaint(); }
    private void ReleaseData() { data?.Dispose(); data = null; }
    private void Tick()
    {
        preview?.Tick();
        if (preview != null && preview.Active) Repaint();
    }
    private void OnInspectorUpdate()
    {
        if (!transitioning && target == null)
        {
            string id = Application.isPlaying ? runtimeId : editId;
            if (!string.IsNullOrEmpty(id) && GlobalObjectId.TryParse(id, out var global))
                target = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(global) as SpinePlayByList;
            if (!Locked && target == null && editTarget != null) target = editTarget;
        }
        if (preview != null && preview.Active && target == null) StopPreview();
        Repaint();
    }
    private void ModeChanged(PlayModeStateChange mode)
    {
        StopPreview();
        if (mode == PlayModeStateChange.ExitingEditMode && target != null)
        {
            data?.ApplyModifiedProperties();
            editTarget = target;
            editId = GlobalObjectId.GetGlobalObjectIdSlow(target).ToString();
            runtimeId = editId;
        }
        transitioning = mode == PlayModeStateChange.ExitingEditMode || mode == PlayModeStateChange.ExitingPlayMode;
        ReleaseData();
        if (mode == PlayModeStateChange.EnteredEditMode) target = editTarget;
        OnInspectorUpdate();
    }
    private void SelectTarget(SpinePlayByList next)
    {
        if (transitioning) return;
        StopPreview();
        if (target != null) data?.ApplyModifiedProperties();
        ReleaseData();
        target = next;
        string id = next != null ? GlobalObjectId.GetGlobalObjectIdSlow(next).ToString() : "";
        if (!Locked) { editTarget = next; editId = id; }
        else runtimeId = id;
        selected = -1;
        search = "";
    }
    private void OnGUI()
    {
        if (transitioning) { EditorGUILayout.HelpBox("正在切換模式，完成後會重新連接目標。", MessageType.Info); return; }
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            var next = (SpinePlayByList)EditorGUILayout.ObjectField(target, typeof(SpinePlayByList), true);
            if (next != target) { SelectTarget(next); GUIUtility.ExitGUI(); }
            if (GUILayout.Button("使用選取物件", EditorStyles.toolbarButton, GUILayout.Width(105)))
            { SelectTarget(Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<SpinePlayByList>() : null); GUIUtility.ExitGUI(); }
            if (target != null && GUILayout.Button("定位", EditorStyles.toolbarButton, GUILayout.Width(45)))
                EditorGUIUtility.PingObject(target.gameObject);
        }
        if (target == null) { EditorGUILayout.HelpBox("請指定 SpinePlayByList；已記住的目標恢復後會自動重連。", MessageType.Info); return; }
        if (data == null || data.targetObject != target) { ReleaseData(); data = new SerializedObject(target); }
        data.Update();
        var groups = data.FindProperty("groups");
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(245))) DrawGroups(groups);
            using (var scroll = new EditorGUILayout.ScrollViewScope(detailScroll))
            { detailScroll = scroll.scrollPosition; DrawDetails(groups); }
        }
        DrawPlayback(groups);
        data.ApplyModifiedProperties();
    }
    private void DrawGroups(SerializedProperty groups)
    {
        search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
        using (new EditorGUI.DisabledScope(Locked))
            if (GUILayout.Button("＋ 新增組"))
            {
                string name = UniqueName(groups, "新分組");
                selected = groups.arraySize++;
                var group = groups.GetArrayElementAtIndex(selected);
                group.FindPropertyRelative("groupName").stringValue = name;
                group.FindPropertyRelative("clips").ClearArray();
                group.FindPropertyRelative("track").intValue = 0;
                group.FindPropertyRelative("nextGroupName").stringValue = "";
                group.FindPropertyRelative("checkGroupName").stringValue = "";
                search = "";
                Commit();
            }
        using (var scroll = new EditorGUILayout.ScrollViewScope(listScroll))
        {
            listScroll = scroll.scrollPosition;
            string active = Application.isPlaying ? target.CurrentGroupName : preview.Group;
            for (int i = 0; i < groups.arraySize; i++)
            {
                var group = groups.GetArrayElementAtIndex(i);
                string name = group.FindPropertyRelative("groupName").stringValue;
                if (!string.IsNullOrEmpty(search) && name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                Color old = GUI.backgroundColor;
                GUI.backgroundColor = name == active ? new Color(0.35f, 0.9f, 0.45f)
                    : selected == i ? new Color(0.4f, 0.7f, 1f) : old;
                bool clicked = GUILayout.Button((i == selected ? "▶ " : "") + name + "  (" + group.FindPropertyRelative("clips").arraySize + ")", GUILayout.Height(30));
                GUI.backgroundColor = old;
                if (clicked) { selected = i; detailScroll = Vector2.zero; Commit(false); }
            }
        }
    }
    private void DrawDetails(SerializedProperty groups)
    {
        using (new EditorGUI.DisabledScope(Locked))
            EditorGUILayout.PropertyField(data.FindProperty("_controller"), new GUIContent("Spine 控制器"));
        if (selected < 0 || selected >= groups.arraySize) { EditorGUILayout.HelpBox("從左側選組，右側即可編輯動畫清單。", MessageType.Info); return; }
        var group = groups.GetArrayElementAtIndex(selected);
        using (new EditorGUI.DisabledScope(Locked))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(group.FindPropertyRelative("groupName"), new GUIContent("組名"));
                if (GUILayout.Button("複製此組", GUILayout.Width(85)))
                {
                    string name = UniqueName(groups, group.FindPropertyRelative("groupName").stringValue + "_副本");
                    if (group.DuplicateCommand())
                    { selected++; groups.GetArrayElementAtIndex(selected).FindPropertyRelative("groupName").stringValue = name; Commit(); }
                }
                if (GUILayout.Button("刪除此組", GUILayout.Width(85)))
                { groups.DeleteArrayElementAtIndex(selected); selected = -1; Commit(); }
            }
            EditorGUILayout.PropertyField(group.FindPropertyRelative("track"), new GUIContent("播放軌道"));
            DrawGroupLink(group.FindPropertyRelative("nextGroupName"), groups, "下一組");
            DrawGroupLink(group.FindPropertyRelative("checkGroupName"), groups, "條件跳轉組");
            string nameNow = group.FindPropertyRelative("groupName").stringValue;
            int matches = 0;
            for (int i = 0; i < groups.arraySize; i++)
                if (groups.GetArrayElementAtIndex(i).FindPropertyRelative("groupName").stringValue == nameNow) matches++;
            if (string.IsNullOrWhiteSpace(nameNow) || matches > 1)
                EditorGUILayout.HelpBox("組名必須非空白且唯一，才能正確播放及跳轉。", MessageType.Warning);
            var clips = group.FindPropertyRelative("clips");
            int loop = -1, random = -1;
            for (int i = 0; i < clips.arraySize; i++)
            {
                var clip = clips.GetArrayElementAtIndex(i);
                if (loop < 0 && clip.FindPropertyRelative("isLoop").boolValue) loop = i;
                if (random < 0 && clip.FindPropertyRelative("isRandom").boolValue) random = i;
            }
            bool hasNext = !string.IsNullOrEmpty(group.FindPropertyRelative("nextGroupName").stringValue);
            EditorGUILayout.HelpBox("Loop／Random 從第一個勾選項目起影響整個後段。綠色＝循環、青色＝隨機、橘色＝兩者。"
                + (hasNext ? "\n已指定下一組，本組 Loop 不生效。" : ""), MessageType.None);
            var names = AnimationNames();
            for (int i = 0; i < clips.arraySize; i++)
            {
                var clip = clips.GetArrayElementAtIndex(i);
                bool inLoop = !hasNext && loop >= 0 && i >= loop, inRandom = random >= 0 && i >= random;
                Color old = GUI.backgroundColor;
                GUI.backgroundColor = inLoop && inRandom ? new Color(1f, 0.7f, 0.35f)
                    : inLoop ? new Color(0.45f, 1f, 0.5f) : inRandom ? Color.cyan : old;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUI.backgroundColor = old;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label((preview.Active && preview.Group == nameNow && preview.Clip == i ? "► " : "") + (i + 1), GUILayout.Width(35));
                        DrawAnimation(clip.FindPropertyRelative("animationName"), names);
                        GUILayout.Label("次數", GUILayout.Width(30));
                        var repeats = clip.FindPropertyRelative("repeatCount");
                        repeats.intValue = Mathf.Max(1, EditorGUILayout.IntField(repeats.intValue, GUILayout.Width(48)));
                        using (new EditorGUI.DisabledScope(i == 0))
                            if (GUILayout.Button("↑", GUILayout.Width(25))) { clips.MoveArrayElement(i, i - 1); Commit(); }
                        using (new EditorGUI.DisabledScope(i == clips.arraySize - 1))
                            if (GUILayout.Button("↓", GUILayout.Width(25))) { clips.MoveArrayElement(i, i + 1); Commit(); }
                        if (GUILayout.Button("複製", GUILayout.Width(45))) { clip.DuplicateCommand(); Commit(); }
                        if (GUILayout.Button("刪除", GUILayout.Width(45))) { clips.DeleteArrayElementAtIndex(i); Commit(); }
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var l = clip.FindPropertyRelative("isLoop"); var r = clip.FindPropertyRelative("isRandom");
                        l.boolValue = EditorGUILayout.ToggleLeft("從此處開始 Loop", l.boolValue, GUILayout.Width(160));
                        r.boolValue = EditorGUILayout.ToggleLeft("從此處開始 Random", r.boolValue, GUILayout.Width(175));
                        GUILayout.Label((inLoop ? "循環後段 " : "") + (inRandom ? "隨機後段" : ""), EditorStyles.miniLabel);
                    }
                }
                GUI.backgroundColor = old;
            }
            if (GUILayout.Button("＋ 新增動畫"))
            {
                int i = clips.arraySize++;
                var clip = clips.GetArrayElementAtIndex(i);
                clip.FindPropertyRelative("animationName").stringValue = "";
                clip.FindPropertyRelative("repeatCount").intValue = 1;
                clip.FindPropertyRelative("isLoop").boolValue = false;
                clip.FindPropertyRelative("isRandom").boolValue = false;
                Commit();
            }
        }
    }
    private List<string> AnimationNames()
    {
        var result = new List<string> { "（未指定）" };
        var renderer = SpineListPreview.SourceRenderer(target);
        var skeletonData = renderer?.SkeletonDataAsset != null ? renderer.SkeletonDataAsset.GetSkeletonData(true) : null;
        if (skeletonData != null) foreach (var animation in skeletonData.Animations) result.Add(animation.Name);
        return result;
    }
    private static void DrawAnimation(SerializedProperty property, List<string> names)
    {
        var options = new List<string>(names);
        int index = string.IsNullOrEmpty(property.stringValue) ? 0 : options.IndexOf(property.stringValue);
        if (index < 0) { index = options.Count; options.Add("⚠ 找不到：" + property.stringValue); }
        int next = EditorGUILayout.Popup(index, options.ToArray());
        if (next != index) property.stringValue = next == 0 ? "" : options[next];
    }
    private static void DrawGroupLink(SerializedProperty property, SerializedProperty groups, string label)
    {
        var names = new List<string> { "（無）" };
        for (int i = 0; i < groups.arraySize; i++) names.Add(groups.GetArrayElementAtIndex(i).FindPropertyRelative("groupName").stringValue);
        int index = string.IsNullOrEmpty(property.stringValue) ? 0 : names.IndexOf(property.stringValue);
        if (index < 0) { index = names.Count; names.Add("⚠ 找不到：" + property.stringValue); }
        int next = EditorGUILayout.Popup(label, index, names.ToArray());
        if (next != index) property.stringValue = next == 0 ? "" : names[next];
    }
    private void DrawPlayback(SerializedProperty groups)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(Application.isPlaying ? "Quick Test｜遊戲播放" : "Quick Test｜Scene View 試播", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(Application.isPlaying ? (target.IsPlaying ? "正在播放：" + target.CurrentGroupName : "未播放")
                : (preview.Paused ? "已暫停｜" : "") + preview.Status, EditorStyles.wordWrappedLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                bool valid = selected >= 0 && selected < groups.arraySize && !EditorUtility.IsPersistent(target);
                using (new EditorGUI.DisabledScope(!valid))
                {
                    if (GUILayout.Button(Application.isPlaying ? "播放所選組" : "試播所選組／重新播放")) Play(false);
                    if (Application.isPlaying && GUILayout.Button("播放後返回")) Play(true);
                }
                if (!Application.isPlaying)
                    using (new EditorGUI.DisabledScope(!preview.Active))
                        if (GUILayout.Button(preview.Paused ? "繼續" : "暫停")) preview.Paused = !preview.Paused;
                if (GUILayout.Button("停止"))
                { if (Application.isPlaying) target.StopPlaying(); else StopPreview(); }
                if (!Application.isPlaying)
                {
                    condition = GUILayout.Toggle(condition, "模擬條件成立", GUILayout.Width(130));
                    preview.Condition = condition;
                }
            }
            if (!Application.isPlaying)
                EditorGUILayout.LabelField("預覽不觸發遊戲事件。編輯後按重新播放套用；停止、存場景或關閉視窗會清理預覽。", EditorStyles.wordWrappedMiniLabel);
        }
    }
    private void Play(bool back)
    {
        data.ApplyModifiedProperties();
        string name = target.groups[selected].groupName;
        if (Application.isPlaying)
        { if (back) target.PlayGroupAndGoBack(name); else target.PlayGroup(name); }
        else { preview.Condition = condition; preview.Start(target, name); SceneView.RepaintAll(); }
        GUI.FocusControl(null); Repaint(); GUIUtility.ExitGUI();
    }
    private static string UniqueName(SerializedProperty groups, string prefix)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < groups.arraySize; i++) names.Add(groups.GetArrayElementAtIndex(i).FindPropertyRelative("groupName").stringValue);
        string name = prefix; int suffix = 2;
        while (names.Contains(name)) name = prefix + suffix++;
        return name;
    }
    private void Commit(bool stop = true)
    {
        if (stop) StopPreview();
        data.ApplyModifiedProperties(); GUI.FocusControl(null); Repaint(); GUIUtility.ExitGUI();
    }
}
