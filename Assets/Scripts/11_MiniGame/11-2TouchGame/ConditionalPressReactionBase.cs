using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HutongGames.PlayMaker;
using System;

/// <summary>
/// (V7.1 - 多來源修正版) 
/// 支援多個 Proxy 同時按壓，並修正了 WatchOut 遺失的問題。
/// </summary>
public abstract class ConditionalPressReactionBase : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    public static event Action<ConditionalPressReactionBase> OnAnyHoverSuccess;

    // === 狀態變數 ===
    private int _pressCount = 0; // 記錄當前有多少個來源正在按壓
    protected bool _isPressed => _pressCount > 0; // 只要計數 > 0 就算按壓中

    [SerializeField]
    protected bool _autoHoverSetting = false;
    protected bool _isLoopActive = false;
    protected bool _isInterrupted = false;
    protected bool _isWaitingInRelease = false;

    private const float AUTO_INTERVAL_MIN = 0.1f;
    private const float AUTO_INTERVAL_MAX = 0.45f;
    private const float AUTO_INTERVAL_DEFAULT = 0.35f;

    [HideInInspector]
    [SerializeField] private float _pressLoopInterval = AUTO_INTERVAL_DEFAULT;

    /// <summary>
    /// Press/Auto loop interval (clamped between 0.1 ~ 0.45).
    /// Subclass can set via GetHoverInterval() override or leave default.
    /// </summary>
    public float PressLoopInterval
    {
        get => _pressLoopInterval;
        set => _pressLoopInterval = Mathf.Clamp(value, AUTO_INTERVAL_MIN, AUTO_INTERVAL_MAX);
    }

    private float _loopTimer = 0f;
    private Button _targetButton;

    // === 隱藏的 FSM 引用 (改由 Awake 自動抓取) ===
    [HideInInspector] protected PlayMakerFSM targetFSM;
    [HideInInspector] protected string hoverTriggerEvent;

    protected virtual void Awake()
    {
        _targetButton = GetComponent<Button>();

        // 自動從同物件抓取 Context
        var context = GetComponent<FsmContext>();
        if (context != null)
        {
            targetFSM = context.sharedFSM;
            hoverTriggerEvent = context.defaultEvent;
        }
        else
        {
            Debug.LogError($"[{name}] 找不到 FsmContext 組件！請在該物件上掛載 FsmContext。");
        }
    }

    // === 公開邏輯入口 (供 PressLogicProxy 呼叫) ===

    public virtual void OnInputDown()
    {
        if (_targetButton != null && !_targetButton.interactable) return;

        _isInterrupted = false;
        _pressCount++;

        // 第一次按下時啟動循環
        if (_pressCount == 1)
        {
            _loopTimer = 0f;
            _isLoopActive = true;
            OnTouchedStart();
        }
    }

    public virtual void OnInputUp()
    {
        _pressCount = Mathf.Max(0, _pressCount - 1);

        // 當所有按鈕都放開時，才停止邏輯
        if (_pressCount == 0)
        {
            _isLoopActive = false;
            _isInterrupted = false;
            _isWaitingInRelease = false;
        }
    }

    // === Unity Event System 介面 (自身點擊) ===

    public void OnPointerDown(PointerEventData eventData) => OnInputDown();
    public void OnPointerUp(PointerEventData eventData) => OnInputUp();

    protected virtual void Update()
    {
        if ((_isLoopActive || _autoHoverSetting) && !_isInterrupted)
        {
            if (_isPressed || _autoHoverSetting)
            {
                if (_targetButton != null && !_targetButton.interactable)
                {
                    _isLoopActive = false;
                    return;
                }

                _loopTimer += Time.deltaTime;
                float interval = GetHoverInterval();

                if (_loopTimer >= interval)
                {
                    _loopTimer = 0f;
                    ExecuteTrigger();
                }
            }
        }
    }

    // === Auto Hover API ===

    /// <summary>
    /// Set Auto Hover mode on/off from external caller.
    /// When enabled: starts loop + triggers first execution.
    /// When disabled: stops loop if not manually pressed.
    /// </summary>
    public void SetAutoHover(bool enabled, float interval = -1f)
    {
        _autoHoverSetting = enabled;

        // If a valid interval is provided, override PressLoopInterval
        if (interval > 0f)
        {
            PressLoopInterval = interval;
        }

        if (enabled)
        {
            _isLoopActive = true;
            _isInterrupted = false;
            _loopTimer = 0f;
            ExecuteTrigger();
        }
        else
        {
            if (!_isPressed)
            {
                _isLoopActive = false;
            }
        }
    }

    /// <summary>
    /// Get current Auto Hover state.
    /// </summary>
    public bool IsAutoHover => _autoHoverSetting;

    // === 狀態控制與虛擬方法 ===

    /// <summary>
    /// 核心方法：發生突發狀況（如媽媽進來）時呼叫。
    /// </summary>
    public virtual void WatchOut()
    {
        _isLoopActive = false;
        _autoHoverSetting = false;
        _isInterrupted = false;
        _isWaitingInRelease = false;
        _pressCount = 0; // 確保中斷後計數清零
    }

    public virtual void ResetToOriginal()
    {
        _isLoopActive = false;
        _autoHoverSetting = false;
        _isInterrupted = false;
        _isWaitingInRelease = false;
        _pressCount = 0;
    }

    public virtual void ForceInterruptRelease()
    {
        _isInterrupted = true;
        _pressCount = 0;
        _isLoopActive = false;
        _isWaitingInRelease = false;
        _loopTimer = 0f;

        if (_targetButton != null)
        {
            bool wasInteractable = _targetButton.interactable;
            _targetButton.interactable = false;
            _targetButton.interactable = wasInteractable;
        }
    }

    protected virtual void OnTouchedStart() => ExecuteTrigger();

    protected void ExecuteTrigger()
    {
        //Debug.Log($"[{name}] ExecuteTrigger → FSM: {targetFSM?.name}, Event: {hoverTriggerEvent}, Controller: {(this as SpineTogglePlayOnPress)?.spineController?.name}");
        OnTouched();
        OnAnyHoverSuccess?.Invoke(this);
        SendFsmEvent(hoverTriggerEvent);
    }

    protected void SendFsmEvent(string eventName)
    {
        if (string.IsNullOrEmpty(eventName) || targetFSM == null) return;
        targetFSM.SendEvent(eventName);
    }

    // === 抽象方法 ===
    protected virtual float GetHoverInterval() => _pressLoopInterval;
    public abstract void OnTouched();
}