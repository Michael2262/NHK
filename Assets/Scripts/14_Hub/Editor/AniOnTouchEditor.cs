using UnityEditor;
using UnityEngine;

/// <summary>
/// AniOnTouch.cs 的自訂編輯器 (Editor)
/// 職責：隱藏基底類別 (ConditionalTouchReactionBase) 的欄位，
/// 只顯示 AniOnTouch 自己的屬性。
/// </summary>
[CustomEditor(typeof(AniOnTouch))]
public class AniOnTouchEditor : Editor
{
    // 1. 宣告要連結的屬性
    private SerializedProperty targetRendererProp;
    private SerializedProperty animationDataProp;

    // 2. 在 OnEnable 時連結屬性
    private void OnEnable()
    {
        // 根據 AniOnTouch.cs 中的變數名稱找到對應屬性
        targetRendererProp = serializedObject.FindProperty("targetRenderer");
        animationDataProp = serializedObject.FindProperty("animationData");
    }

    // 3. 繪製 Inspector 介面
    public override void OnInspectorGUI()
    {
        // 更新序列化物件狀態
        serializedObject.Update();

        // --- 繪製「目標設定」區塊 ---
        EditorGUILayout.LabelField("目標設定", EditorStyles.boldLabel);

        if (targetRendererProp != null)
        {
            EditorGUILayout.PropertyField(targetRendererProp);
        }
        else
        {
            EditorGUILayout.HelpBox("找不到 'targetRenderer' 欄位。", MessageType.Error);
        }

        EditorGUILayout.Space(5);

        // --- 繪製「動畫設定」區塊 ---
        EditorGUILayout.LabelField("動畫設定", EditorStyles.boldLabel);

        if (animationDataProp != null)
        {
            // 因為 animationData 是一個自定義類別 (TouchAnimationData)，
            // 傳入 true 才能顯示其內部的成員屬性 (sprites, duration 等)
            EditorGUILayout.PropertyField(animationDataProp, true);
        }
        else
        {
            EditorGUILayout.HelpBox("找不到 'animationData' 欄位。", MessageType.Error);
        }

        // 套用所有在 Inspector 上的更改
        serializedObject.ApplyModifiedProperties();

        // 註：不呼叫 base.OnInspectorGUI(); 以達成隱藏基底類別欄位的效果
    }
}