using UnityEngine;

/// <summary>
/// 任務目標定義（SO）。
/// - ObjectiveID 直接使用 asset 檔名（與 ProgressBaseDefinition.FlagID 同做法）。
/// - 放置路徑必須在 Resources/Progress/Objective/ 之下，
///   GameStatusService 啟動時會用 Resources.LoadAll 建立目錄。
/// - 建議用 Tools → Progress → Quest Objective Manager 視窗來增減管理。
/// </summary>
[CreateAssetMenu(menuName = "Game/Quest/Objective Definition", fileName = "Obj_NewObjective")]
public class QuestObjectiveDefinition : ScriptableObject
{
    [Tooltip("任務目標顯示文字的 TextTable Key（多語系）。UI 會用 DialogueManager.GetLocalizedText 查表。")]
    public string TextTableKey;

    [Tooltip("UI 清單排序用，數字小的排前面；相同時依 ID 排序。")]
    public int SortOrder = 0;

    [Tooltip("完成此目標時，是否同步加一顆 Persistent Flag（ID = ObjDone_目標ID），" +
             "供 Dialogue System 條件式 / FSM / ProgressConditionTrigger 判斷。預設不映射。")]
    public bool MirrorFlagOnComplete = false;

    [Tooltip("描述用途（僅開發備註，不會顯示在遊戲中）")]
    [SerializeField, TextArea] private string description;

    /// <summary> 目標 ID = asset 檔名 </summary>
    public string ObjectiveID => name;

    /// <summary> 開發備註（Editor 工具用） </summary>
    public string Description
    {
        get => description;
        set => description = value;
    }
}
