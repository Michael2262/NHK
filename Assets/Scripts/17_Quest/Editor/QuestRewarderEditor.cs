#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(QuestRewarder))]
public sealed class QuestRewarderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("預留元件，目前沒有執行功能。獎勵請在 Quest SO 設定，領取順序使用 Quest Database 的任務清單。", MessageType.Info);
    }
}
#endif
