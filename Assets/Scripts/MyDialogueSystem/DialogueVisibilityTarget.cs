using System.Collections.Generic;
using UnityEngine;

/// <summary>提供對話顯示控制的目標參照，不處理點擊或條件判斷。</summary>
[AddComponentMenu("Game/Dialogue/Dialogue Visibility Target")]
public sealed class DialogueVisibilityTarget : MonoBehaviour
{
    [Header("圖片清單")]
    [Tooltip("對話期間隱藏、結束後淡入的所有圖片，例如 Cha_Pic 與 QuestIcon。包含目前未啟用的物件；只控制透明度，不改變物件啟用狀態。")]
    public List<SpriteRenderer> sprites = new List<SpriteRenderer>();

    [Header("碰撞區域")]
    [Tooltip("對話期間停用，淡入完成後恢復原狀的碰撞區域。")]
    public Collider2D[] colliders = new Collider2D[0];
}
