using UnityEngine;

/// <summary>提供對話顯示控制的目標參照，不處理點擊或條件判斷。</summary>
[AddComponentMenu("Game/Dialogue/Dialogue Visibility Target")]
public sealed class DialogueVisibilityTarget : MonoBehaviour
{
    [Tooltip("對話期間隱藏，結束後淡入的圖片。")]
    public SpriteRenderer sprite;

    [Tooltip("對話期間停用，淡入完成後恢復原狀的碰撞區域。")]
    public Collider2D[] colliders = new Collider2D[0];
}
