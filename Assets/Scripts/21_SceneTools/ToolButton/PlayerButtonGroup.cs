using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>全場唯一的固定播放操作列；ID 只切換事件與可點條件，不改動按鈕排列。</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(CanvasGroup))]
public class PlayerButtonGroup : MonoBehaviour
{
    public static PlayerButtonGroup Instance { get; private set; }

    [Serializable]
    public class ButtonAction
    {
        [Tooltip("可點條件；留空永遠成立。")]
        public ProgressFlagDefinition interactableFlag;
        public bool invertInteractable;
        public UnityEvent onClick = new UnityEvent();
    }

    [Serializable]
    public class ButtonGroup
    {
        [Tooltip("切換用 ID（組名），區分大小寫。")]
        public string id;
        [InspectorName("暫停")] public ButtonAction pause = new ButtonAction();
        [InspectorName("撥放")] public ButtonAction play = new ButtonAction();
        [InspectorName("快速")] public ButtonAction fast = new ButtonAction();
    }

    [Header("固定三顆按鈕（圖示／文字與位置直接在場景設定）")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button fastButton;
    [Header("各 ID 的條件與事件")]
    [SerializeField] private List<ButtonGroup> groups = new List<ButtonGroup>();
    [Header("整體顯示")]
    [SerializeField] private CanvasGroup canvasGroup;
    [Min(0.01f)] [SerializeField] private float fadeInDuration = 0.2f;
    [Tooltip("淡出時間最多為淡入時間的一半。")]
    [Min(0f)] [SerializeField] private float fadeOutDuration = 0.1f;

    private ButtonGroup _currentGroup;
    private GameStatusService _service;
    private ProgressFlagModel _flags;
    private bool _configured;
    private bool _visible;
    private bool _fading;
    private float _fadeStartAlpha;
    private float _fadeElapsed;
    private float _fadeDuration;

    public string CurrentID => _currentGroup != null ? _currentGroup.id : null;
    public string CurrentGroupName => CurrentID;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[PlayerButtonGroup] 場景存在多個控制器，停用重複元件。", this);
            enabled = false;
            return;
        }
        Instance = this;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        _configured = pauseButton != null && playButton != null && fastButton != null
            && pauseButton != playButton && pauseButton != fastButton && playButton != fastButton
            && pauseButton.transform.IsChildOf(canvasGroup.transform)
            && playButton.transform.IsChildOf(canvasGroup.transform)
            && fastButton.transform.IsChildOf(canvasGroup.transform);
        if (!_configured)
            Debug.LogError("[PlayerButtonGroup] 請指定三顆不同的 Button，且必須位於 CanvasGroup 本身或子階層。", this);
        _currentGroup = null;
        _visible = false;
        ApplyVisibilityImmediately();
        SetButtonsInteractable(false);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in groups)
        {
            if (group == null || string.IsNullOrWhiteSpace(group.id)) continue;
            if (!ids.Add(group.id))
                Debug.LogWarning($"[PlayerButtonGroup] ID「{group.id}」重複，切換時使用第一筆。", this);
        }
    }

    private void OnEnable()
    {
        if (Instance != this) return;
        if (_configured)
        {
            pauseButton.onClick.AddListener(HandlePause);
            playButton.onClick.AddListener(HandlePlay);
            fastButton.onClick.AddListener(HandleFast);
        }
        _service = GameStatusService.Instance;
        _flags = _service != null ? _service.ProgressFlags : null;
        if (_flags != null)
        {
            _flags.OnFlagChanged += HandleFlagChanged;
            _flags.OnVariableChanged += HandleVariableChanged;
        }
        if (_service != null) _service.OnGameStatusLoaded += Refresh;
        ApplyVisibilityImmediately();
        Refresh();
    }

    private void OnDisable()
    {
        if (Instance != this) return;
        if (pauseButton != null) pauseButton.onClick.RemoveListener(HandlePause);
        if (playButton != null) playButton.onClick.RemoveListener(HandlePlay);
        if (fastButton != null) fastButton.onClick.RemoveListener(HandleFast);
        if (_flags != null)
        {
            _flags.OnFlagChanged -= HandleFlagChanged;
            _flags.OnVariableChanged -= HandleVariableChanged;
        }
        if (_service != null) _service.OnGameStatusLoaded -= Refresh;
        _service = null;
        _flags = null;
        _fading = false;
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        SetButtonsInteractable(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>切換組名，不自動顯示。空白清除 ID；找不到 ID 也清除，避免觸發上一組。</summary>
    public void SetID(string id)
    {
        if (Instance != this) return;
        _currentGroup = null;
        if (!string.IsNullOrWhiteSpace(id))
        {
            _currentGroup = groups.Find(group => group != null
                && string.Equals(group.id, id, StringComparison.Ordinal));
            if (_currentGroup == null)
                Debug.LogWarning($"[PlayerButtonGroup] 找不到 ID「{id}」，已清除目前組名。", this);
        }
        Refresh();
    }

    public void RemoveID() => SetID(null);
    public void removeID() => RemoveID();
    public void Show() => SetVisible(true);
    public void Hide() => SetVisible(false);

    private void HandleFlagChanged(string id, bool active) => Refresh();
    private void HandleVariableChanged(string key, int value) => Refresh();
    private void HandlePause() => HandleClick(pauseButton, _currentGroup?.pause);
    private void HandlePlay() => HandleClick(playButton, _currentGroup?.play);
    private void HandleFast() => HandleClick(fastButton, _currentGroup?.fast);

    private bool CanClick(ButtonAction action)
    {
        if (action == null || _currentGroup == null) return false;
        if (action.interactableFlag == null) return true;
        var service = GameStatusService.Instance;
        if (service == null || service.ProgressFlags == null) return false;
        bool active = service.ProgressFlags.Contains(action.interactableFlag.FlagID);
        return action.invertInteractable ? !active : active;
    }

    public void Refresh()
    {
        if (Instance != this) return;
        if (!_configured || !isActiveAndEnabled)
        {
            SetButtonsInteractable(false);
            return;
        }
        pauseButton.interactable = CanClick(_currentGroup?.pause);
        playButton.interactable = CanClick(_currentGroup?.play);
        fastButton.interactable = CanClick(_currentGroup?.fast);
    }

    private void SetButtonsInteractable(bool value)
    {
        if (pauseButton != null) pauseButton.interactable = value;
        if (playButton != null) playButton.interactable = value;
        if (fastButton != null) fastButton.interactable = value;
    }

    private void HandleClick(Button button, ButtonAction action)
    {
        if (Instance != this || !_configured || !isActiveAndEnabled || !_visible || _fading
            || canvasGroup == null || !canvasGroup.interactable || !canvasGroup.blocksRaycasts
            || button == null || !button.isActiveAndEnabled || !button.IsInteractable()) return;
        // 點擊當下重查條件，防止 Flag 已改變而畫面尚未刷新。
        if (!CanClick(action))
        {
            Refresh();
            return;
        }
        action.onClick?.Invoke();
        if (this != null) Refresh();
    }

    private void SetVisible(bool visible)
    {
        if (Instance != this || canvasGroup == null) return;
        if (_visible == visible && (_fading
            || Mathf.Approximately(canvasGroup.alpha, visible ? 1f : 0f))) return;
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
        canvasGroup.interactable = _visible && _configured && isActiveAndEnabled;
        canvasGroup.blocksRaycasts = _visible && _configured && isActiveAndEnabled;
    }
}
