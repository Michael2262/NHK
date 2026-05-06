using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

/// <summary>
/// 定義單一組 Sprite 序列動畫的資料結構
/// </summary>
[System.Serializable]
public class TouchAnimationData
{
    [Tooltip("要依序播放的 Sprite 序列")]
    public List<Sprite> sprites;

    [Tooltip("每張圖片顯示的秒數 (影格持續時間)")]
    public float frameDuration = 0.1f;

    [Tooltip("這是否是單次播放的動畫？")]
    public bool playOnce = true;

    [Tooltip("若 playOnce=true，播放完畢後是否停在最後一幀？(若 false，則恢復到第一幀)")]
    public bool stopAtLastFrame = true;
}

/// <summary>
/// 【通用點擊動畫腳本】
/// 點擊後播放指定動畫，且必須在滑鼠移開後才能再次觸發。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AniOnTouch : ConditionalTouchReactionBase, IPointerExitHandler
{
    [Header("目標設定")]
    [Tooltip("要播放動畫的 SpriteRenderer。如果為空，會自動嘗試 GetComponent<SpriteRenderer>()")]
    public SpriteRenderer targetRenderer;

    [Header("動畫設定")]
    public TouchAnimationData animationData;

    // --- 內部狀態 ---
    private bool _isPlaying = false;       // 防止動畫播放時重複觸發
    private bool _awaitingMouseExit = false; // 旗標：是否正在等待滑鼠移開
    private Coroutine _animationCoroutine;

    void Start()
    {
        // 1. 獲取 SpriteRenderer
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetRenderer == null)
        {
            Debug.LogError($"[{name}] AniOnTouch: 找不到 SpriteRenderer!", this);
            this.enabled = false;
            return;
        }

        // 2. 初始化基底類別 (僅偵測 Click)
        swipeConds = new SwipeDir[0];
    }

    /// <summary>
    /// 當點擊成功時由基底類別呼叫
    /// </summary>
    public override void OnTouched()
    {
        // 檢查條件：1. 是否正在播放中  2. 是否正在等待滑鼠移開
        if (_isPlaying || _awaitingMouseExit)
        {
            return;
        }

        // 檢查資料有效性
        if (animationData == null || animationData.sprites == null || animationData.sprites.Count == 0)
        {
            Debug.LogWarning($"[{name}] 沒有設定動畫圖片。");
            return;
        }

        // 播放動畫
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }

        _animationCoroutine = StartCoroutine(PlaySpriteAnimation(animationData));

        // 設定旗標：點擊成功後，滑鼠必須移開 Collider 才能再次點擊
        _awaitingMouseExit = true;
    }

    /// <summary>
    /// 播放動畫序列的協程
    /// </summary>
    private IEnumerator PlaySpriteAnimation(TouchAnimationData anim)
    {
        _isPlaying = true;

        if (anim.playOnce)
        {
            // 單次播放
            for (int i = 0; i < anim.sprites.Count; i++)
            {
                targetRenderer.sprite = anim.sprites[i];
                yield return new WaitForSeconds(anim.frameDuration);
            }

            // 結束後處理
            if (!anim.stopAtLastFrame)
            {
                targetRenderer.sprite = anim.sprites[0];
            }

            _isPlaying = false;
            _animationCoroutine = null;
        }
        else
        {
            // 循環播放 (會一直播放直到物件被銷毀或手動停止)
            int index = 0;
            while (true)
            {
                targetRenderer.sprite = anim.sprites[index];
                yield return new WaitForSeconds(anim.frameDuration);
                index = (index + 1) % anim.sprites.Count;
            }
        }
    }

    /// <summary>
    /// 實作 IPointerExitHandler：當滑鼠移出 Collider 時觸發
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        // 重置點擊鎖定旗標
        if (_awaitingMouseExit)
        {
            _awaitingMouseExit = false;
        }
    }
}