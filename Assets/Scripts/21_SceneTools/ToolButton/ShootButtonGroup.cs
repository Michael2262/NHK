using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>單顆射擊按鈕：依 ID 切換事件，顯示主角射精感與剩餘次數。</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(CanvasGroup))]
public class ShootButtonGroup : MonoBehaviour
{
    public static ShootButtonGroup Instance { get; private set; }

    [Serializable]
    public class ButtonAction
    {
        [Tooltip("切換用 ID，區分大小寫。")]
        public string id;
        public UnityEvent onClick = new UnityEvent();
    }

    [Header("按鈕與次數顯示")]
    [Tooltip("事件請設定在下方各 ID，Button 原本的 On Click 請留空。")]
    [SerializeField] private Button shootButton;
    [Tooltip("只顯示 (剩餘/每日初始值)，請使用獨立 TMP，勿掛 LocalizeUI。")]
    [SerializeField] private TMP_Text shootTimesText;
    [SerializeField] private List<ButtonAction> groups = new List<ButtonAction>();
    [Header("整體顯示")]
    [SerializeField] private CanvasGroup canvasGroup;
    [Range(0f, 1f)] [SerializeField] private float disabledAlpha = 0.5f;
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
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in groups)
        {
            if (group == null || string.IsNullOrWhiteSpace(group.id)) continue;
            if (!ids.Add(group.id))
                Debug.LogWarning($"[ShootButtonGroup] ID「{group.id}」重複，切換時使用第一筆。", this);
        }
        ApplyPresentation();
    }

    private void OnEnable()
    {
        if (Instance != this) return;
        if (_configured) shootButton.onClick.AddListener(HandleClick);
        _service = GameStatusService.Instance;
        _protagonist = _service != null ? _service.Protagonist : null;
        if (_protagonist != null)
        {
            _protagonist.OnSemenChanged += HandleValueChanged;
            _protagonist.OnShootTimesChanged += HandleValueChanged;
        }
        if (_service != null) _service.OnGameStatusLoaded += Refresh;
        _visibility = _visible ? 1f : 0f;
        Refresh();
    }

    private void OnDisable()
    {
        if (Instance != this) return;
        if (shootButton != null) shootButton.onClick.RemoveListener(HandleClick);
        if (_protagonist != null)
        {
            _protagonist.OnSemenChanged -= HandleValueChanged;
            _protagonist.OnShootTimesChanged -= HandleValueChanged;
        }
        if (_service != null) _service.OnGameStatusLoaded -= Refresh;
        _service = null;
        _protagonist = null;
        _fading = false;
        _canClick = false;
        ApplyPresentation();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>切換事件，不自動顯示；空白或不存在的 ID 會清除目前事件。</summary>
    public void SetID(string id)
    {
        if (Instance != this) return;
        _currentGroup = null;
        if (!string.IsNullOrWhiteSpace(id))
        {
            _currentGroup = groups.Find(group => group != null
                && string.Equals(group.id, id, StringComparison.Ordinal));
            if (_currentGroup == null)
                Debug.LogWarning($"[ShootButtonGroup] 找不到 ID「{id}」，已清除目前 ID。", this);
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
    }

    private void HandleClick()
    {
        if (Instance != this || !_configured || !isActiveAndEnabled || !_visible || _fading
            || canvasGroup == null || !canvasGroup.interactable || !canvasGroup.blocksRaycasts
            || !shootButton.isActiveAndEnabled || !shootButton.IsInteractable()) return;
        // 點擊當下重查主角狀態，避免畫面尚未刷新時觸發事件。
        if (!CanClick())
        {
            Refresh();
            return;
        }
        _currentGroup.onClick?.Invoke();
        if (this != null) Refresh();
    }

    public void SetVisible(bool visible)
    {
        if (Instance != this || canvasGroup == null) return;
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
        bool interactable = _canClick && _visible && !_fading && isActiveAndEnabled;
        if (shootButton != null) shootButton.interactable = interactable;
        if (canvasGroup == null) return;
        // 淡入淡出與不可用透明度分開計算，數值變更不會中斷顯示動畫。
        canvasGroup.alpha = _visibility * (_canClick ? 1f : Mathf.Clamp01(disabledAlpha));
        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = _visible && _configured && isActiveAndEnabled;
    }
}
