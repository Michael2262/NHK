// 檔名：SpinePlayByListEditor.cs
// 位置：請放在 Assets/Editor/ 或任何 *Editor* 資料夾底下
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpinePlayByList))]
public class SpinePlayByListEditor : Editor
{
    SerializedProperty _skeletonAnimationProp;
    SerializedProperty _skeletonGraphicProp;
    SerializedProperty _groupsProp;

    bool _quickFoldout = true;
    string _filter = "";
    static string _manualGroupName = "";

    void OnEnable()
    {
        _skeletonAnimationProp = serializedObject.FindProperty("skeletonAnimation");
        _skeletonGraphicProp = serializedObject.FindProperty("skeletonGraphic");
        _groupsProp = serializedObject.FindProperty("groups");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // --- 原本欄位 ---
        // 讓 Unity 繪製預設的 Inspector (除了 Groups 列表)
        DrawPropertiesExcluding(serializedObject, "m_Script", "groups");

        // --- Groups 列表 (保持可編輯) ---
        EditorGUILayout.PropertyField(_groupsProp, true);

        EditorGUILayout.Space();
        DrawQuickTestUI();

        serializedObject.ApplyModifiedProperties();
    }

    void DrawQuickTestUI()
    {
        var comp = (SpinePlayByList)target;

        _quickFoldout = EditorGUILayout.Foldout(_quickFoldout, "Quick Test (Inspector Buttons)", true);
        if (!_quickFoldout) return;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            // 狀態列
            var status = comp.IsPlaying ? $"Playing: {comp.CurrentGroupName}" : "Idle";
            EditorGUILayout.LabelField("Status", status);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    if (GUILayout.Button("■ Stop", GUILayout.MaxWidth(100)))
                    {
                        comp.StopPlaying();
                    }
                }
                GUILayout.FlexibleSpace();
                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("需進入 Play Mode 才能播放。", MessageType.None);
                }
            }
            EditorGUILayout.Space();

            // 手動輸入組名播放
            using (new EditorGUILayout.HorizontalScope())
            {
                _manualGroupName = EditorGUILayout.TextField("Manual Group", _manualGroupName);
                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    if (GUILayout.Button("► Play", GUILayout.Width(80)))
                    {
                        if (!string.IsNullOrEmpty(_manualGroupName)) comp.PlayGroup(_manualGroupName);
                    }
                    if (GUILayout.Button("► Play & Back", GUILayout.Width(100)))
                    {
                        if (!string.IsNullOrEmpty(_manualGroupName)) comp.PlayGroupAndGoBack(_manualGroupName);
                    }
                }
            }
            EditorGUILayout.Space();
            _filter = EditorGUILayout.TextField(new GUIContent("Filter (by group name)"), _filter);
            EditorGUILayout.Space();

            // 列出所有組 + Play 按鈕
            if (_groupsProp != null && _groupsProp.isArray)
            {
                int count = _groupsProp.arraySize;
                if (count == 0)
                {
                    EditorGUILayout.HelpBox("尚未建立任何組。請在上方 Groups 新增。", MessageType.Info);
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        var element = _groupsProp.GetArrayElementAtIndex(i);
                        var nameProp = element.FindPropertyRelative("groupName");
                        var clipsProp = element.FindPropertyRelative("clips");
                        // 【新增】取得 checkGroupName 屬性
                        var checkNameProp = element.FindPropertyRelative("checkGroupName");

                        string gname = nameProp != null ? nameProp.stringValue : $"Group{i}";
                        if (!string.IsNullOrEmpty(_filter) &&
                            (gname ?? string.Empty).IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        using (new EditorGUILayout.VerticalScope("box"))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField($"[{i}] {gname}", EditorStyles.boldLabel);
                                GUILayout.FlexibleSpace();
                                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                                {
                                    if (GUILayout.Button("► Play", GUILayout.Width(80)))
                                    {
                                        comp.PlayGroup(gname);
                                        _manualGroupName = gname;
                                    }
                                    if (GUILayout.Button("► Play & Back", GUILayout.Width(100)))
                                    {
                                        comp.PlayGroupAndGoBack(gname);
                                        _manualGroupName = gname;
                                    }
                                }
                            }

                            // 【新增】在摘要中顯示 checkGroupName
                            string checkName = checkNameProp?.stringValue ?? "";
                            if (!string.IsNullOrEmpty(checkName))
                            {
                                // 使用一個稍微不同的顏色或樣式來突顯它
                                var originalColor = GUI.color;
                                GUI.color = Color.cyan;
                                EditorGUILayout.LabelField($"└ Cond. Target: {checkName}");
                                GUI.color = originalColor;
                            }

                            // 顯示此組的條目摘要
                            int clipCount = clipsProp != null && clipsProp.isArray ? clipsProp.arraySize : 0;

                            // 先算出 loop / random 後段起點（第一個被勾的 clip）
                            int loopStart = -1, randomStart = -1;
                            for (int c = 0; c < clipCount; c++)
                            {
                                var clip = clipsProp.GetArrayElementAtIndex(c);
                                if (loopStart < 0 && (clip.FindPropertyRelative("isLoop")?.boolValue ?? false)) loopStart = c;
                                if (randomStart < 0 && (clip.FindPropertyRelative("isRandom")?.boolValue ?? false)) randomStart = c;
                            }

                            EditorGUILayout.LabelField($"Clips: {clipCount}");
                            EditorGUI.indentLevel++;
                            if (clipCount > 0)
                            {
                                var baseColor = GUI.color;
                                for (int c = 0; c < clipCount; c++)
                                {
                                    var clip = clipsProp.GetArrayElementAtIndex(c);
                                    var animName = clip.FindPropertyRelative("animationName")?.stringValue ?? "(null)";
                                    var repeatCount = Mathf.Max(1, clip.FindPropertyRelative("repeatCount").intValue);

                                    bool inLoop = loopStart >= 0 && c >= loopStart;
                                    bool inRandom = randomStart >= 0 && c >= randomStart;

                                    string tag = "";
                                    if (inLoop) tag += "  ⟳Loop";
                                    if (inRandom) tag += "  ⤨Rand";

                                    // 變色：loop+random=橘、loop=綠、random=青、無=預設
                                    if (inLoop && inRandom) GUI.color = new Color(1f, 0.6f, 0.2f);
                                    else if (inLoop) GUI.color = new Color(0.45f, 1f, 0.45f);
                                    else if (inRandom) GUI.color = Color.cyan;
                                    else GUI.color = baseColor;

                                    EditorGUILayout.LabelField($"• {animName}  ×{repeatCount}{tag}");
                                }
                                GUI.color = baseColor;
                            }
                            else
                            {
                                EditorGUILayout.LabelField("（此組尚無任何動畫條目）");
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("提示：Play 按鈕只在 Play Mode 啟用；Stop 可立即中止目前的組播放。", MessageType.None);
        }
    }

    [MenuItem("CONTEXT/SpinePlayByList/Stop Playing")]
    private static void ContextStop(MenuCommand cmd)
    {
        var comp = (SpinePlayByList)cmd.context;
        comp.StopPlaying();
    }
}
#endif
