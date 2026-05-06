using UnityEngine;
using UnityEngine.EventSystems; 
using HutongGames.PlayMaker;
using System;

/// <summary>
/// (V4 - C# 主導的最終版)
/// 整合了「手勢偵測」、「手勢過濾」 
/// 和「FSM 通知」 的基底類別。
/// (已移除 對 InputGestureRouter 的依賴)
/// </summary>
public abstract class ConditionalTouchReactionBase : MonoBehaviour,
    ITouchReaction,       //
    IGestureFilter,       //
    IPointerDownHandler,  // (新) 偵測按下
    IPointerUpHandler,    // (新) 偵測放開
    IDragHandler          // (新) 偵測拖曳
{
    // === (★ 新增) 靜態事件 ===
    /// <summary>
    /// 當任何一個 ConditionalTouchReactionBase 觸發成功時呼叫
    /// (供 AutoRecordedLastTouch 訂閱)
    /// </summary>
    public static event Action<ConditionalTouchReactionBase> OnAnyTouchSuccess;

    // === 1. 手勢條件判定 (Filter) ===
    [Header("1. 手勢條件判定")]
    [UnityEngine.Tooltip("想匹配哪些拖曳方向；留空 = 只接受 Click (點擊)")]
    public SwipeDir[] swipeConds; //

    // === 2. 手勢偵測 (Detector) ===
    [Header("2. 手勢偵測設定")]
    [UnityEngine.Tooltip("拖曳多遠（像素）才算「滑動 (Swipe)」，而非「Click (點擊)")]
    [SerializeField] private float swipeThreshold = 50f; //

    private Vector2 _startPosition;
    private bool _isDragging = false;


    /// <summary>
    /// (功能 1) 檢查傳入的手勢是否滿足此腳本設定的條件
    /// (來自舊版 V3)
    /// </summary>
    public bool Match(TouchGesture g)
    {
        if (swipeConds == null || swipeConds.Length == 0)
        {
            return g.isClick; //
        }
        return System.Array.Exists(swipeConds, cond => cond == g.swipe); //
    }

    /// <summary>
    /// (功能 2) 子類別必須實作的「觸發成功」方法
    /// (來自舊版 V3)
    /// </summary>
    public abstract void OnTouched();

    // === 3. FSM 溝通 (可選) ===
    [Header("3. FSM 溝通 (可選)")]
    [UnityEngine.Tooltip("觸發成功時，要發送給 FSM 的事件名稱")]
    [SerializeField] private string fsmEventToSend = "CSHARP_TRIGGERED_TOUCH"; //

    [UnityEngine.Tooltip("FSM 組件 (可留空，會自動嘗試抓取 GetComponent<PlayMakerFSM>)")]
    [SerializeField] private PlayMakerFSM targetFSM;

    /// <summary>
    /// (輔助方法) 子類別可以在 OnTouched() 中呼叫此方法
    /// (來自舊版 V3)
    /// </summary>
    protected void NotifyFSM()
    {
        if (targetFSM == null)
        {
            targetFSM = GetComponent<PlayMakerFSM>();
        }
        if (targetFSM != null && !string.IsNullOrEmpty(fsmEventToSend))
        {
            targetFSM.SendEvent(fsmEventToSend); //
        }
    }

    // === 4. (★ 新增) Last Touch 記錄 (可選) ===
    [Header("4. Last Touch 記錄 (可選)")]
    [UnityEngine.Tooltip("指定此觸碰物件的手部類型 (供 AutoRecordedLastTouch 追蹤)")]
    public TouchHandType handType = TouchHandType.None;


    // === 4. (新) Event System 介面實作 ===

    /// <summary>
    /// (新) 當「按下」時 (由 Event System 呼叫)
    /// (來自)
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        _startPosition = eventData.position;
        _isDragging = false;
    }

    /// <summary>
    /// (新) 當「拖曳」時 (由 Event System 呼叫)
    /// (來自)
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        _isDragging = true;
    }

    /// <summary>
    /// (新) 當「放開」時 (由 Event System 呼叫)
    /// (來自)
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        // --- 建立 TouchGesture ---
        TouchGesture gesture = new TouchGesture();
        Vector2 delta = eventData.position - _startPosition;

        if (!_isDragging && delta.magnitude < swipeThreshold)
        {
            gesture.isClick = true; //
            gesture.swipe = SwipeDir.None; //
        }
        else if (delta.magnitude > swipeThreshold)
        {
            gesture.isClick = false;
            gesture.swipe = CalculateSwipeDirection(delta); //
        }
        else
        {
            return; // 拖曳距離太短，忽略
        }

        // --- (關鍵) 檢查自己是否 Match ---
        if (this.Match(gesture)) //
        {
            // 如果 Match 成功，才呼叫自己 (或子類別) 的 OnTouched()
            this.OnTouched();
            // 觸發成功後，廣播靜態事件，通知 AutoRecordedLastTouch
            OnAnyTouchSuccess?.Invoke(this);

        }
    }

    private SwipeDir CalculateSwipeDirection(Vector2 delta) //
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            return (delta.x > 0) ? SwipeDir.Right : SwipeDir.Left;
        }
        else
        {
            return (delta.y > 0) ? SwipeDir.Up : SwipeDir.Down;
        }
    }
    // === 6. (★ 新增) 重置 API ===

    /// <summary>
    /// (★ 新增)
    /// 重置此物件狀態的虛擬方法。
    /// AutoRecordedLastTouch 會呼叫此方法。
    /// 子類別 (如 SpineTogglePlayOnTouch) 應覆寫 (override) 此方法。
    /// </summary>
    public virtual void ResetToOriginal()
    {
        // 基底類別的預設實作是空的
        // Debug.Log($"[ConditionalTouchReactionBase] ResetToOriginal() called on {name}, but not implemented.");
    }
}