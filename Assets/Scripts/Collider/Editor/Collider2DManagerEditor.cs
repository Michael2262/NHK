using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Collider2DManager 的自訂 Inspector 編輯器。
/// 功能：
/// 1. 清楚顯示每個 Group Entry 的設定與預覽（掃描到的 Collider 數量）
/// 2. Runtime 時顯示已註冊的 Collider 清單，可即時啟用/停用
/// 3. 提供快速操作按鈕（全部啟用/停用、重新掃描等）
/// 4. 搜尋功能，可透過 ID 快速找到 Collider
/// 
/// 使用方式：放在 Assets/Editor 資料夾底下即可。
/// </summary>
[CustomEditor(typeof(Collider2DManager))]
public class Collider2DManagerEditor : Editor
{
    // ── Serialized Properties ──────────────────────────────────────
    private SerializedProperty groupEntriesProp;
    private SerializedProperty onRegistrationCompleteProp;

    // ── Foldout States ─────────────────────────────────────────────
    private bool showGroupEntries = true;
    private bool showRuntimeRegistry = true;
    private bool showQuickActions = true;
    private Dictionary<int, bool> entryFoldouts = new Dictionary<int, bool>();
    private Dictionary<string, bool> runtimeGroupFoldouts = new Dictionary<string, bool>();

    // ── Search ─────────────────────────────────────────────────────
    private string searchFilter = "";

    // ── Styles (lazy init) ─────────────────────────────────────────
    private GUIStyle _headerStyle;
    private GUIStyle _subHeaderStyle;
    private GUIStyle _boxStyle;
    private GUIStyle _miniButtonStyle;
    private GUIStyle _searchFieldStyle;
    private GUIStyle _richLabelStyle;
    private bool stylesInitialized;

    private GUIStyle HeaderStyle => _headerStyle;
    private GUIStyle SubHeaderStyle => _subHeaderStyle;
    private GUIStyle BoxStyle => _boxStyle;
    private GUIStyle RichLabelStyle => _richLabelStyle;

    private void InitStyles()
    {
        if (stylesInitialized) return;
        stylesInitialized = true;

        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            richText = true
        };

        _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            richText = true
        };

        _boxStyle = new GUIStyle("helpBox")
        {
            padding = new RectOffset(8, 8, 6, 6)
        };

        _miniButtonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fixedHeight = 20
        };

        _richLabelStyle = new GUIStyle(EditorStyles.label)
        {
            richText = true
        };
    }

    // ── Enable / Disable ───────────────────────────────────────────

    private void OnEnable()
    {
        groupEntriesProp = serializedObject.FindProperty("groupEntries");
        onRegistrationCompleteProp = serializedObject.FindProperty("onRegistrationComplete");
    }

    // ── Main Draw ──────────────────────────────────────────────────

    public override void OnInspectorGUI()
    {
        InitStyles();
        serializedObject.Update();

        var manager = (Collider2DManager)target;

        DrawHeader();
        EditorGUILayout.Space(4);

        DrawGroupEntriesSection();
        EditorGUILayout.Space(4);

        DrawEventSection();
        EditorGUILayout.Space(4);

        if (Application.isPlaying)
        {
            DrawRuntimeRegistrySection(manager);
            EditorGUILayout.Space(4);
            DrawQuickActionsSection(manager);
        }
        else
        {
            DrawEditModePreview();
        }

        serializedObject.ApplyModifiedProperties();

        // Runtime 時持續重繪以即時反映狀態
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    // ── Header ─────────────────────────────────────────────────────

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(BoxStyle);
        {
            EditorGUILayout.LabelField("🎯 Collider2D Manager", HeaderStyle);

            string status;
            if (Application.isPlaying)
            {
                bool ready = Collider2DManager.IsReady;
                status = ready
                    ? "<color=#4CAF50>● Running — Ready</color>"
                    : "<color=#FF9800>● Running — Not Ready</color>";
            }
            else
            {
                status = "<color=#9E9E9E>● Edit Mode</color>";
            }
            EditorGUILayout.LabelField(status, RichLabelStyle);
        }
        EditorGUILayout.EndVertical();
    }

    // ── Group Entries (Inspector 設定) ─────────────────────────────

    private void DrawGroupEntriesSection()
    {
        showGroupEntries = EditorGUILayout.BeginFoldoutHeaderGroup(showGroupEntries, "📦 Group Entries（掃描設定）");
        if (showGroupEntries)
        {
            EditorGUI.indentLevel++;

            if (groupEntriesProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("尚未設定任何 Group Entry。\n請按下方「+」按鈕新增。", MessageType.Info);
            }

            for (int i = 0; i < groupEntriesProp.arraySize; i++)
            {
                DrawGroupEntry(i);
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("＋ 新增 Group Entry", GUILayout.Width(160), GUILayout.Height(22)))
            {
                groupEntriesProp.InsertArrayElementAtIndex(groupEntriesProp.arraySize);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawGroupEntry(int index)
    {
        var element = groupEntriesProp.GetArrayElementAtIndex(index);
        var groupNameProp = element.FindPropertyRelative("groupName");
        var rootProp = element.FindPropertyRelative("root");
        var includeInactiveProp = element.FindPropertyRelative("includeInactive");

        if (!entryFoldouts.ContainsKey(index))
            entryFoldouts[index] = true;

        EditorGUILayout.BeginVertical("box");
        {
            // Title row
            EditorGUILayout.BeginHorizontal();
            {
                string entryLabel = groupNameProp.enumDisplayNames[groupNameProp.enumValueIndex];
                entryFoldouts[index] = EditorGUILayout.Foldout(entryFoldouts[index], $"[{index}] {entryLabel}", true);

                // Collider count preview
                Transform root = rootProp.objectReferenceValue as Transform;
                if (root != null)
                {
                    int count = root.GetComponentsInChildren<Collider2D>(includeInactiveProp.boolValue).Length;
                    GUILayout.Label($"({count} colliders)", EditorStyles.miniLabel, GUILayout.Width(90));
                }

                // Delete button
                GUI.color = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(18)))
                {
                    groupEntriesProp.DeleteArrayElementAtIndex(index);
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (entryFoldouts[index])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(groupNameProp, new GUIContent("Group Name"));
                EditorGUILayout.PropertyField(rootProp, new GUIContent("Root Transform"));
                EditorGUILayout.PropertyField(includeInactiveProp, new GUIContent("Include Inactive"));

                // 預覽列出的 Collider
                Transform rootTf = rootProp.objectReferenceValue as Transform;
                if (rootTf != null)
                {
                    DrawColliderPreviewList(rootTf, includeInactiveProp.boolValue);
                }
                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawColliderPreviewList(Transform root, bool includeInactive)
    {
        Collider2D[] colliders = root.GetComponentsInChildren<Collider2D>(includeInactive);
        if (colliders.Length == 0) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("預覽（將掃描到的 Collider）", EditorStyles.miniLabel);

        // 最多顯示 15 個，避免太長
        int showCount = Mathf.Min(colliders.Length, 15);
        for (int i = 0; i < showCount; i++)
        {
            var col = colliders[i];
            var overrideComp = col.GetComponent<ColliderIdOverride>();
            string id = (overrideComp != null && !string.IsNullOrEmpty(overrideComp.id))
                ? overrideComp.id
                : col.gameObject.name;

            string typeName = col.GetType().Name;

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField($"  <b>{id}</b>  <color=#888>({typeName})</color>", RichLabelStyle);

                // Ping 按鈕：點擊可定位到該物件
                if (GUILayout.Button("⊕", GUILayout.Width(22), GUILayout.Height(16)))
                {
                    EditorGUIUtility.PingObject(col.gameObject);
                    Selection.activeGameObject = col.gameObject;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        if (colliders.Length > showCount)
        {
            EditorGUILayout.LabelField($"  ...還有 {colliders.Length - showCount} 個", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    // ── Event Section ──────────────────────────────────────────────

    private void DrawEventSection()
    {
        EditorGUILayout.PropertyField(onRegistrationCompleteProp, new GUIContent("🔔 On Registration Complete"));
    }

    // ── Runtime Registry（Play Mode 專用）─────────────────────────

    private void DrawRuntimeRegistrySection(Collider2DManager manager)
    {
        showRuntimeRegistry = EditorGUILayout.BeginFoldoutHeaderGroup(showRuntimeRegistry, "🗂 Runtime Registry（已註冊的 Collider）");
        if (showRuntimeRegistry)
        {
            // 搜尋欄
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (GUILayout.Button("✕", GUILayout.Width(22)))
            {
                searchFilter = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            // 使用 Reflection 取得 private registry
            var registryField = typeof(Collider2DManager)
                .GetField("colliderRegistry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (registryField == null)
            {
                EditorGUILayout.HelpBox("無法透過 Reflection 存取 colliderRegistry。", MessageType.Warning);
            }
            else
            {
                var registry = registryField.GetValue(manager)
                    as Dictionary<ColliderGroupName, Dictionary<string, Collider2D>>;

                if (registry == null || registry.Count == 0)
                {
                    EditorGUILayout.HelpBox("Registry 為空。", MessageType.Info);
                }
                else
                {
                    foreach (var kvp in registry)
                    {
                        DrawRuntimeGroup(manager, kvp.Key, kvp.Value);
                    }
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawRuntimeGroup(Collider2DManager manager, ColliderGroupName groupName, Dictionary<string, Collider2D> group)
    {
        string key = groupName.ToString();
        if (!runtimeGroupFoldouts.ContainsKey(key))
            runtimeGroupFoldouts[key] = true;

        // 過濾
        var filteredEntries = string.IsNullOrEmpty(searchFilter)
            ? group.ToList()
            : group.Where(e => e.Key.IndexOf(searchFilter, System.StringComparison.OrdinalIgnoreCase) >= 0).ToList();

        if (filteredEntries.Count == 0 && !string.IsNullOrEmpty(searchFilter))
            return; // 搜尋無結果時隱藏該群組

        int enabledCount = filteredEntries.Count(e => e.Value != null && e.Value.enabled);

        EditorGUILayout.BeginVertical("box");
        {
            EditorGUILayout.BeginHorizontal();
            {
                runtimeGroupFoldouts[key] = EditorGUILayout.Foldout(
                    runtimeGroupFoldouts[key],
                    $"{groupName}  ({enabledCount}/{filteredEntries.Count} enabled)",
                    true
                );

                // 群組快速操作
                GUI.color = new Color(0.7f, 1f, 0.7f);
                if (GUILayout.Button("全部啟用", EditorStyles.miniButtonLeft, GUILayout.Width(60)))
                {
                    manager.EnableGroup(groupName);
                }
                GUI.color = new Color(1f, 0.75f, 0.75f);
                if (GUILayout.Button("全部停用", EditorStyles.miniButtonRight, GUILayout.Width(60)))
                {
                    manager.DisableGroup(groupName);
                }
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (runtimeGroupFoldouts[key])
            {
                foreach (var entry in filteredEntries)
                {
                    DrawRuntimeColliderEntry(manager, groupName, entry.Key, entry.Value);
                }
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawRuntimeColliderEntry(Collider2DManager manager, ColliderGroupName groupName, string id, Collider2D collider)
    {
        EditorGUILayout.BeginHorizontal();
        {
            // 狀態指示燈
            bool isActive = collider != null && collider.enabled;
            string dot = isActive ? "<color=#4CAF50>●</color>" : "<color=#F44336>●</color>";
            EditorGUILayout.LabelField(dot, RichLabelStyle, GUILayout.Width(16));

            // ID
            EditorGUILayout.LabelField(id, GUILayout.MinWidth(80));

            // 類型
            string typeName = collider != null ? collider.GetType().Name : "(null)";
            EditorGUILayout.LabelField(typeName, EditorStyles.miniLabel, GUILayout.Width(100));

            // Toggle
            bool newState = EditorGUILayout.Toggle(isActive, GUILayout.Width(20));
            if (newState != isActive && collider != null)
            {
                manager.SetColliderState(groupName, id, newState);
            }

            // Ping
            if (collider != null)
            {
                if (GUILayout.Button("⊕", GUILayout.Width(22), GUILayout.Height(16)))
                {
                    EditorGUIUtility.PingObject(collider.gameObject);
                    Selection.activeGameObject = collider.gameObject;
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    // ── Quick Actions（Play Mode）──────────────────────────────────

    private void DrawQuickActionsSection(Collider2DManager manager)
    {
        showQuickActions = EditorGUILayout.BeginFoldoutHeaderGroup(showQuickActions, "⚡ Quick Actions");
        if (showQuickActions)
        {
            EditorGUILayout.BeginVertical(BoxStyle);

            EditorGUILayout.BeginHorizontal();
            {
                GUI.color = new Color(0.7f, 1f, 0.7f);
                if (GUILayout.Button("所有群組全部啟用", GUILayout.Height(26)))
                {
                    EnableAllGroups(manager);
                }
                GUI.color = new Color(1f, 0.75f, 0.75f);
                if (GUILayout.Button("所有群組全部停用", GUILayout.Height(26)))
                {
                    DisableAllGroups(manager);
                }
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            if (GUILayout.Button("🔄 重新掃描所有 Group Entries", GUILayout.Height(26)))
            {
                // 呼叫 private ScanAndRegisterAll via reflection
                var method = typeof(Collider2DManager)
                    .GetMethod("ScanAndRegisterAll", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(manager, null);
                Debug.Log("[Collider2DManager Editor] 已重新掃描所有 Group Entries。");
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    // ── Edit Mode Preview ──────────────────────────────────────────

    private void DrawEditModePreview()
    {
        EditorGUILayout.BeginVertical(BoxStyle);
        EditorGUILayout.LabelField("ℹ️ 進入 Play Mode 後可查看 Runtime Registry 及即時操控 Collider 狀態。", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndVertical();
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private void EnableAllGroups(Collider2DManager manager)
    {
        var registryField = typeof(Collider2DManager)
            .GetField("colliderRegistry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var registry = registryField?.GetValue(manager)
            as Dictionary<ColliderGroupName, Dictionary<string, Collider2D>>;

        if (registry == null) return;
        foreach (var groupName in registry.Keys.ToList())
        {
            manager.EnableGroup(groupName);
        }
    }

    private void DisableAllGroups(Collider2DManager manager)
    {
        var registryField = typeof(Collider2DManager)
            .GetField("colliderRegistry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var registry = registryField?.GetValue(manager)
            as Dictionary<ColliderGroupName, Dictionary<string, Collider2D>>;

        if (registry == null) return;
        foreach (var groupName in registry.Keys.ToList())
        {
            manager.DisableGroup(groupName);
        }
    }
}
