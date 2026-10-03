using UnityEditor;
using UnityEngine;

/// <summary>一般 Inspector 的行為設定；Parallel 不使用所屬 Timer。</summary>
[CustomPropertyDrawer(typeof(SceneTimerController.BehaviorDefinition))]
public sealed class SceneTimerBehaviorDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (!property.isExpanded) return height;
        foreach (string name in Fields)
            height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(property.FindPropertyRelative(name), true);
        return height;
    }

    private static readonly string[] Fields = { "ID", "StartMode", "Timer", "Duration", "OnCompleted" };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        int indent = EditorGUI.indentLevel;
        try
        {
            position.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
            if (!property.isExpanded) return;
            EditorGUI.indentLevel++;
            foreach (string name in Fields)
            {
                position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                var child = property.FindPropertyRelative(name);
                position.height = EditorGUI.GetPropertyHeight(child, true);
                var mode = property.FindPropertyRelative("StartMode");
                using (new EditorGUI.DisabledScope(name == "Timer" && !mode.hasMultipleDifferentValues
                    && mode.intValue == (int)TimerStartMode.Parallel))
                    EditorGUI.PropertyField(position, child, true);
            }
        }
        finally { EditorGUI.indentLevel = indent; EditorGUI.EndProperty(); }
    }
}
