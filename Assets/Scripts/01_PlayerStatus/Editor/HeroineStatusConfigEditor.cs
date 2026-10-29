#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HeroineStatusConfig))]
public class HeroineStatusConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "ScheduleMode", "FlagSchedules", "AllSchedules");
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("女主角排程（開發設定）", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            var mode = serializedObject.FindProperty("ScheduleMode");
            EditorGUILayout.PropertyField(mode, new GUIContent("排程模式"));
            bool flagMode = mode.enumValueIndex == (int)HeroineScheduleMode.FlagRules;
            EditorGUILayout.PropertyField(serializedObject.FindProperty(flagMode ? "FlagSchedules" : "AllSchedules"),
                new GUIContent(flagMode ? "Flag 規則表清單" : "傳統時程表清單"), true);
            EditorGUILayout.HelpBox("模式與規則在啟動時固定。未啟用的時程表不會被執行，可清空引用後在 Editor 刪除其資產。", MessageType.Info);
            // 隱藏來源也能在開發時清理；不會自動刪除任何資產。
            EditorGUILayout.PropertyField(serializedObject.FindProperty(flagMode ? "AllSchedules" : "FlagSchedules"),
                new GUIContent(flagMode ? "未啟用：傳統表（可清空）" : "未啟用：Flag 表（可清空）"), true);
        }
        serializedObject.ApplyModifiedProperties();
        if (GUILayout.Button("開啟 Flag 規則編輯器")) HeroineFlagScheduleEditorWindow.Open();
    }
}
#endif
