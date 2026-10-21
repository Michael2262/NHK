using System.Collections.Generic;
using NHK.StateTest;
using UnityEditor;
using UnityEngine;

/// <summary>任務與獎勵清單共用的簡化事件介面；不改變原始序列化欄位。</summary>
public abstract class GoodChildEventListEditor : Editor
{
    private readonly Dictionary<string, FilteredUnityEventDrawer> drawers = new Dictionary<string, FilteredUnityEventDrawer>();
    protected abstract string ListName { get; }
    protected abstract string ItemName { get; }
    private bool Locked => EditorApplication.isPlayingOrWillChangePlaymode;

    protected virtual void OnEnable() => Undo.undoRedoPerformed += ClearDrawers;
    protected virtual void OnDisable()
    {
        Undo.undoRedoPerformed -= ClearDrawers;
        drawers.Clear();
    }

    private void ClearDrawers()
    {
        drawers.Clear();
        Repaint();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        using (new EditorGUI.DisabledScope(Locked))
        {
            DrawSettings();
            var list = serializedObject.FindProperty(ListName);
            EditorGUILayout.LabelField($"{ItemName}清單（依序執行）", EditorStyles.boldLabel);
            for (int i = 0; i < list.arraySize; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        item.isExpanded = EditorGUILayout.Foldout(item.isExpanded, $"{ItemName} {i + 1}", true);
                        using (new EditorGUI.DisabledScope(i == 0))
                            if (GUILayout.Button("上移", GUILayout.Width(44)))
                            {
                                list.MoveArrayElement(i, i - 1);
                                CommitStructure();
                            }
                        using (new EditorGUI.DisabledScope(i == list.arraySize - 1))
                            if (GUILayout.Button("下移", GUILayout.Width(44)))
                            {
                                list.MoveArrayElement(i, i + 1);
                                CommitStructure();
                            }
                        if (GUILayout.Button("刪除", GUILayout.Width(44)))
                        {
                            list.DeleteArrayElementAtIndex(i);
                            CommitStructure();
                        }
                    }
                    if (item.isExpanded) DrawItem(item);
                }
            }
            if (GUILayout.Button($"新增{ItemName}"))
            {
                int index = list.arraySize;
                list.arraySize++;
                var item = list.GetArrayElementAtIndex(index);
                InitializeItem(item);
                item.isExpanded = true;
                CommitStructure();
            }
        }
        DrawHelp();
        if (serializedObject.ApplyModifiedProperties()) ClearDrawers();
    }

    // 結構變更後結束本輪 GUI，避免舊事件路徑與布局繼續作用於其他項目。
    private void CommitStructure()
    {
        serializedObject.ApplyModifiedProperties();
        ClearDrawers();
        GUIUtility.ExitGUI();
    }

    protected void DrawEvent(SerializedProperty property, string title)
    {
        string path = property.propertyPath;
        if (!drawers.TryGetValue(path, out var drawer))
        {
            drawer = new FilteredUnityEventDrawer(this, () => target, () => false,
                () => !Locked, candidate => drawers.TryGetValue(path, out var current) && ReferenceEquals(candidate, current));
            drawers.Add(path, drawer);
        }
        var label = new GUIContent(title + "（簡化）");
        var rect = EditorGUILayout.GetControlRect(false, drawer.GetPropertyHeight(property, label));
        drawer.OnGUI(rect, property, label);
    }

    protected static void ClearEvent(SerializedProperty property)
        => property.FindPropertyRelative("m_PersistentCalls.m_Calls").ClearArray();
    protected virtual void DrawSettings() { }
    protected virtual void DrawHelp() { }
    protected abstract void DrawItem(SerializedProperty item);
    protected abstract void InitializeItem(SerializedProperty item);
}

[CustomEditor(typeof(GoodChildTaskDistributor))]
public sealed class GoodChildTaskDistributorEditor : GoodChildEventListEditor
{
    protected override string ListName => "tasks";
    protected override string ItemName => "任務";
    protected override void DrawSettings()
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("firstReceiver"), new GUIContent("第一個任務接收器"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("secondReceiver"), new GUIContent("第二個任務接收器"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("loopTasks"), new GUIContent("循環發放"));
    }
    protected override void DrawItem(SerializedProperty item)
    {
        EditorGUILayout.PropertyField(item.FindPropertyRelative("clickCount"), new GUIContent("需求點擊次數"));
        EditorGUILayout.PropertyField(item.FindPropertyRelative("oncePerDay"),
            new GUIContent("一日限點擊一次", "每次有效點擊後鎖住當天，隔天保留進度並自動解鎖。"));
        EditorGUILayout.PropertyField(item.FindPropertyRelative("conditions"), true);
        DrawEvent(item.FindPropertyRelative("onClick"), "每次點擊事件");
        DrawEvent(item.FindPropertyRelative("onCompleted"), "完成事件");
    }
    protected override void InitializeItem(SerializedProperty item)
    {
        // Unity 擴充陣列會複製上一筆，新增時明確清空事件與條件。
        item.FindPropertyRelative("clickCount").intValue = 4;
        item.FindPropertyRelative("oncePerDay").boolValue = false;
        var conditions = item.FindPropertyRelative("conditions");
        conditions.FindPropertyRelative("prerequisites").intValue = 0;
        conditions.FindPropertyRelative("requiredStamina").intValue = 1;
        conditions.FindPropertyRelative("requiredPhaseSlots").intValue = 1;
        ClearEvent(item.FindPropertyRelative("onClick"));
        ClearEvent(item.FindPropertyRelative("onCompleted"));
    }
}

[CustomEditor(typeof(GoodChildTaskRewarder))]
public sealed class GoodChildTaskRewarderEditor : GoodChildEventListEditor
{
    protected override string ListName => "rewards";
    protected override string ItemName => "獎勵";
    protected override void DrawItem(SerializedProperty item)
        => DrawEvent(item.FindPropertyRelative("onReward"), "獎勵事件");
    protected override void InitializeItem(SerializedProperty item)
        => ClearEvent(item.FindPropertyRelative("onReward"));
    protected override void DrawHelp()
        => EditorGUILayout.HelpBox("呼叫 Reward() 依序領取一筆；全部領完後，每次繼續領最後一筆。進度不存檔，停用再啟用不重置。", MessageType.Info);
}
