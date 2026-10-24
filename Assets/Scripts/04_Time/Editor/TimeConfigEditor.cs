using UnityEditor;
using UnityEngine;

/// <summary>在每個 Slot 標題下直接填寫 Mini 數量，保留舊 Config 的欄位格式。</summary>
[CustomEditor(typeof(TimeConfig))]
public class TimeConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "SlotsPerPhase", "MiniSettings");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("各階段 Slot / Mini 設定", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Mini 是 Slot 內已推進的數量，從 0 開始，累積滿設定值就進入下一個 Slot。未設定時預設為 1。", MessageType.Info);
        var names = serializedObject.FindProperty("PhaseNames");
        var counts = serializedObject.FindProperty("SlotsPerPhase");
        var phases = serializedObject.FindProperty("MiniSettings");

        // 僅在使用者編輯時補齊資料；單純查看 Inspector 不改寫資產。
        for (int p = 0; p < names.arraySize; p++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Phase {p}：{names.GetArrayElementAtIndex(p).stringValue}", EditorStyles.boldLabel);
            int count = p < counts.arraySize ? Mathf.Max(1, counts.GetArrayElementAtIndex(p).intValue) : 1;
            EditorGUI.BeginChangeCheck();
            int editedCount = Mathf.Max(1, EditorGUILayout.IntField("Slot 數量", count));
            if (EditorGUI.EndChangeCheck())
            {
                while (counts.arraySize <= p)
                {
                    int index = counts.arraySize++;
                    counts.GetArrayElementAtIndex(index).intValue = 1;
                }
                counts.GetArrayElementAtIndex(p).intValue = editedCount;
                count = editedCount;
            }

            for (int s = 0; s < count; s++)
            {
                EditorGUILayout.LabelField($"Slot {s + 1}");
                EditorGUI.indentLevel++;
                var minis = p < phases.arraySize
                    ? phases.GetArrayElementAtIndex(p).FindPropertyRelative("MinisPerSlot") : null;
                int value = minis != null && s < minis.arraySize ? Mathf.Max(1, minis.GetArrayElementAtIndex(s).intValue) : 1;
                EditorGUI.BeginChangeCheck();
                int edited = Mathf.Max(1, EditorGUILayout.IntField("Mini 數量", value));
                if (EditorGUI.EndChangeCheck())
                {
                    while (phases.arraySize <= p)
                    {
                        int index = phases.arraySize++;
                        phases.GetArrayElementAtIndex(index).FindPropertyRelative("MinisPerSlot").ClearArray();
                    }
                    minis = phases.GetArrayElementAtIndex(p).FindPropertyRelative("MinisPerSlot");
                    while (minis.arraySize <= s)
                    {
                        int index = minis.arraySize++;
                        minis.GetArrayElementAtIndex(index).intValue = 1;
                    }
                    minis.GetArrayElementAtIndex(s).intValue = edited;
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }
        serializedObject.ApplyModifiedProperties();
    }
}
