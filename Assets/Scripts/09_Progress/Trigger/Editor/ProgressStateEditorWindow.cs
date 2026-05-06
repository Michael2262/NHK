using UnityEditor;
using UnityEngine;

public class ProgressStateEditorWindow : EditorWindow
{
    private ProgressStateController _target;
    private Vector2 _scrollPos;
    private int _selectedStateIndex = -1;

    [MenuItem("Window/Game/Progress State Editor")]
    public static void ShowWindow() => GetWindow<ProgressStateEditorWindow>("進度狀態編輯器");

    private void OnGUI()
    {
        DrawToolbar();

        if (_target == null)
        {
            EditorGUILayout.HelpBox("請選擇場景中的 ProgressStateController。", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        DrawLeftPanel();
        DrawRightPanel();
        EditorGUILayout.EndHorizontal();

        // 執行中自動重繪，達成「發光」動畫感
        if (Application.isPlaying) Repaint();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        _target = (ProgressStateController)EditorGUILayout.ObjectField("目標物件", _target, typeof(ProgressStateController), true);
        GUILayout.FlexibleSpace();
        if (Application.isPlaying)
            GUILayout.Label("● 運作中 (Play Mode)", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(250), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("狀態清單", EditorStyles.boldLabel);

        if (GUILayout.Button("新增狀態 (+)", GUILayout.Height(25)))
            _target.States.Add(new ProgressStateController.ObjectState { StateName = "新狀態" });

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        for (int i = 0; i < _target.States.Count; i++)
        {
            var state = _target.States[i];

            // --- 發光邏輯：判斷目前狀態是否符合條件 ---
            Color originalColor = GUI.backgroundColor;
            if (Application.isPlaying)
            {
                // 我們呼叫 Controller 的判斷邏輯
                bool isMet = CheckStateMetInEditor(state);
                if (isMet) GUI.backgroundColor = new Color(0.4f, 1f, 0.4f); // 符合條件時變綠色
            }

            if (_selectedStateIndex == i) GUI.backgroundColor = Color.cyan;

            if (GUILayout.Button($"{i}: {state.StateName} (P:{state.Priority})", GUILayout.Height(30)))
                _selectedStateIndex = i;

            GUI.backgroundColor = originalColor;
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
        if (_selectedStateIndex >= 0 && _selectedStateIndex < _target.States.Count)
        {
            SerializedObject so = new SerializedObject(_target);
            SerializedProperty statesProp = so.FindProperty("States");
            SerializedProperty currentProp = statesProp.GetArrayElementAtIndex(_selectedStateIndex);

            EditorGUILayout.LabelField("詳細編輯", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(currentProp.FindPropertyRelative("StateName"));
            EditorGUILayout.PropertyField(currentProp.FindPropertyRelative("Priority"));
            EditorGUILayout.PropertyField(currentProp.FindPropertyRelative("Logic"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(currentProp.FindPropertyRelative("Conditions"), true);
            EditorGUILayout.PropertyField(currentProp.FindPropertyRelative("ToActivate"), true);
            EditorGUILayout.PropertyField(currentProp.FindPropertyRelative("ToDeactivate"), true);

            if (GUILayout.Button("刪除選中狀態", GUILayout.Width(120)))
            {
                _target.States.RemoveAt(_selectedStateIndex);
                _selectedStateIndex = -1;
            }
            so.ApplyModifiedProperties();
        }
        EditorGUILayout.EndVertical();
    }

    // 輔助：在編輯器視窗中安全地調用條件檢查
    private bool CheckStateMetInEditor(ProgressStateController.ObjectState state)
    {
        // 這裡需要將 ProgressStateController 的 IsStateMet 設為 Public 或是重新實作
        // 為了方便，我們直接透過 Target 呼叫 Evaluate 邏輯
        var method = _target.GetType().GetMethod("IsStateMet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (bool)method.Invoke(_target, new object[] { state });
    }
}