#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// AdventureCardData 的自訂 Inspector。
/// 效果清單的型別下拉由 AdventureEffectListDrawer 負責。
///
/// 會依 OutcomeMode 隱藏不會生效的欄位，避免填了沒用的東西：
///   Judge        全部顯示
///   AlwaysOnly   隱藏成功/失敗效果與其插圖、成功率算式
///   ForceSuccess 隱藏失敗效果與失敗插圖、成功率算式
/// </summary>
[CustomEditor(typeof(AdventureCardData))]
public class AdventureCardDataEditor : Editor
{
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
        AdventureEffectListDrawer.Draw(serializedObject,
            serializedObject.FindProperty("AlwaysEffects"),
            "效果 - 必有（翻到就觸發）",
            "不分成功失敗都會執行，且先於成功/失敗效果");

        // ── 成功效果 ──
        if (showSuccess)
        {
            EditorGUILayout.Space(6);
            AdventureEffectListDrawer.Draw(serializedObject,
                serializedObject.FindProperty("SuccessEffects"),
                "效果 - 翻牌成功",
                "成功時依序執行。里程推進請放 Mileage 效果");
        }

        // ── 失敗效果 ──
        if (showFailure)
        {
            EditorGUILayout.Space(6);
            AdventureEffectListDrawer.Draw(serializedObject,
                serializedObject.FindProperty("FailureEffects"),
                "效果 - 翻牌失敗",
                "失敗時依序執行（通常放 Stress；也可放 -1 Mileage）");
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
