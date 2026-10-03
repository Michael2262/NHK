using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpineEventToUnityEvent))]
public class SpineEventToUnityEventEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        var databaseProperty = serializedObject.FindProperty("eventDatabase");
        EditorGUILayout.PropertyField(databaseProperty, new GUIContent("共用事件 ID 資料庫"));
        var database = databaseProperty.objectReferenceValue as SpineEventDatabase;
        EditorGUILayout.HelpBox(database == null
            ? "未指定資料庫：沿用手填 ID。指定共用資料庫後可用下拉選單，既有 ID 與 UnityEvent 不會被清除。"
            : "從資料庫選擇事件 ID，再綁定本場景的反應。資料庫未收錄的舊 ID 仍保留並照常觸發；改名不會自動同步。", MessageType.Info);
        if (database != null)
            foreach (var error in database.GetValidationErrors())
                EditorGUILayout.HelpBox(error, MessageType.Warning);
        if (Application.isPlaying)
            EditorGUILayout.HelpBox("事件映射在 Awake 建立。請停止播放後修改設定，再重新進入 Play Mode 驗證。", MessageType.Info);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("eventMappings"), new GUIContent("事件映射"), true);
        serializedObject.ApplyModifiedProperties();

        var receiver = (SpineEventToUnityEvent)target;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (receiver.eventMappings == null) return;
        foreach (var mapping in receiver.eventMappings)
        {
            if (mapping == null || string.IsNullOrWhiteSpace(mapping.spineEventName))
                EditorGUILayout.HelpBox("有映射尚未填寫事件 ID。", MessageType.Warning);
            else if (!seen.Add(mapping.spineEventName))
                EditorGUILayout.HelpBox($"映射 ID 重複：{mapping.spineEventName}。執行時只使用第一筆；多個動作請放在同一筆 UnityEvent。", MessageType.Warning);
        }
    }
}

/// <summary>保留原有字串及 UnityEvent 序列化欄位，使用標準清單支援新增、刪除與排序。</summary>
[CustomPropertyDrawer(typeof(SpineUnityEventMapping))]
public class SpineUnityEventMappingDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 3 + 12
            + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("onEventTriggered"), true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var id = property.FindPropertyRelative("spineEventName");
        var reaction = property.FindPropertyRelative("onEventTriggered");
        var database = property.serializedObject.FindProperty("eventDatabase")?.objectReferenceValue as SpineEventDatabase;
        float line = EditorGUIUtility.singleLineHeight;
        var row = new Rect(position.x, position.y, position.width, line);
        EditorGUI.LabelField(row, label, EditorStyles.boldLabel);
        row.y += line + 4;
        string help;
        if (database == null)
        {
            EditorGUI.PropertyField(row, id, new GUIContent("Spine 事件 ID"));
            help = "手填模式：事件名稱區分大小寫。";
        }
        else
        {
            var ids = new List<string> { id.stringValue };
            var labels = new List<GUIContent> { new GUIContent(string.IsNullOrEmpty(id.stringValue) ? "請選擇事件 ID" : $"保留目前 ID：{id.stringValue}") };
            var seen = new HashSet<string>(StringComparer.Ordinal);
            int selected = 0;
            if (database.Entries != null)
                foreach (var entry in database.Entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.id) || !seen.Add(entry.id)) continue;
                    ids.Add(entry.id);
                    labels.Add(new GUIContent(string.IsNullOrEmpty(entry.displayName) ? entry.id : $"{entry.displayName}（{entry.id}）", entry.description));
                    if (entry.id == id.stringValue) selected = ids.Count - 1;
                }
            EditorGUI.BeginChangeCheck();
            int next = EditorGUI.Popup(row, new GUIContent("Spine 事件 ID"), selected, labels.ToArray());
            if (EditorGUI.EndChangeCheck()) id.stringValue = ids[next];
            var current = database.Find(id.stringValue);
            help = current == null ? "警告：ID 未收錄於資料庫；請選擇有效 ID，或暫時保留舊設定。" : current.description;
        }
        row.y += line + 4;
        EditorGUI.LabelField(row, new GUIContent(help, help), EditorStyles.miniLabel);
        row.y += line + 4;
        row.height = EditorGUI.GetPropertyHeight(reaction, true);
        EditorGUI.PropertyField(row, reaction, new GUIContent("事件觸發時"), true);
        EditorGUI.EndProperty();
    }
}
