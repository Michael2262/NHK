using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayMakerMinigameAdapter_Multi2))]
public class PlayMakerMinigameAdapter_Multi2Editor : Editor
{
    // 對應表：純靜態資料，不佔序列化空間
    private static readonly string[][] MappingTable = new string[][]
    {
        new[]{ "女主角 ID", "fsm_HeroineID", "HeroineID" },
        new[]{ "信賴", "fsm_Trust", "Trust" },
        new[]{ "好感積分", "fsm_Affinity", "Affinity" },
        new[]{ "心情", "fsm_Mood", "Mood" },
        new[]{ "性欲", "fsm_Libido", "Libido" },
        new[]{ "興奮", "fsm_Excitement", "Excitement" },
        new[]{ "高潮", "fsm_Orgasm", "Orgasm" },
        new[]{ "當前情緒", "fsm_Emotion", "CurrentEmotion (HeroineEmotionCardType)" },
    };

    private bool _showMapping = true;

    public override void OnInspectorGUI()
    {
        // 先畫預設的 Inspector（FSM 配置、結果處理器、時間配置等）
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("舊等級／經驗／個人可疑度注入與結算已停用。下列新版數值是選填的初始化輸入；不會自動寫回 Model。", MessageType.Warning);

        EditorGUILayout.Space(8);

        // 可摺疊的對應表
        _showMapping = EditorGUILayout.Foldout(_showMapping, "📋 FSM 變數對應表", true, EditorStyles.foldoutHeader);
        if (!_showMapping) return;

        // 表頭
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("說明", EditorStyles.boldLabel, GUILayout.Width(120));
        GUILayout.Label("FSM 變數名", EditorStyles.boldLabel, GUILayout.Width(200));
        GUILayout.Label("資料來源", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        // 每一列
        Color originalBg = GUI.backgroundColor;
        for (int i = 0; i < MappingTable.Length; i++)
        {
            // 交替底色增加可讀性
            GUI.backgroundColor = (i % 2 == 0) ? new Color(0.85f, 0.85f, 0.95f) : Color.white;
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            GUILayout.Label(MappingTable[i][0], GUILayout.Width(120));

            // FSM 變數名用 SelectableLabel，方便直接複製貼到 PlayMaker
            EditorGUILayout.SelectableLabel(MappingTable[i][1], EditorStyles.textField, GUILayout.Width(200), GUILayout.Height(EditorGUIUtility.singleLineHeight));

            GUILayout.Label(MappingTable[i][2]);

            EditorGUILayout.EndHorizontal();
        }
        GUI.backgroundColor = originalBg;
    }
}