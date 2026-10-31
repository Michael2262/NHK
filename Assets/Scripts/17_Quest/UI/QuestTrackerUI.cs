using System;
using System.Collections.Generic;
using DG.Tweening;
using PixelCrushers;
using UnityEngine;
using UnityEngine.UI;

/// <summary>生成任務追蹤項目；同日保留已顯示名單，換日、讀檔或手動刷新才重新挑選。</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-800)]
[AddComponentMenu("NHK/Quest/Quest Tracker UI")]
public sealed class QuestTrackerUI : MonoBehaviour
{
    private const string NewStickerMissionFlag = "Flag_Event_NewStickerMission";

    /// <summary>共用 UI 的唯一實例；首次啟用前或共用 UI 場景卸載後可能為 null。</summary>
    public static QuestTrackerUI Instance { get; private set; }

    /// <summary>目前要求的開合狀態；動畫進行中也會立即反映目標狀態。</summary>
    public bool IsExpanded => _isExpanded;

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
    [Tooltip("Flag_Event_NewStickerMission 生效時，所有任務優先顯示此文字並使用灰色，包括已完成的任務。")]
    [SerializeField] private string tomorrowTextKey = "Quest.Tomorrow";

    [Header("面板開合")]
    [Tooltip("依此容器排版完成後的實際高度向上收合。高度須包含裝飾與任務清單；箭頭掛在底部外側並 Ignore Layout。場景中的位置就是展開位置，父層不可用 Layout Group 控制它的位置。")]
    [SerializeField] private RectTransform slidingPanel;
    [Tooltip("包含整個任務面板與箭頭的專用 CanvasGroup；零筆時隱藏並停用點擊。請勿使用 UIVisibilityManager 控制的外層群組。")]
    [SerializeField] private CanvasGroup panelVisibilityGroup;
    [Tooltip("開合按鈕，程式自動綁定 TogglePanel，不需再設定相同的 On Click 事件。")]
    [SerializeField] private Button toggleButton;
    [Tooltip("只放箭頭圖案的 RectTransform，初始朝上；隨面板開合同步旋轉，收合時朝下。旋轉沿用面板的動畫時間與曲線。")]
    [SerializeField] private RectTransform arrowRect;
    [Tooltip("自動量測 Panel 高度後，額外向上收合的距離（Panel 父層的 UI 座標單位）。用於外框超出 Panel 或遮擋邊界的微調；不需隨任務筆數調整。")]
    [Min(0f)] [SerializeField] private float collapseExtraDistance;
    [Min(0f)] [SerializeField] private float slideDuration = 0.25f;
    [SerializeField] private Ease slideEase = Ease.OutCubic;

    private readonly Dictionary<string, QuestTrackerItemUI> _items = new Dictionary<string, QuestTrackerItemUI>();
    private GameStatusService _service;
    private QuestModel _quests;
    private TimeSystemModel _time;
    private ProgressFlagModel _flags;
    private QuestTrackerModel _tracker;
    private bool _refreshRequested = true;
    private bool _renderRequested = true;
    private bool _reportedMissingSetup;
    private bool _isDuplicate;
    private bool _isExpanded = true;
    private bool _visibilityRequested = true;
    private int _displayedItemCount = -1;
    private bool _panelSettingsDirty = true;
    private bool _reportedMissingPanelSetup;
    private RectTransform _capturedPanel;
    private RectTransform _capturedArrow;
    private Vector2 _expandedPosition;
    private Quaternion _expandedArrowRotation;
    private readonly Vector3[] _panelCorners = new Vector3[4];
    private Button _boundToggleButton;
    private Tween _slideTween;
    private Tween _arrowTween;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() => Instance = null;

    private void Awake()
    {
        if (RegisterInstance()) CapturePanelPose();
    }

    private bool RegisterInstance()
    {
        if (_isDuplicate) return false;
        if (Instance != null && Instance != this)
        {
            _isDuplicate = true;
            Debug.LogWarning("[QuestTrackerUI] 已有共用任務追蹤 UI，移除重複元件；請只保留一份。", this);
            enabled = false;
            // 只移除腳本，避免誤刪共用 Canvas 或其他 UI。
            Destroy(this);
            return false;
        }
        Instance = this;
        return true;
    }

    private void OnEnable()
    {
        if (!Application.isPlaying || !RegisterInstance()) return;
        BindToggleButton();
        UILocalizationManager.languageChanged += HandleLanguageChanged;
        EnsureBinding();
        _renderRequested = true;
        ApplyPendingUpdates();
        // 重開面板時直接對齊最新狀態，不接續上次被停用打斷的動畫。
        ApplyPanelPresentation(false);
    }

    private void OnDisable()
    {
        UnbindToggleButton();
        KillPanelTweens();
        UILocalizationManager.languageChanged -= HandleLanguageChanged;
        // 保留進度／換日／讀檔訂閱：面板隱藏時也要記住撤下的項目與新存檔狀態。
        // 訂閱只持續到此 UI 物件銷毀，不會讓其他場景的 UI 沿用這份名單。
    }

    private void OnDestroy()
    {
        UnbindToggleButton();
        KillPanelTweens();
        if (Instance == this) Instance = null;
        UILocalizationManager.languageChanged -= HandleLanguageChanged;
        Unbind();
        ClearItems();
    }

    private void OnValidate()
    {
        maxVisibleTasks = Mathf.Max(0, maxVisibleTasks);
        slideDuration = Mathf.Max(0f, slideDuration);
        collapseExtraDistance = Mathf.Max(0f, collapseExtraDistance);
        _panelSettingsDirty = true;
        _renderRequested = true;
    }

    private void LateUpdate()
    {
        if (Instance != this) return;
        // 只檢查服務引用；任務內容由事件標記更新，不每幀重建清單。
        EnsureBinding();
        ApplyPendingUpdates();
    }

    /// <summary>可由 Button、UnityEvent 或其他腳本呼叫；重新取啟動中且未領獎的前 N 筆。</summary>
    [ContextMenu("重新挑選顯示任務")]
    public void RefreshTasks()
    {
        if (!Application.isPlaying || Instance != this) return;
        EnsureBinding();
        _refreshRequested = true;
        _renderRequested = true;
        _panelSettingsDirty = true;
        ApplyPendingUpdates();
    }

    /// <summary>允許顯示目前清單，保留開合狀態；零筆任務仍隱藏。可綁定對話結束事件。</summary>
    public void ShowUI()
    {
        if (!Application.isPlaying || Instance != this) return;
        // 先套用尚未處理的領獎／換日更新，避免顯示過期項目；不額外重新挑選或補位。
        EnsureBinding();
        ApplyPendingUpdates();
        _visibilityRequested = true;
        ApplyPanelPresentation(false);
    }

    /// <summary>隱藏整個面板與箭頭，直到再次要求顯示；任務進度仍持續更新。</summary>
    public void HideUI()
    {
        if (!Application.isPlaying || Instance != this) return;
        _visibilityRequested = false;
        ApplyPanelPresentation(false);
    }

    /// <summary>一次允許顯示並展開；仍受零筆隱藏及外層 CanvasGroup 的顯示條件限制。</summary>
    public void ShowAndExpandUI()
    {
        ShowUI();
        ExpandPanel();
    }

    /// <summary>展開目前任務面板；不重新挑選任務，也不解除 HideUI，零筆時仍保持隱藏。</summary>
    public void ExpandPanel() => SetExpanded(true);

    /// <summary>收合目前任務面板；不重新挑選任務。</summary>
    public void CollapsePanel() => SetExpanded(false);

    /// <summary>切換開合，供箭頭按鈕、UnityEvent 與其他程式使用。</summary>
    public void TogglePanel() => SetExpanded(!_isExpanded);

    public void SetExpanded(bool expanded)
    {
        if (!Application.isPlaying || Instance != this || _isExpanded == expanded) return;
        _isExpanded = expanded;
        ApplyPanelPresentation(true);
    }

    private void BindToggleButton()
    {
        if (_boundToggleButton == toggleButton) return;
        UnbindToggleButton();
        _boundToggleButton = toggleButton;
        if (_boundToggleButton != null) _boundToggleButton.onClick.AddListener(TogglePanel);
    }

    private void UnbindToggleButton()
    {
        if (_boundToggleButton != null) _boundToggleButton.onClick.RemoveListener(TogglePanel);
        _boundToggleButton = null;
    }

    private void CapturePanelPose()
    {
        if (slidingPanel != null && _capturedPanel != slidingPanel)
        {
            _capturedPanel = slidingPanel;
            _expandedPosition = slidingPanel.anchoredPosition;
        }
        if (arrowRect != null && _capturedArrow != arrowRect)
        {
            _capturedArrow = arrowRect;
            _expandedArrowRotation = arrowRect.localRotation;
        }
    }

    private void SetDisplayedItemCount(int count)
    {
        bool changed = _displayedItemCount != count;
        _displayedItemCount = count;
        // 清單變動或刷新時，在同一幀完成排版量測與位置校正，避免收合中的箭頭跳位。
        // 只有 SetExpanded 主動切換開合時播放動畫；零筆恢復也先定位再顯示。
        if (changed || _panelSettingsDirty) ApplyPanelPresentation(false);
    }

    private void ApplyPanelPresentation(bool animate)
    {
        CapturePanelPose();
        KillPanelTweens();
        _panelSettingsDirty = false;
        if (isActiveAndEnabled) BindToggleButton();

        if (slidingPanel == null || panelVisibilityGroup == null || toggleButton == null || arrowRect == null)
        {
            if (!_reportedMissingPanelSetup)
            {
                Debug.LogWarning("[QuestTrackerUI] 請設定移動容器、專用 CanvasGroup、開合按鈕與箭頭，才能完整使用面板開合及零筆隱藏。", this);
                _reportedMissingPanelSetup = true;
            }
        }
        else _reportedMissingPanelSetup = false;

        bool visible = _visibilityRequested && _displayedItemCount > 0;
        bool playTween = animate && visible && isActiveAndEnabled && slideDuration > 0f;
        float distance = _isExpanded ? 0f : MeasurePanelHeight() + Mathf.Max(0f, collapseExtraDistance);
        if (slidingPanel != null)
        {
            Vector2 target = _expandedPosition + Vector2.up * distance;
            if (playTween)
                _slideTween = slidingPanel.DOAnchorPos(target, slideDuration).SetEase(slideEase).SetUpdate(true)
                    .OnKill(() => _slideTween = null);
            else slidingPanel.anchoredPosition = target;
        }
        ApplyArrowDirection(playTween);
        if (panelVisibilityGroup != null)
        {
            // 不停用 GameObject，讓零筆隱藏期間仍能收到換日、讀檔及手動刷新。
            panelVisibilityGroup.alpha = visible ? 1f : 0f;
            panelVisibilityGroup.interactable = visible;
            panelVisibilityGroup.blocksRaycasts = visible;
        }
    }

    private void ApplyArrowDirection(bool animate)
    {
        if (arrowRect == null) return;
        Quaternion target = _expandedArrowRotation * Quaternion.Euler(0f, 0f, _isExpanded ? 0f : 180f);
        // 開合時同步旋轉；清單變動及重新啟用時直接對齊，避免額外動畫。
        if (animate)
            _arrowTween = arrowRect.DOLocalRotateQuaternion(target, slideDuration).SetEase(slideEase).SetUpdate(true)
                .OnKill(() => _arrowTween = null);
        else arrowRect.localRotation = target;
    }

    private void KillPanelTweens()
    {
        // 只終止自己建立的動畫；快速連點時從目前位置反向，不影響其他 UI 動畫。
        _slideTween?.Kill(false);
        _arrowTween?.Kill(false);
        _slideTween = null;
        _arrowTween = null;
    }

    private float MeasurePanelHeight()
    {
        if (slidingPanel == null) return 0f;

        // 僅在開合、筆數變動或明確刷新時量測，不每幀強制重建 Layout。
        // 先更新內層清單，再更新整個面板，避免讀到生成／撤下任務前的高度。
        if (slidingPanel.gameObject.activeInHierarchy)
        {
            var listRect = itemContainer as RectTransform;
            if (listRect != null && listRect != slidingPanel && listRect.IsChildOf(slidingPanel))
                LayoutRebuilder.ForceRebuildLayoutImmediate(listRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(slidingPanel);
        }

        // anchoredPosition 使用父層座標；把面板邊界轉到相同空間，避免縮放導致距離不足。
        // 只量 Panel 本身，不把掛在底部外側的箭頭算進收合距離。
        slidingPanel.GetWorldCorners(_panelCorners);
        var parent = slidingPanel.parent;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;
        for (int i = 0; i < _panelCorners.Length; i++)
        {
            float y = parent != null ? parent.InverseTransformPoint(_panelCorners[i]).y : _panelCorners[i].y;
            minY = Mathf.Min(minY, y);
            maxY = Mathf.Max(maxY, y);
        }
        return Mathf.Max(0f, maxY - minY);
    }

    private void EnsureBinding()
    {
        var service = GameStatusService.Instance;
        var quests = service != null ? service.Quests : null;
        var time = service != null ? service.Time : null;
        var flags = service != null ? service.ProgressFlags : null;
        if (ReferenceEquals(_service, service) && ReferenceEquals(_quests, quests)
            && ReferenceEquals(_time, time) && ReferenceEquals(_flags, flags)) return;
        Unbind();
        _service = service;
        _quests = quests;
        _time = time;
        _flags = flags;
        _tracker = quests != null && time != null ? new QuestTrackerModel(quests) : null;
        if (_service != null) _service.OnGameStatusLoaded += HandleGameStatusLoaded;
        if (_quests != null) _quests.OnChanged += HandleQuestChanged;
        if (_time != null) _time.OnDayPassed += HandleDayPassed;
        if (_flags != null) _flags.OnFlagChanged += HandleFlagChanged;
        _refreshRequested = true;
        _renderRequested = true;
    }

    private void Unbind()
    {
        if (!ReferenceEquals(_service, null)) _service.OnGameStatusLoaded -= HandleGameStatusLoaded;
        if (_quests != null) _quests.OnChanged -= HandleQuestChanged;
        if (_time != null) _time.OnDayPassed -= HandleDayPassed;
        if (_flags != null) _flags.OnFlagChanged -= HandleFlagChanged;
        _service = null;
        _quests = null;
        _time = null;
        _flags = null;
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

    private void HandleFlagChanged(string flagID, bool active)
    {
        // 只標記重繪，不重新挑選任務；渲染時查詢完整 Flag 狀態，涵蓋多個存活週期。
        if (flagID == NewStickerMissionFlag) _renderRequested = true;
    }

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
            SetDisplayedItemCount(0);
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
        bool showTomorrow = _flags != null && _flags.Contains(NewStickerMissionFlag);
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
            item.Render(state, completedTextKey, completedTodayTextKey, incompleteTextKey, incompleteTodayTextKey,
                showTomorrow, tomorrowTextKey);
            item.gameObject.SetActive(true);
        }
        SetDisplayedItemCount(data.Length);
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
