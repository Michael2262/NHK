// 檔名：SpineSkinPresetControllerEditor.cs
// 位置：請放在 Assets/Editor/ 或任何 *Editor* 資料夾底下
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpineSkinPresetController))]
public class SpineSkinPresetControllerEditor : Editor
{
    private SerializedProperty _presetsProp;
    private bool _quickFoldout = true;
    private string _filter = "";
    private static string _manualPresetId = "";
    private SpineSkinPresetController _controller;
    private List<string> _allPresetIds = new List<string>();

    private void OnEnable()
    {
        _presetsProp = serializedObject.FindProperty("presets");
        _controller = (SpineSkinPresetController)target;
    }

    public override void OnInspectorGUI()
    {
        // 先繪製原始的 Inspector 介面
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        // 繪製我們自訂的測試 UI
        DrawQuickTestUI();
    }

    private void DrawQuickTestUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("需進入 Play Mode 才能使用快速測試功能。", MessageType.Info);
            return;
        }

        _quickFoldout = EditorGUILayout.Foldout(_quickFoldout, "Quick Test (Inspector Buttons)", true, EditorStyles.foldoutHeader);
        if (!_quickFoldout) return;

        // 在 Play Mode 下才更新 Preset ID 列表
        if (Application.isPlaying && _controller != null)
        {
            _allPresetIds = _controller.GetAllPresetIds();
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Current Active Skins", _controller.debugActiveList, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            // 手動輸入 Preset ID 播放
            using (new EditorGUILayout.HorizontalScope())
            {
                _manualPresetId = EditorGUILayout.TextField("Manual Preset ID", _manualPresetId);

                if (GUILayout.Button("► Add", GUILayout.Width(60)))
                {
                    if (!string.IsNullOrEmpty(_manualPresetId)) _controller.ChangeSkin(_manualPresetId, SpineSkinPresetController.ApplyMode.Additive);
                    else EditorUtility.DisplayDialog("Quick Test", "請輸入要套用的 Preset ID。", "OK");
                }

                if (GUILayout.Button("► Replace", GUILayout.Width(70)))
                {
                    if (!string.IsNullOrEmpty(_manualPresetId)) _controller.ChangeSkin(_manualPresetId, SpineSkinPresetController.ApplyMode.Replace);
                    else EditorUtility.DisplayDialog("Quick Test", "請輸入要套用的 Preset ID。", "OK");
                }
            }

            EditorGUILayout.Space();
            _filter = EditorGUILayout.TextField(new GUIContent("Filter (by preset id)"), _filter);
            EditorGUILayout.Space();

            // 列出所有 Preset + Play 按鈕
            if (_allPresetIds.Count == 0)
            {
                EditorGUILayout.HelpBox("尚未建立任何組。請在上方 Presets 新增。", MessageType.None);
            }
            else
            {
                int displayedCount = 0;
                foreach (var presetId in _allPresetIds)
                {
                    if (!string.IsNullOrEmpty(_filter) &&
                        presetId.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    displayedCount++;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(presetId, EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("► Add", GUILayout.Width(60)))
                        {
                            _controller.ChangeSkin(presetId, SpineSkinPresetController.ApplyMode.Additive);
                            _manualPresetId = presetId;
                        }

                        if (GUILayout.Button("► Replace", GUILayout.Width(70)))
                        {
                            _controller.ChangeSkin(presetId, SpineSkinPresetController.ApplyMode.Replace);
                            _manualPresetId = presetId;
                        }
                    }
                }

                if (displayedCount == 0 && !string.IsNullOrEmpty(_filter))
                {
                    EditorGUILayout.HelpBox("沒有符合篩選條件的 Preset。", MessageType.None);
                }
            }
        }
    }
}
#endif