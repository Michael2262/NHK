using UnityEngine;

public class TachieTester : MonoBehaviour
{
    [Header("測試對象設定")]
    public string targetID = "Sister";
    public string bodyName = "Normal";

    [Header("面部細節設定")]
    public string eyebrowName = "Normal";
    public string eyeName = "Happy";
    public string mouthName = "Smile";
    public string blushName = "None";
    public string otherName = "None";

    [Header("時間設定")]
    public float duration = 0.5f;

    // --- 1. 基本顯示/隱藏 ---
    [ContextMenu("1. 顯示角色")]
    public void TestShow() => TachieController.Instance.SetVisibility(targetID, true, duration);

    [ContextMenu("1. 消失角色")]
    public void TestHide() => TachieController.Instance.SetVisibility(targetID, false, duration);

    // --- 2. 綜合表情測試 ---
    [ContextMenu("2. 換完整表情 (Face)")]
    public void TestFullFace()
    {
        // 使用我們在 Controller 新增的綜合 API
        TachieController.Instance.ChangeFullFace(targetID, eyeName, mouthName, eyebrowName, blushName);
    }

    // --- 3. 單一部位測試 ---
    [ContextMenu("3-1. 換眉毛 (Eyebrow)")]
    public void TestEyebrow() => TachieController.Instance.ChangeEyebrow(targetID, eyebrowName);

    [ContextMenu("3-2. 換眼睛 (Eye)")]
    public void TestEye() => TachieController.Instance.ChangeEye(targetID, eyeName);

    [ContextMenu("3-3. 換嘴巴 (Mouth)")]
    public void TestMouth() => TachieController.Instance.ChangeMouth(targetID, mouthName);

    [ContextMenu("3-4. 換腮紅 (Blush)")]
    public void TestBlush() => TachieController.Instance.ChangeBlush(targetID, blushName);

    [ContextMenu("3-5. 換其他 (Other)")]
    public void TestOther() => TachieController.Instance.ChangeOther(targetID, otherName);

    // --- 4. 身體與位置測試 ---
    [ContextMenu("4. 換身體")]
    public void TestBody() => TachieController.Instance.ChangeBody(targetID, bodyName);

    [ContextMenu("5. 回原位 (X=0)")]
    public void TestCenter() => TachieController.Instance.MoveToCenter(targetID, duration);

    [ContextMenu("6. 到右側位")]
    public void TestRight() => TachieController.Instance.MoveToRight(targetID, duration);

    [ContextMenu("7. 到左側位")]
    public void TestLeft() => TachieController.Instance.MoveToLeft(targetID, duration);

    [ContextMenu("8. 全部清除歸位")]
    public void TestClearAll() => TachieController.Instance.ClearAll(duration);
}