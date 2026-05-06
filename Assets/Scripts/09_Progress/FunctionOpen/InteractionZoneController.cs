using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PixelCrushers.DialogueSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;

/// <summary>
/// 互動區域控制器：依條件開啟功能物件、切換叢集、顯示可操作區域說明。
/// Singleton，可透過 InteractionZoneController.Instance 存取。
/// </summary>
public class InteractionZoneController : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════
    //  Singleton
    // ══════════════════════════════════════════════════════════

    public static InteractionZoneController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    // ══════════════════════════════════════════════════════════
    //  Enum：叢集分類
    // ══════════════════════════════════════════════════════════

    public enum InteractionGroup
    {
        NoGroup,  // 不屬於任何叢集，永遠跟隨自身條件，不受叢集切換影響
        Touch,
        Lick,
        Clothes,
        Tool,
        Sex
    }

    // ══════════════════════════════════════════════════════════
    //  第一部分：功能物件配置
    // ══════════════════════════════════════════════════════════

    [Serializable]
    public class FunctionEntry
    {
        [Header("開啟條件 (Value > 0 即符合)")]
        [Tooltip("使用 ProgressValueDefinition 作為條件，其值 > 0 時此項目開啟")]
        public ProgressValueDefinition conditionValue;

        [Header("叢集歸屬")]
        public InteractionGroup group = InteractionGroup.NoGroup;

        [Header("關聯物件 (SetActive 開關)")]
        [Tooltip("當此項目啟用時，這些物件 SetActive(true)")]
        public List<GameObject> relatedObjects = new List<GameObject>();

        [Header("Collider 群組 (整批 Enable/Disable)")]
        [Tooltip("當此項目啟用時，這些 Collider 群組會被 Enable；停用時 Disable")]
        public List<ColliderGroupName> colliderGroups = new List<ColliderGroupName>();

        [Header("Collider 單獨 ID (逐一 Enable/Disable)")]
        [Tooltip("直接填入 Collider ID，自動跨群組查找 (ID 不可重複)")]
        public List<string> colliderIds = new List<string>();
    }

    [Serializable]
    public class GroupConfig
    {
        public InteractionGroup group;

        [Header("叢集開啟條件 (Flag)")]
        [Tooltip("此 Flag 存在時該叢集才可被切換到；若未指定則預設開啟")]
        public ProgressFlagDefinition unlockFlag;

        [Header("叢集按鈕")]
        [Tooltip("點擊此按鈕切換到該叢集，所有按鈕同時顯示在畫面上")]
        public Button groupButton;

        [Header("按鈕上的 CanvasGroup")]
        [Tooltip("用於控制未解鎖時的透明度 (未解鎖 → alpha=0.4, 不可互動)")]
        public CanvasGroup buttonCanvasGroup;

        [Header("面板內容")]
        [Tooltip("此叢集是否顯示可操作區域說明面板")]
        public bool showAreaDescription = true;
    }

    [Header("═══ 第一部分：功能物件 ═══")]
    [SerializeField] private List<FunctionEntry> allEntries = new List<FunctionEntry>();

    [Header("叢集配置 (不含 NoGroup)")]
    [SerializeField] private List<GroupConfig> groupConfigs = new List<GroupConfig>();

    [Header("選中按鈕色彩")]
    [Tooltip("選中叢集按鈕的高亮顏色")]
    [SerializeField] private Color selectedColor = new Color(1f, 0.929f, 0.329f, 1f); // #FFED54
    [SerializeField] private Color normalColor = Color.white;

    // ══════════════════════════════════════════════════════════
    //  第二部分：可操作區域說明 (Prefab 動態生成)
    // ══════════════════════════════════════════════════════════

    [Serializable]
    public class AreaDescriptionData
    {
        [Header("歸屬叢集")]
        public InteractionGroup group = InteractionGroup.NoGroup;

        [Header("顯示條件 (可選)")]
        [Tooltip("若未設定，預設開啟。依 Value 決定顯示等級：\n" +
                 "= 0 → 不顯示\n" +
                 "> 0 且 ≤ 1 → 顯示 ?????\n" +
                 "> 1 且 ≤ 2 → 顯示原文 + ?\n" +
                 "> 2 → 正常顯示")]
        public ProgressValueDefinition revealCondition;

        [Header("多語系文字 Key")]
        [Tooltip("Dialogue System Text Table 中的 Key")]
        public string localizationKey;

        [Header("排序 (數字越小越上面)")]
        public int sortOrder;
    }

    private class DescriptionLineInstance
    {
        public AreaDescriptionData data;
        public GameObject lineObject;
        public TextMeshProUGUI textComponent;
    }

    [Header("═══ 第二部分：區域說明 ═══")]
    [Tooltip("純資料設定，不需要拖 UI 元件，運行時會自動從 Prefab 生成")]
    [SerializeField] private List<AreaDescriptionData> areaDescriptions = new List<AreaDescriptionData>();

    [Header("說明面板")]
    [SerializeField] private Button toggleDescPanelButton;
    [SerializeField] private GameObject descriptionPanel;
    [Tooltip("說明面板一開始是否為開啟狀態")]
    [SerializeField] private bool descPanelDefaultOpen = true;

    [Header("動態生成設定")]
    [Tooltip("行 Prefab：需包含一個 TextMeshProUGUI 元件")]
    [SerializeField] private GameObject descriptionLinePrefab;
    [Tooltip("行的父物件 (建議掛 VerticalLayoutGroup)")]
    [SerializeField] private Transform descriptionLineParent;

    // ══════════════════════════════════════════════════════════
    //  第三部分：面板區塊 (PanelSection)
    //  將面板中的各種 UI 區塊獨立管理，依叢集自動開關。
    // ══════════════════════════════════════════════════════════

    [Serializable]
    public class PanelSection
    {
        [Tooltip("方便在 Inspector 中辨識用")]
        public string sectionName;

        [Header("歸屬叢集")]
        [Tooltip("屬於哪個叢集；NoGroup = 所有叢集都顯示")]
        public InteractionGroup group = InteractionGroup.NoGroup;

        [Header("區塊根物件")]
        [Tooltip("切換叢集時，此物件會被 SetActive 開關")]
        public GameObject sectionRoot;

        [Header("顯示條件 (可選)")]
        [Tooltip("此 Flag 存在時才顯示此區塊；若未指定則預設開啟")]
        public ProgressFlagDefinition unlockFlag;
    }

    [Header("═══ 第三部分：面板區塊 ═══")]
    [Tooltip("面板中的各種 UI 區塊 (Auto 按鈕、專屬按鈕組等)，依叢集自動開關")]
    [SerializeField] private List<PanelSection> panelSections = new List<PanelSection>();

    // ══════════════════════════════════════════════════════════
    //  內部狀態
    // ══════════════════════════════════════════════════════════

    private int _currentGroupIndex = 0;
    private bool _descPanelOpen = false;

    // 各叢集是否已解鎖的快取
    private bool[] _groupUnlocked;

    // 動態生成的行實例
    private List<DescriptionLineInstance> _lineInstances = new List<DescriptionLineInstance>();

    // 事件委派快取 (用於 OnDestroy 取消訂閱)
    private Action<string, bool> _onFlagChangedHandler;
    private Action<string, int> _onVariableChangedHandler;

    // 相關性過濾：只有這些 key 變動時才需要刷新
    private HashSet<string> _relevantKeys;

    // ══════════════════════════════════════════════════════════
    //  生命週期
    // ══════════════════════════════════════════════════════════

    private void Start()
    {
        _groupUnlocked = new bool[groupConfigs.Count];

        // 建立相關 key 快取
        BuildRelevantKeys();

        // 動態生成說明行
        BuildDescriptionLines();

        // 靜態初次刷新 (不觸發特效)
        RefreshAll(false);

        // 開始監聽未來的變動
        InitializeEvents();
    }

    private void OnDestroy()
    {
        // 取消訂閱 ProgressFlags 事件
        if (GameStatusService.Instance != null && GameStatusService.Instance.ProgressFlags != null)
        {
            var flags = GameStatusService.Instance.ProgressFlags;
            if (_onFlagChangedHandler != null) flags.OnFlagChanged -= _onFlagChangedHandler;
            if (_onVariableChangedHandler != null) flags.OnVariableChanged -= _onVariableChangedHandler;
        }

        // 移除按鈕監聽
        foreach (var gc in groupConfigs)
        {
            if (gc.groupButton) gc.groupButton.onClick.RemoveAllListeners();
        }
        if (toggleDescPanelButton) toggleDescPanelButton.onClick.RemoveAllListeners();
    }

    // ══════════════════════════════════════════════════════════
    //  相關性過濾：建立需要監聽的 Key 集合
    // ══════════════════════════════════════════════════════════

    private void BuildRelevantKeys()
    {
        _relevantKeys = new HashSet<string>();

        // 第一部分：FunctionEntry 的條件 Value Key
        foreach (var entry in allEntries)
        {
            if (entry.conditionValue != null)
                _relevantKeys.Add(entry.conditionValue.FlagID);
        }

        // 叢集解鎖的 Flag Key
        foreach (var gc in groupConfigs)
        {
            if (gc.unlockFlag != null)
                _relevantKeys.Add(gc.unlockFlag.FlagID);
        }

        // 第二部分：說明行的條件 Value Key
        foreach (var desc in areaDescriptions)
        {
            if (desc.revealCondition != null)
                _relevantKeys.Add(desc.revealCondition.FlagID);
        }

        // 第三部分：PanelSection 的解鎖 Flag Key
        foreach (var section in panelSections)
        {
            if (section.unlockFlag != null)
                _relevantKeys.Add(section.unlockFlag.FlagID);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  事件初始化
    // ══════════════════════════════════════════════════════════

    private void InitializeEvents()
    {
        // 1. 監聽全域進度變動 (帶相關性過濾)
        if (GameStatusService.Instance != null && GameStatusService.Instance.ProgressFlags != null)
        {
            var flags = GameStatusService.Instance.ProgressFlags;

            _onFlagChangedHandler = (key, _) =>
            {
                if (_relevantKeys.Contains(key)) RefreshAll(true);
            };
            _onVariableChangedHandler = (key, _) =>
            {
                if (_relevantKeys.Contains(key)) RefreshAll(true);
            };

            flags.OnFlagChanged += _onFlagChangedHandler;
            flags.OnVariableChanged += _onVariableChangedHandler;
        }

        // 2. 叢集按鈕：為每個按鈕綁定點擊事件 + PointerDown/Up 即時 Icon 變色
        for (int i = 0; i < groupConfigs.Count; i++)
        {
            var gc = groupConfigs[i];
            if (gc.groupButton == null) continue;

            int capturedIndex = i; // 閉包捕獲
            gc.groupButton.onClick.AddListener(() => OnGroupButtonClicked(capturedIndex));

            // 透過 EventTrigger 監聯按下/放開，讓 IconImage 與 Button 的 pressedColor 同步
            var trigger = gc.groupButton.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = gc.groupButton.gameObject.AddComponent<EventTrigger>();

            // PointerDown → IconImage 立即變黃
            var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDown.callback.AddListener((_) => SetIconColor(capturedIndex, selectedColor));
            trigger.triggers.Add(pointerDown);

            // PointerUp → 如果不是選中狀態就還原白色 (選中的由 RefreshAll 處理)
            var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pointerUp.callback.AddListener((_) =>
            {
                if (capturedIndex != _currentGroupIndex)
                    SetIconColor(capturedIndex, normalColor);
            });
            trigger.triggers.Add(pointerUp);
        }

        // 3. 說明面板開關按鈕
        if (toggleDescPanelButton) toggleDescPanelButton.onClick.AddListener(ToggleDescriptionPanel);

        // 說明面板初始狀態
        _descPanelOpen = descPanelDefaultOpen;
        if (descriptionPanel) descriptionPanel.SetActive(_descPanelOpen);
    }

    // ══════════════════════════════════════════════════════════
    //  動態生成說明行
    // ══════════════════════════════════════════════════════════

    private void BuildDescriptionLines()
    {
        foreach (var inst in _lineInstances)
        {
            if (inst.lineObject != null) Destroy(inst.lineObject);
        }
        _lineInstances.Clear();

        if (descriptionLinePrefab == null || descriptionLineParent == null)
        {
            if (areaDescriptions.Count > 0)
                Debug.LogWarning("[InteractionZoneController] 缺少 descriptionLinePrefab 或 descriptionLineParent，跳過說明行生成。");
            return;
        }

        var sorted = areaDescriptions.OrderBy(d => d.sortOrder).ToList();

        foreach (var data in sorted)
        {
            GameObject lineObj = Instantiate(descriptionLinePrefab, descriptionLineParent);
            lineObj.name = $"DescLine_{data.localizationKey}";

            var tmp = lineObj.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp == null)
                Debug.LogWarning($"[InteractionZoneController] Prefab 中找不到 TextMeshProUGUI：{data.localizationKey}");

            _lineInstances.Add(new DescriptionLineInstance
            {
                data = data,
                lineObject = lineObj,
                textComponent = tmp
            });

            lineObj.SetActive(false);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  核心刷新
    // ══════════════════════════════════════════════════════════

    public void RefreshAll(bool canTriggerEffect)
    {
        if (GameStatusService.Instance == null) return;

        var progressFlags = GameStatusService.Instance.ProgressFlags;

        // ── 1. 刷新叢集解鎖狀態 ──
        RefreshGroupUnlockStates(progressFlags);

        // ── 2. 刷新第一部分：功能物件顯示 ──
        RefreshFunctionEntries(progressFlags);

        // ── 3. 刷新第二部分：說明面板文字 ──
        RefreshDescriptionPanel(progressFlags);

        // ── 4. 刷新第三部分：面板區塊 ──
        RefreshPanelSections(progressFlags);

        // ── 5. 更新按鈕視覺 ──
        UpdateGroupButtonVisuals();
    }

    // ══════════════════════════════════════════════════════════
    //  第一部分邏輯
    // ══════════════════════════════════════════════════════════

    private void RefreshGroupUnlockStates(ProgressFlagModel progressFlags)
    {
        for (int i = 0; i < groupConfigs.Count; i++)
        {
            var gc = groupConfigs[i];
            _groupUnlocked[i] = gc.unlockFlag == null || progressFlags.Contains(gc.unlockFlag.FlagID);
        }

        // 如果當前選中的叢集已鎖定，自動跳到第一個解鎖的叢集
        if (groupConfigs.Count > 0 && !IsCurrentGroupUnlocked())
        {
            _currentGroupIndex = FindFirstUnlockedIndex();
        }
    }

    private void RefreshFunctionEntries(ProgressFlagModel progressFlags)
    {
        var currentGroup = GetCurrentGroup();
        InteractionGroup activeGroup = currentGroup != null ? currentGroup.group : InteractionGroup.NoGroup;

        var colliderMgr = Collider2DManager.Instance;

        foreach (var entry in allEntries)
        {
            bool conditionMet;
            if (entry.conditionValue != null)
            {
                int val = progressFlags.GetValue(entry.conditionValue.FlagID);
                conditionMet = val > 0;
            }
            else
            {
                conditionMet = true;
            }

            bool groupMatch = entry.group == InteractionGroup.NoGroup || entry.group == activeGroup;
            bool shouldBeActive = conditionMet && groupMatch;

            // ── 1. 一般物件 SetActive ──
            foreach (var obj in entry.relatedObjects)
            {
                if (obj != null) obj.SetActive(shouldBeActive);
            }

            // ── 2. Collider 群組整批開關 ──
            if (colliderMgr != null)
            {
                foreach (var groupName in entry.colliderGroups)
                {
                    if (shouldBeActive)
                        colliderMgr.EnableGroup(groupName);
                    else
                        colliderMgr.DisableGroup(groupName);
                }

                // ── 3. Collider 單獨 ID 開關 ──
                foreach (var id in entry.colliderIds)
                {
                    colliderMgr.SetColliderStateById(id, shouldBeActive);
                }
            }
        }
    }

    private void UpdateGroupButtonVisuals()
    {
        for (int i = 0; i < groupConfigs.Count; i++)
        {
            var gc = groupConfigs[i];
            bool unlocked = _groupUnlocked[i];
            bool isSelected = (i == _currentGroupIndex) && unlocked;

            // ── CanvasGroup：未解鎖時完全隱藏 ──
            if (gc.buttonCanvasGroup != null)
            {
                gc.buttonCanvasGroup.alpha = unlocked ? 1f : 0f;
                gc.buttonCanvasGroup.interactable = unlocked;
                gc.buttonCanvasGroup.blocksRaycasts = unlocked;
            }

            if (gc.groupButton != null)
            {
                // ── 修改 Button 的 ColorBlock ──
                var colors = gc.groupButton.colors;
                if (isSelected)
                {
                    colors.normalColor = selectedColor;
                    colors.highlightedColor = selectedColor;
                    colors.pressedColor = selectedColor;
                    colors.selectedColor = selectedColor;
                }
                else
                {
                    colors.normalColor = normalColor;
                    colors.highlightedColor = normalColor;
                    colors.pressedColor = selectedColor;
                    colors.selectedColor = normalColor;
                }
                gc.groupButton.colors = colors;

                // ── 子物件 IconImage 的顏色 ──
                SetIconColor(i, isSelected ? selectedColor : normalColor);
            }
        }
    }

    private void SetIconColor(int index, Color color)
    {
        if (index < 0 || index >= groupConfigs.Count) return;
        var gc = groupConfigs[index];
        if (gc.groupButton == null) return;

        Transform iconTf = gc.groupButton.transform.Find("IconImage");
        if (iconTf != null)
        {
            var iconImage = iconTf.GetComponent<Image>();
            if (iconImage != null)
                iconImage.color = color;
        }
    }

    private void OnGroupButtonClicked(int index)
    {
        if (index < 0 || index >= groupConfigs.Count) return;
        if (!_groupUnlocked[index]) return;

        _currentGroupIndex = index;
        RefreshAll(true);
    }

    private GroupConfig GetCurrentGroup()
    {
        if (groupConfigs.Count == 0) return null;
        if (_currentGroupIndex < 0 || _currentGroupIndex >= groupConfigs.Count) return null;
        return groupConfigs[_currentGroupIndex];
    }

    private bool IsCurrentGroupUnlocked()
    {
        if (_currentGroupIndex < 0 || _currentGroupIndex >= _groupUnlocked.Length) return false;
        return _groupUnlocked[_currentGroupIndex];
    }

    private int FindFirstUnlockedIndex()
    {
        for (int i = 0; i < _groupUnlocked.Length; i++)
        {
            if (_groupUnlocked[i]) return i;
        }
        return 0;
    }

    // ══════════════════════════════════════════════════════════
    //  第二部分邏輯：可操作區域說明
    // ══════════════════════════════════════════════════════════

    private void ToggleDescriptionPanel()
    {
        _descPanelOpen = !_descPanelOpen;
        if (descriptionPanel) descriptionPanel.SetActive(_descPanelOpen);

        if (_descPanelOpen && GameStatusService.Instance != null)
        {
            RefreshDescriptionPanel(GameStatusService.Instance.ProgressFlags);
        }
    }

    private void RefreshDescriptionPanel(ProgressFlagModel progressFlags)
    {
        var currentGroup = GetCurrentGroup();

        // 判斷當前叢集是否顯示說明面板
        bool groupShowsDesc = currentGroup == null || currentGroup.showAreaDescription;

        // 控制說明面板整體顯示：需要 _descPanelOpen 且叢集允許
        if (descriptionPanel)
            descriptionPanel.SetActive(_descPanelOpen && groupShowsDesc);

        // 控制開關按鈕：叢集不顯示說明時，按鈕也隱藏
        if (toggleDescPanelButton)
            toggleDescPanelButton.gameObject.SetActive(groupShowsDesc);

        // 如果面板不顯示，不需要刷新行內容
        if (!_descPanelOpen || !groupShowsDesc) return;

        InteractionGroup activeGroup = currentGroup != null ? currentGroup.group : InteractionGroup.NoGroup;

        foreach (var line in _lineInstances)
        {
            var data = line.data;

            bool belongsToCurrentGroup = data.group == InteractionGroup.NoGroup || data.group == activeGroup;

            if (!belongsToCurrentGroup)
            {
                if (line.lineObject != null) line.lineObject.SetActive(false);
                continue;
            }

            int revealLevel = GetRevealLevel(data, progressFlags);

            if (revealLevel == 0)
            {
                if (line.lineObject != null) line.lineObject.SetActive(false);
                continue;
            }

            if (line.lineObject != null) line.lineObject.SetActive(true);

            if (line.textComponent != null)
            {
                line.textComponent.text = GetDisplayText(data, revealLevel);
            }
        }
    }

    private int GetRevealLevel(AreaDescriptionData data, ProgressFlagModel progressFlags)
    {
        if (data.revealCondition == null)
            return 3;

        int val = progressFlags.GetValue(data.revealCondition.FlagID);

        if (val <= 0) return 0;
        if (val <= 1) return 1;
        if (val <= 2) return 2;
        return 3;
    }

    private string GetDisplayText(AreaDescriptionData data, int revealLevel)
    {
        string originalText = GetLocalizedText(data.localizationKey);

        const string bullet = "• ";

        switch (revealLevel)
        {
            case 1:
                return bullet + "?????";
            case 2:
                return bullet + originalText + "?";
            case 3:
            default:
                return bullet + originalText;
        }
    }

    private string GetLocalizedText(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";

        string localized = DialogueManager.GetLocalizedText(key);
        return !string.IsNullOrEmpty(localized) ? localized : key;
    }

    // ══════════════════════════════════════════════════════════
    //  第三部分邏輯：面板區塊 (PanelSection)
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// 刷新面板區塊：依叢集歸屬與解鎖條件控制 SetActive。
    /// </summary>
    private void RefreshPanelSections(ProgressFlagModel progressFlags)
    {
        var currentGroup = GetCurrentGroup();
        InteractionGroup activeGroup = currentGroup != null ? currentGroup.group : InteractionGroup.NoGroup;

        foreach (var section in panelSections)
        {
            if (section.sectionRoot == null) continue;

            // 叢集歸屬判斷：NoGroup = 所有叢集都顯示
            bool groupMatch = section.group == InteractionGroup.NoGroup || section.group == activeGroup;

            // 解鎖條件判斷：未指定 Flag = 預設開啟
            bool unlocked = section.unlockFlag == null || progressFlags.Contains(section.unlockFlag.FlagID);

            section.sectionRoot.SetActive(groupMatch && unlocked);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  公開 API (供 Dialogue System 等外部呼叫)
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// 以 enum 切換叢集。
    /// 用法：InteractionZoneController.Instance.SwitchGroup(InteractionGroup.Lick);
    /// </summary>
    public void SwitchGroup(InteractionGroup targetGroup)
    {
        for (int i = 0; i < groupConfigs.Count; i++)
        {
            if (groupConfigs[i].group == targetGroup)
            {
                OnGroupButtonClicked(i);
                return;
            }
        }
        Debug.LogWarning($"[InteractionZoneController] 找不到叢集：{targetGroup}");
    }

    /// <summary>
    /// 以字串切換叢集 (方便 Dialogue System 的 Lua 或 Sequencer 呼叫)。
    /// 用法：InteractionZoneController.Instance.SwitchGroup("Lick");
    /// </summary>
    public void SwitchGroup(string groupName)
    {
        if (Enum.TryParse<InteractionGroup>(groupName, true, out var group))
        {
            SwitchGroup(group);
        }
        else
        {
            Debug.LogWarning($"[InteractionZoneController] 無法解析叢集名稱：{groupName}");
        }
    }

    /// <summary>
    /// 取得當前叢集。
    /// </summary>
    public InteractionGroup CurrentGroup
    {
        get
        {
            var gc = GetCurrentGroup();
            return gc != null ? gc.group : InteractionGroup.NoGroup;
        }
    }

    /// <summary>
    /// 開啟說明面板。
    /// </summary>
    public void OpenDescriptionPanel()
    {
        if (_descPanelOpen) return;
        ToggleDescriptionPanel();
    }

    /// <summary>
    /// 關閉說明面板。
    /// </summary>
    public void CloseDescriptionPanel()
    {
        if (!_descPanelOpen) return;
        ToggleDescriptionPanel();
    }

    /// <summary>
    /// 設定說明面板開關狀態。
    /// </summary>
    public void SetDescriptionPanel(bool open)
    {
        if (open) OpenDescriptionPanel();
        else CloseDescriptionPanel();
    }

    /// <summary>
    /// 強制刷新所有狀態 (供外部在特殊時機呼叫)。
    /// </summary>
    public void ForceRefresh()
    {
        RefreshAll(true);
    }
}