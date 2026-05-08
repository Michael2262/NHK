using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// NHK 女主角情緒卡變化提示 View（重構版：純文字）。
///
/// 用途:
/// - 當情緒卡新增或移除時，顯示文字提示，例如「妹妹 有點 擔心」。
/// - 使用 TextTable Key：Emotion.Change（內容「{0} 有點 {1}」）。
///   {0} = 角色名（從 NameTextKey 查表）
///   {1} = 情緒名稱（從 EmotionCardCatalog.GetEmotionNameTextKey 查表）
///
/// 注意:
/// - 不再使用 EmotionCard prefab 或 EmotionCardChangeLineView。
/// - 只顯示新增的情緒（+1），移除不單獨提示。
///   若需要顯示移除，可自行擴充。
/// - ReplaceEmotionCard 的同時增減會合併：只顯示新增的那一筆。
/// </summary>
public class HeroineEmotionCardChangeView : MonoBehaviour
{
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
    [Tooltip("要監聽哪位女主角的情緒卡變化。")]
    [SerializeField] private string heroineID = "sister";

    [Header("View References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform viewRoot;

    [Tooltip("顯示「{角色名} 有點 {情緒}」的文字元件。")]
    [SerializeField] private TextMeshProUGUI textMessage;

    [Header("Emotion Card Catalog")]
    [Tooltip("情緒卡對照表。讀取情緒名稱 TextTable Key。")]
    [SerializeField] private EmotionCardCatalog catalog;

    [Header("Text Table Settings")]
    [Tooltip("情緒新增文字的 TextTable Key。內容例如「{0} 有點 {1}」。")]
    [SerializeField] private string changeTextKey = "Emotion.Change";

    [Tooltip("情緒移除文字的 TextTable Key。內容例如「{0} 不再 {1}」。")]
    [SerializeField] private string removeTextKey = "Emotion.Remove";

    [Header("Timing")]
    [Tooltip("同一瞬間連續收到的增減事件,會在這段時間內合併成同一次提示。")]
    [SerializeField, Min(0f)] private float combineWindowSeconds = 0.03f;
    [SerializeField, Min(0f)] private float holdSeconds = 2.0f;
    [SerializeField, Min(0f)] private float fadeSeconds = 0.35f;
    [SerializeField, Min(0f)] private float queueIntervalSeconds = 0.1f;

    [Header("Options")]
    [SerializeField] private bool hideWhenDisabled = true;

    [Header("Optional Position")]
    [SerializeField] private bool applyInitialAnchoredPosition = false;
    [SerializeField] private Vector2 initialAnchoredPosition;

    private HeroineStatusModel currentModel;
    private readonly List<EmotionCardChangeData> pendingChanges = new List<EmotionCardChangeData>();
    private readonly Queue<EmotionCardChangeBatch> batchQueue = new Queue<EmotionCardChangeBatch>();

    private Coroutine pendingRoutine;
    private Coroutine playQueueRoutine;
    private bool serviceEventSubscribed;

    public string HeroineID => heroineID;

    /// <summary>
    /// 是否仍有情緒卡變化提示正在合併、排隊或播放。
    /// </summary>
    public bool IsBusy
    {
        get
        {
            if (pendingRoutine != null) return true;
            if (playQueueRoutine != null) return true;
            if (pendingChanges.Count > 0) return true;
            if (batchQueue.Count > 0) return true;
            if (canvasGroup != null && canvasGroup.alpha > 0.001f) return true;
            return false;
        }
    }

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

        if (applyInitialAnchoredPosition && viewRoot != null)
            viewRoot.anchoredPosition = initialAnchoredPosition;

        HideImmediate();
    }

    private void OnEnable()
    {
        SubscribeServiceEvent();
        RefreshSubscription();
    }

    private void Start()
    {
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
        if (textMessage == null) return;

        if (batch == null || batch.Changes.Count == 0)
        {
            textMessage.text = string.Empty;
            return;
        }

        List<string> lines = new List<string>();
        string heroineName = GetHeroineName();

        foreach (var change in batch.Changes)
        {
            if (change.Delta == 0) continue;

            string emotionName = GetEmotionName(change.Type);

            if (change.Delta > 0)
            {
                // 新增：「{角色名} 有點 {情緒}」
                string template = DialogueManager.GetLocalizedText(changeTextKey);
                if (string.IsNullOrEmpty(template))
                    template = "{0} 有點 {1}";
                lines.Add(string.Format(template, heroineName, emotionName));
            }
            else
            {
                // 移除：「{角色名} 不再 {情緒}」
                string template = DialogueManager.GetLocalizedText(removeTextKey);
                if (string.IsNullOrEmpty(template))
                    template = "{0} 不再 {1}";
                lines.Add(string.Format(template, heroineName, emotionName));
            }
        }

        textMessage.text = string.Join("\n", lines);
    }

    private string GetHeroineName()
    {
        if (currentModel == null) return heroineID;

        if (!string.IsNullOrEmpty(currentModel.NameTextKey))
        {
            string localizedName = DialogueManager.GetLocalizedText(currentModel.NameTextKey);
            if (!string.IsNullOrEmpty(localizedName))
                return localizedName;
        }

        return currentModel.Name;
    }

    private string GetEmotionName(HeroineEmotionCardType type)
    {
        if (catalog != null)
        {
            string textKey = catalog.GetEmotionNameTextKey(type);
            if (!string.IsNullOrEmpty(textKey))
            {
                string localized = DialogueManager.GetLocalizedText(textKey);
                if (!string.IsNullOrEmpty(localized))
                    return localized;
            }
        }

        // fallback：直接用 enum 名稱
        return type.ToString();
    }

    public void SetAnchoredPosition(float x, float y)
    {
        if (viewRoot == null) viewRoot = GetComponent<RectTransform>();
        if (viewRoot != null) viewRoot.anchoredPosition = new Vector2(x, y);
    }

    public void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (textMessage != null)
            textMessage.text = string.Empty;
    }
}
