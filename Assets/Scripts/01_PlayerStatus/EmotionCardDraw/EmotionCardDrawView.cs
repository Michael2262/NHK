using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 情緒卡抽選表演 View。
/// 只負責 UI 表演，不決定抽選結果。
///
/// 建議用法：
/// - 為每種情緒做一個 EmotionCard prefab。
/// - 把 prefab 登錄到 emotionCardPrefabs。
/// - 抽卡演出時，本 View 會實例化對應 EmotionCard。
/// - 抽卡演出不顯示情緒代表字，只顯示卡圖。
/// </summary>
public class EmotionCardDrawView : MonoBehaviour
{
    [Serializable]
    public class EmotionCardPrefabEntry
    {
        public HeroineEmotionCardType Type;
        public EmotionCardView Prefab;
    }

    [Serializable]
    public class EmotionSpriteEntry
    {
        public HeroineEmotionCardType Type;
        public Sprite Sprite;
    }

    [Header("UI References")]
    [Tooltip("建議指定子物件 root。若未指定，不會停用本 GameObject，以避免 Coroutine 無法啟動。")]
    [SerializeField] private GameObject root;

    [Tooltip("EmotionCard prefab 生成位置。若未指定，會使用 root 或本物件 transform。")]
    [SerializeField] private Transform cardRoot;

    [Header("Emotion Card Prefabs")]
    [SerializeField] private List<EmotionCardPrefabEntry> emotionCardPrefabs = new List<EmotionCardPrefabEntry>();

    [Header("Fallback UI")]
    [Tooltip("沒有指定 EmotionCard prefab 時，才會使用這個舊圖示欄位。")]
    [SerializeField] private Image emotionImage;

    [Tooltip("舊圖示欄位用。新做法建議改用 EmotionCard prefab。")]
    [SerializeField] private List<EmotionSpriteEntry> emotionSprites = new List<EmotionSpriteEntry>();

    [Header("Options")]
    [SerializeField] private bool hideAfterComplete = true;
    [SerializeField, Min(0f)] private float completeHoldSeconds = 0.15f;

    private Coroutine currentRoutine;
    private EmotionCardView currentCard;
    private readonly Dictionary<HeroineEmotionCardType, EmotionCardView> cardPrefabMap = new Dictionary<HeroineEmotionCardType, EmotionCardView>();
    private readonly Dictionary<HeroineEmotionCardType, Sprite> spriteMap = new Dictionary<HeroineEmotionCardType, Sprite>();

    private void Awake()
    {
        RebuildMaps();
        if (cardRoot == null)
        {
            if (root != null) cardRoot = root.transform;
            if (cardRoot == null) cardRoot = transform;
        }
        SetVisible(false);
    }

    private void OnValidate()
    {
        RebuildMaps();
        if (cardRoot == null)
        {
            if (root != null) cardRoot = root.transform;
            if (cardRoot == null) cardRoot = transform;
        }
    }

    public void PlaySmallDrawShow(List<HeroineEmotionCardType> sequence, HeroineEmotionCardType finalResult, float duration, Action onComplete)
    {
        PlayDrawShow(sequence, finalResult, duration, onComplete);
    }

    public void PlayBigDrawShow(List<HeroineEmotionCardType> sequence, HeroineEmotionCardType finalResult, float duration, Action onComplete)
    {
        PlayDrawShow(sequence, finalResult, duration, onComplete);
    }

    public void PlayDrawShow(List<HeroineEmotionCardType> sequence, HeroineEmotionCardType finalResult, float duration, Action onComplete)
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        currentRoutine = StartCoroutine(PlayRoutine(sequence, finalResult, duration, onComplete));
    }

    private IEnumerator PlayRoutine(List<HeroineEmotionCardType> sequence, HeroineEmotionCardType finalResult, float duration, Action onComplete)
    {
        SetVisible(true);

        if (sequence == null || sequence.Count == 0)
        {
            sequence = new List<HeroineEmotionCardType> { finalResult };
        }

        duration = Mathf.Max(0f, duration);
        float interval = sequence.Count > 0 ? duration / sequence.Count : 0f;

        for (int i = 0; i < sequence.Count; i++)
        {
            ShowEmotion(sequence[i]);
            if (interval > 0f) yield return new WaitForSeconds(interval);
            else yield return null;
        }

        ShowEmotion(finalResult);
        if (completeHoldSeconds > 0f) yield return new WaitForSeconds(completeHoldSeconds);

        if (hideAfterComplete) SetVisible(false);

        currentRoutine = null;
        onComplete?.Invoke();
    }

    public void ShowEmotion(HeroineEmotionCardType type)
    {
        ClearCurrentCard();

        if (cardPrefabMap.TryGetValue(type, out var prefab) && prefab != null)
        {
            Transform parent = cardRoot != null ? cardRoot : transform;
            currentCard = Instantiate(prefab, parent);
            currentCard.Setup(type, false); // 抽卡演出不顯示情緒代表字。

            if (emotionImage != null) emotionImage.enabled = false;
            return;
        }

        // Fallback：沒有 EmotionCard prefab 時，沿用舊 Image 顯示。
        if (emotionImage != null)
        {
            if (spriteMap.TryGetValue(type, out var sprite) && sprite != null)
            {
                emotionImage.enabled = true;
                emotionImage.sprite = sprite;
            }
            else
            {
                emotionImage.enabled = false;
            }
        }
    }

    public void SetVisible(bool visible)
    {
        if (root != null)
        {
            root.SetActive(visible);
            if (!visible) ClearCurrentCard();
            return;
        }

        if (emotionImage != null) emotionImage.enabled = visible;
        if (!visible) ClearCurrentCard();
    }

    private void ClearCurrentCard()
    {
        if (currentCard != null)
        {
            Destroy(currentCard.gameObject);
            currentCard = null;
        }
    }

    private void RebuildMaps()
    {
        cardPrefabMap.Clear();
        if (emotionCardPrefabs != null)
        {
            foreach (var entry in emotionCardPrefabs)
            {
                if (entry == null) continue;
                cardPrefabMap[entry.Type] = entry.Prefab;
            }
        }

        spriteMap.Clear();
        if (emotionSprites != null)
        {
            foreach (var entry in emotionSprites)
            {
                if (entry == null) continue;
                spriteMap[entry.Type] = entry.Sprite;
            }
        }
    }
}
