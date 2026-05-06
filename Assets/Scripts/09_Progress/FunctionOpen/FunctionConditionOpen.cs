using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

public class FunctionConditionOpen : MonoBehaviour
{
    [Serializable]
    public class FunctionMapping
    {
        [Header("排序")]
        [Tooltip("數字越小越優先顯示")]
        public int sortOrder;

        [Header("1. 計數器條件")]
        public NumberCounter targetCounter;
        public int counterRequiredValue = -1;
        [Tooltip("勾選則判斷是否「大於等於」需求值，false則只有等於才運作")]
        public bool counterGreaterOrEqual = true;

        [Header("2. 數值變數條件")]
        public ProgressValueDefinition valueDef;
        public int valueRequiredValue = -1;
        [Tooltip("勾選則判斷是否「大於等於」需求值，false則只有等於才運作")]
        public bool valueGreaterOrEqual = true;

        [Header("3. 進度標記條件 (On/Off)")]
        public ProgressFlagDefinition flagDef;

        [Header("關聯場景物件")]
        [Tooltip("當此功能被選中時要開啟的物件，其餘功能的物件會自動關閉")]
        public List<GameObject> relatedObjects = new List<GameObject>();
    }

    [Header("功能配置")]
    [SerializeField] private List<FunctionMapping> allMappings = new List<FunctionMapping>();

    [Header("切換控制")]
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;


    [Header("飄浮文字設定")]
    [Tooltip("請拖入一個 GameObject 作為文字生成的錨點中心")]
    [SerializeField] private Transform floatingTextSpawnPoint;

    // 內部狀態
    private List<FunctionMapping> _availableMappings = new List<FunctionMapping>();
    private int _currentIndex = 0;

    // 用於取消訂閱的委派參照
    private Action<string, bool> _onFlagChangedHandler;
    private Action<string, int> _onVariableChangedHandler;
    private Dictionary<NumberCounter, Action> _counterHandlers = new Dictionary<NumberCounter, Action>();

    private void Start()
    {
        // 靜默更新，把「既有的解鎖項目」記錄下來
        RefreshAvailability(true, false);
        // 再開始監聽未來的變動
        InitializeEvents();
    }

    private void InitializeEvents()
    {
        // 1. 監聽全域進度變動
        if (GameStatusService.Instance != null && GameStatusService.Instance.ProgressFlags != null)
        {
            var flags = GameStatusService.Instance.ProgressFlags;

            // 建立委派並保存參照，以便之後取消訂閱
            _onFlagChangedHandler = (f, s) => RefreshAvailability(true);
            _onVariableChangedHandler = (k, v) => RefreshAvailability(true);

            flags.OnFlagChanged += _onFlagChangedHandler;
            flags.OnVariableChanged += _onVariableChangedHandler;
        }

        // 2. 監聽計數器變動
        var counters = allMappings
            .Where(m => m.targetCounter != null)
            .Select(m => m.targetCounter)
            .Distinct();

        foreach (var c in counters)
        {
            Action handler = () => RefreshAvailability(true);
            _counterHandlers[c] = handler;
            c.OnCountChanged += handler;
        }

        // 3. 按鈕綁定
        if (downButton) downButton.onClick.AddListener(() => SwitchFunction(-1));
        if (upButton) upButton.onClick.AddListener(() => SwitchFunction(1));
    }

    /// <summary>
    /// 場景卸載時取消所有事件訂閱，避免 MissingReferenceException
    /// </summary>
    private void OnDestroy()
    {
        // 取消訂閱 GameStatusService 的事件
        if (GameStatusService.Instance != null && GameStatusService.Instance.ProgressFlags != null)
        {
            var flags = GameStatusService.Instance.ProgressFlags;

            if (_onFlagChangedHandler != null)
                flags.OnFlagChanged -= _onFlagChangedHandler;

            if (_onVariableChangedHandler != null)
                flags.OnVariableChanged -= _onVariableChangedHandler;
        }

        // 取消訂閱 NumberCounter 的事件
        foreach (var kvp in _counterHandlers)
        {
            if (kvp.Key != null)
                kvp.Key.OnCountChanged -= kvp.Value;
        }
        _counterHandlers.Clear();

        // 移除按鈕監聽
        if (downButton) downButton.onClick.RemoveAllListeners();
        if (upButton) upButton.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// 核心邏輯：偵測是否有新的 Mapping 被加入可用清單
    /// </summary>
    /// <param name="resetToTop">是否回到第一項</param>
    /// <param name="canTriggerEffect">是否允許觸發飄浮文字</param>
    public void RefreshAvailability(bool resetToTop, bool canTriggerEffect = true)
    {
        if (GameStatusService.Instance == null) return;

        var flags = GameStatusService.Instance.ProgressFlags;

        // 記錄「更新前」的可用物件清單，用來對比是否有新東西
        var previousAvailableSet = new HashSet<FunctionMapping>(_availableMappings);
        int prevCount = _availableMappings.Count;

        // 篩選
        _availableMappings = allMappings.Where(m =>
        {
            bool c1 = false;
            if (m.targetCounter != null)
            {
                int currentCount = m.targetCounter.CurrentCount;
                c1 = m.counterGreaterOrEqual ? currentCount >= m.counterRequiredValue : currentCount == m.counterRequiredValue;
            }

            bool c2 = false;
            if (m.valueDef != null)
            {
                int currentValue = flags.GetValue(m.valueDef.name);
                c2 = m.valueGreaterOrEqual ? currentValue >= m.valueRequiredValue : currentValue == m.valueRequiredValue;
            }

            bool c3 = m.flagDef != null && flags.Contains(m.flagDef.name);

            return c1 || c2 || c3;
        })
        .OrderBy(m => m.sortOrder)
        .ToList();

        // --- 偵測是否有新解鎖的項目 ---
        if (canTriggerEffect)
        {
            bool hasNewUnlock = prevCount > 0 && _availableMappings.Any(m => !previousAvailableSet.Contains(m));

            
        }
        // ---------------------------------

        /*if (_availableMappings.Count != prevCount || resetToTop)
        {
            _currentIndex = 0;
        }*/

        UpdateObjectsAndArrows();
    }

    

    private void UpdateObjectsAndArrows()
    {
        // 如果沒有任何功能可用，隱藏整個控制
        if (_availableMappings.Count == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        var current = _availableMappings[_currentIndex];

        // 1. 處理上下按鈕顯示 (不循環)
        bool hasMultiple = _availableMappings.Count >= 2;
        if (downButton) downButton.gameObject.SetActive(hasMultiple && _currentIndex > 0);
        if (upButton) upButton.gameObject.SetActive(hasMultiple && _currentIndex < _availableMappings.Count - 1);

        // 2. 切換場景物件 Active 狀態
        // 遍歷所有配置，確保非當前選中的功能物件都被關閉
        foreach (var mapping in allMappings)
        {
            bool shouldBeActive = (mapping == current);
            foreach (var obj in mapping.relatedObjects)
            {
                if (obj != null) obj.SetActive(shouldBeActive);
            }
        }
    }

    private void SwitchFunction(int direction)
    {
        int nextIndex = _currentIndex + direction;
        if (nextIndex >= 0 && nextIndex < _availableMappings.Count)
        {
            _currentIndex = nextIndex;
            UpdateObjectsAndArrows();
        }
    }
}