#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 自訂 ItemConfigData 的 Inspector 繪製。
/// 為 [SerializeReference] 的 ItemEffect / GiftCondition 列表提供「篩選過的類型選擇下拉選單」。
/// 
/// ★ SelfEffects 只顯示 Protagonist + Any 效果
/// ★ GiftEffects 只顯示 Heroine + Any 效果
/// ★ 每個送禮區塊後面都會帶 Reaction(DefaultGiftReaction / Override.Reaction)
/// 
/// 【放置路徑】Assets/Editor/ItemConfigDataEditor.cs
/// </summary>
[CustomEditor(typeof(ItemConfigData))]
public class ItemConfigDataEditor : Editor
{
    // ── 快取所有 ItemEffect 子類（依 Target 分組）──
    private static Type[] _allEffectTypes;
    private static Type[] _protagonistEffectTypes;
    private static Type[] _heroineEffectTypes;
    private static string[] _protagonistEffectNames;
    private static string[] _heroineEffectNames;

    // ── 快取所有 GiftCondition 子類 ──
    private static Type[] _conditionTypes;
    private static string[] _conditionTypeNames;

    // ── 快取所有 UseCondition 子類 ──
    private static Type[] _useConditionTypes;
    private static string[] _useConditionTypeNames;

    static ItemConfigDataEditor()
    {
        _allEffectTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(ItemEffect)))
            .OrderBy(t => t.Name)
            .ToArray();

        _protagonistEffectTypes = _allEffectTypes
            .Where(t =>
            {
                var instance = (ItemEffect)Activator.CreateInstance(t);
                return instance.Target == EffectTarget.Protagonist || instance.Target == EffectTarget.Any;
            }).ToArray();
        _protagonistEffectNames = _protagonistEffectTypes.Select(t => FormatTypeName(t)).ToArray();

        _heroineEffectTypes = _allEffectTypes
            .Where(t =>
            {
                var instance = (ItemEffect)Activator.CreateInstance(t);
                return instance.Target == EffectTarget.Heroine || instance.Target == EffectTarget.Any;
            }).ToArray();
        _heroineEffectNames = _heroineEffectTypes.Select(t => FormatTypeName(t)).ToArray();

        _conditionTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(GiftCondition)))
            .OrderBy(t => t.Name)
            .ToArray();
        _conditionTypeNames = _conditionTypes.Select(t => FormatTypeName(t)).ToArray();

        _useConditionTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(UseCondition)))
            .OrderBy(t => t.Name)
            .ToArray();
        _useConditionTypeNames = _useConditionTypes.Select(t => FormatTypeName(t)).ToArray();
    }

    private static string FormatTypeName(Type t)
    {
        string name = t.Name;
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                result.Append(' ');
            result.Append(name[i]);
        }
        return result.ToString();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── 繪製基礎屬性(排除所有我們自訂繪製的欄位)──
        // ★ 新增排除 DefaultGiftReaction
        DrawPropertiesExcluding(serializedObject,
            "UseConditions",
            "SelfEffects",
            "DefaultGiftEffects",
            "DefaultGiftReaction",
            "HeroineOverrides");

        EditorGUILayout.Space(10);

        var targetData = (ItemConfigData)target;
        var category = targetData.TargetingRule != null
            ? targetData.TargetingRule.Category
            : TargetCategory.AnyCharacter;

        bool showSelfConditions = (category == TargetCategory.ProtagonistOnly ||
                                   category == TargetCategory.AnyCharacter);
        bool showSelfEffects = (category == TargetCategory.ProtagonistOnly ||
                                   category == TargetCategory.AnyCharacter ||
                                   category == TargetCategory.NoBody);
        bool showGiftEffects = (category == TargetCategory.HeroineOnly ||
                                   category == TargetCategory.AnyCharacter);

        // ── 條件 - 自用 ──
        if (showSelfConditions)
        {
            DrawUseConditionList(
                serializedObject.FindProperty("UseConditions"),
                "條件 - 自用(對主角)",
                "使用前的條件檢查(全部通過才能使用)");
            EditorGUILayout.Space(6);
        }

        // ── 效果 - 自用 ──
        if (showSelfEffects)
        {
            DrawEffectList(
                serializedObject.FindProperty("SelfEffects"),
                "效果 - 自用(對主角)",
                "主角使用此道具時的效果",
                _protagonistEffectTypes,
                _protagonistEffectNames);
            EditorGUILayout.Space(6);
        }

        // ── 效果 + 反應 - 送禮(預設)──
        if (showGiftEffects)
        {
            DrawEffectList(
                serializedObject.FindProperty("DefaultGiftEffects"),
                "效果 - 送禮(預設)",
                "送禮給女主角的預設效果",
                _heroineEffectTypes,
                _heroineEffectNames);
            EditorGUILayout.Space(6);

            // ★ 新增:畫預設反應
            DrawReaction(
                serializedObject.FindProperty("DefaultGiftReaction"),
                "反應 - 送禮(預設)",
                "收下禮物後的預設反應(對話 + UnityEvent)。\n" +
                "留空 = fallback 到 HeroineReactionManager 全域反應。");
            EditorGUILayout.Space(6);

            DrawHeroineOverrides();
        }

        serializedObject.ApplyModifiedProperties();
    }


    // ==========================================================
    // ★ 新增:繪製 GiftReaction
    // ==========================================================

    private void DrawReaction(SerializedProperty reactionProp, string headerLabel, string tooltip)
    {
        EditorGUILayout.LabelField(new GUIContent(headerLabel, tooltip), EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUI.indentLevel++;

        var dialogueProp = reactionProp.FindPropertyRelative("DialogueConversationTitle");
        var eventProp = reactionProp.FindPropertyRelative("ReactionEvent");

        if (dialogueProp != null)
            EditorGUILayout.PropertyField(dialogueProp, new GUIContent("對話 Title"));

        if (eventProp != null)
            EditorGUILayout.PropertyField(eventProp, new GUIContent("Unity Event"));

        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
    }


    // ==========================================================
    // 繪製 ItemEffect 列表(接收篩選過的類型陣列)
    // ==========================================================

    private void DrawEffectList(SerializedProperty listProp, string headerLabel, string tooltip,
        Type[] filteredTypes, string[] filteredNames)
    {
        EditorGUILayout.LabelField(new GUIContent(headerLabel, tooltip), EditorStyles.boldLabel);

        EditorGUI.indentLevel++;

        for (int i = 0; i < listProp.arraySize; i++)
        {
            var elementProp = listProp.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            string typeName = elementProp.managedReferenceValue != null
                ? FormatTypeName(elementProp.managedReferenceValue.GetType())
                : "(空)";
            EditorGUILayout.LabelField($"[{i}] {typeName}", EditorStyles.boldLabel);

            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                listProp.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                break;
            }
            EditorGUILayout.EndHorizontal();

            if (elementProp.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                DrawChildProperties(elementProp);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("＋ 新增效果", GUILayout.Width(200)))
        {
            ShowTypeMenu(filteredTypes, filteredNames, (type) =>
            {
                listProp.arraySize++;
                var newElement = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
                newElement.managedReferenceValue = Activator.CreateInstance(type);
                serializedObject.ApplyModifiedProperties();
            });
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
    }


    // ==========================================================
    // 繪製 GiftCondition 列表
    // ==========================================================

    private void DrawConditionList(SerializedProperty listProp, string headerLabel)
    {
        EditorGUILayout.LabelField(headerLabel, EditorStyles.miniLabel);
        EditorGUI.indentLevel++;

        for (int i = 0; i < listProp.arraySize; i++)
        {
            var elementProp = listProp.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            string typeName = elementProp.managedReferenceValue != null
                ? FormatTypeName(elementProp.managedReferenceValue.GetType())
                : "(空)";
            EditorGUILayout.LabelField($"[{i}] {typeName}", EditorStyles.miniLabel);

            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                listProp.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                break;
            }
            EditorGUILayout.EndHorizontal();

            if (elementProp.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                DrawChildProperties(elementProp);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("＋ 新增條件", GUILayout.Width(180)))
        {
            ShowTypeMenu(_conditionTypes, _conditionTypeNames, (type) =>
            {
                listProp.arraySize++;
                var newElement = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
                newElement.managedReferenceValue = Activator.CreateInstance(type);
                serializedObject.ApplyModifiedProperties();
            });
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
    }


    // ==========================================================
    // 繪製 UseCondition 列表
    // ==========================================================

    private void DrawUseConditionList(SerializedProperty listProp, string headerLabel, string tooltip)
    {
        EditorGUILayout.LabelField(new GUIContent(headerLabel, tooltip), EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        for (int i = 0; i < listProp.arraySize; i++)
        {
            var elementProp = listProp.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            string typeName = elementProp.managedReferenceValue != null
                ? FormatTypeName(elementProp.managedReferenceValue.GetType())
                : "(空)";
            EditorGUILayout.LabelField($"[{i}] {typeName}", EditorStyles.boldLabel);

            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                listProp.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                break;
            }
            EditorGUILayout.EndHorizontal();

            if (elementProp.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                DrawChildProperties(elementProp);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("＋ 新增條件", GUILayout.Width(200)))
        {
            ShowTypeMenu(_useConditionTypes, _useConditionTypeNames, (type) =>
            {
                listProp.arraySize++;
                var newElement = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
                newElement.managedReferenceValue = Activator.CreateInstance(type);
                serializedObject.ApplyModifiedProperties();
            });
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
    }


    // ==========================================================
    // 繪製 HeroineOverrides
    // ==========================================================

    private void DrawHeroineOverrides()
    {
        var overridesProp = serializedObject.FindProperty("HeroineOverrides");

        EditorGUILayout.LabelField("送禮 - 女主角專屬覆寫", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        for (int i = 0; i < overridesProp.arraySize; i++)
        {
            var overrideProp = overridesProp.GetArrayElementAtIndex(i);
            var heroineIDProp = overrideProp.FindPropertyRelative("HeroineID");
            var conditionsProp = overrideProp.FindPropertyRelative("Conditions");
            var effectsProp = overrideProp.FindPropertyRelative("Effects");
            var reactionProp = overrideProp.FindPropertyRelative("Reaction"); // ★ 新增

            string heroineLabel = string.IsNullOrEmpty(heroineIDProp.stringValue)
                ? $"Override [{i}] (未設定 ID)"
                : $"Override [{i}]: {heroineIDProp.stringValue}";

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            overrideProp.isExpanded = EditorGUILayout.Foldout(overrideProp.isExpanded, heroineLabel, true);
            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                overridesProp.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                break;
            }
            EditorGUILayout.EndHorizontal();

            if (overrideProp.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(heroineIDProp, new GUIContent("女主角 ID"));

                EditorGUILayout.Space(4);
                DrawConditionList(conditionsProp, "條件(全部通過才能送):");

                EditorGUILayout.Space(4);
                DrawEffectList(effectsProp, "專屬效果:", "取代 DefaultGiftEffects",
                    _heroineEffectTypes, _heroineEffectNames);

                // ★ 新增:畫該女主角的專屬反應
                EditorGUILayout.Space(4);
                if (reactionProp != null)
                {
                    DrawReaction(reactionProp, "專屬反應:",
                        "此女主角收到這個禮物的專屬反應。\n" +
                        "留空 = fallback 到 DefaultGiftReaction。");
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("＋ 新增女主角覆寫", GUILayout.Width(200)))
        {
            overridesProp.arraySize++;
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
    }


    // ==========================================================
    // 共用工具
    // ==========================================================

    private void ShowTypeMenu(Type[] types, string[] names, Action<Type> onSelected)
    {
        var menu = new GenericMenu();
        for (int i = 0; i < types.Length; i++)
        {
            var type = types[i];
            menu.AddItem(new GUIContent(names[i]), false, () => onSelected(type));
        }
        menu.ShowAsContext();
    }

    private void DrawChildProperties(SerializedProperty parentProp)
    {
        var iterator = parentProp.Copy();
        var endProp = parentProp.GetEndProperty();

        if (!iterator.NextVisible(true)) return;

        do
        {
            if (SerializedProperty.EqualContents(iterator, endProp)) break;
            EditorGUILayout.PropertyField(iterator, true);
        }
        while (iterator.NextVisible(false));
    }
}
#endif