using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>滿值倒數與動態中央選單；逾時一律依順位選擇第一個可用選項。</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(CanvasGroup))]
public class ShootButtonGroup : MonoBehaviour
{
    public static ShootButtonGroup Instance { get; private set; }

    [Serializable]
    public class ButtonAction : ToolButtonGroupDisplayControl.GroupOption
    {
        [Tooltip("切換用 ID，區分大小寫。")]
        public string id;
        // 繼承的 onClick 保持原序列化路徑；本身即第一個選項，不需要重綁舊事件。
        [Tooltip("第一個選項之後的其他選項，依清單順序生成按鈕。")]
        public List<ToolButtonGroupDisplayControl.GroupOption> options = new List<ToolButtonGroupDisplayControl.GroupOption>();
    }

    [Header("按鈕與次數顯示")]
    [Tooltip("點擊開啟中央選單，倒數不重置。請移除舊版取消倒數或直接 Shoot 的 On Click 綁定。")]
    [SerializeField] private Button shootButton;
    [Tooltip("只顯示 (剩餘/每日初始值)，請使用獨立 TMP，勿掛 LocalizeUI。")]
    [SerializeField] private TMP_Text shootTimesText;
    [SerializeField] private List<ButtonAction> groups = new List<ButtonAction>();
    [Tooltip("初始化時套用的組名／ID，必須與 Groups 其中一筆完全相同；留空不指定 ID，不會自動顯示。")]
    [SerializeField] private string defaultID;
    [Header("滿值閃爍（Button 使用 Color Tint）")]
    [SerializeField] private Color flashColorA = new Color(156f / 255f, 253f / 255f, 1f, 1f);
    [SerializeField] private Color flashColorB = Color.white;
    [Tooltip("每秒交替的完整週期數，使用不受時間縮放影響的時間。")]
    [Min(0.1f)] [SerializeField] private float flashFrequency = 4f;
    [Tooltip("整數倒數的總秒數。開啟中央選單不重置時間；使用不受時間縮放影響的時間。")]
    [Min(0.01f)] [SerializeField] private float flashDuration = 2f;
    [Header("中央選單與倒數")]
    [Tooltip("主按鈕點擊後等待幾秒才開啟中央選單，使用不受時間縮放影響的時間。等待期間倒數繼續；若先到期則依未開啟選單的規則結算。設為 0 可立即開啟。")]
    [Min(0f)] [SerializeField] private float menuOpenDelay = 1.5f;
    [Tooltip("獨立中央面板，不可與整體 CanvasGroup 相同，也不可包含主按鈕。保持物件啟用。")]
    [SerializeField] private CanvasGroup optionsCanvasGroup;
    [Tooltip("Project 內的按鈕 Prefab，根物件需掛 ToolButtonGroupItemView 並完成 Button、Text、Icon Image 綁定。Button 的 On Click 留空。")]
    [SerializeField] private ToolButtonGroupItemView shootButtonPrefab;
    [Tooltip("中央面板內的 Content；按鈕在此生成並重複使用，位置與尺寸交給此容器的 Layout。")]
    [SerializeField] private RectTransform optionsContent;
    [Tooltip("倒數整體物件，包含文字、背景與裝飾；一起啟用或停用。請放在中央選單外，不可包含 ShootButtonGroup 控制器。未指定時使用 Countdown Text 所在物件。")]
    [SerializeField] private GameObject countdownRoot;
    [Tooltip("倒數整體物件內的數字文字，勿掛 LocalizeUI。")]
    [SerializeField] private TMP_Text countdownText;
    [Tooltip("中央選單開啟後使用的倒數整體物件，包含背景與裝飾；不可指定選單本身或控制器的父物件。未指定時使用 Menu Countdown Text 所在物件。")]
    [SerializeField] private GameObject menuCountdownRoot;
    [Tooltip("中央選單內另一個位置的倒數文字，延續原本剩餘時間，勿掛 LocalizeUI。")]
    [SerializeField] private TMP_Text menuCountdownText;
    [SerializeField] private List<ToolButtonGroupDisplayControl.IconEntry> icons = new List<ToolButtonGroupDisplayControl.IconEntry>
    {
        new ToolButtonGroupDisplayControl.IconEntry { name = "Heart" },
        new ToolButtonGroupDisplayControl.IconEntry { name = "Ng" },
        new ToolButtonGroupDisplayControl.IconEntry { name = "Menu" },
        new ToolButtonGroupDisplayControl.IconEntry { name = "Question" }
    };
    [Header("整體顯示")]
    [SerializeField] private CanvasGroup canvasGroup;
    [Min(0.01f)] [SerializeField] private float fadeInDuration = 0.2f;
    [Tooltip("淡出時間最多為淡入時間的一半。")]
    [Min(0f)] [SerializeField] private float fadeOutDuration = 0.1f;

    private ButtonAction _currentGroup;
    private GameStatusService _service;
    private ProtagonistStatusModel _protagonist;
    private Color _normalTextColor;
    private bool _configured;
    private bool _visible;
    private bool _fading;
    private bool _canClick;
    private float _visibility;
    private float _fadeStart;
    private float _fadeElapsed;
    private float _fadeDuration;
    private bool _flashDismissed;
    private bool _flashApplied;
    private float _flashElapsed;
    private ColorBlock _originalButtonColors;
    private ProgressFlagModel _flags;
    private bool _menuOpen;
    private bool _menuOpening;
    private float _menuOpenElapsed;
    private bool _settling;
    private float _zeroElapsed;
    private UnityEvent _pendingEvent;
    private readonly List<SlotBinding> _bindings = new List<SlotBinding>();
    private readonly HashSet<string> _iconWarnings = new HashSet<string>();
    private bool _optionsConfigured;
    private bool _countdownRootWarning;

    private sealed class SlotBinding
    {
        public ToolButtonGroupItemView view;
        public ToolButtonGroupDisplayControl.GroupOption option;
    }

    public string CurrentID => _currentGroup != null ? _currentGroup.id : null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[ShootButtonGroup] 場景存在多個控制器，停用重複元件。", this);
            enabled = false;
            return;
        }
        Instance = this;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        _configured = shootButton != null && canvasGroup != null
            && shootButton.transform.IsChildOf(canvasGroup.transform);
        if (!_configured)
            Debug.LogError("[ShootButtonGroup] 請指定 Button，且必須位於 CanvasGroup 本身或子階層。", this);
        if (shootTimesText != null) _normalTextColor = shootTimesText.color;
        InitializeOptions();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in groups)
        {
            if (group == null || string.IsNullOrWhiteSpace(group.id)) continue;
            if (!ids.Add(group.id))
                Debug.LogWarning($"[ShootButtonGroup] ID「{group.id}」重複，切換時使用第一筆。", this);
        }
        SetID(defaultID);
    }

    private void OnEnable()
    {
        if (Instance != this) return;
        if (_configured) shootButton.onClick.AddListener(HandleClick);
        _service = GameStatusService.Instance;
        _protagonist = _service != null ? _service.Protagonist : null;
        _flags = _service != null ? _service.ProgressFlags : null;
        if (_flags != null)
        {
            _flags.OnFlagChanged += HandleFlagChanged;
            _flags.OnVariableChanged += HandleVariableChanged;
        }
        PixelCrushers.UILocalizationManager.languageChanged += HandleLanguageChanged;
        foreach (var binding in _bindings)
            if (binding.view != null) binding.view.Bind(() => HandleOptionClick(binding));
        if (_protagonist != null)
        {
            _protagonist.OnSemenChanged += HandleValueChanged;
            _protagonist.OnShootTimesChanged += HandleValueChanged;
        }
        if (_service != null) _service.OnGameStatusLoaded += HandleGameStatusLoaded;
        _visibility = _visible ? 1f : 0f;
        Refresh();
    }

    private void OnDisable()
    {
        if (Instance != this) return;
        StopFlashing();
        foreach (var binding in _bindings)
            if (binding.view != null) binding.view.Unbind();
        if (_flags != null)
        {
            _flags.OnFlagChanged -= HandleFlagChanged;
            _flags.OnVariableChanged -= HandleVariableChanged;
        }
        _flags = null;
        PixelCrushers.UILocalizationManager.languageChanged -= HandleLanguageChanged;
        if (shootButton != null) shootButton.onClick.RemoveListener(HandleClick);
        if (_protagonist != null)
        {
            _protagonist.OnSemenChanged -= HandleValueChanged;
            _protagonist.OnShootTimesChanged -= HandleValueChanged;
        }
        if (_service != null) _service.OnGameStatusLoaded -= HandleGameStatusLoaded;
        _service = null;
        _protagonist = null;
        _fading = false;
        _canClick = false;
        ApplyPresentation();
    }

    private void OnDestroy()
    {
        // 面板可在控制器以外的階層；只清理由本控制器生成的物件。
        foreach (var binding in _bindings)
        {
            if (binding.view == null) continue;
            binding.view.Unbind();
            Destroy(binding.view.gameObject);
        }
        _bindings.Clear();
        if (Instance == this) Instance = null;
    }

    /// <summary>切換事件，不自動顯示；空白或不存在的 ID 會清除目前事件。</summary>
    public void SetID(string id)
    {
        if (Instance != this) return;
        // 到期已鎖定的選擇不可被切組帶到下一組；一般倒數切組則保留剩餘時間。
        if (_settling) StopFlashing();
        bool hadGroup = _currentGroup != null;
        _currentGroup = null;
        if (!string.IsNullOrWhiteSpace(id))
        {
            _currentGroup = groups.Find(group => group != null
                && string.Equals(group.id, id, StringComparison.Ordinal));
            if (_currentGroup == null)
                Debug.LogWarning($"[ShootButtonGroup] 找不到 ID「{id}」，已清除目前 ID。", this);
        }
        if (_currentGroup == null) StopFlashing();
        else if (!hadGroup)
        {
            _flashDismissed = false;
            _flashElapsed = 0f;
        }
        Refresh();
    }

    public void RemoveID() => SetID(null);
    public void removeID() => RemoveID();
    public void Show() => SetVisible(true);
    public void Hide() => SetVisible(false);
    public void show() => Show();
    public void hide() => Hide();

    private void HandleValueChanged(int value) => Refresh();

    private void HandleGameStatusLoaded()
    {
        StopFlashing();
        _flashDismissed = false;
        _flashElapsed = 0f;
        Refresh();
    }

    /// <summary>滿值且可用時重新啟動閃爍與自動 Shoot 倒數，不修改主角數值。</summary>
    public void StartFlashing()
    {
        if (Instance != this || !isActiveAndEnabled || !CanClick()) return;
        StopFlashing();
        _flashDismissed = false;
        _flashElapsed = 0f;
        Refresh();
    }

    /// <summary>取消這一次閃爍與自動 Shoot；降到未滿後再次滿值，或呼叫 StartFlashing 才會重新啟動。</summary>
    public void StopFlashing()
    {
        if (Instance != this) return;
        _flashDismissed = true;
        _settling = false;
        _pendingEvent = null;
        _menuOpen = false;
        _menuOpening = false;
        _menuOpenElapsed = 0f;
        RestoreButtonColors();
        RenderOptions();
        RenderCountdown(false);
    }

    private bool CanClick()
    {
        var service = GameStatusService.Instance;
        var protagonist = service != null ? service.Protagonist : null;
        return _currentGroup != null && protagonist != null
            && protagonist.Semen == protagonist.SemenMax;
    }

    public void Refresh()
    {
        if (Instance != this) return;
        var service = GameStatusService.Instance;
        var protagonist = service != null ? service.Protagonist : null;
        if (protagonist == null || protagonist.Semen != protagonist.SemenMax)
        {
            StopFlashing();
            _flashDismissed = false;
            _flashElapsed = 0f;
        }
        if (shootTimesText != null)
        {
            shootTimesText.text = protagonist != null
                ? $"({protagonist.ShootTimes}/{ProtagonistStatusModel.SHOOT_TIMES_MAX})"
                : $"(--/{ProtagonistStatusModel.SHOOT_TIMES_MAX})";
            // 使用整個 TMP 的顏色，括號、斜線與每日初始值也一起變紅。
            shootTimesText.color = protagonist != null && protagonist.ShootTimes < 0
                ? new Color(1f, 0f, 0f, _normalTextColor.a) : _normalTextColor;
        }
        _canClick = _configured && isActiveAndEnabled && CanClick();
        ApplyPresentation();
        RenderOptions();
    }

    private void HandleClick()
    {
        if (!CanChoose || _menuOpen || _menuOpening || !_optionsConfigured || optionsCanvasGroup == null
            || optionsContent == null || shootButtonPrefab == null) return;
        _menuOpening = true;
        _menuOpenElapsed = 0f;
        RestoreButtonColors();
        if (shootButton != null) shootButton.interactable = false;
        if (menuOpenDelay <= 0f) OpenMenu();
    }

    private void OpenMenu()
    {
        _menuOpening = false;
        _menuOpen = true;
        RenderOptions();
        RenderCountdown(true);
    }

    public void SetVisible(bool visible)
    {
        if (Instance != this || canvasGroup == null) return;
        if (!visible) StopFlashing();
        if (_visible == visible)
        {
            Refresh();
            return;
        }
        _visible = visible;
        _fadeStart = _visibility;
        _fadeElapsed = 0f;
        float duration = visible ? Mathf.Max(0.01f, fadeInDuration)
            : Mathf.Min(Mathf.Max(0f, fadeOutDuration), Mathf.Max(0.01f, fadeInDuration) * 0.5f);
        _fadeDuration = duration * Mathf.Abs((visible ? 1f : 0f) - _fadeStart);
        _fading = _fadeDuration > 0f && isActiveAndEnabled;
        if (!_fading) _visibility = visible ? 1f : 0f;
        Refresh();
    }

    private void Update()
    {
        if (!_fading) return;
        _fadeElapsed += Time.unscaledDeltaTime;
        _visibility = Mathf.Lerp(_fadeStart, _visible ? 1f : 0f, _fadeElapsed / _fadeDuration);
        if (_fadeElapsed >= _fadeDuration)
        {
            _fading = false;
            _visibility = _visible ? 1f : 0f;
        }
        ApplyPresentation();
    }

    private void ApplyPresentation()
    {
        bool interactable = _canClick && _visible && !_fading && isActiveAndEnabled && !_flashDismissed && !_settling && !_menuOpen && !_menuOpening;
        if (shootButton != null) shootButton.interactable = interactable;
        if (canvasGroup == null) return;
        // CanvasGroup 只控制顯示淡入淡出；不可用外觀交由 Button 的 Disabled 設定。
        canvasGroup.alpha = _visibility;
        canvasGroup.interactable = _visible && !_fading && _configured && isActiveAndEnabled;
        canvasGroup.blocksRaycasts = _visible && _configured && isActiveAndEnabled;
        UpdateFlashing(0f);
    }

    private void LateUpdate() => UpdateFlashing(Time.unscaledDeltaTime);

    private void UpdateFlashing(float deltaTime)
    {
        if (Instance != this) return;
        if (_settling)
        {
            _zeroElapsed += deltaTime;
            if (_zeroElapsed >= 0.1f)
            {
                var action = _pendingEvent;
                StopFlashing();
                ApplyPresentation();
                action?.Invoke();
                if (this != null) Refresh();
            }
            return;
        }
        bool shouldFlash = _canClick && !_flashDismissed && _visible && !_fading
            && isActiveAndEnabled && shootButton != null && shootButton.isActiveAndEnabled;
        if (!shouldFlash)
        {
            RestoreButtonColors();
            UpdateOptionFlashing(false);
            RenderCountdown(false);
            return;
        }
        _flashElapsed += deltaTime;
        RenderCountdown(true);
        // 只在逐幀更新完成倒數，避免 Refresh 或事件重入時同步觸發 Shoot。
        if (deltaTime > 0f && _flashElapsed >= Mathf.Max(0.01f, flashDuration))
        {
            if (!CanClick())
            {
                Refresh();
                return;
            }
            ResolveTimeout();
            return;
        }
        if (_menuOpening)
        {
            _menuOpenElapsed += deltaTime;
            RestoreButtonColors();
            if (_menuOpenElapsed >= Mathf.Max(0f, menuOpenDelay)) OpenMenu();
            return;
        }
        if (_menuOpen)
        {
            RestoreButtonColors();
            UpdateOptionFlashing(true);
            return;
        }
        if (shootButton.transition != Selectable.Transition.ColorTint) return;
        if (!_flashApplied)
        {
            _originalButtonColors = shootButton.colors;
            _flashApplied = true;
        }
        Color tint = CurrentFlashColor();
        // 透過 Button 自己的狀態色閃爍，避免另一個圖片動畫與 Color Tint 搶控制權。
        // Disabled 保持原設定；滑鼠移入、選取、按住時沿用同一組閃爍色。
        var colors = _originalButtonColors;
        colors.normalColor = colors.highlightedColor = colors.pressedColor = colors.selectedColor = tint;
        colors.fadeDuration = 0f;
        shootButton.colors = colors;
    }

    private void RestoreButtonColors()
    {
        if (!_flashApplied) return;
        if (shootButton != null) shootButton.colors = _originalButtonColors;
        _flashApplied = false;
    }

    private Color CurrentFlashColor()
    {
        float pulse = 0.5f - 0.5f * Mathf.Cos(_flashElapsed * Mathf.Max(0.1f, flashFrequency) * Mathf.PI * 2f);
        return Color.Lerp(flashColorA, flashColorB, pulse);
    }

    private void UpdateOptionFlashing(bool flashing)
    {
        Color color = CurrentFlashColor();
        foreach (var binding in _bindings)
        {
            if (binding.view == null) continue;
            if (flashing && binding.option != null && binding.view.isActiveAndEnabled)
                binding.view.SetBackgroundFlash(color);
            else binding.view.StopBackgroundFlash();
        }
    }

    private bool CanChoose => Instance == this && isActiveAndEnabled && _configured
        && _visible && !_fading && !_flashDismissed && !_settling && CanClick();

    private void InitializeOptions()
    {
        RenderCountdown(false);
        if (optionsCanvasGroup == null) return;
        if (optionsCanvasGroup == canvasGroup || transform.IsChildOf(optionsCanvasGroup.transform)
            || (shootButton != null && shootButton.transform.IsChildOf(optionsCanvasGroup.transform)))
        {
            Debug.LogError("[ShootButtonGroup] 中央面板必須獨立，不可包含控制器或主按鈕。", this);
            optionsCanvasGroup = null;
            return;
        }
        if (optionsContent == null || !optionsContent.IsChildOf(optionsCanvasGroup.transform))
        {
            Debug.LogError("[ShootButtonGroup] 請指定中央面板內的 Options Content，並在容器設定 Layout。", this);
        }
        else if (shootButtonPrefab == null || !shootButtonPrefab.IsConfigured
            || !shootButtonPrefab.enabled || shootButtonPrefab.transform.parent != null
            || shootButtonPrefab.gameObject.scene.IsValid())
        {
            Debug.LogError("[ShootButtonGroup] 請指定 Project 內的按鈕 Prefab，根物件需有啟用且完成關聯的 ToolButtonGroupItemView。", this);
        }
        else _optionsConfigured = true;
        RenderOptions();
    }

    private void EnsureButtonCount(int count)
    {
        if (!_optionsConfigured || shootButtonPrefab == null || optionsContent == null) return;
        // 外部若銷毀某顆按鈕，下一次刷新會補回，不保留失效的事件綁定。
        _bindings.RemoveAll(binding => binding.view == null);
        while (_bindings.Count < count)
        {
            var view = Instantiate(shootButtonPrefab, optionsContent, false);
            view.name = shootButtonPrefab.name + "_" + (_bindings.Count + 1);
            view.Hide();
            var binding = new SlotBinding { view = view };
            _bindings.Add(binding);
            view.Bind(() => HandleOptionClick(binding));
        }
    }

    private IEnumerable<ToolButtonGroupDisplayControl.GroupOption> CurrentOptions()
    {
        if (_currentGroup == null) yield break;
        yield return _currentGroup;
        if (_currentGroup.options == null) yield break;
        foreach (var option in _currentGroup.options)
            if (option != null) yield return option;
    }

    private bool Evaluate(ProgressFlagDefinition flag, bool invert)
    {
        if (flag == null) return true;
        var flags = GameStatusService.Instance != null ? GameStatusService.Instance.ProgressFlags : null;
        if (flags == null) return false;
        bool value = flags.Contains(flag.FlagID);
        return invert ? !value : value;
    }

    private bool IsEligible(ToolButtonGroupDisplayControl.GroupOption option) => option != null
        && Evaluate(option.visibilityFlag, option.invertVisibility)
        && Evaluate(option.interactableFlag, option.invertInteractable);

    private bool IsNg(ToolButtonGroupDisplayControl.GroupOption option) => option.iconName == "Ng"
        || (option.ngFlag != null && Evaluate(option.ngFlag, option.invertNgFlag));

    private UnityEvent ResolveEvent(ToolButtonGroupDisplayControl.GroupOption option) => option == null
        ? null : option.useNgClickEvent && IsNg(option) ? option.onNgClick : option.onClick;

    private void HandleFlagChanged(string id, bool value) => Refresh();
    private void HandleVariableChanged(string id, int value) => Refresh();
    private void HandleLanguageChanged(string language) => Refresh();

    private void RenderOptions()
    {
        bool shown = _menuOpen && _visible && isActiveAndEnabled && _optionsConfigured
            && optionsContent != null && optionsCanvasGroup != null;
        if (optionsCanvasGroup != null)
        {
            optionsCanvasGroup.alpha = shown ? 1f : 0f;
            optionsCanvasGroup.interactable = shown && !_settling;
            optionsCanvasGroup.blocksRaycasts = shown;
        }
        var visibleOptions = new List<ToolButtonGroupDisplayControl.GroupOption>();
        if (shown)
            foreach (var option in CurrentOptions())
                if (Evaluate(option.visibilityFlag, option.invertVisibility)) visibleOptions.Add(option);
        if (shown) EnsureButtonCount(visibleOptions.Count);
        for (int i = 0; i < _bindings.Count; i++)
        {
            var binding = _bindings[i];
            binding.option = i < visibleOptions.Count ? visibleOptions[i] : null;
            if (binding.view == null) continue;
            if (binding.option == null) { binding.view.Hide(); continue; }
            // 依選項順序排到容器最後，確保按鈕位於既有背景／Blocker 上方。
            // 不使用從 0 開始的索引，避免把透明遮擋層推到按鈕前面攔截點擊。
            // 位置與尺寸仍交由 Layout 決定。
            binding.view.transform.SetAsLastSibling();
            var option = binding.option;
            bool ng = IsNg(option);
            binding.view.Present(Localize(option.textKey), !_settling && IsEligible(option),
                ResolveIcon(ng ? "Ng" : option.iconName), ng);
        }
        UpdateOptionFlashing(shown && !_settling && !_flashDismissed && !_fading && _canClick);
        if (shown) LayoutRebuilder.MarkLayoutForRebuild(optionsContent);
    }

    private string Localize(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        string text = PixelCrushers.DialogueSystem.DialogueManager.GetLocalizedText(key);
        if (!string.IsNullOrEmpty(text)) return text;
        Debug.LogWarning($"[ShootButtonGroup] Text Table 找不到 Key: {key}", this);
        return key;
    }

    private Sprite ResolveIcon(string name)
    {
        foreach (var icon in icons)
            if (icon != null && icon.name == name && icon.sprite != null) return icon.sprite;
        if (_iconWarnings.Add(name ?? string.Empty))
            Debug.LogWarning($"[ShootButtonGroup] Icon「{name}」不存在或未指定 Sprite。", this);
        return null;
    }

    private void RenderCountdown(bool shown)
    {
        RenderCountdownDisplay(countdownRoot, countdownText, shown && !_menuOpen);
        RenderCountdownDisplay(menuCountdownRoot, menuCountdownText, shown && _menuOpen);
    }

    private void RenderCountdownDisplay(GameObject root, TMP_Text text, bool shown)
    {
        if (text != null)
        {
            text.enabled = shown;
            text.text = shown ? Mathf.CeilToInt(Mathf.Max(0f, flashDuration - _flashElapsed)).ToString() : string.Empty;
        }
        if (root == null && text != null) root = text.gameObject;
        if (root == null) return;
        // 避免關閉控制器本身，否則倒數無法再更新或重新顯示。
        if (transform.IsChildOf(root.transform)
            || (optionsCanvasGroup != null && optionsCanvasGroup.transform.IsChildOf(root.transform)))
        {
            if (!_countdownRootWarning)
            {
                Debug.LogError("[ShootButtonGroup] 倒數 Root 不可包含控制器或整個中央選單，請指定獨立的倒數 UI 物件。", this);
                _countdownRootWarning = true;
            }
            return;
        }
        if (root.activeSelf != shown) root.SetActive(shown);
    }

    private void HandleOptionClick(SlotBinding binding)
    {
        if (!CanChoose || !_menuOpen || binding.view == null || !binding.view.isActiveAndEnabled
            || optionsCanvasGroup == null || !optionsCanvasGroup.interactable
            || !optionsCanvasGroup.blocksRaycasts || !binding.view.Button.isActiveAndEnabled
            || !binding.view.Button.IsInteractable()) return;
        if (!IsEligible(binding.option)) { Refresh(); return; }
        var action = ResolveEvent(binding.option);
        // 關閉 UI 與封鎖再次結算後才呼叫外部事件。
        StopFlashing();
        ApplyPresentation();
        action?.Invoke();
        if (this != null) Refresh();
    }

    private void ResolveTimeout()
    {
        ToolButtonGroupDisplayControl.GroupOption selected = null;
        // 無論中央選單是否開啟，皆依設定順序選擇第一個符合顯示與可點條件的選項。
        foreach (var option in CurrentOptions())
            if (IsEligible(option)) { selected = option; break; }
        _pendingEvent = ResolveEvent(selected);
        _menuOpening = false;
        _menuOpenElapsed = 0f;
        _settling = true;
        _zeroElapsed = 0f;
        _flashElapsed = Mathf.Max(0.01f, flashDuration);
        RestoreButtonColors();
        RenderCountdown(true);
        RenderOptions();
        if (shootButton != null) shootButton.interactable = false;
        if (selected == null)
            Debug.LogWarning("[ShootButtonGroup] 倒數結束時沒有可用選項，本次不執行事件。", this);
    }
}
