using System;
using System.Collections.Generic;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

/// <summary>編輯隨機動畫群組，提供名稱檢查與數字尾碼批次加入。</summary>
[CustomEditor(typeof(SpineRandomAnimationPlayer))]
public class SpineRandomAnimationPlayerEditor : Editor
{
    // 搜尋文字與預覽位置只屬於編輯器，不寫入遊戲資料。
    private readonly Dictionary<string, string> prefixes = new Dictionary<string, string>();
    private readonly Dictionary<string, Vector2> scrolls = new Dictionary<string, Vector2>();
    private int structureVersion;

    private void OnEnable() => Undo.undoRedoPerformed += OnUndoRedo;
    private void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

    private void OnUndoRedo()
    {
        structureVersion++;
        prefixes.Clear();
        scrolls.Clear();
        Repaint();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("controller"));

        var data = ResolveSkeletonData(out string sourceMessage);
        var names = new List<string>();
        if (data != null)
            foreach (var animation in data.Animations) names.Add(animation.Name);
        var validNames = new HashSet<string>(names, StringComparer.Ordinal);
        EditorGUILayout.HelpBox(sourceMessage, data == null ? MessageType.Warning : MessageType.Info);

        var groups = serializedObject.FindProperty("groups");
        for (int i = 0; i < groups.arraySize; i++)
        {
            var group = groups.GetArrayElementAtIndex(i);
            var id = group.FindPropertyRelative("id");
            bool remove;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    group.isExpanded = EditorGUILayout.Foldout(group.isExpanded,
                        $"群組 {i + 1}：{id.stringValue}", true);
                    remove = GUILayout.Button("刪除群組", GUILayout.Width(80));
                }
                if (group.isExpanded && !remove)
                {
                    EditorGUILayout.PropertyField(id, new GUIContent("群組 ID"));
                    EditorGUILayout.PropertyField(group.FindPropertyRelative("track"));
                    EditorGUILayout.PropertyField(group.FindPropertyRelative("allowRepeat"));
                    EditorGUILayout.PropertyField(group.FindPropertyRelative("restartWhenExhausted"));
                    if (string.IsNullOrWhiteSpace(id.stringValue))
                        EditorGUILayout.HelpBox("請填寫群組 ID。", MessageType.Warning);
                    else
                        for (int j = 0; j < groups.arraySize; j++)
                            if (j != i && groups.GetArrayElementAtIndex(j).FindPropertyRelative("id").stringValue == id.stringValue)
                            {
                                EditorGUILayout.HelpBox("群組 ID 重複，播放時會拒絕執行。", MessageType.Error);
                                break;
                            }

                    var animations = group.FindPropertyRelative("animations");
                    DrawAnimations(animations, names, validNames, data != null);
                    DrawBatch(animations, names, data != null);
                }
            }
            if (remove)
            {
                groups.DeleteArrayElementAtIndex(i);
                OnUndoRedo();
                break;
            }
        }

        if (GUILayout.Button("新增群組"))
        {
            int index = groups.arraySize++;
            var group = groups.GetArrayElementAtIndex(index);
            group.FindPropertyRelative("id").stringValue = "";
            group.FindPropertyRelative("track").intValue = 0;
            group.FindPropertyRelative("allowRepeat").boolValue = false;
            group.FindPropertyRelative("restartWhenExhausted").boolValue = true;
            group.FindPropertyRelative("animations").ClearArray();
            group.isExpanded = true;
            structureVersion++;
        }
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawAnimations(SerializedProperty animations, List<string> names,
        HashSet<string> validNames, bool canValidate)
    {
        EditorGUILayout.LabelField($"動畫清單（{animations.arraySize}）", EditorStyles.boldLabel);
        if (animations.arraySize == 0)
            EditorGUILayout.HelpBox("請新增動畫，或使用下方的批次加入功能。", MessageType.Info);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < animations.arraySize; i++)
        {
            var item = animations.GetArrayElementAtIndex(i);
            bool remove;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label((i + 1).ToString(), GUILayout.Width(25));
                // 直接畫文字欄位，避開 SpineAnimation attribute 的下拉專用繪製。
                Rect rect = EditorGUILayout.GetControlRect();
                EditorGUI.BeginProperty(rect, GUIContent.none, item);
                EditorGUI.BeginChangeCheck();
                string value = EditorGUI.TextField(rect, item.stringValue);
                if (EditorGUI.EndChangeCheck()) item.stringValue = value;
                EditorGUI.EndProperty();
                using (new EditorGUI.DisabledScope(!canValidate || names.Count == 0))
                    if (GUILayout.Button("選單", GUILayout.Width(45))) ShowMenu(item, names);
                remove = GUILayout.Button("−", GUILayout.Width(25));
            }
            if (remove)
            {
                animations.DeleteArrayElementAtIndex(i);
                structureVersion++;
                break;
            }

            if (!canValidate)
                EditorGUILayout.LabelField("尚無動畫來源，無法檢查", EditorStyles.miniLabel);
            else if (string.IsNullOrWhiteSpace(item.stringValue))
                EditorGUILayout.HelpBox("動畫名稱不可為空。", MessageType.Error);
            else if (!validNames.Contains(item.stringValue))
                EditorGUILayout.HelpBox("找不到動畫：請檢查大小寫及空白。", MessageType.Error);
            else if (!seen.Add(item.stringValue))
                EditorGUILayout.HelpBox("名稱重複：播放時只算一個候選。", MessageType.Warning);
            else
                EditorGUILayout.LabelField("✓ 動畫存在", EditorStyles.miniLabel);
        }
        if (GUILayout.Button("新增動畫欄位"))
        {
            Append(animations, "");
            structureVersion++;
        }
    }

    private void ShowMenu(SerializedProperty item, List<string> names)
    {
        string path = item.propertyPath;
        string original = item.stringValue;
        var owner = target;
        int version = structureVersion;
        // 先提交文字修改；回呼重新取得 SerializedProperty，避免持有失效引用。
        serializedObject.ApplyModifiedProperties();
        var menu = new GenericMenu();
        foreach (string name in names)
        {
            string selected = name;
            menu.AddItem(new GUIContent(name.Replace("&", "＆")), original == name, () =>
            {
                if (this == null || owner == null || target != owner || version != structureVersion) return;
                using (var fresh = new SerializedObject(owner))
                {
                    var destination = fresh.FindProperty(path);
                    if (destination == null || destination.stringValue != original) return;
                    destination.stringValue = selected;
                    fresh.ApplyModifiedProperties();
                }
                Repaint();
            });
        }
        menu.ShowAsContext();
    }

    private void DrawBatch(SerializedProperty animations, List<string> names, bool canValidate)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("依數字尾碼批次加入", EditorStyles.boldLabel);
        string key = animations.propertyPath;
        prefixes.TryGetValue(key, out string prefix);
        prefix = EditorGUILayout.TextField(new GUIContent("共同名稱", "例如 Semen_Head 或 Semen_Head_；後面必須全為數字。"), prefix ?? "");
        prefixes[key] = prefix;
        if (string.IsNullOrWhiteSpace(prefix) || !canValidate) return;

        string stem = prefix.EndsWith("_", StringComparison.Ordinal) ? prefix : prefix + "_";
        var matches = new List<string>();
        foreach (string name in names)
        {
            if (!name.StartsWith(stem, StringComparison.Ordinal) || name.Length == stem.Length) continue;
            bool digitsOnly = true;
            for (int i = stem.Length; i < name.Length; i++)
                if (name[i] < '0' || name[i] > '9') { digitsOnly = false; break; }
            if (digitsOnly) matches.Add(name);
        }
        // 以有效位數及字典序比較數值，支援前導零與任意位數，避免整數溢位。
        matches.Sort((a, b) =>
        {
            string left = a.Substring(stem.Length).TrimStart('0');
            string right = b.Substring(stem.Length).TrimStart('0');
            int order = left.Length.CompareTo(right.Length);
            if (order == 0) order = string.CompareOrdinal(left, right);
            return order != 0 ? order : string.CompareOrdinal(a, b);
        });

        var existing = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < animations.arraySize; i++) existing.Add(animations.GetArrayElementAtIndex(i).stringValue);
        var additions = matches.FindAll(name => !existing.Contains(name));
        EditorGUILayout.LabelField($"找到 {matches.Count} 個動畫，可新增 {additions.Count} 個");
        if (matches.Count == 0)
        {
            EditorGUILayout.HelpBox($"沒有符合「{stem}＋純數字」的動畫。", MessageType.Info);
            return;
        }
        scrolls.TryGetValue(key, out Vector2 scroll);
        using (var view = new EditorGUILayout.ScrollViewScope(scroll,
            GUILayout.Height(Mathf.Min(150f, matches.Count * 20f + 8f))))
        {
            scrolls[key] = view.scrollPosition;
            foreach (string name in matches)
                EditorGUILayout.LabelField(name + (existing.Contains(name) ? "（已加入）" : ""));
        }
        using (new EditorGUI.DisabledScope(additions.Count == 0))
            if (GUILayout.Button($"加入此群組（{additions.Count} 個）"))
            {
                foreach (string name in additions) Append(animations, name);
                structureVersion++;
                GUI.FocusControl(null);
            }
    }

    private static void Append(SerializedProperty animations, string name)
    {
        int index = animations.arraySize++;
        animations.GetArrayElementAtIndex(index).stringValue = name;
    }

    private Spine.SkeletonData ResolveSkeletonData(out string message)
    {
        var player = (SpineRandomAnimationPlayer)target;
        var controller = serializedObject.FindProperty("controller").objectReferenceValue as SpineAnimationController;
        if (controller == null) controller = player.GetComponent<SpineAnimationController>();
        message = "尚未指定動畫來源：請指定 Controller，並確認其 SkeletonAnimation 或 SkeletonGraphic 已設定 Spine 資料。";
        if (controller == null) return null;

        // Play Mode 以實際播放狀態為準；編輯模式只讀取資產，不初始化或播放場景元件。
        if (Application.isPlaying && controller.GetAnimationState() != null)
        {
            message = $"檢查來源：{controller.name} 的播放資料（區分大小寫）";
            return controller.GetAnimationState().Data.SkeletonData;
        }

        SkeletonDataAsset asset;
        using (var source = new SerializedObject(controller))
        {
            var animation = source.FindProperty("skeletonAnimation").objectReferenceValue as SkeletonAnimation;
            var graphic = source.FindProperty("skeletonGraphic").objectReferenceValue as SkeletonGraphic;
            if (animation == null) animation = controller.GetComponent<SkeletonAnimation>();
            if (graphic == null) graphic = controller.GetComponent<SkeletonGraphic>();
            // 與 Controller.Initialize 的來源優先順序一致。
            asset = animation != null ? animation.SkeletonDataAsset : graphic != null ? graphic.SkeletonDataAsset : null;
        }
        if (asset == null) return null;
        var data = asset.GetSkeletonData(true);
        message = data != null ? $"檢查來源：{asset.name}（區分大小寫）" : $"無法讀取 Spine 資料：{asset.name}，目前無法檢查名稱。";
        return data;
    }
}
