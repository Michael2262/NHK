using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>依 Actor ID 播放 UI 插圖。跑條滿格後保留 0.8 秒，瞬間歸零再通知完成。</summary>
[DisallowMultipleComponent]
public sealed class InsertCGActor : MonoBehaviour
{
    [Serializable]
    private sealed class ImageEntry
    {
        public string imageID;
        public Sprite sprite;
        public UnityEvent onComplete = new UnityEvent();
    }

    private const double HoldSeconds = 0.8;
    private static readonly Dictionary<string, InsertCGActor> actors =
        new Dictionary<string, InsertCGActor>(StringComparer.OrdinalIgnoreCase);

    [SerializeField] private string actorID;
    [Tooltip("留空時使用同一物件上的 Image。")]
    [SerializeField] private Image targetImage;
    [SerializeField] private Slider slider;
    [Tooltip("選填。指定包含插圖和跑條的 CanvasGroup，清圖時一起隱藏。")]
    [SerializeField] private CanvasGroup canvasGroup;
    [Min(0f)] [SerializeField] private float defaultDuration = 3f;
    [SerializeField] private List<ImageEntry> images = new List<ImageEntry>();

    private readonly Dictionary<string, ImageEntry> entries =
        new Dictionary<string, ImageEntry>(StringComparer.OrdinalIgnoreCase);
    private InsertCGRefreshSettings refreshSettings;
    private ImageEntry playing;
    private Action<bool> onFinished;
    private double startTime;
    private double duration;
    private double lastRefreshTime;
    private double? fullSince;
    private bool groupVisible;
    private bool groupInteractable;
    private bool groupBlocksRaycasts;

    public static InsertCGActor Find(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        actors.TryGetValue(id.Trim(), out var actor);
        return actor;
    }

    private void Awake()
    {
        if (targetImage == null) targetImage = GetComponent<Image>();
        if (targetImage != null && targetImage.sprite == null) targetImage.enabled = false;
        if (canvasGroup != null)
        {
            groupInteractable = canvasGroup.interactable;
            groupBlocksRaycasts = canvasGroup.blocksRaycasts;
        }
        refreshSettings = Resources.Load<InsertCGRefreshSettings>(nameof(InsertCGRefreshSettings));
        foreach (var entry in images)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.imageID)) continue;
            if (!entries.TryAdd(entry.imageID.Trim(), entry))
                Debug.LogWarning($"[InsertCGActor] {actorID} 有重複的圖片 ID「{entry.imageID}」，使用第一筆。", this);
        }
    }

    private void OnEnable()
    {
        SetGroupVisible(targetImage != null && targetImage.sprite != null);
        SetProgress(0f);
        if (string.IsNullOrWhiteSpace(actorID))
        {
            Debug.LogWarning("[InsertCGActor] 請設定 Actor ID 供對話指令查找。", this);
            return;
        }
        actorID = actorID.Trim();
        if (actors.ContainsKey(actorID))
            Debug.LogWarning($"[InsertCGActor] Actor ID「{actorID}」重複，使用後啟用的物件。", this);
        actors[actorID] = this;
    }

    private void OnDisable()
    {
        if (Find(actorID) == this) actors.Remove(actorID);
        FinishPlayback(false);
    }

    public void Play(string imageID) => TryPlay(imageID, null, null);
    public void Play(string imageID, float seconds) => TryPlay(imageID, seconds, null);

    internal bool TryPlay(string imageID, float? seconds, Action<bool> finished)
    {
        if (!isActiveAndEnabled || targetImage == null)
        {
            Debug.LogWarning($"[InsertCGActor] {actorID} 尚未啟用或未指定 Image。", this);
            return false;
        }

        imageID = imageID?.Trim();
        if (string.Equals(imageID, "None", StringComparison.OrdinalIgnoreCase))
        {
            SetImage(null);
            FinishPlayback(false);
            finished?.Invoke(true);
            return true;
        }

        float secondsToFill = seconds ?? defaultDuration;
        if (slider == null || float.IsNaN(secondsToFill) || float.IsInfinity(secondsToFill) || secondsToFill < 0f)
        {
            Debug.LogWarning($"[InsertCGActor] {actorID} 需要 Slider 與有效的非負跑條秒數。", this);
            return false;
        }
        if (string.IsNullOrEmpty(imageID) || !entries.TryGetValue(imageID, out var entry) || entry.sprite == null)
        {
            Debug.LogWarning($"[InsertCGActor] {actorID} 找不到可用的圖片「{imageID}」。", this);
            return false;
        }

        var previousFinished = onFinished;
        playing = entry;
        onFinished = finished;
        duration = secondsToFill;
        startTime = lastRefreshTime = Time.unscaledTimeAsDouble;
        fullSince = secondsToFill == 0f ? startTime : (double?)null;
        SetImage(entry.sprite);
        SetProgress(fullSince.HasValue ? 1f : 0f);
        previousFinished?.Invoke(false);
        return true;
    }

    private void Update()
    {
        // 也同步其他腳本直接變更 Source Image 的情形。
        if (canvasGroup != null)
        {
            bool hasSprite = targetImage != null && targetImage.sprite != null;
            if (groupVisible != hasSprite) SetGroupVisible(hasSprite);
        }
        if (playing == null) return;
        double now = Time.unscaledTimeAsDouble;
        if (fullSince.HasValue)
        {
            if (now - fullSince.Value >= HoldSeconds) FinishPlayback(true);
            return;
        }

        double elapsed = now - startTime;
        if (elapsed >= duration)
        {
            SetProgress(1f);
            fullSince = now;
            return;
        }

        float interval = refreshSettings != null ? refreshSettings.refreshTime : InsertCGRefreshSettings.DefaultRefreshTime;
        bool stepped = refreshSettings == null || refreshSettings.nonRealtimeRefresh;
        if (stepped && now - lastRefreshTime < interval) return;
        lastRefreshTime = now;
        SetProgress((float)(elapsed / duration));
    }

    private void FinishPlayback(bool completed)
    {
        var entry = playing;
        var finished = onFinished;
        // 先歸零並釋放本輪狀態，讓事件可立即播放下一張或停用物件。
        playing = null;
        onFinished = null;
        SetProgress(0f);
        try
        {
            if (completed)
            {
                SetImage(null);
                entry?.onComplete?.Invoke();
            }
        }
        finally
        {
            finished?.Invoke(completed);
        }
    }

    private void SetImage(Sprite sprite)
    {
        if (targetImage != null)
        {
            targetImage.sprite = sprite;
            targetImage.enabled = sprite != null;
        }
        SetGroupVisible(sprite != null);
    }

    private void SetGroupVisible(bool visible)
    {
        if (canvasGroup == null) return;
        groupVisible = visible;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible && groupInteractable;
        canvasGroup.blocksRaycasts = visible && groupBlocksRaycasts;
    }

    private void SetProgress(float progress)
    {
        if (slider != null)
            slider.SetValueWithoutNotify(Mathf.Lerp(slider.minValue, slider.maxValue, progress));
    }
}
