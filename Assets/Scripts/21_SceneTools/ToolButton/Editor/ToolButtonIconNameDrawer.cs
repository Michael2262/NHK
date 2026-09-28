using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ToolButtonIconNameAttribute))]
public class ToolButtonIconNameDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var icons = property.serializedObject.FindProperty("icons");
        var names = new List<string>();
        if (icons != null)
        {
            for (int i = 0; i < icons.arraySize; i++)
            {
                string name = icons.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                if (!string.IsNullOrWhiteSpace(name) && !names.Contains(name)) names.Add(name);
            }
        }
        // 保留既有名稱，清單改名時不默默切換成另一種 Icon。
        int selected = names.IndexOf(property.stringValue);
        bool missing = selected < 0;
        if (selected < 0)
        {
            names.Insert(0, $"（未定義：{property.stringValue}）");
            selected = 0;
        }
        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
        EditorGUI.BeginChangeCheck();
        int next = EditorGUI.Popup(position, label.text, selected, names.ToArray());
        if (EditorGUI.EndChangeCheck() && !(missing && next == 0)) property.stringValue = names[next];
        EditorGUI.showMixedValue = false;
        EditorGUI.EndProperty();
    }
}
