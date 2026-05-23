using UnityEngine;

public class TachieTester : MonoBehaviour
{
    [Header("測試對象設定")]
    [Tooltip("單一角色 ID（用於顯隱、移動等不連動操作）")]
    public string targetID = "SisterA";
    [Tooltip("群組 ID（用於外觀類連動操作，也可填單一角色 ID 來只動一個）")]
    public string groupID = "Sister";

    [Header("身體設定")]
    public string bodyName = "Normal";

    [Header("表情預設")]
    [Tooltip("對應 TachieExpressionConfig 中的 presetName")]
    public string expressionName = "Angry";

    [Header("面部細節設定 (手動微調用)")]
    public string eyebrowName = "Normal";
    public string eyeName = "Happy";
    public string mouthName = "Smile";
    public string blushName = "None";
    public string otherName = "None";
    public string aboveName = "None";

    [Header("時間設定")]
    public float duration = 0.5f;

    // --- 1. 基本顯示/隱藏 (用 targetID，不連動) ---
    [ContextMenu("1. 顯示角色")]
    public void TestShow() => TachieController.Instance.SetVisibility(targetID, true, duration);

    [ContextMenu("1. 消失角色")]
    public void TestHide() => TachieController.Instance.SetVisibility(targetID, false, duration);

    // --- 2. 表情預設 (用 groupID，會連動) ---
    [ContextMenu("2. 套用表情預設 (Expression) [群組]")]
    public void TestExpression() => TachieController.Instance.ChangeExpression(groupID, expressionName);

    // --- 3. 綜合表情測試 (用 groupID，會連動) ---
    [ContextMenu("3. 換完整表情 (Face) [群組]")]
    public void TestFullFace()
    {
        TachieController.Instance.ChangeFullFace(groupID, eyeName, mouthName, eyebrowName, blushName);
    }

    // --- 4. 單一部位測試 (用 groupID，會連動) ---
    [ContextMenu("4-1. 換眉毛 (Eyebrow) [群組]")]
    public void TestEyebrow() => TachieController.Instance.ChangeEyebrow(groupID, eyebrowName);

    [ContextMenu("4-2. 換眼睛 (Eye) [群組]")]
    public void TestEye() => TachieController.Instance.ChangeEye(groupID, eyeName);

    [ContextMenu("4-3. 換嘴巴 (Mouth) [群組]")]
    public void TestMouth() => TachieController.Instance.ChangeMouth(groupID, mouthName);

    [ContextMenu("4-4. 換腮紅 (Blush) [群組]")]
    public void TestBlush() => TachieController.Instance.ChangeBlush(groupID, blushName);

    [ContextMenu("4-5. 換其他 (Other) [群組]")]
    public void TestOther() => TachieController.Instance.ChangeOther(groupID, otherName);

    [ContextMenu("4-6. 換最上層 (Above) [群組]")]
    public void TestAbove() => TachieController.Instance.ChangeAbove(groupID, aboveName);

    // --- 5. 身體測試 (用 groupID，會連動) ---
    [ContextMenu("5. 換身體 [群組]")]
    public void TestBody() => TachieController.Instance.ChangeBody(groupID, bodyName);

    // --- 6. 位置測試 (用 targetID，不連動) ---
    [ContextMenu("6. 回原位 (X=0)")]
    public void TestCenter() => TachieController.Instance.MoveToCenter(targetID, duration);

    [ContextMenu("7. 到右側位")]
    public void TestRight() => TachieController.Instance.MoveToRight(targetID, duration);

    [ContextMenu("8. 到左側位")]
    public void TestLeft() => TachieController.Instance.MoveToLeft(targetID, duration);

    // --- 7. 全部清除 ---
    [ContextMenu("9. 全部清除歸位")]
    public void TestClearAll() => TachieController.Instance.ClearAll(duration);
}