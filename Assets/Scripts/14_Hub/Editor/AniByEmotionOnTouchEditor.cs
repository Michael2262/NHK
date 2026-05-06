using UnityEditor;
using UnityEngine;

/// <summary>
/// AniByEmotionOnTouch.cs 的自訂編輯器 (Editor)
/// 職責：隱藏基底類別 (ConditionalTouchReactionBase) 的欄位，
/// 只顯示 AniByEmotionOnTouch 自己的欄位。
/// 
/// ★ 重要：此腳本必須放置在名為 "Editor" 的資料夾中。
/// </summary>
[CustomEditor(typeof(AniByEmotionOnTouch))]
public class AniByEmotionOnTouchEditor : Editor
{
    // 1. 宣告我們要連結的屬性 (對應 AniByEmotionOnTouch.cs 中的 public 欄位)
    private SerializedProperty targetRendererProp;
    private SerializedProperty heroineIdProp;
    private SerializedProperty emotionAnimationsProp;

    // 2. 在 OnEnable 時連結屬性
    private void OnEnable()
    {
        // 根據 AniByEmotionOnTouch.cs 中的變數名稱 "targetRenderer" 找到它
        targetRendererProp = serializedObject.FindProperty("targetRenderer");
        // 根據 "heroineId" 找到它
        heroineIdProp = serializedObject.FindProperty("heroineId");
        // 根據 "emotionAnimations" 找到它
        emotionAnimationsProp = serializedObject.FindProperty("emotionAnimations");
    }

    // 3. 繪製 Inspector
    public override void OnInspectorGUI()
    {
        // 更新序列化物件
        serializedObject.Update();

        // --- 繪製「目標設定」區塊 ---
        // (為了美觀，我們手動加上 Header，對應腳本中的 [Header("目標設定")])
        EditorGUILayout.LabelField("目標設定", EditorStyles.boldLabel);

        if (targetRendererProp != null)
        {
            EditorGUILayout.PropertyField(targetRendererProp);
        }
        else
        {
            EditorGUILayout.HelpBox("找不到 'targetRenderer' 欄位。", MessageType.Error);
        }

        if (heroineIdProp != null)
        {
            EditorGUILayout.PropertyField(heroineIdProp);
        }
        else
        {
            EditorGUILayout.HelpBox("找不到 'heroineId' 欄位。", MessageType.Error);
        }

        EditorGUILayout.Space(10); // 加一點間距

        // --- 繪製「動畫列表」區塊 ---
        EditorGUILayout.LabelField("動畫列表", EditorStyles.boldLabel);

        if (emotionAnimationsProp != null)
        {
            // PropertyField 會自動繪製整個 List (emotion, sprites, frameDuration...)
            // 傳入 true 代表 "includeChildren"，這樣 List 才能被正確展開和編輯
            EditorGUILayout.PropertyField(emotionAnimationsProp, true);
        }
        else
        {
            EditorGUILayout.HelpBox("找不到 'emotionAnimations' 欄位。", MessageType.Error);
        }

        // 套用更改
        serializedObject.ApplyModifiedProperties();

        // ★ 關鍵：
        // 我們故意「不」呼叫 base.OnInspectorGUI(); 
        // 這樣 ConditionalTouchReactionBase 中的 'swipeConds' 欄位就不會被繪製出來。
        // base.OnInspectorGUI();
    }
}