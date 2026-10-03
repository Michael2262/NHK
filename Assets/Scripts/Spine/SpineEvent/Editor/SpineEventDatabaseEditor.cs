using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpineEventDatabase))]
public class SpineEventDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("ID 與 Spine 事件名稱必須完全一致。displayName 為中文名稱，description 為用途說明。修改或刪除 ID 不會自動更新場景或 Spine 動畫；已使用的 ID 建議保持固定。", MessageType.Info);
        DrawDefaultInspector();
        foreach (var error in ((SpineEventDatabase)target).GetValidationErrors())
            EditorGUILayout.HelpBox(error, MessageType.Warning);
    }

    [MenuItem("Tools/NHK/Spine/建立共用事件 ID 資料庫")]
    private static void CreateDatabase()
    {
        if (!AssetDatabase.IsValidFolder("Assets/GameData"))
            AssetDatabase.CreateFolder("Assets", "GameData");
        if (!AssetDatabase.IsValidFolder("Assets/GameData/Spine"))
            AssetDatabase.CreateFolder("Assets/GameData", "Spine");
        var path = AssetDatabase.GenerateUniqueAssetPath("Assets/GameData/Spine/SpineEventDatabase.asset");
        var database = ScriptableObject.CreateInstance<SpineEventDatabase>();
        AssetDatabase.CreateAsset(database, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = database;
        EditorGUIUtility.PingObject(database);
    }
}
