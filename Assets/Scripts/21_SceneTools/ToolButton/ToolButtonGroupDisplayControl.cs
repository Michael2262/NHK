using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 群組選單顯示控制器：共用固定槽位，依選項順位、Flag 填入文字、圖示及事件。
/// 首次開啟選擇 Main（不存在則第一組）；面板重開保留目前群組與返回歷史。
/// 群組與歷史僅供此 UI 使用，不寫入遊戲存檔。
/// </summary>
[DisallowMultipleComponent]
public class ToolButtonGroupDisplayControl : MonoBehaviour
{
    [Serializable]
    public class GroupOption
    {
        [Tooltip("Inspector 備註。")]
        public string label;
        [Tooltip("Text Table 的多語系文字 Key。")]
        public string textKey;
        [Header("是否出現（留空永遠成立）")]
        public ProgressFlagDefinition visibilityFlag;
        public bool invertVisibility;
        [Header("是否可點（留空永遠成立，不可點仍占槽位）")]
        public ProgressFlagDefinition interactableFlag;
        public bool invertInteractable;
        [Header("狀態圖示")]
        [Tooltip("true 顯示 OK，false 顯示 NG；未設定則隱藏圖示。")]
        public ProgressFlagDefinition statusFlag;
        [Tooltip("此 Flag 為 true 時優先隱藏圖示；留空則不阻擋。")]
        public ProgressFlagDefinition noImageFlag;
        [Header("點擊事件")]
        public UnityEvent onClick = new UnityEvent();
    }

    [Serializable]
    public class ButtonGroup
    {
        [Tooltip("呼叫 ShowGroup 時使用的名稱，區分大小寫。Main 為首頁。")]
        public string groupName;
        [Tooltip("BackGroup 的目的地；留空使用 LastGroup。")]
        public string backGroupName;
        [Tooltip("越前面的選項順位越高。")]
        public List<GroupOption> options = new List<GroupOption>();
    }

    [Header("固定按鈕（有效槽位數即顯示上限）")]
    [SerializeField] private List<ToolButtonGroupItemView> slots = new List<ToolButtonGroupItemView>();
    [Header("群組")]
    [SerializeField] private List<ButtonGroup> groups = new List<ButtonGroup>();

    private sealed class SlotBinding
    {
        public ToolButtonGroupItemView view;
        public GroupOption option;
        public UnityAction listener;
    }

    private readonly List<SlotBinding> _bindings = new List<SlotBinding>();
    private readonly List<int> _history = new List<int>();
    private int _currentIndex = -1;
    private bool _initialized;
    private GameStatusService _service;
    private ProgressFlagModel _flags;

    public string CurrentGroupName => CurrentGroup != null ? CurrentGroup.groupName : string.Empty;
    private ButtonGroup CurrentGroup => _currentIndex >= 0 && _currentIndex < groups.Count
        ? groups[_currentIndex] : null;

    private void Awake() => Initialize();

    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        var seen = new HashSet<Button>();
        foreach (var view in slots)
        {
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
            var binding = new SlotBinding { view = view };
            binding.listener = () => HandleClick(binding);
            _bindings.Add(binding);
        }

        _currentIndex = FindGroup("Main");
        if (_currentIndex < 0)
        {
            _currentIndex = groups.FindIndex(group => group != null);
            Debug.LogWarning("[ToolButtonGroupDisplayControl] 沒有 Main，改用第一組；若無任何群組則隱藏所有槽位。", this);
        }
    }

    private void OnEnable()
    {
        Initialize();
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
        foreach (var binding in _bindings)
        {
            if (binding.view != null) binding.view.Unbind();
            binding.option = null;
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

    private int FindGroup(string groupName)
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
        if (count > 1)
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
        if (!isActiveAndEnabled) return;
        Initialize();
        var options = CurrentGroup?.options;
        int next = 0;
        foreach (var binding in _bindings)
        {
            if (binding.view == null || !binding.view.IsConfigured) continue;
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
            var status = ToolButtonGroupItemView.StatusDisplay.Hidden;
            if (option.statusFlag != null && !HasFlag(option.noImageFlag))
                status = HasFlag(option.statusFlag)
                    ? ToolButtonGroupItemView.StatusDisplay.OK
                    : ToolButtonGroupItemView.StatusDisplay.NG;
            binding.view.Present(Localize(option.textKey),
                Evaluate(option.interactableFlag, option.invertInteractable), status);
        }
    }

    private bool HasFlag(ProgressFlagDefinition flag)
    {
        var service = GameStatusService.Instance;
        return flag != null && service != null && service.ProgressFlags != null
            && service.ProgressFlags.Contains(flag.FlagID);
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
        if (!isActiveAndEnabled || option == null || binding.view == null
            || !binding.view.isActiveAndEnabled || binding.view.Button == null
            || !binding.view.Button.isActiveAndEnabled || !binding.view.Button.IsInteractable()) return;
        if (!Evaluate(option.visibilityFlag, option.invertVisibility)
            || !Evaluate(option.interactableFlag, option.invertInteractable))
        {
            Refresh();
            return;
        }
        option.onClick?.Invoke();
        // 事件可切組、改 Flag、關閉面板或銷毀控制器；刷新時使用最新群組。
        if (this != null && isActiveAndEnabled) Refresh();
    }
}
