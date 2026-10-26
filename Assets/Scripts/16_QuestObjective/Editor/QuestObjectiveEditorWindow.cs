#if UNITY_EDITOR
using UnityEditor;

/// <summary>舊任務管理視窗已退役；暫留型別供 Unity Editor 清理。</summary>
public class QuestObjectiveEditorWindow : EditorWindow
{
    [MenuItem("Tools/Progress/Quest Objective Manager")]
    public static void ShowWindow()
    {
        GetWindow<QuestObjectiveEditorWindow>("Quest Objectives（已退役）");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "舊 QuestObjective 系統已退役，不再建立、編輯或操作任務。請在 Project 視窗清理舊任務資產，再刪除相關腳本。",
            MessageType.Warning);
    }
}
#endif
