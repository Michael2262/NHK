using UnityEditor;
using UnityEngine;

/// <summary>
/// InteractableCharacter.cs 的自訂編輯器 (Editor)
/// (★ 已更新：顯示新的「條件按鈕列表」)
/// </summary>
[CustomEditor(typeof(InteractableCharacter))]
public class InteractableCharacterEditor : Editor
{
    // 1. 宣告我們要連結的屬性
    private SerializedProperty characterSpriteProp;   // ★ 新增
    private SerializedProperty interactionUIPanelProp;
    private SerializedProperty conditionalButtonsProp;

    // 2. 在 OnEnable 時連結屬性
    private void OnEnable()
    {
        characterSpriteProp = serializedObject.FindProperty("characterSprite");       // ★ 新增
        interactionUIPanelProp = serializedObject.FindProperty("interactionUIPanel");
        conditionalButtonsProp = serializedObject.FindProperty("conditionalButtons");
    }

    // 3. 繪製 Inspector
    public override void OnInspectorGUI()
    {
        // 更新序列化物件
        serializedObject.Update();

        // --- ★ 繪製「角色顯示」 ---
        EditorGUILayout.LabelField("角色顯示", EditorStyles.boldLabel);

        if (characterSpriteProp != null)
        {
            EditorGUILayout.PropertyField(characterSpriteProp);
        }
        else
        {
            EditorGUILayout.HelpBox("找不到 'characterSprite' 欄位。", MessageType.Error);
        }

        EditorGUILayout.Space(10);

        // --- 繪製 UI 連結 ---
        EditorGUILayout.LabelField("UI 連結", EditorStyles.boldLabel);
        if (interactionUIPanelProp != null)
        {
            EditorGUILayout.PropertyField(interactionUIPanelProp);
        }
        else
        {
            EditorGUILayout.HelpBox("找不到 'interactionUIPanel' 欄位。", MessageType.Error);
        }

        EditorGUILayout.Space(10); // 加一點間距

        // --- ★ 繪製「條件按鈕列表」 ---
        if (conditionalButtonsProp != null)
        {
            // PropertyField 會自動繪製整個 List，包含所有子欄位
            EditorGUILayout.PropertyField(conditionalButtonsProp, true); // true = 包含子項
        }
        else
        {
            EditorGUILayout.HelpBox("找不到 'conditionalButtons' 欄位。", MessageType.Error);
        }

        // 套用更改
        serializedObject.ApplyModifiedProperties();

        // ★ 關鍵：
        // 仍然不呼叫 base.OnInspectorGUI()，以隱藏基底類別的欄位
        // base.OnInspectorGUI();
    }
}