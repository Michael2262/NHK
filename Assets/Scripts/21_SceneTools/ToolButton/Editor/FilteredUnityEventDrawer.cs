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
    private readonly UnityEngine.Object owner;
    private readonly Func<UnityEngine.Object> getTarget;
    private readonly Func<bool> showUnityFunctions;
    private readonly Func<bool> canEdit;
    private readonly Func<FilteredUnityEventDrawer, bool> isCurrent;
    private readonly Action commitStructureChange;

    public FilteredUnityEventDrawer(UnityEngine.Object owner, Func<UnityEngine.Object> getTarget,
        Func<bool> showUnityFunctions, Func<bool> canEdit, Func<FilteredUnityEventDrawer, bool> isCurrent,
        Action commitStructureChange = null)
    {
        this.owner = owner;
        this.getTarget = getTarget;
        this.showUnityFunctions = showUnityFunctions;
        this.canEdit = canEdit;
        this.isCurrent = isCurrent;
        this.commitStructureChange = commitStructureChange;
    }
    private ReorderableList eventList;
    private bool hasSelection;
    private bool structureChanged;
    public int SelectedIndex => hasSelection && eventList != null ? eventList.index : -1;
    protected override void SetupReorderableList(ReorderableList list)
    {
        base.SetupReorderableList(list);
        eventList = list;
        hasSelection = false;
        // 完成條件同步的呼叫端須提供結構提交回呼，才啟用條件 UI。
        // Shoot 等尚未接入的編輯器即使共用選項型別，也不會出現尚未支援的條件設定。
        if (commitStructureChange == null || FlagConditionalEventEditor.GetConditions(list.serializedProperty) == null) return;
        list.multiSelect = false;
        float originalHeight = list.elementHeight;
        list.elementHeightCallback = index => originalHeight + ConditionHeight(index);
        var add = list.onAddCallback;
        list.onAddCallback = current =>
        {
            if (!canEdit()) return;
            FlagConditionalEventEditor.EnsureConditions(current.serializedProperty);
            add(current);
            FlagConditionalEventEditor.EnsureConditions(current.serializedProperty);
            structureChanged = true;
        };
        var remove = list.onRemoveCallback;
        list.onRemoveCallback = current =>
        {
            if (!canEdit()) return;
            var conditions = FlagConditionalEventEditor.EnsureConditions(current.serializedProperty);
            int index = current.index;
            int count = current.serializedProperty.arraySize;
            remove(current);
            if (current.serializedProperty.arraySize == count - 1 && index >= 0 && index < conditions.arraySize)
                conditions.DeleteArrayElementAtIndex(index);
            structureChanged = true;
        };
        list.onReorderCallbackWithDetails = (current, oldIndex, newIndex) =>
        {
            var conditions = FlagConditionalEventEditor.EnsureConditions(current.serializedProperty);
            conditions.MoveArrayElement(oldIndex, newIndex);
            structureChanged = true;
        };
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        base.OnGUI(position, property, label);
        if (!structureChanged) return;
        // 先讓 ReorderableList 結束拖曳並釋放滑鼠，再重建繪製器與高度快取。
        structureChanged = false;
        CommitStructureChange();
    }

    private void RepaintOwner()
    {
        if (owner is EditorWindow window) window.Repaint();
        else if (owner is Editor editor) editor.Repaint();
    }

    private void CommitStructureChange()
    {
        eventList.serializedProperty.serializedObject.ApplyModifiedProperties();
        RepaintOwner();
        commitStructureChange?.Invoke();
        GUIUtility.ExitGUI();
    }

    private float ConditionHeight(int index)
    {
        var conditions = FlagConditionalEventEditor.GetConditions(eventList.serializedProperty);
        if (conditions == null || index < 0 || index >= conditions.arraySize) return 0;
        var condition = conditions.GetArrayElementAtIndex(index);
        if (!condition.FindPropertyRelative("Enabled").boolValue) return 0;
        bool missing = condition.FindPropertyRelative("Flag").objectReferenceValue == null;
        return (EditorGUIUtility.singleLineHeight + 2) * (missing ? 2 : 1);
    }

    private Rect DrawCondition(Rect rect, int index)
    {
        if (commitStructureChange == null) return rect;
        var conditions = FlagConditionalEventEditor.GetConditions(eventList.serializedProperty);
        if (conditions == null) return rect;
        var condition = index < conditions.arraySize ? conditions.GetArrayElementAtIndex(index) : null;
        bool enabled = condition != null && condition.FindPropertyRelative("Enabled").boolValue;
        float height = EditorGUIUtility.singleLineHeight;
        // 在拖曳把手旁保留獨立勾選欄；不占用原有的拖曳區域。
        Rect toggleRect = new Rect(rect.x, rect.y + 2, 18, height);
        using (new EditorGUI.DisabledScope(!canEdit()))
        {
            EditorGUI.BeginChangeCheck();
            bool next = EditorGUI.Toggle(toggleRect, new GUIContent("", "啟用此筆事件的 Flag 條件；未勾選時無條件執行。各筆條件在本清單開始執行時判斷。"), enabled);
            if (EditorGUI.EndChangeCheck())
            {
                condition = FlagConditionalEventEditor.EnsureConditions(eventList.serializedProperty).GetArrayElementAtIndex(index);
                condition.FindPropertyRelative("Enabled").boolValue = next;
                CommitStructureChange();
            }
            rect.xMin += 22;
            if (!enabled) return rect;

            var flag = condition.FindPropertyRelative("Flag");
            var invert = condition.FindPropertyRelative("Invert");
            float extraHeight = ConditionHeight(index);
            bool wasMissing = flag.objectReferenceValue == null;
            Rect flagRect = new Rect(rect.x, rect.y + 2, Mathf.Max(60, rect.width - 64), height);
            EditorGUI.BeginProperty(flagRect, GUIContent.none, flag);
            EditorGUI.BeginChangeCheck();
            var nextFlag = EditorGUI.ObjectField(flagRect, flag.objectReferenceValue, typeof(ProgressFlagDefinition), false);
            if (EditorGUI.EndChangeCheck())
            {
                flag.objectReferenceValue = nextFlag;
                EditorGUI.EndProperty();
                if (wasMissing != (nextFlag == null)) CommitStructureChange();
            }
            else EditorGUI.EndProperty();
            Rect invertRect = new Rect(flagRect.xMax + 4, flagRect.y, 60, height);
            EditorGUI.BeginProperty(invertRect, GUIContent.none, invert);
            invert.boolValue = EditorGUI.ToggleLeft(invertRect,
                new GUIContent("反向", "勾選後，沒有此 Flag 才執行；旗標服務未就緒時仍略過。"), invert.boolValue);
            EditorGUI.EndProperty();
            if (wasMissing)
                EditorGUI.LabelField(new Rect(rect.x, flagRect.yMax + 2, rect.width, height),
                    "尚未指定 Flag：此筆事件不執行", EditorStyles.miniLabel);
            rect.yMin += extraHeight;
        }
        return rect;
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
        rect = DrawCondition(rect, index);
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
            RepaintOwner();
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

/// <summary>共用事件列的條件配對操作；所有寫入皆透過 SerializedProperty 支援 Undo。</summary>
internal static class FlagConditionalEventEditor
{
    private const string CallsSuffix = ".m_PersistentCalls.m_Calls";

    internal sealed class ConditionSnapshot
    {
        public bool Enabled;
        public ProgressFlagDefinition Flag;
        public bool Invert;
    }

    public static SerializedProperty GetConditions(SerializedProperty calls)
    {
        if (!calls.propertyPath.EndsWith(CallsSuffix, StringComparison.Ordinal)) return null;
        string eventPath = calls.propertyPath.Substring(0, calls.propertyPath.Length - CallsSuffix.Length);
        return calls.serializedObject.FindProperty(eventPath + ".callConditions");
    }

    public static SerializedProperty EnsureConditions(SerializedProperty calls)
    {
        var conditions = GetConditions(calls);
        if (conditions == null) return null;
        int oldCount = conditions.arraySize;
        conditions.arraySize = calls.arraySize;
        for (int i = oldCount; i < conditions.arraySize; i++)
            Write(conditions.GetArrayElementAtIndex(i), new ConditionSnapshot());
        return conditions;
    }

    public static ConditionSnapshot CaptureCondition(SerializedProperty calls, int index)
    {
        var conditions = GetConditions(calls);
        if (conditions == null || index < 0 || index >= conditions.arraySize) return new ConditionSnapshot();
        var condition = conditions.GetArrayElementAtIndex(index);
        return new ConditionSnapshot
        {
            Enabled = condition.FindPropertyRelative("Enabled").boolValue,
            Flag = condition.FindPropertyRelative("Flag").objectReferenceValue as ProgressFlagDefinition,
            Invert = condition.FindPropertyRelative("Invert").boolValue
        };
    }

    public static void PasteCondition(SerializedProperty calls, int index, ConditionSnapshot snapshot)
    {
        var conditions = EnsureConditions(calls);
        if (conditions != null) Write(conditions.GetArrayElementAtIndex(index), snapshot ?? new ConditionSnapshot());
    }

    private static void Write(SerializedProperty condition, ConditionSnapshot snapshot)
    {
        condition.FindPropertyRelative("Enabled").boolValue = snapshot.Enabled;
        condition.FindPropertyRelative("Flag").objectReferenceValue = snapshot.Flag;
        condition.FindPropertyRelative("Invert").boolValue = snapshot.Invert;
    }

    public static void DeleteCall(SerializedProperty calls, int index)
    {
        var conditions = EnsureConditions(calls);
        conditions?.DeleteArrayElementAtIndex(index);
        calls.DeleteArrayElementAtIndex(index);
    }

    public static void ClearEvent(SerializedProperty property)
    {
        property.FindPropertyRelative("m_PersistentCalls.m_Calls").ClearArray();
        property.FindPropertyRelative("callConditions")?.ClearArray();
    }
}
