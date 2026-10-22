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
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("角色名字框", EditorStyles.boldLabel);
            var controlNames = serializedObject.FindProperty("controlNameVisibility");
            EditorGUILayout.PropertyField(controlNames, new GUIContent("啟用角色名字框控制"));
            if (controlNames.boolValue)
            {
                var container = serializedObject.FindProperty("nameContainer");
                EditorGUILayout.PropertyField(container, new GUIContent("名字框容器"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("actorsWithVisibleNames"),
                    new GUIContent("顯示名字框的角色"), true);
                if (container.objectReferenceValue == null)
                    EditorGUILayout.HelpBox("請指定名字文字與背景框的共同父物件；未指定時不會套用控制。", MessageType.Warning);
                EditorGUILayout.HelpBox("角色請填資料庫的 Name，例如 Sister；不要填翻譯後的 Display Name。容器只能包含名字區域，不可包含整個字幕面板或對話內容。", MessageType.Info);
                if (serializedObject.FindProperty("onlyShowNPCPortraits").boolValue)
                    EditorGUILayout.HelpBox("請取消勾選 Only Show NPC Portraits，避免 Player 說話時沿用上一位 NPC 的名字。", MessageType.Warning);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
