using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(ConditionalButtonListCaller))]
public class ConditionalButtonListCallerEditor : Editor
{
    private SerializedProperty _groupsProp;
    private SerializedProperty _circleButtonContainerProp;

    private List<bool> _groupFoldouts = new List<bool>();
    private List<List<bool>> _entryFoldouts = new List<List<bool>>();

    private void OnEnable()
    {
        _groupsProp = serializedObject.FindProperty("groups");
        _circleButtonContainerProp = serializedObject.FindProperty("circleButtonContainer");
        SyncFoldoutState();
    }

    private void SyncFoldoutState()
    {
        while (_groupFoldouts.Count < _groupsProp.arraySize)
            _groupFoldouts.Add(true);
        while (_groupFoldouts.Count > _groupsProp.arraySize)
            _groupFoldouts.RemoveAt(_groupFoldouts.Count - 1);

        while (_entryFoldouts.Count < _groupsProp.arraySize)
            _entryFoldouts.Add(new List<bool>());
        while (_entryFoldouts.Count > _groupsProp.arraySize)
            _entryFoldouts.RemoveAt(_entryFoldouts.Count - 1);

        for (int g = 0; g < _groupsProp.arraySize; g++)
        {
            int entryCount = _groupsProp.GetArrayElementAtIndex(g)
                .FindPropertyRelative("entries").arraySize;

            while (_entryFoldouts[g].Count < entryCount)
                _entryFoldouts[g].Add(false);
            while (_entryFoldouts[g].Count > entryCount)
                _entryFoldouts[g].RemoveAt(_entryFoldouts[g].Count - 1);
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SyncFoldoutState();

        EditorGUILayout.PropertyField(_circleButtonContainerProp, new GUIContent("圓按鈕容器"));
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("按鈕群組", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        for (int g = 0; g < _groupsProp.arraySize; g++)
        {
            DrawGroup(g);
            EditorGUILayout.Space(4);
        }

        // 群組增減
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("＋ 新增群組", GUILayout.Width(120)))
        {
            _groupsProp.InsertArrayElementAtIndex(_groupsProp.arraySize);
            SyncFoldoutState();
        }

        EditorGUI.BeginDisabledGroup(_groupsProp.arraySize == 0);
        if (GUILayout.Button("－ 移除最後", GUILayout.Width(120)))
        {
            _groupsProp.DeleteArrayElementAtIndex(_groupsProp.arraySize - 1);
            SyncFoldoutState();
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }

    // ══════════════════════════════════════════
    //  群組繪製
    // ══════════════════════════════════════════

    private void DrawGroup(int groupIndex)
    {
        var groupProp = _groupsProp.GetArrayElementAtIndex(groupIndex);
        var circleBtnProp = groupProp.FindPropertyRelative("circleButton");
        var listPosProp = groupProp.FindPropertyRelative("listPosition");
        var entriesProp = groupProp.FindPropertyRelative("entries");

        string btnName = circleBtnProp.objectReferenceValue != null
            ? circleBtnProp.objectReferenceValue.name
            : "(未指定)";

        EditorGUILayout.BeginVertical("box");

        _groupFoldouts[groupIndex] = EditorGUILayout.Foldout(
            _groupFoldouts[groupIndex], $"群組 {groupIndex} — {btnName}", true, EditorStyles.foldoutHeader);

        if (_groupFoldouts[groupIndex])
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(circleBtnProp, new GUIContent("圓形按鈕"));
            EditorGUILayout.PropertyField(listPosProp, new GUIContent("面板位置"));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField($"選項列表（{entriesProp.arraySize} 個）", EditorStyles.boldLabel);

            for (int e = 0; e < entriesProp.arraySize; e++)
            {
                DrawEntry(groupIndex, e, entriesProp);
            }

            // entry 增減
            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("＋ 新增選項", GUILayout.Width(100)))
            {
                entriesProp.InsertArrayElementAtIndex(entriesProp.arraySize);
                SyncFoldoutState();
                _entryFoldouts[groupIndex][entriesProp.arraySize - 1] = true;
            }

            EditorGUI.BeginDisabledGroup(entriesProp.arraySize == 0);
            if (GUILayout.Button("－ 移除最後", GUILayout.Width(100)))
            {
                entriesProp.DeleteArrayElementAtIndex(entriesProp.arraySize - 1);
                SyncFoldoutState();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    // ══════════════════════════════════════════
    //  Entry 繪製
    // ══════════════════════════════════════════

    private void DrawEntry(int groupIndex, int entryIndex, SerializedProperty entriesProp)
    {
        var entryProp = entriesProp.GetArrayElementAtIndex(entryIndex);

        var buttonTypeProp = entryProp.FindPropertyRelative("buttonType");
        var locKeyProp = entryProp.FindPropertyRelative("localizationKey");
        var conditionModeProp = entryProp.FindPropertyRelative("conditionMode");

        var buttonType = (ResponseListService.ButtonType)buttonTypeProp.enumValueIndex;
        var conditionMode = (ResponseListService.ConditionMode)conditionModeProp.enumValueIndex;
        string locKey = locKeyProp.stringValue;

        // 摘要標題
        string typeIcon;
        switch (buttonType)
        {
            case ResponseListService.ButtonType.Complex: typeIcon = "★"; break;
            case ResponseListService.ButtonType.Rest: typeIcon = "💤"; break;
            default: typeIcon = "○"; break;
        }
        string condIcon = conditionMode == ResponseListService.ConditionMode.Flag ? "🚩" : "🔢";
        string displayName = string.IsNullOrEmpty(locKey) ? "(未設定)" : locKey;
        string title = $"{typeIcon} [{buttonType}] {condIcon} {displayName}";

        EditorGUILayout.BeginVertical("helpbox");

        // Foldout + 刪除按鈕
        EditorGUILayout.BeginHorizontal();

        _entryFoldouts[groupIndex][entryIndex] = EditorGUILayout.Foldout(
            _entryFoldouts[groupIndex][entryIndex], title, true);

        if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(18)))
        {
            entriesProp.DeleteArrayElementAtIndex(entryIndex);
            SyncFoldoutState();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.EndHorizontal();

        if (_entryFoldouts[groupIndex][entryIndex])
        {
            EditorGUI.indentLevel++;

            // ── 共用欄位 ──
            EditorGUILayout.PropertyField(buttonTypeProp, new GUIContent("按鈕類型"));
            EditorGUILayout.PropertyField(locKeyProp, new GUIContent("多語系 Key"));

            EditorGUILayout.Space(4);

            // ── 條件模式 ──
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(conditionModeProp, new GUIContent("條件模式"));
            bool conditionModeChanged = EditorGUI.EndChangeCheck();

            // 切換時清除另一種的資料
            if (conditionModeChanged)
            {
                var newMode = (ResponseListService.ConditionMode)conditionModeProp.enumValueIndex;
                if (newMode == ResponseListService.ConditionMode.Value)
                {
                    var flagsProp = entryProp.FindPropertyRelative("conditionFlags");
                    flagsProp.ClearArray();
                }
                else
                {
                    entryProp.FindPropertyRelative("conditionValue").objectReferenceValue = null;
                }
            }

            // 依條件模式顯示對應欄位
            conditionMode = (ResponseListService.ConditionMode)conditionModeProp.enumValueIndex;

            if (conditionMode == ResponseListService.ConditionMode.Value)
            {
                DrawValueCondition(entryProp);
            }
            else
            {
                DrawFlagCondition(entryProp);
            }

            EditorGUILayout.Space(4);

            // ── 依按鈕類型顯示專屬欄位 ──
            buttonType = (ResponseListService.ButtonType)buttonTypeProp.enumValueIndex;

            switch (buttonType)
            {
                case ResponseListService.ButtonType.Simple:
                    DrawSimpleFields(entryProp);
                    break;
                case ResponseListService.ButtonType.Complex:
                    DrawComplexFields(entryProp);
                    break;
                case ResponseListService.ButtonType.Rest:
                    DrawRestFields(entryProp);
                    break;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    // ══════════════════════════════════════════
    //  條件欄位
    // ══════════════════════════════════════════

    private void DrawValueCondition(SerializedProperty entryProp)
    {
        EditorGUILayout.PropertyField(
            entryProp.FindPropertyRelative("conditionValue"),
            new GUIContent("條件 Value", "0=隱藏, ≥1=正常, <0=半透明, 留空=永遠顯示"));
    }

    private void DrawFlagCondition(SerializedProperty entryProp)
    {
        var flagsProp = entryProp.FindPropertyRelative("conditionFlags");
        var logicProp = entryProp.FindPropertyRelative("flagLogic");

        EditorGUILayout.PropertyField(logicProp, new GUIContent("Flag 邏輯", "All=全部為true, Any=任一為true"));

        EditorGUILayout.LabelField("條件 Flags", EditorStyles.miniBoldLabel);

        EditorGUI.indentLevel++;

        for (int i = 0; i < flagsProp.arraySize; i++)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PropertyField(
                flagsProp.GetArrayElementAtIndex(i),
                new GUIContent($"Flag {i}"));

            if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(18)))
            {
                flagsProp.DeleteArrayElementAtIndex(i);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUI.indentLevel--;

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("＋ Flag", GUILayout.Width(80)))
        {
            flagsProp.InsertArrayElementAtIndex(flagsProp.arraySize);
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    // ══════════════════════════════════════════
    //  Simple / Complex / Rest 欄位
    // ══════════════════════════════════════════

    private void DrawSimpleFields(SerializedProperty entryProp)
    {
        EditorGUILayout.LabelField("Simple：點擊事件", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(entryProp.FindPropertyRelative("onClicked"), new GUIContent("onClicked"));
    }

    private void DrawComplexFields(SerializedProperty entryProp)
    {
        EditorGUILayout.LabelField("Complex：資源檢查", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(entryProp.FindPropertyRelative("checkType"), new GUIContent("資源類型"));
        EditorGUILayout.PropertyField(entryProp.FindPropertyRelative("resourceAmount"), new GUIContent("消耗數量"));

        var checkTimeProp = entryProp.FindPropertyRelative("checkTime");
        EditorGUILayout.PropertyField(checkTimeProp, new GUIContent("檢查時間"));

        if (checkTimeProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(entryProp.FindPropertyRelative("timeAmount"), new GUIContent("時段數量"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Complex：結果事件", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(entryProp.FindPropertyRelative("onSuccess"), new GUIContent("onSuccess（成功→關閉）"));
        EditorGUILayout.PropertyField(entryProp.FindPropertyRelative("onFailure"), new GUIContent("onFailure（資源不足）"));
        EditorGUILayout.PropertyField(entryProp.FindPropertyRelative("onTimeFailure"), new GUIContent("onTimeFailure（時間不足）"));
    }

    private void DrawRestFields(SerializedProperty entryProp)
    {
        EditorGUILayout.LabelField("Rest：休息設定", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(
            entryProp.FindPropertyRelative("restPreviewMode"),
            new GUIContent("預覽模式", "選擇休息類型，Hover 時會在體力條顯示回復預覽"));
        EditorGUILayout.PropertyField(
            entryProp.FindPropertyRelative("onRestClicked"),
            new GUIContent("onRestClicked（點擊事件）"));
    }
}