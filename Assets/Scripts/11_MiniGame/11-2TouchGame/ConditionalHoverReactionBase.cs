using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HutongGames.PlayMaker;
using System;

/// <summary>
/// (V4 - 統一鎖定版) 
/// 支援 一般物件(3D/2D) 與 UI Button。
/// 功能：長按循環觸發、AutoHover鎖定、強制打斷機制。
/// </summary>
public abstract class ConditionalHoverReactionBase : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    // === 靜態事件 ===
    public static event Action<ConditionalHoverReactionBase> OnAnyHoverSuccess;

    // === 1. 狀態變數 ===
    protected bool _isPressed = false;        // 物理手勢是否按下
    protected bool _autoHoverSetting = false; // 設定：是否啟用 Auto 鎖定功能
    protected bool _isLoopActive = false;     // 狀態：目前 Loop 是否正在跑
    protected bool _isInterrupted = false;    // 標記：是否處於強制打斷狀態 (用於防止按鈕卡死或外部終止)

    private float _timer = 0f;
    private Button _targetButton;             // 自動抓取的 Button 組件 (若有)

    // === 2. FSM 溝通 ===
    [Header("FSM Communication")]
    [UnityEngine.Tooltip("觸發/Loop 時發送的事件名稱。預設為 CSHARP_TRIGGERED_TOUCH。")]
    [SerializeField] private string hoverTriggerEvent = "CSHARP_TRIGGERED_TOUCH";

    [UnityEngine.Tooltip("目標 FSM 組件。留空則自動抓取同物件上的 PlayMakerFSM。")]
    [SerializeField] private PlayMakerFSM targetFSM;

    // === 初始化 ===
    protected virtual void Awake()
    {
        // 嘗試抓取 Button，若無則視為一般互動轉發物件
        _targetButton = GetComponent<Button>();
    }

    // === 3. Unity Event System 介面實作 ===

    public void OnPointerDown(PointerEventData eventData)
    {
        // 若是 Button 且不可互動，則直接無視
        if (_targetButton != null && !_targetButton.interactable) return;

        // 只要玩家重新按下去，就解除打斷狀態
        _isInterrupted = false;
        _isPressed = true;

        if (!_isLoopActive)
        {
            _isLoopActive = true;
            _timer = 0f;
            OnTouchedStart();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPressed = false;

        // 若處於「強制打斷」狀態，放開手時不執行 Reset (由 ForceInterruptRelease 負責)
        if (_isInterrupted) return;

        // 若未開啟 Auto 鎖定，則停止
        if (!_autoHoverSetting)
        {
            StopAndReset();
        }
    }

    // === 4. 核心 Loop 邏輯 ===

    protected virtual void Update()
    {
        // 必須在 Loop 啟動且未被打斷的狀態下執行
        if (_isLoopActive && !_isInterrupted)
        {
            // [UI檢查] 如果按鈕突然變為不可互動，立刻停止
            if (_targetButton != null && !_targetButton.interactable)
            {
                StopAndReset();
                return;
            }

            _timer += Time.deltaTime;
            float interval = GetHoverInterval();

            if (_timer >= interval)
            {
                _timer = 0f;
                ExecuteTrigger();
            }
        }
    }

    // === 5. 強制打斷與重置邏輯 ===

    /// <summary>
    /// 強制解除按下狀態。
    /// 1. 停止 Loop。
    /// 2. 若是 UI Button，會強制將按鈕視覺狀態彈回 Normal。
    /// </summary>
    public virtual void ForceInterruptRelease()
    {
        _isInterrupted = true;
        _isPressed = false;
        _isLoopActive = false;
        _timer = 0f;

        // [UI視覺重置] 透過快速切換開關，強迫 UGUI 釋放對按鈕的 Pressed 狀態顯示
        if (_targetButton != null)
        {
            bool wasInteractable = _targetButton.interactable;
            _targetButton.interactable = false;
            _targetButton.interactable = wasInteractable;
        }
    }

    private void StopAndReset()
    {
        _isLoopActive = false;
        _isInterrupted = false;
        ResetToOriginal();
    }

    // === 6. 觸發方法 ===

    protected virtual void OnTouchedStart()
    {
        ExecuteTrigger();
    }

    protected void ExecuteTrigger()
    {
        OnTouched();
        OnAnyHoverSuccess?.Invoke(this);
        SendFsmEvent(hoverTriggerEvent);
    }

    // === 7. AutoHover (鎖定控制) ===

    public void SetAutoHover(bool active)
    {
        _autoHoverSetting = active;

        // 如果關閉了 Auto 且手沒按著，則立刻停止
        if (!_autoHoverSetting && !_isPressed)
        {
            if (_isLoopActive) StopAndReset();
        }
    }

    public void SwitchAutoHover() => SetAutoHover(!_autoHoverSetting);
    public bool IsAutoHoverEnabled() => _autoHoverSetting;

    // === 8. FSM 輔助 ===

    protected void SendFsmEvent(string eventName)
    {
        if (string.IsNullOrEmpty(eventName)) return;
        if (targetFSM == null) targetFSM = GetComponent<PlayMakerFSM>();

        if (targetFSM != null)
        {
            targetFSM.SendEvent(eventName);
        }
        else
        {
            Debug.LogWarning($"[{name}] 找不到 PlayMakerFSM，無法發送 {eventName}");
        }
    }

    // === 9. 抽象與虛擬方法 (供子類別實作) ===

    protected abstract float GetHoverInterval();

    public abstract void OnTouched();

    public virtual void ResetToOriginal()
    {
        _isLoopActive = false;
        _autoHoverSetting = false;
        _isInterrupted = false;
    }
}