#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

/// <summary>
/// [SerializeReference] 的 AdventureEffect 清單共用繪製器。
/// Unity 內建不會替 [SerializeReference] 長出型別選單，所以 Card / Dungeon 的
/// 自訂 Inspector 都靠這支畫出「＋ 新增效果」下拉。
/// </summary>
public static class AdventureEffectListDrawer
{
    private static Type[] _effectTypes;
    private static string[] _effectNames;

    private static void EnsureCache()
    {
        if (_effectTypes != null) return;

        _effectTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(AdventureEffect)))
            .OrderBy(t => t.Name)
            .ToArray();
        _effectNames = _effectTypes.Select(FormatTypeName).ToArray();
    }

    /// <summary>去掉 Adv 前綴、Effect 後綴，並在駝峰處插空格，讓選單好讀。</summary>
    public static string FormatTypeName(Type t)
    {
        string name = t.Name;
        if (name.StartsWith("Adv")) name = name.Substring(3);
        if (name.EndsWith("Effect")) name = name.Substring(0, name.Length - "Effect".Length);

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1])) sb.Append(' ');
            sb.Append(name[i]);
        }
        return sb.ToString();
    }

    /// <summary>畫一組效果清單（含新增 / 刪除 / 展開子欄位）。</summary>
    public static void Draw(SerializedObject serializedObject, SerializedProperty listProp,
                            string headerLabel, string tooltip)
    {
        if (listProp == null) return;
        EnsureCache();

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
            var menu = new GenericMenu();
            for (int i = 0; i < _effectTypes.Length; i++)
            {
                var type = _effectTypes[i];
                menu.AddItem(new GUIContent(_effectNames[i]), false, () =>
                {
                    listProp.arraySize++;
                    var newElement = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
                    newElement.managedReferenceValue = Activator.CreateInstance(type);
                    serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
    }

    private static void DrawChildProperties(SerializedProperty parentProp)
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
