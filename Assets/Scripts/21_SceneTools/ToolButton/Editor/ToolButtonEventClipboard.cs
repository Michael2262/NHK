using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>UnityEvent 的序列化快照；不持有來源 Property，不經 JSON 轉換物件參照。</summary>
internal static class ToolButtonEventClipboard
{
    private sealed class Value
    {
        public string Path;
        public SerializedPropertyType Type;
        public object Data;
        public bool IsArray;
    }

    private static List<Value>[] events;
    private static bool fromPlayMode;
    private static List<Value> singleEvent;
    private static bool singleFromPlayMode;
    public static string SingleLabel { get; private set; }
    public static bool CanPasteSingle => singleEvent != null && !singleFromPlayMode
        && !EditorApplication.isPlayingOrWillChangePlaymode;
    public static string Label { get; private set; }
    public static bool CanPaste(int count) => events != null && events.Length == count && !fromPlayMode
        && !EditorApplication.isPlayingOrWillChangePlaymode;

    public static void Copy(string label, params SerializedProperty[] properties)
    {
        var snapshot = new List<Value>[properties.Length];
        for (int i = 0; i < properties.Length; i++) snapshot[i] = Capture(properties[i]);
        events = snapshot;
        Label = label;
        fromPlayMode = EditorApplication.isPlayingOrWillChangePlaymode;
    }

    public static bool Paste(ToolButtonGroupDisplayControl target, out string error,
        params SerializedProperty[] destinations)
    {
        error = string.Empty;
        if (target == null || !CanPaste(destinations.Length))
        { error = "剪貼簿類型不符，或目前不允許貼上。"; return false; }

        // 先檢查全部參照，兩組事件貼上時不會只套用一半。
        foreach (var snapshot in events)
            if (!ValidateReferences(target, snapshot, out error)) return false;
        for (int i = 0; i < destinations.Length; i++) Apply(destinations[i], events[i]);
        return true;
    }

    public static void CopySingle(string label, SerializedProperty property)
    {
        singleEvent = Capture(property);
        SingleLabel = label;
        singleFromPlayMode = EditorApplication.isPlayingOrWillChangePlaymode;
    }

    public static bool PasteSingle(ToolButtonGroupDisplayControl target, SerializedProperty destination,
        out string error)
    {
        error = string.Empty;
        if (target == null || destination == null || !CanPasteSingle)
        { error = "尚未複製單項事件、未選取貼上目標，或目前不允許貼上。"; return false; }
        if (!ValidateReferences(target, singleEvent, out error)) return false;
        Apply(destination, singleEvent);
        return true;
    }

    private static bool ValidateReferences(ToolButtonGroupDisplayControl target, List<Value> snapshot,
        out string error)
    {
        error = string.Empty;
        foreach (var value in snapshot)
        {
            if (value.Type != SerializedPropertyType.ObjectReference || ReferenceEquals(value.Data, null)) continue;
            var reference = (UnityEngine.Object)value.Data;
            if (reference == null)
            { error = "複製的物件已被刪除或卸載，請重新複製事件。"; return false; }
            if (EditorUtility.IsPersistent(reference)) continue;
            if (EditorUtility.IsPersistent(target))
            { error = "事件含場景物件，無法貼入 Prefab 資產。"; return false; }
            GameObject referencedObject = reference as GameObject;
            if (reference is Component component) referencedObject = component.gameObject;
            if (referencedObject == null || referencedObject.scene != target.gameObject.scene)
            { error = "事件含其他場景或 Prefab Stage 的物件，請在相同場景貼上或重新指定參照。"; return false; }
        }
        return true;
    }

    private static List<Value> Capture(SerializedProperty property)
    {
        var result = new List<Value>();
        var iterator = property.Copy();
        var end = property.GetEndProperty();
        bool children = true;
        while (iterator.Next(children) && !SerializedProperty.EqualContents(iterator, end))
        {
            children = iterator.propertyType == SerializedPropertyType.Generic;
            var value = new Value
            {
                Path = iterator.propertyPath.Substring(property.propertyPath.Length + 1),
                Type = iterator.propertyType,
                IsArray = iterator.isArray && iterator.propertyType != SerializedPropertyType.String
            };
            if (value.IsArray) value.Data = iterator.arraySize;
            else switch (value.Type)
            {
                case SerializedPropertyType.Generic:
                case SerializedPropertyType.ArraySize: continue;
                case SerializedPropertyType.Integer: value.Data = iterator.longValue; break;
                case SerializedPropertyType.Boolean: value.Data = iterator.boolValue; break;
                case SerializedPropertyType.Float: value.Data = iterator.doubleValue; break;
                case SerializedPropertyType.String: value.Data = iterator.stringValue; break;
                case SerializedPropertyType.Enum: value.Data = iterator.intValue; break;
                case SerializedPropertyType.ObjectReference: value.Data = iterator.objectReferenceValue; break;
                default: throw new NotSupportedException("不支援的事件欄位：" + value.Type);
            }
            result.Add(value);
        }
        return result;
    }

    private static void Apply(SerializedProperty property, List<Value> values)
    {
        foreach (var value in values)
        {
            var destination = property.FindPropertyRelative(value.Path);
            if (value.IsArray) destination.arraySize = (int)value.Data;
            else switch (value.Type)
            {
                case SerializedPropertyType.Integer: destination.longValue = (long)value.Data; break;
                case SerializedPropertyType.Boolean: destination.boolValue = (bool)value.Data; break;
                case SerializedPropertyType.Float: destination.doubleValue = (double)value.Data; break;
                case SerializedPropertyType.String: destination.stringValue = (string)value.Data; break;
                case SerializedPropertyType.Enum: destination.intValue = (int)value.Data; break;
                case SerializedPropertyType.ObjectReference: destination.objectReferenceValue = (UnityEngine.Object)value.Data; break;
            }
        }
    }
}
