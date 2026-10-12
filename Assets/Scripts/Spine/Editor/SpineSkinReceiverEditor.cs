using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpineSkinReceiver))]
public class SpineSkinReceiverEditor : Editor
{
    private SpineSkinReceiver receiver;
    private bool showPlayModeSettings;
    private readonly List<string> skinNames = new List<string>();
    private readonly HashSet<string> selectedSkinNames = new HashSet<string>(System.StringComparer.Ordinal);
    private readonly HashSet<string> groupIDs = new HashSet<string>(System.StringComparer.Ordinal);

    private void OnEnable()
    {
        receiver = (SpineSkinReceiver)target;
        Undo.undoRedoPerformed += Refresh;
        EditorApplication.delayCall += Refresh;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= Refresh;
        EditorApplication.delayCall -= Refresh;
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
    }

    private void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        // 即使停用 Domain Reload，每次開始試玩仍預設使用精簡介面。
        showPlayModeSettings = false;
        Repaint();
    }

    private void Refresh()
    {
        if (receiver == null) return;
        receiver.RefreshPreview();
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
        Repaint();
    }

    public override void OnInspectorGUI()
    {
        if (receiver == null) return;

        if (Application.isPlaying)
        {
            showPlayModeSettings = EditorGUILayout.Foldout(showPlayModeSettings, "試玩中編輯換裝設定（展開才繪製清單）", true);
            if (!showPlayModeSettings)
            {
                // 略過序列化清單、選單及大量勾選框，保留觀察狀態與手動刷新。
                DrawStatus();
                return;
            }
        }

        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("skeletonAnimation"), new GUIContent("Spine 目標"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("receiverID"), new GUIContent("接收器 ID"));
        EditorGUILayout.HelpBox("組合採整組取代。未指定保存狀態或找不到組合時使用自身預設。請勿讓其他換裝元件同時控制同一個 Spine。", MessageType.Info);

        var defaults = serializedObject.FindProperty("defaultPresetID");
        EditorGUILayout.PropertyField(defaults, new GUIContent("預設／預覽組合 ID"));
        DrawPresetPopup(defaults);
        EditorGUILayout.HelpBox("組合 ID 留空時使用手動清單。此起始設定會保留到直接試玩；遊戲保存狀態優先。", MessageType.None);

        var data = receiver.DataAsset != null ? receiver.DataAsset.GetSkeletonData(true) : null;
        skinNames.Clear();
        if (data != null)
            foreach (var skin in data.Skins) skinNames.Add(skin.Name);
        if (data == null) EditorGUILayout.HelpBox("請指定已設定 SkeletonDataAsset 的 SkeletonAnimation，才能列出可選 skin。", MessageType.Warning);

        DrawSkins(serializedObject.FindProperty("defaultSkins"), "預設手動清單（可多選）", skinNames);

        var groups = serializedObject.FindProperty("groups");
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("本地 skin 組合", EditorStyles.boldLabel);
        groupIDs.Clear();
        for (int i = 0; i < groups.arraySize; i++)
        {
            var group = groups.GetArrayElementAtIndex(i);
            var id = group.FindPropertyRelative("id");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(id, new GUIContent("組合 ID"));
                if (string.IsNullOrWhiteSpace(id.stringValue) || !groupIDs.Add(id.stringValue))
                    EditorGUILayout.HelpBox("組合 ID 必須填寫且不可重複。", MessageType.Warning);
                DrawSkins(group.FindPropertyRelative("skins"), "啟用的 skin", skinNames);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("設為預設並預覽")) defaults.stringValue = id.stringValue;
                    if (GUILayout.Button("移除此組"))
                    {
                        groups.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
            }
        }
        if (GUILayout.Button("新增組合"))
        {
            int index = groups.arraySize;
            groups.InsertArrayElementAtIndex(index);
            var group = groups.GetArrayElementAtIndex(index);
            group.FindPropertyRelative("id").stringValue = "";
            group.FindPropertyRelative("skins").ClearArray();
        }

        bool changed = serializedObject.ApplyModifiedProperties();
        if (changed) Refresh();

        DrawStatus();
    }

    private void DrawStatus()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("本次接收的組合 ID", receiver.RequestedPresetID ?? "（無）");
        EditorGUILayout.LabelField("目前顯示來源", receiver.DisplaySource ?? "（尚未初始化）");
        EditorGUILayout.LabelField("實際合成的 skin", receiver.ActiveSkins ?? "", EditorStyles.wordWrappedLabel);
        if (Application.IsPlaying(receiver.gameObject) && GUILayout.Button("立即讀取最新保存狀態"))
        {
            receiver.RefreshFromState();
            SceneView.RepaintAll();
        }
    }

    private void DrawPresetPopup(SerializedProperty defaults)
    {
        var groups = serializedObject.FindProperty("groups");
        var labels = new List<string> { "手動清單" };
        var values = new List<string> { "" };
        for (int i = 0; i < groups.arraySize; i++)
        {
            string id = groups.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue;
            if (string.IsNullOrWhiteSpace(id) || values.Contains(id)) continue;
            labels.Add(id);
            values.Add(id);
        }
        int selected = values.IndexOf(defaults.stringValue);
        EditorGUI.BeginChangeCheck();
        int next = EditorGUILayout.Popup("依 ID 選擇", selected, labels.ToArray());
        if (EditorGUI.EndChangeCheck() && next >= 0) defaults.stringValue = values[next];
    }

    private void DrawSkins(SerializedProperty list, string label, List<string> available)
    {
        list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, label, true);
        if (!list.isExpanded) return;
        EditorGUI.indentLevel++;
        // 每份清單只讀一次選取狀態，避免每個 skin 都掃描全部序列化項目。
        selectedSkinNames.Clear();
        for (int i = 0; i < list.arraySize; i++)
            selectedSkinNames.Add(list.GetArrayElementAtIndex(i).stringValue);
        foreach (string name in available)
        {
            bool wasSelected = selectedSkinNames.Contains(name);
            bool selected = EditorGUILayout.ToggleLeft(name, wasSelected);
            if (selected && !wasSelected)
            {
                int index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                list.GetArrayElementAtIndex(index).stringValue = name;
                selectedSkinNames.Add(name);
            }
            else if (!selected && wasSelected)
            {
                for (int i = list.arraySize - 1; i >= 0; i--)
                    if (list.GetArrayElementAtIndex(i).stringValue == name) list.DeleteArrayElementAtIndex(i);
                selectedSkinNames.Remove(name);
            }
        }
        EditorGUILayout.LabelField("合成順序（相同附件由後方覆蓋）", EditorStyles.miniBoldLabel);
        for (int i = 0; i < list.arraySize; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string name = list.GetArrayElementAtIndex(i).stringValue;
                EditorGUILayout.LabelField((i + 1) + ". " + name + (available.Contains(name) ? "" : "（找不到 skin）"));
                using (new EditorGUI.DisabledScope(i == 0))
                    if (GUILayout.Button("↑", GUILayout.Width(28))) list.MoveArrayElement(i, i - 1);
                using (new EditorGUI.DisabledScope(i == list.arraySize - 1))
                    if (GUILayout.Button("↓", GUILayout.Width(28))) list.MoveArrayElement(i, i + 1);
                if (GUILayout.Button("移除", GUILayout.Width(42))) { list.DeleteArrayElementAtIndex(i); break; }
            }
        }
        EditorGUI.indentLevel--;
    }
}
