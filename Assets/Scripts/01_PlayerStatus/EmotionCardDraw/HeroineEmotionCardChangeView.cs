using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// NHK 女主角情緒卡變化提示 View。
///
/// 用途：
/// - 常駐在場景或不卸載 UI 場景中。
/// - Inspector 指定 heroineID。
/// - 訂閱該 HeroineStatusModel 的 OnEmotionCardAdded / OnEmotionCardRemoved。
/// - 當情緒卡新增或移除時，顯示 EmotionCard + 「+1 / -1」。
/// - ReplaceEmotionCard 這種同一瞬間的增減，會合併成同一次提示，例如：
///   害羞 +1
///   生氣 -1
///
/// 注意：
/// - 本腳本只負責演出，不修改情緒卡資料。
/// - 若沒有設定 linePrefab / entriesRoot，會退回使用 textMessage 顯示純文字。
/// </summary>
public class HeroineEmotionCardChangeView : MonoBehaviour
{
    [Serializable]
    public class EmotionCardPrefabEntry
    {
        public HeroineEmotionCardType Type;
        public EmotionCardView Prefab;
    }

    private struct EmotionCardChangeData
    {
        public HeroineEmotionCardType Type;
        public int Delta;

        public EmotionCardChangeData(HeroineEmotionCardType type, int delta)
        {
            Type = type;
            Delta = delta;
        }
    }

    private class EmotionCardChangeBatch
    {
        public readonly List<EmotionCardChangeData> Changes = new List<EmotionCardChangeData>();
    }

    [Header("Target Heroine")]
    [Tooltip("要監聽哪位女主角的情緒卡變化。需對應 GameStatusService.Instance.Heroines 的 key。")]
    [SerializeField] private string heroineID = "sister";

    [Header("View References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform viewRoot;

    [Tooltip("純文字 fallback。若有設定 entriesRoot + linePrefab，這個可以不填。")]
    [SerializeField] private TextMeshProUGUI textMessage;

    [Header("Emotion Card UI")]
    [Tooltip("變化列的父物件。建議加 VerticalLayoutGroup。")]
    [SerializeField] private RectTransform entriesRoot;

    [Tooltip("一列變化 prefab：左邊放 EmotionCard，右邊顯示 +1 / -1。")]
    [SerializeField] private EmotionCardChangeLineView linePrefab;

    [Tooltip("每種情緒對應一個 EmotionCard prefab。")]
    [SerializeField] private List<EmotionCardPrefabEntry> emotionCardPrefabs = new List<EmotionCardPrefabEntry>();

    [Tooltip("增減提示要顯示 EmotionCard 上方的情緒代表字。")]
    [SerializeField] private bool showRepresentativeTextOnCard = true;

    [Header("Timing")]
    [Tooltip("同一瞬間連續收到的增減事件，會在這段時間內合併成同一次提示。")]
    [SerializeField, Min(0f)] private float combineWindowSeconds = 0.03f;
    [SerializeField, Min(0f)] private float holdSeconds = 2.0f;
    [SerializeField, Min(0f)] private float fadeSeconds = 0.35f;
    [SerializeField, Min(0f)] private float queueIntervalSeconds = 0.1f;

    [Header("Text Fallback Display")]
    [SerializeField] private string addedFormat = "{0} +1";
    [SerializeField] private string removedFormat = "{0} -1";
    [SerializeField] private bool hideWhenDisabled = true;

    [Header("Optional Position")]
    [Tooltip("啟動時是否套用 anchored position。")]
    [SerializeField] private bool applyInitialAnchoredPosition = false;
    [SerializeField] private Vector2 initialAnchoredPosition;

    private HeroineStatusModel currentModel;
    private readonly Dictionary<HeroineEmotionCardType, EmotionCardView> cardPrefabMap = new Dictionary<HeroineEmotionCardType, EmotionCardView>();
    private readonly List<EmotionCardChangeData> pendingChanges = new List<EmotionCardChangeData>();
    private readonly Queue<EmotionCardChangeBatch> batchQueue = new Queue<EmotionCardChangeBatch>();
    private readonly List<GameObject> spawnedLines = new List<GameObject>();

    private Coroutine pendingRoutine;
    private Coroutine playQueueRoutine;
    private bool serviceEventSubscribed;

    public string HeroineID => heroineID;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        viewRoot = GetComponent<RectTransform>();
        textMessage = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (viewRoot == null) viewRoot = GetComponent<RectTransform>();
        if (textMessage == null) textMessage = GetComponentInChildren<TextMeshProUGUI>(true);

        RebuildCardPrefabMap();

        if (applyInitialAnchoredPosition && viewRoot != null)
            viewRoot.anchoredPosition = initialAnchoredPosition;

        HideImmediate();
    }

    private void OnValidate()
    {
        RebuildCardPrefabMap();
    }

    private void OnEnable()
    {
        SubscribeServiceEvent();
        RefreshSubscription();
    }

    private void Start()
    {
        // 如果本 View 比 GameStatusService 更早啟用，Start 時再嘗試一次。
        SubscribeServiceEvent();
        RefreshSubscription();
    }

    private void OnDisable()
    {
        UnsubscribeHeroine();
        UnsubscribeServiceEvent();

        if (pendingRoutine != null)
        {
            StopCoroutine(pendingRoutine);
            pendingRoutine = null;
        }

        if (playQueueRoutine != null)
        {
            StopCoroutine(playQueueRoutine);
            playQueueRoutine = null;
        }

        pendingChanges.Clear();
        batchQueue.Clear();

        if (hideWhenDisabled) HideImmediate();
    }

    private void SubscribeServiceEvent()
    {
        if (serviceEventSubscribed) return;
        var service = GameStatusService.Instance;
        if (service == null) return;

        service.OnGameStatusLoaded += HandleGameStatusLoaded;
        serviceEventSubscribed = true;
    }

    private void UnsubscribeServiceEvent()
    {
        if (!serviceEventSubscribed) return;
        var service = GameStatusService.Instance;
        if (service != null)
            service.OnGameStatusLoaded -= HandleGameStatusLoaded;

        serviceEventSubscribed = false;
    }

    private void HandleGameStatusLoaded()
    {
        RefreshSubscription();
    }

    /// <summary>
    /// 修改目前監聽的女主角 ID，並重新訂閱。
    /// 可由 PlayMaker / UnityEvent / 其他 UI 呼叫。
    /// </summary>
    public void SetHeroineID(string newHeroineID)
    {
        if (string.IsNullOrWhiteSpace(newHeroineID))
        {
            Debug.LogWarning("[HeroineEmotionCardChangeView] SetHeroineID received empty heroineID.", this);
            return;
        }

        if (heroineID == newHeroineID) return;

        heroineID = newHeroineID;
        RefreshSubscription();
    }

    /// <summary>
    /// 重新尋找 GameStatusService 中對應 heroineID 的 HeroineStatusModel 並訂閱。
    /// </summary>
    public void RefreshSubscription()
    {
        UnsubscribeHeroine();

        var service = GameStatusService.Instance;
        if (service == null || service.Heroines == null)
        {
            Debug.LogWarning("[HeroineEmotionCardChangeView] GameStatusService or Heroines is not ready.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(heroineID))
        {
            Debug.LogWarning("[HeroineEmotionCardChangeView] heroineID is empty.", this);
            return;
        }

        if (!service.Heroines.TryGetValue(heroineID, out currentModel) || currentModel == null)
        {
            Debug.LogWarning($"[HeroineEmotionCardChangeView] HeroineID not found: {heroineID}", this);
            currentModel = null;
            return;
        }

        currentModel.OnEmotionCardAdded += HandleEmotionCardAdded;
        currentModel.OnEmotionCardRemoved += HandleEmotionCardRemoved;
    }

    private void UnsubscribeHeroine()
    {
        if (currentModel == null) return;

        currentModel.OnEmotionCardAdded -= HandleEmotionCardAdded;
        currentModel.OnEmotionCardRemoved -= HandleEmotionCardRemoved;
        currentModel = null;
    }

    private void HandleEmotionCardAdded(HeroineEmotionCardType type)
    {
        AddPendingChange(type, 1);
    }

    private void HandleEmotionCardRemoved(HeroineEmotionCardType type)
    {
        AddPendingChange(type, -1);
    }

    private void AddPendingChange(HeroineEmotionCardType type, int delta)
    {
        pendingChanges.Add(new EmotionCardChangeData(type, delta));

        if (pendingRoutine == null)
            pendingRoutine = StartCoroutine(FlushPendingRoutine());
    }

    private IEnumerator FlushPendingRoutine()
    {
        if (combineWindowSeconds > 0f)
            yield return new WaitForSeconds(combineWindowSeconds);
        else
            yield return null;

        if (pendingChanges.Count > 0)
        {
            var batch = new EmotionCardChangeBatch();
            batch.Changes.AddRange(pendingChanges);
            pendingChanges.Clear();
            batchQueue.Enqueue(batch);
        }

        pendingRoutine = null;

        if (playQueueRoutine == null && batchQueue.Count > 0)
            playQueueRoutine = StartCoroutine(PlayQueueRoutine());
    }

    private IEnumerator PlayQueueRoutine()
    {
        while (batchQueue.Count > 0)
        {
            EmotionCardChangeBatch batch = batchQueue.Dequeue();
            yield return PlayBatchRoutine(batch);

            if (queueIntervalSeconds > 0f)
                yield return new WaitForSeconds(queueIntervalSeconds);
        }

        playQueueRoutine = null;
    }

    private IEnumerator PlayBatchRoutine(EmotionCardChangeBatch batch)
    {
        ShowBatch(batch);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        yield return new WaitForSeconds(Mathf.Max(0f, holdSeconds));

        if (canvasGroup != null && fadeSeconds > 0f)
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeSeconds);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }
        }

        HideImmediate();
    }

    private void ShowBatch(EmotionCardChangeBatch batch)
    {
        ClearSpawnedLines();

        if (batch == null || batch.Changes.Count == 0)
        {
            if (textMessage != null) textMessage.text = string.Empty;
            return;
        }

        if (linePrefab != null && entriesRoot != null)
        {
            for (int i = 0; i < batch.Changes.Count; i++)
            {
                EmotionCardChangeData change = batch.Changes[i];
                var line = Instantiate(linePrefab, entriesRoot);
                spawnedLines.Add(line.gameObject);

                cardPrefabMap.TryGetValue(change.Type, out var cardPrefab);
                line.Setup(cardPrefab, change.Type, change.Delta, showRepresentativeTextOnCard);
            }

            if (textMessage != null) textMessage.text = string.Empty;
        }
        else if (textMessage != null)
        {
            textMessage.text = BuildFallbackText(batch);
        }
    }

    private string BuildFallbackText(EmotionCardChangeBatch batch)
    {
        if (batch == null || batch.Changes.Count == 0) return string.Empty;

        List<string> lines = new List<string>();
        foreach (var change in batch.Changes)
        {
            string label = GetEmotionLabel(change.Type);
            string format = change.Delta >= 0 ? addedFormat : removedFormat;
            lines.Add(string.Format(format, label));
        }

        return string.Join("\n", lines);
    }

    public void SetAnchoredPosition(float x, float y)
    {
        if (viewRoot == null) viewRoot = GetComponent<RectTransform>();
        if (viewRoot != null) viewRoot.anchoredPosition = new Vector2(x, y);
    }

    public void HideImmediate()
    {
        ClearSpawnedLines();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (textMessage != null)
            textMessage.text = string.Empty;
    }

    private void ClearSpawnedLines()
    {
        for (int i = 0; i < spawnedLines.Count; i++)
        {
            if (spawnedLines[i] != null)
                Destroy(spawnedLines[i]);
        }

        spawnedLines.Clear();
    }

    private void RebuildCardPrefabMap()
    {
        cardPrefabMap.Clear();
        if (emotionCardPrefabs == null) return;

        foreach (var entry in emotionCardPrefabs)
        {
            if (entry == null) continue;
            cardPrefabMap[entry.Type] = entry.Prefab;
        }
    }

    private string GetEmotionLabel(HeroineEmotionCardType type)
    {
        return EmotionCardView.GetDefaultDisplayName(type);
    }
}
