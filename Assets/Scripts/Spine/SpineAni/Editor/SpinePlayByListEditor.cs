using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpinePlayByList))]
public class SpinePlayByListEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        if (GUILayout.Button("開啟 SpinePlayByList編輯器（編輯／試播）", GUILayout.Height(30)))
            SpinePlayByListWindow.Open((SpinePlayByList)target);
        EditorGUILayout.HelpBox("Groups 編輯與 Quick Test 已統一到視窗，支援編輯模式的 Scene View 試播。", MessageType.Info);
    }
}
