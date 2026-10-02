using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;

/// <summary>自製視窗共用的事件選單；預設隱藏 Unity 內建功能，保留原生事件格式與排序。</summary>
internal sealed class FilteredUnityEventDrawer : UnityEventDrawer
{
    private readonly EditorWindow owner;
    private readonly Func<UnityEngine.Object> getTarget;
    private readonly Func<bool> showUnityFunctions;
    private readonly Func<bool> canEdit;
    private readonly Func<FilteredUnityEventDrawer, bool> isCurrent;

    public FilteredUnityEventDrawer(EditorWindow owner, Func<UnityEngine.Object> getTarget,
        Func<bool> showUnityFunctions, Func<bool> canEdit, Func<FilteredUnityEventDrawer, bool> isCurrent)
    {
        this.owner = owner;
        this.getTarget = getTarget;
        this.showUnityFunctions = showUnityFunctions;
        this.canEdit = canEdit;
        this.isCurrent = isCurrent;
    }
    private ReorderableList eventList;
    private bool hasSelection;
    public int SelectedIndex => hasSelection && eventList != null ? eventList.index : -1;
    protected override void SetupReorderableList(ReorderableList list)
    {
        base.SetupReorderableList(list);
        eventList = list;
        hasSelection = false;
    }
    protected override void OnSelectEvent(ReorderableList list)
    {
        base.OnSelectEvent(list);
        hasSelection = true;
    }
    protected override void OnReorderEvent(ReorderableList list)
    {
        base.OnReorderEvent(list);
        hasSelection = true;
    }

    protected override void DrawEvent(Rect rect, int index, bool isActive, bool isFocused)
    {
        if (showUnityFunctions()) { base.DrawEvent(rect, index, isActive, isFocused); return; }
        var call = eventList.serializedProperty.GetArrayElementAtIndex(index);
        var target = call.FindPropertyRelative("m_Target");
        var method = call.FindPropertyRelative("m_MethodName");
        float height = EditorGUIUtility.singleLineHeight;
        float leftWidth = rect.width * 0.36f;
        var stateRect = new Rect(rect.x, rect.y + 2, leftWidth, height);
        var targetRect = new Rect(rect.x, stateRect.yMax + 2, leftWidth, height);
        var methodRect = new Rect(stateRect.xMax + 4, stateRect.y, rect.width - leftWidth - 4, height);
        var argumentRect = new Rect(methodRect.x, targetRect.y, methodRect.width, height);
        EditorGUI.PropertyField(stateRect, call.FindPropertyRelative("m_CallState"), GUIContent.none);
        EditorGUI.BeginChangeCheck();
        var nextTarget = EditorGUI.ObjectField(targetRect, target.objectReferenceValue, typeof(UnityEngine.Object), true);
        if (EditorGUI.EndChangeCheck())
        {
            // 更換目標時清除函式選擇，避免同名函式意外綁到另一種元件。
            target.objectReferenceValue = nextTarget;
            call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = nextTarget != null ? nextTarget.GetType().AssemblyQualifiedName : "";
            method.stringValue = "";
            call.FindPropertyRelative("m_Mode").intValue = (int)PersistentListenerMode.Void;
        }
        string caption = string.IsNullOrEmpty(method.stringValue) ? "選擇功能" :
            (target.objectReferenceValue != null ? target.objectReferenceValue.GetType().Name : "遺失目標") + "." + method.stringValue;
        using (new EditorGUI.DisabledScope(target.objectReferenceValue == null))
            if (EditorGUI.DropdownButton(methodRect, new GUIContent(caption, caption), FocusType.Keyboard))
            {
                eventList.index = index;
                hasSelection = true;
                ShowMethods(call, methodRect);
            }
        // 已綁定的內建函式仍顯示並可編輯參數，篩選只作用於選單。
        var args = call.FindPropertyRelative("m_Arguments");
        var mode = (PersistentListenerMode)call.FindPropertyRelative("m_Mode").intValue;
        if (mode == PersistentListenerMode.Object)
        {
            string typeName = args.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue;
            Type type = string.IsNullOrEmpty(typeName) ? null : Type.GetType(typeName, false);
            if (type == null || !typeof(UnityEngine.Object).IsAssignableFrom(type)) type = typeof(UnityEngine.Object);
            var argument = args.FindPropertyRelative("m_ObjectArgument");
            EditorGUI.BeginChangeCheck();
            var value = EditorGUI.ObjectField(argumentRect, argument.objectReferenceValue, type, true);
            if (EditorGUI.EndChangeCheck()) argument.objectReferenceValue = value;
        }
        else
        {
            string field = mode == PersistentListenerMode.String ? "m_StringArgument"
                : mode == PersistentListenerMode.Bool ? "m_BoolArgument"
                : mode == PersistentListenerMode.Int ? "m_IntArgument"
                : mode == PersistentListenerMode.Float ? "m_FloatArgument" : null;
            if (field != null) EditorGUI.PropertyField(argumentRect, args.FindPropertyRelative(field), GUIContent.none);
        }
    }

    private void ShowMethods(SerializedProperty call, Rect rect)
    {
        call.serializedObject.ApplyModifiedProperties();
        string path = call.propertyPath;
        string arrayPath = eventList.serializedProperty.propertyPath;
        var ownerTarget = getTarget();
        var before = Capture(eventList.serializedProperty);
        var originalTarget = call.FindPropertyRelative("m_Target").objectReferenceValue;
        string originalMethod = call.FindPropertyRelative("m_MethodName").stringValue;
        var originalMode = (PersistentListenerMode)call.FindPropertyRelative("m_Mode").intValue;
        string originalArgumentType = call.FindPropertyRelative("m_Arguments.m_ObjectArgumentAssemblyTypeName").stringValue;
        Action<UnityEngine.Object, MethodInfo, PersistentListenerMode> select = (target, method, mode) =>
        {
            if (owner == null || !canEdit() || ownerTarget == null
                || getTarget() != ownerTarget || !isCurrent(this) || (method != null && target == null)) return;
            using (var fresh = new SerializedObject(ownerTarget))
            {
                var calls = fresh.FindProperty(arrayPath);
                if (calls == null || !SameSnapshot(before, Capture(calls))) return;
                var destination = fresh.FindProperty(path);
                if (destination == null) return;
                destination.FindPropertyRelative("m_MethodName").stringValue = method != null ? method.Name : "";
                destination.FindPropertyRelative("m_Mode").intValue = (int)mode;
                if (method != null)
                {
                    destination.FindPropertyRelative("m_Target").objectReferenceValue = target;
                    destination.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = target.GetType().AssemblyQualifiedName;
                    var args = destination.FindPropertyRelative("m_Arguments");
                    var objectArg = args.FindPropertyRelative("m_ObjectArgument");
                    Type argumentType = mode == PersistentListenerMode.Object ? method.GetParameters()[0].ParameterType : typeof(UnityEngine.Object);
                    args.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue = argumentType.AssemblyQualifiedName;
                    if (mode != PersistentListenerMode.Object || (objectArg.objectReferenceValue != null && !argumentType.IsInstanceOfType(objectArg.objectReferenceValue)))
                        objectArg.objectReferenceValue = null;
                }
                fresh.ApplyModifiedProperties();
            }
            owner.Repaint();
        };
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("不指定功能"), string.IsNullOrEmpty(originalMethod), () => select(null, null, PersistentListenerMode.Void));
        menu.AddSeparator("");
        var targets = new List<UnityEngine.Object>();
        GameObject go = originalTarget as GameObject;
        if (originalTarget is Component component) go = component.gameObject;
        if (go != null)
        {
            targets.Add(go);
            foreach (var item in go.GetComponents<Component>()) if (item != null) targets.Add(item);
        }
        else targets.Add(originalTarget);
        int count = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            if (target == null) continue;
            // 相同型別的多個元件也各自保留可選入口。
            string prefix = target.GetType().Name + " [" + (i + 1) + "]/";
            var methods = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            Array.Sort(methods, (a, b) => string.CompareOrdinal(a.ToString(), b.ToString()));
            foreach (var method in methods)
            {
                if (!TryGetMode(method, out var mode)) continue;
                var parameters = method.GetParameters();
                string argumentName = parameters.Length == 0 ? "" : mode == PersistentListenerMode.Object
                    ? parameters[0].ParameterType.FullName : parameters[0].ParameterType.Name;
                string signature = method.Name + " (" + argumentName + ")";
                bool chosen = target == originalTarget && method.Name == originalMethod
                    && (mode == originalMode || (mode == PersistentListenerMode.Void && originalMode == PersistentListenerMode.EventDefined))
                    && (mode != PersistentListenerMode.Object || parameters[0].ParameterType.AssemblyQualifiedName == originalArgumentType);
                menu.AddItem(new GUIContent(prefix + signature), chosen, () => select(target, method, mode));
                count++;
            }
        }
        if (count == 0) menu.AddDisabledItem(new GUIContent("沒有可用的自訂功能（可勾選顯示 Unity 內建功能）"));
        menu.DropDown(rect);
    }

    private static bool TryGetMode(MethodInfo method, out PersistentListenerMode mode)
    {
        mode = PersistentListenerMode.Void;
        string assembly = method.DeclaringType.Assembly.GetName().Name;
        if (assembly.StartsWith("UnityEngine", StringComparison.Ordinal) || assembly.StartsWith("UnityEditor", StringComparison.Ordinal)
            || method.ReturnType != typeof(void) || method.ContainsGenericParameters
            || method.IsDefined(typeof(ObsoleteAttribute), true)
            || (method.IsSpecialName && !method.Name.StartsWith("set_", StringComparison.Ordinal))) return false;
        var args = method.GetParameters();
        if (args.Length == 0) return true;
        if (args.Length != 1) return false;
        Type type = args[0].ParameterType;
        if (type == typeof(string)) mode = PersistentListenerMode.String;
        else if (type == typeof(bool)) mode = PersistentListenerMode.Bool;
        else if (type == typeof(int)) mode = PersistentListenerMode.Int;
        else if (type == typeof(float)) mode = PersistentListenerMode.Float;
        else if (typeof(UnityEngine.Object).IsAssignableFrom(type)) mode = PersistentListenerMode.Object;
        else return false;
        return true;
    }

    private static bool SameSnapshot(Snapshot a, Snapshot b)
    {
        if (a.Values.Count != b.Values.Count) return false;
        for (int i = 0; i < a.Values.Count; i++)
        {
            var x = a.Values[i]; var y = b.Values[i];
            if (x.Path != y.Path || x.Type != y.Type || x.IsArray != y.IsArray || !Equals(x.Data, y.Data)) return false;
        }
        return true;
    }

    private sealed class Value
    {
        public string Path;
        public SerializedPropertyType Type;
        public object Data;
        public bool IsArray;
    }

    private sealed class Snapshot
    {
        public readonly List<Value> Values = new List<Value>();
    }

    private static Snapshot Capture(SerializedProperty property)
    {
        var result = new Snapshot();
        var iterator = property.Copy();
        var end = property.GetEndProperty();
        bool children = true;
        while (iterator.Next(children) && !SerializedProperty.EqualContents(iterator, end))
        {
            children = iterator.propertyType == SerializedPropertyType.Generic;
            var value = new Value { Path = iterator.propertyPath.Substring(property.propertyPath.Length + 1),
                Type = iterator.propertyType, IsArray = iterator.isArray && iterator.propertyType != SerializedPropertyType.String };
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
            result.Values.Add(value);
        }
        return result;
    }

}
