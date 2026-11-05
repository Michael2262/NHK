using UnityEditor;

[CustomEditor(typeof(MultiModeButtonHoldTrigger))]
public sealed class MultiModeButtonHoldTriggerEditor : GoodChildEventListEditor
{
    protected override string ListName => "modes";
    protected override string ItemName => "模式";
    protected override string ListTitle => "按鈕模式（第一組為預設）";

    protected override void DrawSettings()
    {
        DrawPropertiesExcluding(serializedObject, "m_Script", ListName);
    }

    protected override void DrawItem(SerializedProperty mode)
    {
        EditorGUILayout.PropertyField(mode.FindPropertyRelative("modeID"));
        EditorGUILayout.PropertyField(mode.FindPropertyRelative("textTableKey"));
        EditorGUILayout.PropertyField(mode.FindPropertyRelative("holdDuration"));
        DrawEvent(mode.FindPropertyRelative("onPressed"), "On Pressed");
        DrawEvent(mode.FindPropertyRelative("onHoldCompleted"), "On Hold Completed");
        EditorGUILayout.PropertyField(mode.FindPropertyRelative("showCompletionFeedback"));
        EditorGUILayout.PropertyField(mode.FindPropertyRelative("completionTextKey"));
    }

    protected override void InitializeItem(SerializedProperty mode)
    {
        // Unity 擴充清單時會複製前一組；新模式從空白事件與預設秒數開始。
        mode.FindPropertyRelative("modeID").stringValue = string.Empty;
        mode.FindPropertyRelative("textTableKey").stringValue = string.Empty;
        mode.FindPropertyRelative("holdDuration").floatValue = 1f;
        ClearEvent(mode.FindPropertyRelative("onPressed"));
        ClearEvent(mode.FindPropertyRelative("onHoldCompleted"));
        mode.FindPropertyRelative("showCompletionFeedback").boolValue = false;
        mode.FindPropertyRelative("completionTextKey").stringValue = string.Empty;
    }
}
