using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 情緒卡抽選表演 View。
/// 只負責 UI 表演,不決定抽選結果。
///
/// 設計:
/// - 從 EmotionCardCatalog 取得每種情緒對應的 EmotionCard prefab。
/// - Awake 階段預先 Instantiate 每張 prefab,並重設 Transform 對齊到 cardRoot。
/// - 中抽選 / 大抽選：新卡以較大尺寸出現,縮小到正常尺寸後取代舊卡。
/// - 小抽選：不翻牌,直接顯示最終結果。
/// </summary>
public class EmotionCardDrawView : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("建議指定子物件 root。若未指定,不會停用本 GameObject,以避免 Coroutine 無法啟動。")]
    [SerializeField] private GameObject root;

    [Tooltip("EmotionCard 生成位置。所有卡片都會置中於此 (anchor/pivot 0.5,anchoredPosition 0)。")]
    [SerializeField] private Transform cardRoot;

    [Header("Emotion Card Catalog")]
    [Tooltip("情緒卡 prefab 對照表。所有用到 EmotionCard 的 View 共用同一份。")]
    [SerializeField] private EmotionCardCatalog catalog;

    [Header("Options")]
    [SerializeField] private bool hideAfterComplete = true;
    [SerializeField, Min(0f)] private float completeHoldSeconds = 0.25f;

    [Header("Hit Replacement Animation")]
    [Tooltip("中抽選/大抽選時,新卡出現的起始倍率。")]
    [SerializeField, Min(1f)] private float incomingStartScale = 1.35f;

    [Tooltip("每張卡切換間隔中,有多少比例用來做縮小打下去動畫。")]
    [SerializeField, Range(0.1f, 1f)] private float hitAnimationRatio = 0.75f;

    [Tooltip("每次打下去動畫的最短秒數。")]
    [SerializeField, Min(0f)] private float minHitAnimationSeconds = 0.04f;

    private Coroutine currentRoutine;
    private EmotionCard currentCard;

    // 預先 Instantiate 好的卡片實例,共用,不再每張 Destroy/Instantiate。
    private readonly Dictionary<HeroineEmotionCardType, EmotionCard> cardInstances = new Dictionary<HeroineEmotionCardType, EmotionCard>();

    private void Awake()
    {
        if (cardRoot == null) cardRoot = transform;
        BuildCardInstances();
        SetVisible(false);
    }

    public void PlaySmallDrawShow(List<HeroineEmotionCardType> sequence, HeroineEmotionCardType finalResult, float duration, Action onComplete)
    {
        // 小抽選不做翻牌/打牌表演,直接顯示目前主導情緒。
        PlayInstantResultShow(finalResult, duration, onComplete);
    }

    public void PlayMediumDrawShow(List<HeroineEmotionCardType> sequence, HeroineEmotionCardType finalResult, float duration, Action onComplete)
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

    private void PlayInstantResultShow(HeroineEmotionCardType finalResult, float duration, Action onComplete)
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        currentRoutine = StartCoroutine(InstantResultRoutine(finalResult, duration, onComplete));
    }

    private IEnumerator InstantResultRoutine(HeroineEmotionCardType finalResult, float duration, Action onComplete)
    {
        SetVisible(true);
        ShowEmotion(finalResult);

        float holdSeconds = Mathf.Max(duration, completeHoldSeconds);
        if (holdSeconds > 0f) yield return new WaitForSeconds(holdSeconds);
        else yield return null;

        if (hideAfterComplete) SetVisible(false);

        currentRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator PlayRoutine(List<HeroineEmotionCardType> sequence, HeroineEmotionCardType finalResult, float duration, Action onComplete)
    {
        SetVisible(true);

        if (sequence == null || sequence.Count == 0)
        {
            sequence = new List<HeroineEmotionCardType> { finalResult };
        }
        else if (sequence[sequence.Count - 1] != finalResult)
        {
            // 保險：避免結果卡沒有出現在最後。
            sequence = new List<HeroineEmotionCardType>(sequence) { finalResult };
        }

        duration = Mathf.Max(0f, duration);
        float interval = sequence.Count > 0 ? duration / sequence.Count : 0f;
        float hitSeconds = interval > 0f
            ? Mathf.Min(interval, Mathf.Max(minHitAnimationSeconds, interval * hitAnimationRatio))
            : 0f;

        for (int i = 0; i < sequence.Count; i++)
        {
            if (hitSeconds > 0f)
            {
                yield return HitReplaceEmotionRoutine(sequence[i], hitSeconds);
                float restSeconds = interval - hitSeconds;
                if (restSeconds > 0f) yield return new WaitForSeconds(restSeconds);
            }
            else
            {
                ShowEmotion(sequence[i]);
                yield return null;
            }
        }

        if (completeHoldSeconds > 0f) yield return new WaitForSeconds(completeHoldSeconds);

        if (hideAfterComplete) SetVisible(false);

        currentRoutine = null;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 直接切成指定卡片,不做打下去動畫。
    /// 小抽選與外部強制顯示使用。
    /// </summary>
    public void ShowEmotion(HeroineEmotionCardType type)
    {
        HideAllCards();

        if (!cardInstances.TryGetValue(type, out var instance) || instance == null)
        {
            Debug.LogWarning($"[EmotionCardDrawView] EmotionCard instance not found: {type}", this);
            return;
        }

        instance.gameObject.SetActive(true);
        instance.Setup(false);
        ResetTransform(instance.transform);
        currentCard = instance;
    }

    private IEnumerator HitReplaceEmotionRoutine(HeroineEmotionCardType type, float seconds)
    {
        if (!cardInstances.TryGetValue(type, out var incomingCard) || incomingCard == null)
        {
            Debug.LogWarning($"[EmotionCardDrawView] EmotionCard instance not found: {type}", this);
            yield break;
        }

        EmotionCard previousCard = currentCard;

        incomingCard.gameObject.SetActive(true);
        incomingCard.Setup(false);
        ResetTransform(incomingCard.transform);
        incomingCard.transform.localScale = Vector3.one * incomingStartScale;
        incomingCard.transform.SetAsLastSibling();

        // 如果新舊是同一張實例,就不可能同時顯示兩張。
        // 這種情況只重播縮放感,避免把自己關掉。
        bool sameCard = previousCard == incomingCard;

        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            float t = seconds > 0f ? Mathf.Clamp01(elapsed / seconds) : 1f;
            incomingCard.transform.localScale = Vector3.Lerp(Vector3.one * incomingStartScale, Vector3.one, t);
            yield return null;
        }

        incomingCard.transform.localScale = Vector3.one;

        if (!sameCard && previousCard != null)
        {
            previousCard.gameObject.SetActive(false);
        }

        currentCard = incomingCard;
    }

    public void SetVisible(bool visible)
    {
        if (root != null)
        {
            root.SetActive(visible);
        }

        if (!visible)
        {
            HideAllCards();
        }
    }

    private void HideAllCards()
    {
        foreach (var pair in cardInstances)
        {
            if (pair.Value == null) continue;
            pair.Value.gameObject.SetActive(false);
            ResetTransform(pair.Value.transform);
        }

        currentCard = null;
    }

    private void BuildCardInstances()
    {
        cardInstances.Clear();

        if (catalog == null)
        {
            Debug.LogWarning("[EmotionCardDrawView] EmotionCardCatalog is not assigned.", this);
            return;
        }

        foreach (var entry in catalog.Entries)
        {
            if (entry == null || entry.Prefab == null) continue;
            if (cardInstances.ContainsKey(entry.Type))
            {
                Debug.LogWarning($"[EmotionCardDrawView] Duplicate emotion type in catalog: {entry.Type}", this);
                continue;
            }

            var instance = Instantiate(entry.Prefab, cardRoot);
            ResetTransform(instance.transform);
            instance.gameObject.SetActive(false);
            cardInstances[entry.Type] = instance;
        }
    }

    private static void ResetTransform(Transform t)
    {
        if (t is RectTransform rt)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
        else
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }
    }
}
