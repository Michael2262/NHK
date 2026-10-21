using NHK.StateTest;
using UnityEditor;
using UnityEngine;

/// <summary>固定保留需求欄位的位置，未選取的條件以灰色顯示，避免切換時改變列表布局。</summary>
[CustomPropertyDrawer(typeof(TaskClickConditions))]
public sealed class TaskClickConditionsDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUIUtility.singleLineHeight * 4 + EditorGUIUtility.standardVerticalSpacing * 3;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        int indent = EditorGUI.indentLevel;
        var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        float step = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        EditorGUI.LabelField(row, new GUIContent("每次點擊先決條件", "全部通過才計次及執行事件；不自動扣除資源。"));
        EditorGUI.indentLevel++;
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            row.y += step;
            var flags = property.FindPropertyRelative("prerequisites");
            EditorGUI.BeginChangeCheck();
            var selected = (ButtonPrerequisites)EditorGUI.EnumFlagsField(row, new GUIContent("先決條件"),
                (ButtonPrerequisites)flags.intValue);
            if (EditorGUI.EndChangeCheck())
                flags.intValue = (int)(selected & (ButtonPrerequisites.Stamina | ButtonPrerequisites.PhaseSlots));

            var mask = (ButtonPrerequisites)flags.intValue;
            row.y += step;
            using (new EditorGUI.DisabledScope((mask & ButtonPrerequisites.Stamina) == 0))
                EditorGUI.PropertyField(row, property.FindPropertyRelative("requiredStamina"), new GUIContent("最低所需體力"));
            row.y += step;
            using (new EditorGUI.DisabledScope((mask & ButtonPrerequisites.PhaseSlots) == 0))
                EditorGUI.PropertyField(row, property.FindPropertyRelative("requiredPhaseSlots"),
                    new GUIContent("最低所需 Slot", "Phase 總格數減目前 Slot；最後一格剩餘為 0。"));
        }
        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }
}
