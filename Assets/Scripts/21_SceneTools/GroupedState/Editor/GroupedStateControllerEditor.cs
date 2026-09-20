using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>分組編輯、事件剪貼簿與執行期測試介面。</summary>
[CustomEditor(typeof(GroupedStateController))]
public sealed class GroupedStateControllerEditor : Editor
{
    private static readonly Color ActiveColor = new Color(0.18f, 0.72f, 0.35f, 0.32f);
    private static readonly Color ExitedColor = new Color(1f, 0.58f, 0.12f, 0.32f);
    private static List<EventValue> clipboard;
    private static string clipboardLabel;
    private static bool clipboardFromPlayMode;
    private bool showChecks;

    private sealed class EventValue
    {
        public string Path;
        public SerializedPropertyType Type;
        public object Value;
        public bool IsArray;
    }

    private bool Locked => EditorApplication.isPlayingOrWillChangePlaymode;
    private GroupedStateController Controller => (GroupedStateController)target;
    private bool CanTest => Application.isPlaying && target != null
        && !EditorUtility.IsPersistent(target) && Controller.gameObject.scene.IsValid();

    public override bool RequiresConstantRepaint() => Application.isPlaying;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        EditorGUILayout.HelpBox(Application.isPlaying
            ? "綠底：目前成立　橘底：最後退出（保留至下次退出）。\n在組或狀態標題按右鍵切換測試；設定於執行中唯讀。"
            : "每組最多一個狀態成立，也可皆不成立。展開狀態可編輯或複製事件；貼上會覆蓋目標事件，可用 Undo 還原。",
            MessageType.Info);
        if (clipboard != null)
            EditorGUILayout.LabelField("事件剪貼簿：" + clipboardLabel, EditorStyles.wordWrappedMiniLabel);

        SerializedProperty groups = serializedObject.FindProperty("groups");
        for (int i = 0; i < groups.arraySize; i++) DrawGroup(groups, i);
        using (new EditorGUI.DisabledScope(Locked))
        {
            if (GUILayout.Button("＋ 新增分組"))
            {
                int index = groups.arraySize++;
                SerializedProperty group = groups.GetArrayElementAtIndex(index);
                group.FindPropertyRelative("GroupName").stringValue = UniqueName(groups, "GroupName", "新分組", index);
                group.FindPropertyRelative("States").ClearArray();
                group.isExpanded = true;
                CommitAndExit();
            }
        }

        EditorGUILayout.Space();
        showChecks = EditorGUILayout.Foldout(showChecks, "CheckState 判斷結果事件", true);
        if (showChecks)
        {
            DrawEvent(serializedObject.FindProperty("onCheckTrue"), "成立時", "CheckState / 成立時");
            DrawEvent(serializedObject.FindProperty("onCheckFalse"), "不成立時", "CheckState / 不成立時");
        }
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawGroup(SerializedProperty groups, int index)
    {
        SerializedProperty group = groups.GetArrayElementAtIndex(index);
        SerializedProperty name = group.FindPropertyRelative("GroupName");
        SerializedProperty states = group.FindPropertyRelative("States");
        string current = string.Empty, exited = string.Empty;
        bool ready = CanTest && Controller.TryGetStateSnapshot(name.stringValue, out current, out exited);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        Rect header = EditorGUILayout.GetControlRect(false, 25);
        EditorGUI.DrawRect(header, new Color(0.3f, 0.5f, 0.75f, 0.18f));
        HandleTestMenu(header, name.stringValue, states, ready, current);
        group.isExpanded = EditorGUI.Foldout(header, group.isExpanded,
            $"組 {index + 1}：{name.stringValue}　（{states.arraySize} 個狀態）", true, EditorStyles.foldoutHeader);
        if (CanTest)
            EditorGUILayout.LabelField(ready ? "目前：" + (current.Length == 0 ? "皆不成立" : current)
                + "　｜　最後退出：" + (exited.Length == 0 ? "無" : exited) : "尚未初始化，或設定無效", EditorStyles.wordWrappedMiniLabel);

        if (group.isExpanded)
        {
            using (new EditorGUI.DisabledScope(Locked))
                EditorGUILayout.PropertyField(name, new GUIContent("組名"));
            DrawNameWarning(groups, index, "GroupName");
            DrawArrayButtons(groups, index, "分組");
            for (int i = 0; i < states.arraySize; i++)
                DrawState(states, i, name.stringValue, ready, current, exited);
            using (new EditorGUI.DisabledScope(Locked))
            {
                if (GUILayout.Button("＋ 新增狀態"))
                {
                    int stateIndex = states.arraySize++;
                    SerializedProperty state = states.GetArrayElementAtIndex(stateIndex);
                    state.FindPropertyRelative("StateName").stringValue = UniqueName(states, "StateName", "新狀態", stateIndex);
                    // Unity 插入陣列元素可能複製前一項，必須清除繼承的事件。
                    ClearEvent(state.FindPropertyRelative("OnEnter"));
                    ClearEvent(state.FindPropertyRelative("OnExit"));
                    state.isExpanded = true;
                    CommitAndExit();
                }
            }
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DrawState(SerializedProperty states, int index, string groupId, bool ready, string current, string exited)
    {
        SerializedProperty state = states.GetArrayElementAtIndex(index);
        SerializedProperty name = state.FindPropertyRelative("StateName");
        bool active = ready && name.stringValue.Length > 0 && current == name.stringValue;
        bool last = ready && name.stringValue.Length > 0 && exited == name.stringValue;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        Rect header = EditorGUILayout.GetControlRect(false, 24);
        if (active || last) EditorGUI.DrawRect(header, active ? ActiveColor : ExitedColor);
        HandleTestMenu(header, groupId, states, ready, current);
        string badge = active ? "　● 目前成立" : last ? "　● 最後退出" : string.Empty;
        state.isExpanded = EditorGUI.Foldout(header, state.isExpanded,
            $"狀態 {index + 1}：{name.stringValue}{badge}", true);
        if (state.isExpanded)
        {
            using (new EditorGUI.DisabledScope(Locked))
                EditorGUILayout.PropertyField(name, new GUIContent("狀態名"));
            DrawNameWarning(states, index, "StateName");
            DrawArrayButtons(states, index, "狀態");
            string label = groupId + " / " + name.stringValue;
            DrawEvent(state.FindPropertyRelative("OnEnter"), "OnEnter（進入）", label + " / OnEnter");
            DrawEvent(state.FindPropertyRelative("OnExit"), "OnExit（解除）", label + " / OnExit");
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawArrayButtons(SerializedProperty array, int index, string label)
    {
        using (new EditorGUI.DisabledScope(Locked))
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(index == 0))
                if (GUILayout.Button("上移", GUILayout.Width(45)))
                { array.MoveArrayElement(index, index - 1); CommitAndExit(); }
            using (new EditorGUI.DisabledScope(index == array.arraySize - 1))
                if (GUILayout.Button("下移", GUILayout.Width(45)))
                { array.MoveArrayElement(index, index + 1); CommitAndExit(); }
            if (GUILayout.Button("刪除" + label, GUILayout.Width(70)))
            { array.DeleteArrayElementAtIndex(index); CommitAndExit(); }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawEvent(SerializedProperty property, string title, string label)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        if (GUILayout.Button("複製", GUILayout.Width(45)))
        {
            clipboard = CaptureEvent(property);
            clipboardLabel = label;
            clipboardFromPlayMode = Application.isPlaying;
        }
        using (new EditorGUI.DisabledScope(Locked || clipboard == null || clipboardFromPlayMode))
        {
            if (GUILayout.Button(new GUIContent("貼上", "覆蓋此事件清單，支援 Undo。"), GUILayout.Width(45)))
            {
                // 避免把場景物件參照寫入 Prefab 資產。
                bool invalidReference = EditorUtility.IsPersistent(target) && clipboard.Exists(value =>
                    value.Type == SerializedPropertyType.ObjectReference && value.Value is UnityEngine.Object reference
                    && reference != null && !EditorUtility.IsPersistent(reference));
                if (invalidReference)
                    Debug.LogWarning("[GroupedStateControllerEditor] 剪貼簿含場景物件，無法貼入 Prefab 資產。", target);
                else
                {
                    PasteEvent(property, clipboard);
                    CommitAndExit();
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        using (new EditorGUI.DisabledScope(Locked))
            EditorGUILayout.PropertyField(property, new GUIContent(title), true);
    }

    private void HandleTestMenu(Rect rect, string groupId, SerializedProperty states, bool ready, string current)
    {
        if (!CanTest || Event.current.type != EventType.ContextClick || !rect.Contains(Event.current.mousePosition)) return;
        var menu = new GenericMenu();
        GroupedStateController controller = Controller;
        for (int i = 0; i < states.arraySize; i++)
        {
            string stateId = states.GetArrayElementAtIndex(i).FindPropertyRelative("StateName").stringValue;
            var label = new GUIContent("切換到/" + stateId);
            if (!ready) menu.AddDisabledItem(label);
            else menu.AddItem(label, current == stateId, () =>
            {
                if (Application.isPlaying && controller != null)
                { controller.SetState(groupId, stateId); Repaint(); }
            });
        }
        menu.AddSeparator("");
        if (!ready) menu.AddDisabledItem(new GUIContent("解除目前狀態"));
        else menu.AddItem(new GUIContent("解除目前狀態"), current.Length == 0, () =>
        {
            if (Application.isPlaying && controller != null)
            { controller.ClearState(groupId); Repaint(); }
        });
        menu.ShowAsContext();
        Event.current.Use();
    }

    private static void DrawNameWarning(SerializedProperty array, int index, string field)
    {
        string value = array.GetArrayElementAtIndex(index).FindPropertyRelative(field).stringValue;
        if (string.IsNullOrWhiteSpace(value) || value.Contains("/") || value != value.Trim())
        {
            EditorGUILayout.HelpBox("名稱不可空白、包含 / 或帶有頭尾空白。", MessageType.Error);
            return;
        }
        for (int i = 0; i < array.arraySize; i++)
            if (i != index && array.GetArrayElementAtIndex(i).FindPropertyRelative(field).stringValue == value)
            { EditorGUILayout.HelpBox("名稱重複，請使用唯一名稱。", MessageType.Error); break; }
    }

    private static string UniqueName(SerializedProperty array, string field, string prefix, int newIndex)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < array.arraySize; i++)
            if (i != newIndex) names.Add(array.GetArrayElementAtIndex(i).FindPropertyRelative(field).stringValue);
        int suffix = 1;
        while (names.Contains(prefix + suffix)) suffix++;
        return prefix + suffix;
    }

    private static void ClearEvent(SerializedProperty property)
    {
        property.FindPropertyRelative("m_PersistentCalls.m_Calls").ClearArray();
    }

    // 逐項快照序列化欄位；物件參照直接保留，不經 JSON，也不持有來源的 SerializedProperty。
    private static List<EventValue> CaptureEvent(SerializedProperty property)
    {
        var values = new List<EventValue>();
        SerializedProperty iterator = property.Copy();
        SerializedProperty end = property.GetEndProperty();
        bool enterChildren = true;
        while (iterator.Next(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
        {
            // 只深入容器，避免展開字串的內部字元或物件參照的內部欄位。
            enterChildren = iterator.propertyType == SerializedPropertyType.Generic;
            var value = new EventValue
            {
                Path = iterator.propertyPath.Substring(property.propertyPath.Length + 1),
                Type = iterator.propertyType,
                IsArray = iterator.isArray && iterator.propertyType != SerializedPropertyType.String
            };
            if (value.IsArray) value.Value = iterator.arraySize;
            else switch (iterator.propertyType)
            {
                case SerializedPropertyType.Generic:
                case SerializedPropertyType.ArraySize: continue;
                case SerializedPropertyType.Integer: value.Value = iterator.longValue; break;
                case SerializedPropertyType.Boolean: value.Value = iterator.boolValue; break;
                case SerializedPropertyType.Float: value.Value = iterator.doubleValue; break;
                case SerializedPropertyType.String: value.Value = iterator.stringValue; break;
                case SerializedPropertyType.Enum: value.Value = iterator.intValue; break;
                case SerializedPropertyType.ObjectReference: value.Value = iterator.objectReferenceValue; break;
                default: throw new NotSupportedException("不支援的事件欄位：" + iterator.propertyType);
            }
            values.Add(value);
        }
        return values;
    }

    private static void PasteEvent(SerializedProperty property, List<EventValue> values)
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

    private void CommitAndExit()
    {
        // SerializedObject 負責 Undo 與 Prefab override；結束本次 GUI，避免陣列改動後使用失效的 Property。
        serializedObject.ApplyModifiedProperties();
        Repaint();
        GUIUtility.ExitGUI();
    }
}
