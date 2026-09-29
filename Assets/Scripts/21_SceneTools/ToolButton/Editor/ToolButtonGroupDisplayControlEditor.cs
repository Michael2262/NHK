using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ToolButtonGroupDisplayControl))]
public sealed class ToolButtonGroupDisplayControlEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("開啟 ToolButtonGroup編輯器", GUILayout.Height(28)))
            ToolButtonGroupEditorWindow.Open((ToolButtonGroupDisplayControl)target);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            DrawDefaultInspector();
    }
}
