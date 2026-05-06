using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProgressStateController))]
public class ProgressStateControllerEditor : Editor
{
    // 用來記錄「原始數據」區塊是否展開的狀態
    private bool _showRawData = false;

    public override void OnInspectorGUI()
    {
        EditorGUILayout.Space();

        // 設置按鈕顏色
        GUI.backgroundColor = new Color(0.7f, 0.8f, 1f);
        if (GUILayout.Button("⚡ 開啟進度狀態專屬編輯器", GUILayout.Height(45)))
        {
            ProgressStateEditorWindow.ShowWindow();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();

        // 使用 Foldout 替代 BeginFoldoutHeaderGroup 以避免 Nesting 錯誤
        _showRawData = EditorGUILayout.Foldout(_showRawData, "原始數據結構 (Raw Data)", true);

        if (_showRawData)
        {
            // 繪製原本的 Inspector 內容
            DrawDefaultInspector();
        }
    }
}