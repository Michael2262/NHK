using System;
using System.Collections.Generic;
using PixelCrushers;
using UnityEngine;

/// <summary>生成任務追蹤項目；同日保留已顯示名單，換日、讀檔或手動刷新才重新挑選。</summary>
[DisallowMultipleComponent]
[AddComponentMenu("NHK/Quest/Quest Tracker UI")]
public sealed class QuestTrackerUI : MonoBehaviour
{
    [Header("生成設定")]
    [SerializeField] private QuestTrackerItemUI itemPrefab;
    [Tooltip("建議使用專用的空容器並掛 Vertical Layout Group。只管理本元件生成的項目。")]
    [SerializeField] private Transform itemContainer;
    [Min(0)] [SerializeField] private int maxVisibleTasks = 3;

    [Header("任務狀態文字（Text Table Key）")]
    [Tooltip("全部目標次數達成時顯示，優先於今日已完成。")]
    [SerializeField] private string completedTextKey;
    [Tooltip("每日限一次的任務，今天已成功計次但尚未全部完成時顯示。")]
    [SerializeField] private string completedTodayTextKey;
    [Tooltip("每日限一次的任務，今天尚未成功計次且尚未全部完成時顯示。")]
    [SerializeField] private string incompleteTodayTextKey;
    [Tooltip("不限制每日次數的任務，尚未全部完成時顯示。")]
    [SerializeField] private string incompleteTextKey;

    private readonly Dictionary<string, QuestTrackerItemUI> _items = new Dictionary<string, QuestTrackerItemUI>();
    private GameStatusService _service;
    private QuestModel _quests;
    private TimeSystemModel _time;
    private QuestTrackerModel _tracker;
    private bool _refreshRequested = true;
    private bool _renderRequested = true;
    private bool _reportedMissingSetup;

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        UILocalizationManager.languageChanged += HandleLanguageChanged;
        EnsureBinding();
        _renderRequested = true;
        ApplyPendingUpdates();
    }

    private void OnDisable()
    {
        UILocalizationManager.languageChanged -= HandleLanguageChanged;
        // 保留進度／換日／讀檔訂閱：面板隱藏時也要記住撤下的項目與新存檔狀態。
        // 訂閱只持續到此 UI 物件銷毀，不會讓其他場景的 UI 沿用這份名單。
    }

    private void OnDestroy()
    {
        UILocalizationManager.languageChanged -= HandleLanguageChanged;
        Unbind();
        ClearItems();
    }

    private void OnValidate()
    {
        maxVisibleTasks = Mathf.Max(0, maxVisibleTasks);
        _renderRequested = true;
    }

    private void LateUpdate()
    {
        // 只檢查服務引用；任務內容由事件標記更新，不每幀重建清單。
        EnsureBinding();
        ApplyPendingUpdates();
    }

    /// <summary>可由 Button、UnityEvent 或其他腳本呼叫；重新取啟動中且未領獎的前 N 筆。</summary>
    [ContextMenu("重新挑選顯示任務")]
    public void RefreshTasks()
    {
        if (!Application.isPlaying) return;
        EnsureBinding();
        _refreshRequested = true;
        _renderRequested = true;
        ApplyPendingUpdates();
    }

    private void EnsureBinding()
    {
        var service = GameStatusService.Instance;
        var quests = service != null ? service.Quests : null;
        var time = service != null ? service.Time : null;
        if (ReferenceEquals(_service, service) && ReferenceEquals(_quests, quests) && ReferenceEquals(_time, time)) return;
        Unbind();
        _service = service;
        _quests = quests;
        _time = time;
        _tracker = quests != null && time != null ? new QuestTrackerModel(quests) : null;
        if (_service != null) _service.OnGameStatusLoaded += HandleGameStatusLoaded;
        if (_quests != null) _quests.OnChanged += HandleQuestChanged;
        if (_time != null) _time.OnDayPassed += HandleDayPassed;
        _refreshRequested = true;
        _renderRequested = true;
    }

    private void Unbind()
    {
        if (!ReferenceEquals(_service, null)) _service.OnGameStatusLoaded -= HandleGameStatusLoaded;
        if (_quests != null) _quests.OnChanged -= HandleQuestChanged;
        if (_time != null) _time.OnDayPassed -= HandleDayPassed;
        _service = null;
        _quests = null;
        _time = null;
        _tracker = null;
    }

    private void HandleQuestChanged()
    {
        _tracker?.UpdateProgress(_time.DayIndex, maxVisibleTasks);
        _renderRequested = true;
    }

    private void HandleDayPassed()
    {
        // 延到本幀其他換日處理完成後再挑選，避免讀到換日結算中途的狀態。
        _refreshRequested = true;
        _renderRequested = true;
    }

    private void HandleGameStatusLoaded()
    {
        EnsureBinding();
        _refreshRequested = true;
        _renderRequested = true;
        // 這是全部 Model 套用完畢後的統一事件，包含同一天的切槽讀檔。
        ApplyPendingUpdates();
    }

    private void HandleLanguageChanged(string language) => _renderRequested = true;

    private void ApplyPendingUpdates()
    {
        if (_tracker != null)
        {
            if (_refreshRequested || _tracker.DisplayDay != _time.DayIndex || _tracker.MaxCount != maxVisibleTasks)
            {
                _tracker.RefreshTasks(_time.DayIndex, maxVisibleTasks);
                _refreshRequested = false;
                _renderRequested = true;
            }
            else if (_renderRequested) _tracker.UpdateProgress(_time.DayIndex, maxVisibleTasks);
        }
        if (!_renderRequested || !isActiveAndEnabled) return;
        _renderRequested = false;
        RenderItems();
    }

    private void RenderItems()
    {
        var data = _tracker != null ? _tracker.GetItems() : Array.Empty<QuestTrackerItemState>();
        if (itemPrefab == null || itemContainer == null)
        {
            ClearItems();
            if (!_reportedMissingSetup)
            {
                Debug.LogWarning("[QuestTrackerUI] 請指定任務項目 Prefab 與生成容器。", this);
                _reportedMissingSetup = true;
            }
            return;
        }
        _reportedMissingSetup = false;
        var visible = new HashSet<string>();
        foreach (var item in data) visible.Add(item.QuestID);
        var removed = new List<string>();
        foreach (var pair in _items)
            if (!visible.Contains(pair.Key) || pair.Value == null) removed.Add(pair.Key);
        foreach (string id in removed)
        {
            DestroyItem(_items[id]);
            _items.Remove(id);
        }
        for (int i = 0; i < data.Length; i++)
        {
            var state = data[i];
            if (!_items.TryGetValue(state.QuestID, out var item))
            {
                item = Instantiate(itemPrefab, itemContainer, false);
                item.name = "Quest_" + state.QuestID;
                _items.Add(state.QuestID, item);
            }
            item.transform.SetParent(itemContainer, false);
            item.transform.SetSiblingIndex(i);
            item.Render(state, completedTextKey, completedTodayTextKey, incompleteTextKey, incompleteTodayTextKey);
            item.gameObject.SetActive(true);
        }
    }

    private void ClearItems()
    {
        foreach (var item in _items.Values) DestroyItem(item);
        _items.Clear();
    }

    private void DestroyItem(QuestTrackerItemUI item)
    {
        if (item == null) return;
        // Destroy 在幀末才執行，先隱藏，讓 Layout 不保留已領獎項目的空間。
        item.gameObject.SetActive(false);
        Destroy(item.gameObject);
    }
}
