using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>保留原生事件資料，提供反向條件與共用的簡化事件選單。</summary>
[CustomEditor(typeof(ProgressSequenceRouter))]
public sealed class ProgressSequenceRouterEditor : Editor
{
    private readonly Dictionary<string, FilteredUnityEventDrawer> drawers =
        new Dictionary<string, FilteredUnityEventDrawer>();
    private ReorderableList branchList;
    private bool showUnityFunctions;
    private bool Locked => EditorApplication.isPlayingOrWillChangePlaymode;
    private float Line => EditorGUIUtility.singleLineHeight;

    private void OnEnable()
    {
        Undo.undoRedoPerformed += ClearDrawers;
        var branches = serializedObject.FindProperty("branches");
        branchList = new ReorderableList(serializedObject, branches, true, true, true, true);
        branchList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "分支清單（由上往下，第一組成立即停止）");
        branchList.elementHeightCallback = index =>
        {
            var entry = branches.GetArrayElementAtIndex(index);
            var evt = entry.FindPropertyRelative("onTriggered");
            return 2 * (Line + 4) + GetDrawer(evt).GetPropertyHeight(evt, GUIContent.none) + 12;
        };
        branchList.drawElementCallback = (rect, index, active, focused) =>
        {
            var entry = branches.GetArrayElementAtIndex(index);
            var flag = entry.FindPropertyRelative("flagDefinition");
            var positive = entry.FindPropertyRelative("triggerIfActive");
            rect.y += 4;
            rect.height = Line;
            EditorGUI.PropertyField(rect, flag, new GUIContent("Flag"));
            rect.y += Line + 4;
            using (new EditorGUI.DisabledScope(flag.objectReferenceValue == null))
            {
                EditorGUI.BeginProperty(rect, new GUIContent("反向"), positive);
                EditorGUI.BeginChangeCheck();
                bool inverse = EditorGUI.Toggle(rect,
                    new GUIContent("反向", "勾選：Flag 不存在才成立；未勾選：Flag 存在才成立。Flag 留空一律跳過。"),
                    !positive.boolValue);
                if (EditorGUI.EndChangeCheck()) positive.boolValue = !inverse;
                EditorGUI.EndProperty();
            }
            rect.y += Line + 4;
            var evt = entry.FindPropertyRelative("onTriggered");
            var drawer = GetDrawer(evt);
            var label = new GUIContent("成立時執行");
            rect.height = drawer.GetPropertyHeight(evt, label);
            drawer.OnGUI(rect, evt, label);
        };
        branchList.onAddCallback = list =>
        {
            // 明確建立空分支，避免 Unity 複製上一組的 Flag 與事件。
            int index = branches.arraySize;
            branches.arraySize++;
            var entry = branches.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("flagDefinition").objectReferenceValue = null;
            entry.FindPropertyRelative("triggerIfActive").boolValue = true;
            entry.FindPropertyRelative("onTriggered").FindPropertyRelative("m_PersistentCalls.m_Calls").ClearArray();
            list.index = index;
            ClearDrawers();
        };
        branchList.onRemoveCallback = list =>
        {
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            ClearDrawers();
        };
        branchList.onReorderCallback = list => ClearDrawers();
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= ClearDrawers;
        drawers.Clear();
    }

    private void ClearDrawers()
    {
        drawers.Clear();
        Repaint();
    }

    private FilteredUnityEventDrawer GetDrawer(SerializedProperty property)
    {
        if (!drawers.TryGetValue(property.propertyPath, out var drawer))
        {
            drawer = new FilteredUnityEventDrawer(this, () => target,
                () => showUnityFunctions, () => !Locked,
                item => drawers.ContainsValue(item));
            drawers.Add(property.propertyPath, drawer);
        }
        return drawer;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        showUnityFunctions = EditorGUILayout.Toggle("顯示 Unity 內建函式", showUnityFunctions);
        EditorGUILayout.HelpBox("Flag 留空會跳過該組。全部分支都不成立時，執行下方預設事件。", MessageType.Info);
        using (new EditorGUI.DisabledScope(Locked))
        {
            branchList.DoLayoutList();
            EditorGUILayout.Space();
            var evt = serializedObject.FindProperty("onDefault");
            var drawer = GetDrawer(evt);
            var label = new GUIContent("預設事件（皆不符合時觸發）");
            Rect rect = EditorGUILayout.GetControlRect(false, drawer.GetPropertyHeight(evt, label));
            drawer.OnGUI(rect, evt, label);
        }
        if (serializedObject.ApplyModifiedProperties()) ClearDrawers();
    }
}
