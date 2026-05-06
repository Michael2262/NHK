using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems; //  1. 導入 EventSystems

/// <summary>
/// 【[Serializable]】
/// 職責：定義一個「情緒」與「Sprite 序列動畫」的對應關係。
/// </summary>
[System.Serializable]
public class EmotionSpriteAnimation
{
    [Tooltip("對應的情緒")]
    public HeroineEmotionCardType emotion;

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
/// 【Prefab 腳本】
/// 職責：繼承自 ConditionalTouchReactionBase，
/// 點擊後，會根據指定女主角的當前情緒(Emotion)，
/// 在指定的 SpriteRenderer 上播放對應的 Sprite 序列動畫。
/// (必須在滑鼠移開後，才能再次觸發)
/// </summary>
[RequireComponent(typeof(Collider2D))]
//  2. 實作 IPointerExitHandler 介面
public class AniByEmotionOnTouch : ConditionalTouchReactionBase, IPointerExitHandler
{
    [Header("目標設定")]
    [Tooltip("要播放動畫的 SpriteRenderer。如果為空，會自動嘗試 GetComponent<SpriteRenderer>()")]
    public SpriteRenderer targetRenderer;

    [Tooltip("要查詢情緒的女主角 ID (來自 HeroineStat 的 ID)")]
    public string heroineId;

    [Header("動畫列表")]
    [Tooltip("根據情緒對應的動畫序列。請至少設定一個 Idle 狀態作為預設。")]
    public List<EmotionSpriteAnimation> emotionAnimations;

    [Header("Fallback")]
    [Tooltip("找不到 CurrentEmotion 對應動畫時，優先使用的備用情緒。若仍找不到，使用列表第一個有效動畫。")]
    public HeroineEmotionCardType fallbackEmotion = HeroineEmotionCardType.Angry;

    // --- 內部狀態 ---
    private GameStatusService _service;
    private HeroineStatusModel _heroine;
    private bool _isPlaying = false; // 防止動畫播放時重複觸發
    private Coroutine _animationCoroutine; // 用於停止當前動畫

    //  3. 新增旗標：是否正在等待滑鼠移開
    private bool _awaitingMouseExit = false;

    // 用於查找的字典，效能比 List 遍歷更好
    private Dictionary<HeroineEmotionCardType, EmotionSpriteAnimation> _animationMap;

    // 用於快取同物件上的播放器
    private EmotionAnimationPlayer _linkedPlayer; 

    void Start()
    {
        // --- 1. 獲取依賴 ---
        _service = GameStatusService.Instance;
        if (_service == null)
        {
            Debug.LogError($"[{name}] AniByEmotionOnTouch: GameStatusService is not found!", this);
            this.enabled = false;
            return;
        }

        if (string.IsNullOrEmpty(heroineId))
        {
            Debug.LogError($"[{name}] AniByEmotionOnTouch: HeroineID is not set!", this);
            this.enabled = false;
            return;
        }

        // --- 2. 獲取女主角 Model ---
        if (!_service.Heroines.TryGetValue(heroineId, out _heroine))
        {
            Debug.LogError($"[{name}] AniByEmotionOnTouch: Heroine with ID '{heroineId}' not found in GameStatusService.Heroines!", this);
            this.enabled = false;
            return;
        }

        // --- 3. 獲取 SpriteRenderer ---
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }
        if (targetRenderer == null)
        {
            Debug.LogError($"[{name}] AniByEmotionOnTouch: Target SpriteRenderer is not set and not found on this GameObject!", this);
            this.enabled = false;
            return;
        }

        // --- 4. 初始化基底類別 (我們只關心點擊) ---
        swipeConds = new SwipeDir[0];

        // 嘗試獲取同物件上的 EmotionAnimationPlayer
        _linkedPlayer = GetComponent<EmotionAnimationPlayer>();

        // --- 5. 將 List 轉換為 Dictionary 以便快速查找 ---
        _animationMap = new Dictionary<HeroineEmotionCardType, EmotionSpriteAnimation>();
        foreach (var anim in emotionAnimations)
        {
            if (!_animationMap.ContainsKey(anim.emotion))
            {
                _animationMap.Add(anim.emotion, anim);
            }
            else
            {
                Debug.LogWarning($"[{name}] 重複的情緒動畫設定: {anim.emotion}", this);
            }
        }
    }

    /// <summary>
    /// 當「點擊」手勢 (Click) 成功匹配時，由基底類別呼叫。
    /// </summary>
    public override void OnTouched()
    {
        //如果有掛載 Player 且它正在播放動畫，則不允許點擊觸發
        if (_linkedPlayer != null && _linkedPlayer.IsPlaying)
        {
            Debug.Log($"[{name}] 因為 EmotionAnimationPlayer 正在播放，忽略點擊。");
            return;
        }

        //  如果正在等待滑鼠移開，則不觸發
        if (_awaitingMouseExit) return;

        // 如果動畫正在播放中，不允許再次觸發
        if (_isPlaying) return;

        // 1. 獲取當前情緒
        HeroineEmotionCardType currentEmotion = _heroine.CurrentEmotion;

        // 2. 尋找對應的動畫 (已包含 "找不到就用 Idle" 的邏輯)
        EmotionSpriteAnimation animToPlay = null;
        if (!_animationMap.TryGetValue(currentEmotion, out animToPlay))
        {
            if (!_animationMap.TryGetValue(fallbackEmotion, out animToPlay))
            {
                animToPlay = emotionAnimations != null ? emotionAnimations.Find(a => a != null && a.sprites != null && a.sprites.Count > 0) : null;
                if (animToPlay == null)
                {
                    Debug.LogWarning($"[{name}] 找不到情緒 {currentEmotion}，且沒有可用的備用動畫。", this);
                    return;
                }
            }
        }

        // 3. 檢查動畫是否有效
        if (animToPlay == null || animToPlay.sprites == null || animToPlay.sprites.Count == 0)
        {
            Debug.LogWarning($"[{name}] 情緒 {currentEmotion} 對應的動畫序列為空。", this);
            return;
        }

        // 4. 播放動畫 (使用協程)
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }

        _animationCoroutine = StartCoroutine(PlaySpriteAnimation(animToPlay));

        //  5. 觸發成功後，設定旗標，要求滑鼠必須移開
        _awaitingMouseExit = true;
    }

    /// <summary>
    /// 播放 Sprite 序列動畫的協程
    /// </summary>
    private IEnumerator PlaySpriteAnimation(EmotionSpriteAnimation anim)
    {
        _isPlaying = true;

        if (anim.playOnce)
        {
            // --- 單次播放 ---
            for (int i = 0; i < anim.sprites.Count; i++)
            {
                targetRenderer.sprite = anim.sprites[i];
                yield return new WaitForSeconds(anim.frameDuration);
            }
            if (!anim.stopAtLastFrame)
            {
                targetRenderer.sprite = anim.sprites[0];
            }
        }
        else
        {
            // --- 循環播放 (直到被中斷) ---
            int index = 0;
            while (true)
            {
                targetRenderer.sprite = anim.sprites[index];
                yield return new WaitForSeconds(anim.frameDuration);
                index = (index + 1) % anim.sprites.Count;
            }
        }

        if (anim.playOnce)
        {
            _isPlaying = false;
            _animationCoroutine = null;
        }
    }

    // ★ 6. 新增 IPointerExitHandler 的實作
    /// <summary>
    /// 當滑鼠移出此物件的 Collider 時 (由 Event System 呼叫)
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        // 如果我們正在等待滑鼠移開，現在它移開了，重設旗標
        // 這樣下次滑鼠再移入並點擊時，OnTouched() 就能再次觸發
        if (_awaitingMouseExit)
        {
            _awaitingMouseExit = false;
        }
    }
}