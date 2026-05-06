using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 【Prefab 腳本】
/// 職責：作為一個「情緒動畫顯示器」。
/// (★ 已修改：允許在 Inspector 中手動指定 targetRenderer)
/// </summary>
// (★ 移除 [RequireComponent]，因為 Renderer 可能在子物件上)
// [RequireComponent(typeof(SpriteRenderer))] 
public class EmotionAnimationPlayer : MonoBehaviour
{
    [Header("目標設定")]
    [Tooltip("要播放動畫的 SpriteRenderer。如果為空，會自動嘗試 GetComponent<SpriteRenderer>()")]
    // ★ 1. 將 _targetRenderer 改為 public
    public SpriteRenderer targetRenderer;

    [Tooltip("要查詢情緒的女主角 ID (來自 HeroineStat 的 ID)")]
    public string heroineId;

    [Header("動畫列表")]
    [Tooltip("根據情緒對應的動畫序列。請至少設定一個 Idle 狀態作為預設。")]
    public List<EmotionSpriteAnimation> emotionAnimations;

    [Tooltip("讓外部知道目前是否正在播放動畫")]
    public bool IsPlaying => _animationCoroutine != null;

    // --- 內部狀態 ---
    private GameStatusService _service;
    private HeroineStatusModel _heroine;
    private Coroutine _animationCoroutine; // 用於停止當前動畫



    // 用於查找的字典，效能比 List 遍歷更好
    private Dictionary<HeroineEmotionCardType, EmotionSpriteAnimation> _animationMap;

    [Header("Fallback")]
    [Tooltip("找不到 CurrentEmotion 對應動畫時，優先使用的備用情緒。若仍找不到，使用列表第一個有效動畫。")]
    public HeroineEmotionCardType fallbackEmotion = HeroineEmotionCardType.Angry;

    void Awake()
    {
        // ★ 2. 修改 Awake 邏輯
        // 如果使用者沒有在 Inspector 中拖曳，
        // 則嘗試自動抓取「同物件上」的 Renderer
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }

        // ★ 3. 檢查 Renderer 是否真的存在
        if (targetRenderer == null)
        {
            Debug.LogError($"[{name}] EmotionAnimationPlayer: 'Target Renderer' 欄位為空，且在 {name} 物件上找不到 SpriteRenderer。動畫將無法播放。", this);
            this.enabled = false; // 禁用此腳本
            return;
        }

        // 4. 將 List 轉換為 Dictionary 以便快速查找
        _animationMap = new Dictionary<HeroineEmotionCardType, EmotionSpriteAnimation>();
        foreach (var anim in emotionAnimations)
        {
            if (!_animationMap.ContainsKey(anim.emotion))
            {
                _animationMap.Add(anim.emotion, anim);
            }
        }
    }

    
    void OnEnable()
    {
        // 1. 確保服務與 Model 存在 (這部分邏輯不變)
        _service = GameStatusService.Instance;
        if (_service == null || string.IsNullOrEmpty(heroineId)) return;

        if (_service.Heroines.TryGetValue(heroineId, out _heroine))
        {
            // 2. 訂閱事件
            _heroine.OnEmotionChanged += HandleEmotionChanged;

            // 3. 【關鍵】每次物件被啟用時，立即根據當前情緒重新啟動動畫
            // 這解決了物件被 SetActive(true) 後協程沒在跑的問題
            HandleEmotionChanged(_heroine.CurrentEmotion);
        }
    }

    void OnDisable()
    {
        // 4. 物件隱藏時取消訂閱，避免記憶體洩漏與報錯
        if (_heroine != null)
        {
            _heroine.OnEmotionChanged -= HandleEmotionChanged;
        }

        // 這裡不需要手動 StopCoroutine，因為 SetActive(false) 會自動停止它
        _animationCoroutine = null;
    }

    /// <summary>
    /// (事件處理器)
    /// 當 HeroineStatusModel 的 CurrentEmotion 改變時，此方法會被觸發。
    /// </summary>
    private void HandleEmotionChanged(HeroineEmotionCardType newEmotion)
    {
        // 1. 尋找對應的動畫
        EmotionSpriteAnimation animToPlay = null;
        if (!_animationMap.TryGetValue(newEmotion, out animToPlay))
        {
            if (!_animationMap.TryGetValue(fallbackEmotion, out animToPlay))
            {
                animToPlay = emotionAnimations != null ? emotionAnimations.Find(a => a != null && a.sprites != null && a.sprites.Count > 0) : null;
                if (animToPlay == null)
                {
                    Debug.LogWarning($"[{name}] 找不到情緒 {newEmotion}，且沒有可用的備用動畫。", this);
                    return;
                }
            }
        }

        // 2. 檢查動畫是否有效
        if (animToPlay == null || animToPlay.sprites == null || animToPlay.sprites.Count == 0)
        {
            Debug.LogWarning($"[{name}] 情緒 {newEmotion} 對應的動畫序列為空。", this);
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }
            return;
        }

        // 3. 播放動畫 (使用協程)
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }

        _animationCoroutine = StartCoroutine(PlaySpriteAnimation(animToPlay));
    }

    /// <summary>
   // * 播放 Sprite 序列動畫的協程
    /// </summary>
    private IEnumerator PlaySpriteAnimation(EmotionSpriteAnimation anim)
    {
        if (anim.playOnce)
        {
            // --- 單次播放 ---
            for (int i = 0; i < anim.sprites.Count; i++)
            {
                // ★ 檢查點：確保 targetRenderer 在這裡不是 null
                if (targetRenderer == null) yield break;
                targetRenderer.sprite = anim.sprites[i];
                yield return new WaitForSeconds(anim.frameDuration);
            }

            if (!anim.stopAtLastFrame && targetRenderer != null)
            {
                targetRenderer.sprite = anim.sprites[0];
            }
            _animationCoroutine = null;
        }
        else
        {
            // --- 循環播放 (直到被中斷) ---
            int index = 0;
            while (true)
            {
                if (anim.sprites == null || anim.sprites.Count == 0 || targetRenderer == null) yield break;
                targetRenderer.sprite = anim.sprites[index];
                yield return new WaitForSeconds(anim.frameDuration);

                index = (index + 1) % anim.sprites.Count;
            }
        }
    }

    /// <summary>
    /// (清理)
    /// 當物件被銷毀時，取消訂閱事件，避免記憶體洩漏。
    /// </summary>
    void OnDestroy()
    {
        if (_heroine != null)
        {
            _heroine.OnEmotionChanged -= HandleEmotionChanged;
        }
    }
}