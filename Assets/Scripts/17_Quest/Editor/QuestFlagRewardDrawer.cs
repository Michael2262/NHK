#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Quest Window 與 SO Inspector 共用；只有開啟 Flag 才能編輯存活週期。</summary>
[CustomPropertyDrawer(typeof(QuestFlagReward))]
public sealed class QuestFlagRewardDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUIUtility.singleLineHeight * (property.isExpanded ? 4 : 1)
            + EditorGUIUtility.standardVerticalSpacing * (property.isExpanded ? 3 : 0);

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        int indent = EditorGUI.indentLevel;
        try
        {
            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);
            if (!property.isExpanded) return;
            EditorGUI.indentLevel++;
            float step = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            line.y += step;
            EditorGUI.PropertyField(line, property.FindPropertyRelative("Flag"), new GUIContent("Flag SO"));
            line.y += step;
            var enabled = property.FindPropertyRelative("Enabled");
            EditorGUI.PropertyField(line, enabled, new GUIContent("開啟"));
            line.y += step;
            using (new EditorGUI.DisabledScope(enabled.hasMultipleDifferentValues || !enabled.boolValue))
                EditorGUI.PropertyField(line, property.FindPropertyRelative("Lifetime"),
                    new GUIContent("存活週期", "只有開啟 Flag 才會使用；關閉時移除所有週期與隔日預約中的同名 Flag。"));
        }
        finally
        {
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}
#endif
