using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayMakerMinigameAdapter_Multi2))]
public class PlayMakerMinigameAdapter_Multi2Editor : Editor
{
    // 對應表：純靜態資料，不佔序列化空間
    private static readonly string[][] MappingTable = new string[][]
    {
        //  說明                   FSM 變數名                    資料來源
        new[]{ "女主角唯一 ID",     "fsm_HeroineID",             "HeroineID" },
        new[]{ "基礎興奮等級",      "fsm_LocalExcitedLv",        "BaseExcitementLevel" },
        //new[]{ "基礎興奮經驗值",    "fsm_LocalExcitement",       "BaseExcitementExp" },
        new[]{ "興奮經驗門檻",       "fsm_ExcitementMax",         "GetCurrentExcitementThreshold()" },
        new[]{ "興奮等級已達上限",   "IsExcitementMaxLv",         "IsExcitementLevelLocked()" },
        new[]{ "開發度等級",        "fsm_LewdnessLevel",         "LewdnessLevel" },
        new[]{ "開發度經驗值",      "fsm_LewdnessExp",           "LewdnessExp" },
        new[]{ "親密度等級",        "fsm_LocalAffinityLv",       "BaseAffinityLevel" },
        new[]{ "親密度經驗值",      "fsm_LocalAffinityExp",      "BaseAffinityExp" },
        new[]{ "親密度經驗門檻",     "fsm_AffinityMax",           "GetCurrentAffinityThreshold()" },
        new[]{ "親密度等級已達上限", "IsAffinityMaxLv",           "IsAffinityLevelLocked()" },
        new[]{ "目前情緒",          "fsm_Emotion",               "CurrentEmotion (enum)" },
        new[]{ "遊戲得分",          "fsm_GameScore",             "FSM 回報 (int)" },
        //new[]{ "個人可疑度",        "fsm_PersonalSuspicion",     "PersonalSuspicion" },
        new[]{ "個人可疑度上限",    "fsm_PersonalSuspicionMax",  "PersonalSuspicionMax" },
    };

    private bool _showMapping = true;

    public override void OnInspectorGUI()
    {
        // 先畫預設的 Inspector（FSM 配置、結果處理器、時間配置等）
        DrawDefaultInspector();

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