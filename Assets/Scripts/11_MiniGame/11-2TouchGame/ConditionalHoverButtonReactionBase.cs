using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HutongGames.PlayMaker;
using System;

/// <summary>
/// (UI Button 版 - Latch/鎖定機制)
/// 專門掛載在 UI Button 上
/// 會檢查 Button 是否 Interactable (可互動)。
/// </summary>
[RequireComponent(typeof(Button))]
public abstract class ConditionalHoverButtonReactionBase : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    // === 靜態事件 ===
    public static event Action<ConditionalHoverButtonReactionBase> OnAnyHoverSuccess;

    // === 1. 狀態變數 ===
    protected bool _isPressed = false;        // 物理手勢是否按下
    protected bool _autoHoverSetting = false; // 設定：是否啟用 Auto 鎖定功能
    protected bool _isLoopActive = false;     // 狀態：目前 Loop 是否正在跑

    // [新增] 標記：是否處於強制打斷狀態
    protected bool _isInterrupted = false;

    private float _timer = 0f;
    private Button _targetButton;

    // === 2. FSM 溝通 ===
    [Header("FSM Communication")]
    [UnityEngine.Tooltip("觸發/Loop 時發送的事件名稱")]
    [SerializeField] private string hoverTriggerEvent = "CSHARP_TRIGGERED_TOUCH";

    [UnityEngine.Tooltip("FSM 組件 (留空會自動抓取)")]
    [SerializeField] private PlayMakerFSM targetFSM;

    // === 初始化 ===
    protected virtual void Awake()
    {
        _targetButton = GetComponent<Button>();
    }

    // === 3. Unity Event System 介面實作 ===

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_targetButton != null && !_targetButton.interactable)
        {
            return;
        }

        // [新增] 只要玩家重新按下去，就解除打斷狀態，回歸正常邏輯
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

        // [修改] 如果目前處於「被強制打斷」的狀態，放開手時不要執行 Reset
        // 這樣可以保留 ResetToOriginal 不被觸發，直到下次按下
        if (_isInterrupted)
        {
            return;
        }

        if (!_autoHoverSetting)
        {
            StopAndReset();
        }
    }

    // === 4. 核心 Loop 邏輯 ===

    protected virtual void Update()
    {
        // [修改] 如果被打斷 (_isInterrupted) 或是 Loop 沒開，就不執行
        if (_isLoopActive && !_isInterrupted)
        {
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

    // === 強制打斷 ===

    /// <summary>
    /// 強制解除按下狀態 (Force Release)。
    /// <para>1. 邏輯上停止 Loop，但不觸發 ResetToOriginal。</para>
    /// <para>2. 視覺上強制按鈕彈起 (Normal State)。</para>
    /// <para>3. 狀態會保持直到玩家下一次 OnPointerDown。</para>
    /// </summary>
    public virtual void ForceInterruptRelease()
    {
        // 1. 設定標記，阻止 Update 跑 Loop，並阻止 OnPointerUp 觸發 Reset
        _isInterrupted = true;

        // 2. 邏輯狀態解除
        _isPressed = false;
        _isLoopActive = false;
        _timer = 0f; // 選擇性：歸零計時器，看你需求

        // 3. 視覺強制重置 (Visual Reset)
        // 原理：快速開關 interactable 會強迫 Unity UI EventSystem 釋放對該按鈕的控制，
        // 並將按鈕狀態切回 Normal (沒有 Highlight/Pressed)。
        if (_targetButton != null)
        {
            bool wasInteractable = _targetButton.interactable;
            _targetButton.interactable = false;
            _targetButton.interactable = wasInteractable; // 恢復原本的可互動狀態
        }
    }

    // === 5. 觸發方法 ===

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

    // === 6. AutoHover (鎖定機制) ===

    public void SetAutoHover(bool active)
    {
        _autoHoverSetting = active;

        if (!_autoHoverSetting && !_isPressed)
        {
            if (_isLoopActive)
            {
                StopAndReset();
            }
        }
    }

    public void SwitchAutoHover()
    {
        SetAutoHover(!_autoHoverSetting);
    }

    public bool IsAutoHoverEnabled()
    {
        return _autoHoverSetting;
    }

    // === 7. 內部重置邏輯 ===

    private void StopAndReset()
    {
        _isLoopActive = false;
        // 確保重置時也解除打斷標記，以防萬一
        _isInterrupted = false;
        ResetToOriginal();
    }

    // === 8. FSM 輔助 ===

    protected void SendFsmEvent(string eventName)
    {
        if (string.IsNullOrEmpty(eventName)) return;

        if (targetFSM == null)
        {
            targetFSM = GetComponent<PlayMakerFSM>();
        }

        if (targetFSM != null)
        {
            targetFSM.SendEvent(eventName);
        }
    }

    // === 9. 抽象與虛擬方法 (供子類別實作) ===

    protected abstract float GetHoverInterval();

    public abstract void OnTouched();

    public virtual void ResetToOriginal()
    {
        _isLoopActive = false;
        _autoHoverSetting = false;
        _isInterrupted = false; // 重置時確保標記歸零
    }
}