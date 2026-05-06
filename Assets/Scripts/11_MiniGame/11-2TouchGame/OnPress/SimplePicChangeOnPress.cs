using UnityEngine;

/// <summary>
/// 極簡圖片切換：收到 GesturePressLogicProxy 的觸發後，依序切換 Sprite。
/// 不與 FSM 溝通，不處理 Reset / WatchOut。
/// </summary>
public class SimplePicChangeOnPress : ConditionalPressReactionBase
{
    [Header("Sprite Settings")]
    [Tooltip("目標 SpriteRenderer，未指定則自動抓同物件上的")]
    public SpriteRenderer targetRenderer;

    [Tooltip("要輪流切換的圖片清單（至少放 2 張）")]
    public Sprite[] sprites;

    private int _currentIndex = 0;

    protected override void Awake()
    {
        base.Awake();
        if (!targetRenderer)
            targetRenderer = GetComponent<SpriteRenderer>();

        
    }

    /// <summary>
    /// 被觸發時切換到下一張圖片
    /// </summary>
    public override void OnTouched()
    {
        if (targetRenderer == null || sprites == null || sprites.Length == 0)
            return;

        targetRenderer.sprite = sprites[_currentIndex];
        _currentIndex = (_currentIndex + 1) % sprites.Length;
    }

    // === 以下覆寫為空，本元件不需要這些行為 ===

    public override void WatchOut() { }
    public override void ResetToOriginal()
    {
        _currentIndex = 0;
    }
}
