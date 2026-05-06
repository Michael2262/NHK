using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 自動觸發器按鈕
/// 功能：點擊此物件後，會自動去呼叫指定的 ConditionalTouchReactionBase.OnTouched()
/// 特性：可設定次數、間隔時間、或是無限循環
/// </summary>
public class TestAutoTouchTrigger : MonoBehaviour, IPointerClickHandler
{
    [Header("目標設定")]
    [Tooltip("要觸發的目標反應組件 (繼承自 ConditionalTouchReactionBase 的腳本)")]
    public ConditionalTouchReactionBase targetReaction;

    [Header("執行設定")]
    [Tooltip("每次觸發之間的間隔時間 (秒)")]
    public float interval = 0.5f;

    [Tooltip("是否無限循環 (勾選後將忽略次數設定，直到再次點擊或被Disable)")]
    public bool isInfinite = false;

    [Tooltip("執行次數 (若未勾選無限循環)")]
    public int repetitionCount = 1;

    [Header("狀態 (唯讀)")]
    [SerializeField] private bool _isRunning = false;
    private Coroutine _currentCoroutine;

    /// <summary>
    /// 實作 IPointerClickHandler
    /// 當點擊此按鈕時觸發
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 如果已經在跑，再次點擊時通常有兩種設計：
        // 1. 停止 (Toggle)
        // 2. 重置並重新開始 (Restart)
        // 這裡採用「如果正在跑則停止，如果沒在跑則開始」的 Toggle 邏輯，方便控制無限循環
        if (_isRunning)
        {
            StopAutoTouch();
        }
        else
        {
            StartAutoTouch();
        }
    }

    /// <summary>
    /// 公開方法：開始執行自動觸摸
    /// </summary>
    public void StartAutoTouch()
    {
        if (targetReaction == null)
        {
            Debug.LogWarning($"[AutoTouchTrigger] 未綁定 Target Reaction，無法執行 on {name}");
            return;
        }

        StopAutoTouch(); // 先確保舊的被殺掉
        _currentCoroutine = StartCoroutine(AutoTouchRoutine());
    }

    /// <summary>
    /// 公開方法：停止執行
    /// </summary>
    public void StopAutoTouch()
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
            _currentCoroutine = null;
        }
        _isRunning = false;
    }

    /// <summary>
    /// 核心 Coroutine
    /// </summary>
    private IEnumerator AutoTouchRoutine()
    {
        _isRunning = true;
        int executedCount = 0;

        // 判斷迴圈條件：如果是無限，或次數未達標
        while (isInfinite || executedCount < repetitionCount)
        {
            // 1. 執行目標的 OnTouched
            // 注意：這裡直接呼叫 OnTouched，會觸發 FSM 和 Spine 動畫，
            // 但不會經過 ConditionalTouchReactionBase 的手勢判定 (因為是強制執行)
            targetReaction.OnTouched();

            executedCount++;

            // 2. 等待間隔
            // 如果不是最後一次，或者要無限循環，才需要等待
            if (isInfinite || executedCount < repetitionCount)
            {
                yield return new WaitForSeconds(interval);
            }
        }

        _isRunning = false;
        _currentCoroutine = null;
    }

    // 當物件被隱藏或停用時，強制停止 Coroutine，避免背景報錯
    private void OnDisable()
    {
        StopAutoTouch();
    }
}