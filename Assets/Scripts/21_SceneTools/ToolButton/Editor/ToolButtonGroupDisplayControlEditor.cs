using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ToolButtonGroupDisplayControl))]
public sealed class ToolButtonGroupDisplayControlEditor : Editor
{
    private readonly Dictionary<string, FilteredUnityEventDrawer> eventDrawers = new Dictionary<string, FilteredUnityEventDrawer>();
    private bool Locked => EditorApplication.isPlayingOrWillChangePlaymode;

    private void OnEnable() => Undo.undoRedoPerformed += ClearDrawers;

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= ClearDrawers;
        eventDrawers.Clear();
    }

    private void ClearDrawers()
    {
        eventDrawers.Clear();
        Repaint();
    }

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("開啟 ToolButtonGroup編輯器", GUILayout.Height(28)))
            ToolButtonGroupEditorWindow.Open((ToolButtonGroupDisplayControl)target);
        serializedObject.Update();
        var property = serializedObject.GetIterator();
        bool enterChildren = true;
        int originalIndent = EditorGUI.indentLevel;
        try
        {
            // 逐層繪製，讓巢狀群組內的事件也使用條件繪製器，不露出可單獨改長度的條件陣列。
            while (property.NextVisible(enterChildren))
            {
                EditorGUI.indentLevel = originalIndent + property.depth;
                using (new EditorGUI.DisabledScope(Locked || property.propertyPath == "m_Script"))
                {
                    if (property.type == nameof(FlagConditionalEvent))
                    {
                        DrawConditionalEvent(property);
                        enterChildren = false;
                    }
                    else enterChildren = EditorGUILayout.PropertyField(property, false);
                }
            }
        }
        finally { EditorGUI.indentLevel = originalIndent; }
        if (serializedObject.ApplyModifiedProperties()) ClearDrawers();
    }

    private void DrawConditionalEvent(SerializedProperty property)
    {
        if (!eventDrawers.TryGetValue(property.propertyPath, out var drawer))
        {
            drawer = new FilteredUnityEventDrawer(this, () => target, () => true, () => !Locked,
                item => eventDrawers.ContainsValue(item), Commit);
            eventDrawers.Add(property.propertyPath, drawer);
        }
        var label = new GUIContent(property.displayName, property.tooltip);
        Rect rect = EditorGUILayout.GetControlRect(false, drawer.GetPropertyHeight(property, label));
        drawer.OnGUI(rect, property, label);
    }

    private void Commit()
    {
        serializedObject.ApplyModifiedProperties();
        ClearDrawers();
        GUIUtility.ExitGUI();
    }
}
