#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// AdventureCardData 的自訂 Inspector。
/// 為 [SerializeReference] 的 Always / Success / Failure 效果清單提供「＋ 新增效果」型別下拉。
/// （Unity 內建對 [SerializeReference] 不會自動長出型別選單，必須靠這支 Editor。）
///
/// 會依 OutcomeMode 隱藏不會生效的欄位，避免填了沒用的東西：
///   Judge        全部顯示
///   AlwaysOnly   隱藏成功/失敗效果與其插圖、成功率算式
///   ForceSuccess 隱藏失敗效果與失敗插圖、成功率算式
/// </summary>
[CustomEditor(typeof(AdventureCardData))]
public class AdventureCardDataEditor : Editor
{
    private static Type[] _effectTypes;
    private static string[] _effectNames;

    static AdventureCardDataEditor()
    {
        _effectTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(AdventureEffect)))
            .OrderBy(t => t.Name)
            .ToArray();
        _effectNames = _effectTypes.Select(FormatTypeName).ToArray();
    }

    private static string FormatTypeName(Type t)
    {
        // 去掉 Adv 前綴、Effect 後綴，並在駝峰處插空格，讓選單好讀
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

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var mode = (AdventureOutcomeMode)serializedObject.FindProperty("OutcomeMode").enumValueIndex;
        bool showSuccess = mode != AdventureOutcomeMode.AlwaysOnly;
        bool showFailure = mode == AdventureOutcomeMode.Judge;
        bool showRateFormula = mode == AdventureOutcomeMode.Judge;

        // ── 基礎欄位（效果清單自己畫；依模式隱藏不會生效的欄位）──
        var excluded = new List<string> { "AlwaysEffects", "SuccessEffects", "FailureEffects" };
        if (!showSuccess) excluded.Add("SuccessIllustration");
        if (!showFailure) excluded.Add("FailureIllustration");
        if (!showRateFormula)
        {
            excluded.Add("Mode");
            excluded.Add("BaseRate");
            excluded.Add("SocialCoef");
            excluded.Add("LifeCoef");
        }
        DrawPropertiesExcluding(serializedObject, excluded.ToArray());

        EditorGUILayout.Space(4);
        switch (mode)
        {
            case AdventureOutcomeMode.AlwaysOnly:
                EditorGUILayout.HelpBox(
                    "AlwaysOnly：必有效果跑完就結束，不判定成敗。\n" +
                    "・成功 / 失敗效果與其插圖已停用\n" +
                    "・成功率算式已停用",
                    MessageType.Info);
                break;
            case AdventureOutcomeMode.ForceSuccess:
                EditorGUILayout.HelpBox(
                    "ForceSuccess：必有效果後不擲骰，必定跑成功效果。\n" +
                    "・失敗效果與失敗插圖已停用\n" +
                    "・成功率算式已停用（一律視為 100%）",
                    MessageType.Info);
                break;
        }

        // ── 必有效果 ──
        EditorGUILayout.Space(10);
        DrawEffectList(serializedObject.FindProperty("AlwaysEffects"),
            "效果 - 必有（翻到就觸發）",
            "不分成功失敗都會執行，且先於成功/失敗效果");

        // ── 成功效果 ──
        if (showSuccess)
        {
            EditorGUILayout.Space(6);
            DrawEffectList(serializedObject.FindProperty("SuccessEffects"),
                "效果 - 翻牌成功",
                "成功時依序執行。里程推進請放 Mileage 效果");
        }

        // ── 失敗效果 ──
        if (showFailure)
        {
            EditorGUILayout.Space(6);
            DrawEffectList(serializedObject.FindProperty("FailureEffects"),
                "效果 - 翻牌失敗",
                "失敗時依序執行（通常放 Stress；也可放 -1 Mileage）");
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEffectList(SerializedProperty listProp, string headerLabel, string tooltip)
    {
        if (listProp == null) return;

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
