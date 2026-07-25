#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// AdventureDungeonData 的自訂 Inspector。
/// 主要目的是替 [SerializeReference] 的 CompletionEffects 提供型別下拉
/// （Unity 內建不會自動長出來）。其餘欄位維持預設繪製。
/// </summary>
[CustomEditor(typeof(AdventureDungeonData))]
public class AdventureDungeonDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "CompletionEffects");

        EditorGUILayout.Space(10);
        AdventureEffectListDrawer.Draw(serializedObject,
            serializedObject.FindProperty("CompletionEffects"),
            "效果 - 里程完成時",
            "里程達標時執行一次，時機在該次翻牌完全結算之後");

        EditorGUILayout.HelpBox(
            "「結束大冒險」已是里程達標的固定行為，不需要在這裡放 End Adventure。\n\n" +
            "這裡通常放：\n" +
            "・Mark Dungeon Cleared —— 標記此地點已攻克（寫入 persistent 旗標）\n" +
            "・Play Conversation —— 通關演出\n" +
            "・獎勵類效果（道具 / 數值套組等）\n\n" +
            "觸發時機（等待秒數）由 AdventureCardPresenter 控制。",
            MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
