// Copyright (c) NHK Project. All rights reserved.
// NhkUISubtitlePanel 的自訂 Inspector。
//
// 為什麼需要這支：
//   官方的 StandardUISubtitlePanelEditor 用 [CustomEditor(typeof(StandardUISubtitlePanel), true)]
//   接管了所有子類，且它是「逐一手動畫固定欄位、不呼叫 DrawDefaultInspector」，
//   所以 NhkUISubtitlePanel 新增的 narrationText / narrationContainer / narrationFieldName
//   不會被畫出來。這支 Editor 繼承官方 Editor，先畫完原本欄位，再補畫這三個。

using UnityEngine;
using UnityEditor;

namespace PixelCrushers.DialogueSystem
{
    [CustomEditor(typeof(NhkUISubtitlePanel), true)]
    public class NhkUISubtitlePanelEditor : StandardUISubtitlePanelEditor
    {
        public override void OnInspectorGUI()
        {
            // 先畫官方原本的所有欄位
            base.OnInspectorGUI();

            // 再補畫本類新增的額外敘述欄位
            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Extra Narration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("narrationText"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("narrationContainer"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("narrationFieldName"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("narrationFlagFieldName"), true);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
