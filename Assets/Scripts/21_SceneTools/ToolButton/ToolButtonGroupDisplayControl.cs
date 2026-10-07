using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 群組選單顯示控制器：共用固定槽位，依選項順位、Flag 填入文字、圖示及事件。
/// 首次開啟選擇 Main（不存在則第一組）；面板重開保留目前群組與返回歷史。
/// 進場時立即隱藏 CanvasGroup，待外部呼叫 Show 後淡入。
/// 群組與歷史僅供此 UI 使用，不寫入遊戲存檔。
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class ToolButtonGroupDisplayControl : MonoBehaviour
{
    public static ToolButtonGroupDisplayControl Instance { get; private set; }

    [Serializable]
    public class IconEntry
    {
        public string name;
        public Sprite sprite;
    }

    [Serializable]
    public class GroupOption
    {
        [Tooltip("Inspector 備註。")]
        public string label;
        [Tooltip("Text Table 的多語系文字 Key。")]
        public string textKey;
        [ToolButtonIconName]
        [Tooltip("選擇 Ng 時強制視為 NG：顯示 NG 圖示與紅字；啟用 NG 點擊事件時走 NG 事件。")]
        public string iconName = "Heart";
        [Header("是否出現（留空永遠成立）")]
        public ProgressFlagDefinition visibilityFlag;
        public bool invertVisibility;
        [Header("是否可點（留空永遠成立，不可點仍占槽位）")]
        public ProgressFlagDefinition interactableFlag;
        public bool invertInteractable;
        [Header("狀態圖示")]
        [Tooltip("Icon 為 Ng 時強制 NG；其他 Icon 依此 Flag 與反轉設定判斷，未設定 Flag 則正常。不影響可點條件。")]
        public ProgressFlagDefinition ngFlag;
        [Tooltip("預設勾選：Flag 為 false 時進入 NG；取消勾選則 Flag 為 true 時進入 NG。")]
        public bool invertNgFlag = true;
        [Header("點擊事件")]
        [Tooltip("未啟用 NG 點擊事件，或 NG 條件（含反轉）不成立時執行。")]
        public UnityEvent onClick = new UnityEvent();
        [Header("NG 點擊事件")]
        [Tooltip("預設關閉。開啟後，NG 時才改走 NG 點擊事件；關閉時一律走一般事件，不影響 NG 圖示與紅字。")]
        public bool useNgClickEvent = false;
        [Tooltip("啟用 NG 點擊事件且 NG 條件（含反轉）成立時只執行此事件；留空不會回退至一般事件。仍須符合可點條件。")]
        public UnityEvent onNgClick = new UnityEvent();
    }

    [Serializable]
    public class ButtonGroup
    {
        [Tooltip("呼叫 ShowGroup 時使用的名稱，區分大小寫。Main 為首頁。")]
        public string groupName;
        [Tooltip("BackGroup 的目的地；留空使用 LastGroup。")]
        public string backGroupName;
        [Tooltip("第一個 Slot 改為返回按鈕，其餘 Slot 向右縮排；一般選項容量減少一格。")]
        public bool showBackButton;
        [Tooltip("越前面的選項順位越高。")]
        public List<GroupOption> options = new List<GroupOption>();
    }

    [Header("固定按鈕（有效槽位數即顯示上限）")]
    [SerializeField] private List<ToolButtonGroupItemView> slots = new List<ToolButtonGroupItemView>();
    [Header("群組")]
    [SerializeField] private List<ButtonGroup> groups = new List<ButtonGroup>();
    [Header("Icon 種類（名稱區分大小寫，Ng／Back 為固定用途）")]
    [SerializeField] private List<IconEntry> icons = new List<IconEntry>
    {
        new IconEntry { name = "Heart" }, new IconEntry { name = "Ng" },
        new IconEntry { name = "Menu" }, new IconEntry { name = "Back" },
        new IconEntry { name = "Question" }
    };
    [Header("返回按鈕設定（使用第一個 Slot）")]
    [SerializeField] private string backTextKey = "Button.Back";
    [SerializeField] private Color backBackgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [Tooltip("顯示返回按鈕時，其餘槽位相對原位置向右偏移的距離；不改變高度與寬度。")]
    [Min(0f)] [SerializeField] private float submenuIndent = 40f;
    [Header("整體顯示（保持物件啟用，透過 CanvasGroup 隱藏）")]
    [SerializeField] private CanvasGroup canvasGroup;
    [Min(0.01f)] [SerializeField] private float fadeInDuration = 0.2f;
    [Tooltip("實際淡出時間會限制為淡入時間的一半以內。")]
    [Min(0f)] [SerializeField] private float fadeOutDuration = 0.1f;

    private sealed class SlotBinding
    {
        public ToolButtonGroupItemView view;
        public GroupOption option;
        public UnityAction listener;
        public bool isFirstSlot;
        public bool isBack;
    }

    private readonly List<SlotBinding> _bindings = new List<SlotBinding>();
    private readonly List<int> _history = new List<int>();
    private int _currentIndex = -1;
    private bool _initialized;
    private GameStatusService _service;
    private ProgressFlagModel _flags;
    private bool _visible;
    private bool _fading;
    private float _fadeStartAlpha;
    private float _fadeElapsed;
    private float _fadeDuration;
    private readonly HashSet<string> _iconWarnings = new HashSet<string>();

    public string CurrentGroupName => CurrentGroup != null ? CurrentGroup.groupName : string.Empty;
    /// <summary>
    /// 唯讀導覽快照，供編輯器標示實際索引（同名群組也能正確區分）。
    /// 返回索引代表此刻呼叫 BackGroup 的目的地；-1 代表無有效目的地。
    /// 不初始化、不修改歷史，也不輸出重複名稱警告。
    /// </summary>
    public bool TryGetNavigationSnapshot(out int currentIndex, out int backIndex)
    {
        currentIndex = backIndex = -1;
        if (!Application.isPlaying || Instance != this || !_initialized || CurrentGroup == null) return false;
        currentIndex = _currentIndex;
        if (!string.IsNullOrWhiteSpace(CurrentGroup.backGroupName))
        {
            backIndex = FindGroup(CurrentGroup.backGroupName, false);
            return true;
        }
        for (int i = _history.Count - 1; i >= 0; i--)
        {
            int candidate = _history[i];
            if (candidate == _currentIndex || candidate < 0 || candidate >= groups.Count || groups[candidate] == null) continue;
            backIndex = candidate;
            break;
        }
        return true;
    }

    private ButtonGroup CurrentGroup => _currentIndex >= 0 && _currentIndex < groups.Count
        ? groups[_currentIndex] : null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[ToolButtonGroupDisplayControl] 場景存在多個控制器，停用重複元件。", this);
            enabled = false;
            return;
        }
        Instance = this;
        // 固定進場隱藏，不受 CanvasGroup 原始 Alpha 或舊版顯示設定影響。
        _visible = false;
        Initialize();
        ApplyVisibilityImmediately();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        if (canvasGroup == null)
            Debug.LogWarning("[ToolButtonGroupDisplayControl] 尚未指定 CanvasGroup，無法使用整體淡入淡出。", this);
        var iconNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var icon in icons)
        {
            if (icon == null || string.IsNullOrWhiteSpace(icon.name)) continue;
            if (!iconNames.Add(icon.name))
                Debug.LogWarning($"[ToolButtonGroupDisplayControl] Icon 名稱「{icon.name}」重複，使用第一筆。", this);
        }
        var seen = new HashSet<Button>();
        for (int i = 0; i < slots.Count; i++)
        {
            var view = slots[i];
            if (view == null || !view.IsConfigured || transform.IsChildOf(view.transform))
            {
                Debug.LogWarning("[ToolButtonGroupDisplayControl] 槽位無效：請確認 Button／文字位於視圖內、狀態圖為獨立 Image，且視圖不是控制器本身或其父層。已略過。", this);
                continue;
            }
            if (!seen.Add(view.Button))
            {
                Debug.LogWarning("[ToolButtonGroupDisplayControl] 固定按鈕重複指定，已略過。", this);
                continue;
            }
            var binding = new SlotBinding { view = view, isFirstSlot = i == 0 };
            binding.listener = () => HandleClick(binding);
            _bindings.Add(binding);
        }
        if (!_bindings.Exists(binding => binding.isFirstSlot)
            && groups.Exists(group => group != null && group.showBackButton))
            Debug.LogWarning("[ToolButtonGroupDisplayControl] 第一個 Slot 無效，無法顯示返回按鈕，請檢查 Slots 第 1 格的 ItemView 關聯。", this);

        _currentIndex = FindGroup("Main");
        if (_currentIndex < 0)
        {
            _currentIndex = groups.FindIndex(group => group != null);
            Debug.LogWarning("[ToolButtonGroupDisplayControl] 沒有 Main，改用第一組；若無任何群組則隱藏所有槽位。", this);
        }
    }

    private void OnEnable()
    {
        if (Instance != this) return;
        Initialize();
        ApplyVisibilityImmediately();
        foreach (var binding in _bindings)
            if (binding.view != null) binding.view.Bind(binding.listener);
        _service = GameStatusService.Instance;
        _flags = _service != null ? _service.ProgressFlags : null;
        if (_flags != null)
        {
            _flags.OnFlagChanged += HandleFlagChanged;
            _flags.OnVariableChanged += HandleVariableChanged;
        }
        if (_service != null) _service.OnGameStatusLoaded += Refresh;
        PixelCrushers.UILocalizationManager.languageChanged += HandleLanguageChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (Instance != this) return;
        _fading = false;
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        foreach (var binding in _bindings)
        {
            if (binding.view != null) binding.view.Unbind();
            binding.option = null;
            binding.isBack = false;
        }
        if (_flags != null)
        {
            _flags.OnFlagChanged -= HandleFlagChanged;
            _flags.OnVariableChanged -= HandleVariableChanged;
        }
        if (_service != null) _service.OnGameStatusLoaded -= Refresh;
        PixelCrushers.UILocalizationManager.languageChanged -= HandleLanguageChanged;
        _service = null;
        _flags = null;
    }

    private void HandleFlagChanged(string id, bool active) => Refresh();
    private void HandleVariableChanged(string key, int value) => Refresh();
    private void HandleLanguageChanged(string language) => Refresh();

    private int FindGroup(string groupName, bool reportDuplicates = true)
    {
        if (string.IsNullOrWhiteSpace(groupName)) return -1;
        int first = -1;
        int count = 0;
        for (int i = 0; i < groups.Count; i++)
        {
            if (groups[i] == null || !string.Equals(groups[i].groupName, groupName, StringComparison.Ordinal)) continue;
            if (first < 0) first = i;
            count++;
        }
        if (count > 1 && reportDuplicates)
            Debug.LogWarning($"[ToolButtonGroupDisplayControl] 群組名稱「{groupName}」重複 {count} 次，使用第一個。", this);
        return first;
    }

    /// <summary>前往指定組；成功切換到不同組時，將原組加入返回歷史。</summary>
    public void ShowGroup(string groupName)
    {
        Initialize();
        int target = FindGroup(groupName);
        if (target < 0)
        {
            WarnMissingGroup(groupName);
            return;
        }
        if (target != _currentIndex)
        {
            if (CurrentGroup != null) _history.Add(_currentIndex);
            _currentIndex = target;
        }
        Refresh();
    }

    /// <summary>逐層返回，不把剛離開的組重新加入歷史；無歷史時保持原組。</summary>
    public void LastGroup()
    {
        Initialize();
        while (_history.Count > 0)
        {
            int last = _history.Count - 1;
            int target = _history[last];
            _history.RemoveAt(last);
            if (target == _currentIndex || target < 0 || target >= groups.Count || groups[target] == null) continue;
            _currentIndex = target;
            Refresh();
            return;
        }
    }

    public void BackGroup()
    {
        Initialize();
        if (CurrentGroup == null || string.IsNullOrWhiteSpace(CurrentGroup.backGroupName)) LastGroup();
        else ReturnToGroup(CurrentGroup.backGroupName);
    }

    public void MainGroup()
    {
        Initialize();
        ReturnToGroup("Main");
    }

    private void ReturnToGroup(string name)
    {
        int target = FindGroup(name);
        if (target < 0)
        {
            WarnMissingGroup(name);
            return;
        }
        if (target != _currentIndex)
        {
            // 返回歷史中的目的地時，捨棄目的地之後的路徑，避免來回跳轉。
            // 目的地不在歷史中時清空舊路徑，從該組重新開始導覽。
            int position = _history.LastIndexOf(target);
            if (position < 0) _history.Clear();
            else _history.RemoveRange(position, _history.Count - position);
            _currentIndex = target;
        }
        Refresh();
    }

    private void WarnMissingGroup(string name) =>
        Debug.LogWarning($"[ToolButtonGroupDisplayControl] 找不到群組「{name}」，保持目前群組。", this);

    public void Refresh()
    {
        if (!isActiveAndEnabled || Instance != this) return;
        Initialize();
        bool showBack = CurrentGroup != null && CurrentGroup.showBackButton;
        var options = CurrentGroup?.options;
        int next = 0;
        foreach (var binding in _bindings)
        {
            binding.option = null;
            binding.isBack = false;
            if (binding.view == null || !binding.view.IsConfigured) continue;
            binding.isBack = showBack && binding.isFirstSlot;
            binding.view.SetOffset(showBack && !binding.isFirstSlot
                ? new Vector2(Mathf.Max(0f, submenuIndent), 0f) : Vector2.zero);
            if (binding.isBack)
            {
                // 返回占用第一格，不消耗選項；事件沿用同一個監聽，依目前角色分流。
                binding.view.Present(Localize(backTextKey), true, ResolveIcon("Back"), false,
                    true, backBackgroundColor);
                continue;
            }
            GroupOption option = null;
            while (options != null && next < options.Count)
            {
                var candidate = options[next++];
                if (candidate == null || !Evaluate(candidate.visibilityFlag, candidate.invertVisibility)) continue;
                option = candidate;
                break;
            }
            binding.option = option;
            if (option == null)
            {
                binding.view.Hide();
                continue;
            }
            bool isNg = IsNg(option);
            binding.view.Present(Localize(option.textKey),
                Evaluate(option.interactableFlag, option.invertInteractable),
                ResolveIcon(isNg ? "Ng" : option.iconName), isNg);
        }
    }

    /// <summary>外觀與點擊共用 NG 判斷；Icon 為 Ng 時強制 NG，其他情況依 Flag 判斷。</summary>
    private bool IsNg(GroupOption option)
    {
        if (option == null) return false;
        if (string.Equals(option.iconName, "Ng", StringComparison.Ordinal)) return true;
        return option.ngFlag != null
            && Evaluate(option.ngFlag, option.invertNgFlag);
    }

    private bool Evaluate(ProgressFlagDefinition flag, bool invert)
    {
        if (flag == null) return true;
        var service = GameStatusService.Instance;
        if (service == null || service.ProgressFlags == null) return false;
        bool has = service.ProgressFlags.Contains(flag.FlagID);
        return invert ? !has : has;
    }

    private string Localize(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        string text = PixelCrushers.DialogueSystem.DialogueManager.GetLocalizedText(key);
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning($"[ToolButtonGroupDisplayControl] Text Table 找不到 Key: {key}", this);
            return key;
        }
        return text;
    }

    private void HandleClick(SlotBinding binding)
    {
        var option = binding.option;
        if (!CanReceiveInput || binding.view == null
            || !binding.view.isActiveAndEnabled || binding.view.Button == null
            || !binding.view.Button.isActiveAndEnabled || !binding.view.Button.IsInteractable()) return;
        if (binding.isBack)
        {
            if (CurrentGroup != null && CurrentGroup.showBackButton) BackGroup();
            return;
        }
        if (option == null) return;
        if (!Evaluate(option.visibilityFlag, option.invertVisibility)
            || !Evaluate(option.interactableFlag, option.invertInteractable))
        {
            Refresh();
            return;
        }
        // 以點擊當下的 Icon 與 Flag 決定分支，一次點擊只執行其中一個事件。
        if (option.useNgClickEvent && IsNg(option)) option.onNgClick?.Invoke();
        else option.onClick?.Invoke();
        // 事件可切組、改 Flag、關閉面板或銷毀控制器；刷新時使用最新群組。
        if (this != null && isActiveAndEnabled) Refresh();
    }

    private bool CanReceiveInput => isActiveAndEnabled && Instance == this && _visible && !_fading
        && (canvasGroup == null || (canvasGroup.interactable && canvasGroup.blocksRaycasts));

    private Sprite ResolveIcon(string name)
    {
        foreach (var entry in icons)
        {
            if (entry == null || entry.name != name) continue;
            if (entry.sprite != null) return entry.sprite;
            break;
        }
        if (_iconWarnings.Add(name ?? string.Empty))
            Debug.LogWarning($"[ToolButtonGroupDisplayControl] Icon「{name}」不存在或未指定 Sprite。", this);
        return null;
    }

    /// <summary>淡入目前群組；不重設群組與歷史。</summary>
    public void Show() => SetVisible(true);

    /// <summary>淡出並立即停用輸入，保留物件啟用以供單例再次呼叫。</summary>
    public void Hide() => SetVisible(false);

    private void SetVisible(bool visible)
    {
        if (Instance != this) return;
        if (canvasGroup == null)
        {
            Debug.LogError("[ToolButtonGroupDisplayControl] Show／Hide 需要先指定 CanvasGroup。", this);
            return;
        }
        if (_visible == visible && (_fading || Mathf.Approximately(canvasGroup.alpha, visible ? 1f : 0f))) return;
        _visible = visible;
        _fadeStartAlpha = canvasGroup.alpha;
        _fadeElapsed = 0f;
        float duration = visible ? Mathf.Max(0.01f, fadeInDuration)
            : Mathf.Min(Mathf.Max(0f, fadeOutDuration), Mathf.Max(0.01f, fadeInDuration) * 0.5f);
        _fadeDuration = duration * Mathf.Abs((visible ? 1f : 0f) - _fadeStartAlpha);
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        _fading = _fadeDuration > 0f && isActiveAndEnabled;
        if (!_fading) ApplyVisibilityImmediately();
        if (visible) Refresh();
    }

    private void Update()
    {
        if (!_fading || canvasGroup == null) return;
        _fadeElapsed += Time.unscaledDeltaTime;
        canvasGroup.alpha = Mathf.Lerp(_fadeStartAlpha, _visible ? 1f : 0f, _fadeElapsed / _fadeDuration);
        if (_fadeElapsed >= _fadeDuration) ApplyVisibilityImmediately();
    }

    private void ApplyVisibilityImmediately()
    {
        _fading = false;
        if (canvasGroup == null) return;
        canvasGroup.alpha = _visible ? 1f : 0f;
        canvasGroup.interactable = _visible && isActiveAndEnabled;
        canvasGroup.blocksRaycasts = _visible && isActiveAndEnabled;
    }
}
