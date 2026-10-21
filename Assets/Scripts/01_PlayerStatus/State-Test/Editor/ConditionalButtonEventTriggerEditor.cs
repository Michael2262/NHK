using NHK.StateTest;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>沿用 Tool Button Group 的事件選單，固定隱藏 Unity 原生方法。</summary>
[CustomEditor(typeof(ConditionalButtonEventTrigger))]
public sealed class ConditionalButtonEventTriggerEditor : Editor
{
    private FilteredUnityEventDrawer eventDrawer;
    private bool Locked => EditorApplication.isPlayingOrWillChangePlaymode;

    private const string MoveMenu = "CONTEXT/ConditionalButtonEventTrigger/移入 Button 的 On Click 事件";

    [MenuItem(MoveMenu, true)]
    private static bool CanMoveButtonEvents(MenuCommand command)
    {
        var trigger = command.context as ConditionalButtonEventTrigger;
        return !EditorApplication.isPlayingOrWillChangePlaymode && trigger != null
            && trigger.GetComponent<Button>() != null;
    }

    [MenuItem(MoveMenu)]
    private static void MoveButtonEvents(MenuCommand command)
    {
        if (!CanMoveButtonEvents(command)) return;
        var trigger = (ConditionalButtonEventTrigger)command.context;
        var button = trigger.GetComponent<Button>();
        using (var source = new SerializedObject(button))
        using (var destination = new SerializedObject(trigger))
        {
            var sourceCalls = source.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            var destinationCalls = destination.FindProperty("onTriggered.m_PersistentCalls.m_Calls");
            if (sourceCalls == null || destinationCalls == null) return;

            int moved = 0;
            for (int i = 0; i < sourceCalls.arraySize; i++)
            {
                var call = sourceCalls.GetArrayElementAtIndex(i);
                // 不移入自己的入口，避免事件再次呼叫自身；重複操作也不會增加入口。
                if (call.FindPropertyRelative("m_Target").objectReferenceValue == trigger
                    && call.FindPropertyRelative("m_MethodName").stringValue == nameof(ConditionalButtonEventTrigger.Trigger))
                    continue;

                int index = destinationCalls.arraySize;
                destinationCalls.arraySize++;
                CopyCall(call, destinationCalls.GetArrayElementAtIndex(index));
                moved++;
            }

            if (moved == 0) return;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("移入 Button 的 On Click 事件");

            // 保留目的端既有事件，依原順序追加；Button 僅留下條件檢查入口。
            sourceCalls.ClearArray();
            sourceCalls.arraySize = 1;
            var entry = sourceCalls.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("m_Target").objectReferenceValue = trigger;
            entry.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = trigger.GetType().AssemblyQualifiedName;
            entry.FindPropertyRelative("m_MethodName").stringValue = nameof(ConditionalButtonEventTrigger.Trigger);
            entry.FindPropertyRelative("m_Mode").intValue = (int)PersistentListenerMode.Void;
            entry.FindPropertyRelative("m_CallState").intValue = (int)UnityEventCallState.RuntimeOnly;

            destination.ApplyModifiedProperties();
            source.ApplyModifiedProperties();
            Undo.CollapseUndoOperations(undoGroup);
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            Debug.Log($"[ConditionalButtonEventTrigger] 已追加移入 {moved} 筆事件，Button 已改為呼叫 Trigger()。可用 Undo 還原。", trigger);
        }
    }

    // 直接複製持久化事件欄位，保留方法、參數、順序與 Runtime Only／Off 等設定。
    private static void CopyCall(SerializedProperty source, SerializedProperty destination)
    {
        var cursor = source.Copy();
        var end = source.GetEndProperty();
        while (cursor.Next(true) && !SerializedProperty.EqualContents(cursor, end))
        {
            string relativePath = cursor.propertyPath.Substring(source.propertyPath.Length + 1);
            var targetProperty = destination.FindPropertyRelative(relativePath);
            switch (cursor.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    targetProperty.objectReferenceValue = cursor.objectReferenceValue;
                    break;
                case SerializedPropertyType.String:
                    targetProperty.stringValue = cursor.stringValue;
                    break;
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Enum:
                    targetProperty.intValue = cursor.intValue;
                    break;
                case SerializedPropertyType.Float:
                    targetProperty.floatValue = cursor.floatValue;
                    break;
                case SerializedPropertyType.Boolean:
                    targetProperty.boolValue = cursor.boolValue;
                    break;
            }
        }
    }

    private void OnEnable() => Undo.undoRedoPerformed += ClearDrawer;

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= ClearDrawer;
        eventDrawer = null;
    }

    private void ClearDrawer()
    {
        eventDrawer = null;
        Repaint();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        using (new EditorGUI.DisabledScope(Locked))
        {
            var conditions = serializedObject.FindProperty("prerequisites");
            EditorGUI.BeginChangeCheck();
            var selected = (ButtonPrerequisites)EditorGUILayout.EnumFlagsField(
                new GUIContent("先決條件", "可多選；全部通過才執行。選無則直接執行。"),
                (ButtonPrerequisites)conditions.intValue);
            if (EditorGUI.EndChangeCheck())
            {
                conditions.intValue = (int)(selected & (ButtonPrerequisites.Stamina | ButtonPrerequisites.PhaseSlots));
                // 下拉選單回傳選擇時先保存，再讓下一輪 Layout 建立需求欄位。
                // 避免同一輪 GUI 臨時增加控制項，導致後續事件列表中斷寫入。
                serializedObject.ApplyModifiedProperties();
                ClearDrawer();
                GUIUtility.ExitGUI();
            }

            var mask = (ButtonPrerequisites)conditions.intValue;
            if ((mask & ButtonPrerequisites.Stamina) != 0)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("requiredStamina"),
                    new GUIContent("最低所需體力"));
            if ((mask & ButtonPrerequisites.PhaseSlots) != 0)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("requiredPhaseSlots"),
                    new GUIContent("最低所需 Slot", "Phase 總格數減目前 Slot；最後一格剩餘為 0。"));

            EditorGUILayout.Space();
            var property = serializedObject.FindProperty("onTriggered");
            if (eventDrawer == null)
                eventDrawer = new FilteredUnityEventDrawer(this, () => target, () => false,
                    () => !Locked, drawer => ReferenceEquals(eventDrawer, drawer));
            var label = new GUIContent("條件通過後事件（簡化）");
            var rect = EditorGUILayout.GetControlRect(false, eventDrawer.GetPropertyHeight(property, label));
            eventDrawer.OnGUI(rect, property, label);
        }

        EditorGUILayout.HelpBox("Button 的 On Click 請綁定此元件的 Trigger()。後續動作放在上方事件；本元件只檢查，不扣體力、不推進時間。", MessageType.Info);
        if (serializedObject.ApplyModifiedProperties()) ClearDrawer();
    }
}
