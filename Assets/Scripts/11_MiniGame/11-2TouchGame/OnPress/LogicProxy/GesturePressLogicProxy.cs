using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// (V1 - 手勢獨立版)
/// 代理器：必須玩家完成指定手勢 (如特定方向滑動) 後，
/// 才會觸發目標物件上所有的 ConditionalPressReactionBase 邏輯。
/// </summary>
public class GesturePressLogicProxy : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("1. 關聯的邏輯主體")]
    [Tooltip("請將掛有 ConditionalPressReactionBase 的物件拖到這裡")]
    [SerializeField] private GameObject targetLogicObject;

    [Header("2. 手勢判定設定")]
    [Tooltip("想匹配哪些拖曳方向；留空 = 只接受 Click (點擊)")]
    public SwipeDir[] swipeConds;

    [Tooltip("拖曳多遠（像素）才算「滑動 (Swipe)」，而非「Click (點擊)」")]
    [SerializeField] private float swipeThreshold = 50f;

    // 緩存組件以提高效能
    private ConditionalPressReactionBase[] _cachedLogics;
    private Vector2 _startPosition;
    private bool _isDragging = false;

    private void Awake()
    {
        if (targetLogicObject != null)
        {
            // 一次抓取物件上所有的相關邏輯組件
            _cachedLogics = targetLogicObject.GetComponents<ConditionalPressReactionBase>();
        }
    }

    // --- Event System 介面實作 ---

    public void OnPointerDown(PointerEventData eventData)
    {
        _startPosition = eventData.position;
        _isDragging = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        _isDragging = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_cachedLogics == null || _cachedLogics.Length == 0) return;

        // 1. 計算手勢數據
        Vector2 delta = eventData.position - _startPosition;
        SwipeDir currentSwipe = SwipeDir.None;
        bool isClick = false;

        if (!_isDragging && delta.magnitude < swipeThreshold)
        {
            isClick = true;
        }
        else if (delta.magnitude >= swipeThreshold)
        {
            currentSwipe = CalculateSwipeDirection(delta);
        }
        else
        {
            return; // 距離太短，不視為有效操作
        }

        // 2. 檢查是否符合設定的條件 (參考 ConditionalTouchReactionBase 的 Match 邏輯)
        if (IsMatch(isClick, currentSwipe))
        {
            TriggerTargetLogics();
        }
    }

    // --- 核心邏輯 ---

    private bool IsMatch(bool isClick, SwipeDir swipe)
    {
        // 如果沒設定任何滑動條件，則預設判定是否為點擊
        if (swipeConds == null || swipeConds.Length == 0)
        {
            return isClick;
        }
        // 檢查當前滑動方向是否在允許名單中
        return System.Array.Exists(swipeConds, cond => cond == swipe);
    }

    private void TriggerTargetLogics()
    {
        foreach (var logic in _cachedLogics)
        {
            if (logic != null && logic.enabled)
            {
                // 因為手勢是在放開後才判定成功，
                // 所以這裡執行「一次性」的按下與放開流程
                logic.OnInputDown();
                logic.OnInputUp();
            }
        }
    }

    public void ForceExecute()
    {
        TriggerTargetLogics(); //強制執行（不考慮手勢條件，直接觸發目標邏輯）
    }

    private SwipeDir CalculateSwipeDirection(Vector2 delta)
    {
        // 判斷是水平滑動還是垂直滑動
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            return (delta.x > 0) ? SwipeDir.Right : SwipeDir.Left;
        }
        else
        {
            return (delta.y > 0) ? SwipeDir.Up : SwipeDir.Down;
        }
    }
}