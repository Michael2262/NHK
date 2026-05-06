using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// (V1 - 條件路由擴充版)
/// 繼承自 GesturePressLogicProxy 的設計風格。
/// 在原本「單一目標邏輯體」的基礎上，新增條件路由：
/// 可依據 ProgressFlag (true/false) 或 ProgressValue (數值比較)，
/// 將觸發導向不同的目標邏輯體。
/// 條件不符合時，回退至 defaultLogicObject。
/// 所有符合的 ConditionalPressReactionBase 元件都會被觸發（與原版行為一致）。
/// </summary>
public class GestureRouterPressLogicProxy : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    // ──────────────────────────────────────────────
    // 比較運算子
    // ──────────────────────────────────────────────
    public enum CompareOperator
    {
        GreaterThan,            // >
        LessThan,               // <
        EqualTo,                // ==
        GreaterThanOrEqualTo,   // >=
        LessThanOrEqualTo       // <=
    }

    // ──────────────────────────────────────────────
    // 單條條件定義
    // ──────────────────────────────────────────────
    [System.Serializable]
    public class RoutingCondition
    {
        [Tooltip("條件名稱（僅供 Inspector 辨識用）")]
        public string ConditionLabel = "New Condition";

        [Tooltip("true = 讀 Flag（布林）；false = 讀 Value（數值）")]
        public bool IsFlag = true;

        [Tooltip("Flag / Value 的 Key（對應 ProgressFlagModel）")]
        public string Key = "";

        [Tooltip("Flag 模式：期望 Flag 為 true 還是 false")]
        public bool ExpectedFlagValue = true;

        [Tooltip("Value 模式：比較運算子")]
        public CompareOperator Operator = CompareOperator.GreaterThan;

        [Tooltip("Value 模式：比較的目標數值")]
        public int CompareValue = 0;

        [Tooltip("條件符合時，觸發此目標邏輯體上的所有 ConditionalPressReactionBase")]
        public GameObject TargetLogicObject;

        public bool Evaluate(ProgressFlagModel flags)
        {
            if (flags == null || string.IsNullOrEmpty(Key)) return false;

            if (IsFlag)
            {
                return flags.Contains(Key) == ExpectedFlagValue;
            }
            else
            {
                int current = flags.GetValue(Key);
                return Operator switch
                {
                    CompareOperator.GreaterThan          => current > CompareValue,
                    CompareOperator.LessThan             => current < CompareValue,
                    CompareOperator.EqualTo              => current == CompareValue,
                    CompareOperator.GreaterThanOrEqualTo => current >= CompareValue,
                    CompareOperator.LessThanOrEqualTo    => current <= CompareValue,
                    _                                    => false
                };
            }
        }
    }

    // ──────────────────────────────────────────────
    // Inspector 欄位
    // ──────────────────────────────────────────────
    [Header("1. 預設邏輯體（條件皆不符合時使用）")]
    [Tooltip("請將掛有 ConditionalPressReactionBase 的物件拖入這裡")]
    [SerializeField] private GameObject defaultLogicObject;

    [Header("2. 條件路由列表")]
    [SerializeField] private List<RoutingCondition> conditions = new List<RoutingCondition>();

    [Header("3. 滑動判定設定")]
    [Tooltip("設定允許的滑動方向；空白 = 只接受 Click")]
    public SwipeDir[] swipeConds;

    [Tooltip("超過此距離才算「滑動 (Swipe)」，否則視為「Click」")]
    [SerializeField] private float swipeThreshold = 50f;

    // ──────────────────────────────────────────────
    // 內部狀態
    // ──────────────────────────────────────────────
    private Vector2 _startPosition;
    private bool _isDragging = false;

    // ──────────────────────────────────────────────
    // Event System 介面實作（與原版相同）
    // ──────────────────────────────────────────────
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
        // 1. 計算手勢
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
            return; // 距離太短，不觸發任何動作
        }

        // 2. 檢查是否符合設定的手勢條件
        if (!IsMatch(isClick, currentSwipe)) return;

        // 3. 路由：決定目標邏輯體
        GameObject target = ResolveTarget();
        if (target == null)
        {
            Debug.LogWarning($"[GestureRouterPressLogicProxy] {gameObject.name}: 沒有可執行的目標邏輯體。");
            return;
        }

        // 4. 觸發目標物件上的所有 ConditionalPressReactionBase（與原版行為一致）
        TriggerTargetLogics(target);
    }

    // ──────────────────────────────────────────────
    // 核心邏輯
    // ──────────────────────────────────────────────

    /// <summary>
    /// 依條件順序評估，回傳應執行的目標 GameObject。
    /// 皆不符合則回傳 defaultLogicObject。
    /// </summary>
    private GameObject ResolveTarget()
    {
        ProgressFlagModel flags = GameStatusService.Instance?.ProgressFlags;

        if (flags == null)
        {
            Debug.LogWarning($"[GestureRouterPressLogicProxy] {gameObject.name}: 找不到 ProgressFlagModel，回退至 defaultLogicObject。");
            return defaultLogicObject;
        }

        foreach (var condition in conditions)
        {
            if (condition == null) continue;

            if (condition.Evaluate(flags))
            {
                Debug.Log($"[GestureRouterPressLogicProxy] {gameObject.name}: 條件「{condition.ConditionLabel}」符合，導向「{condition.TargetLogicObject?.name ?? "null"}」。");
                return condition.TargetLogicObject;
            }
        }

        return defaultLogicObject;
    }

    /// <summary>
    /// 觸發目標物件上的所有 ConditionalPressReactionBase（與原版 TriggerTargetLogics 完全一致）。
    /// </summary>
    private void TriggerTargetLogics(GameObject target)
    {
        var logics = target.GetComponents<ConditionalPressReactionBase>();

        if (logics == null || logics.Length == 0)
        {
            Debug.LogWarning($"[GestureRouterPressLogicProxy] 目標「{target.name}」上找不到任何 ConditionalPressReactionBase。");
            return;
        }

        foreach (var logic in logics)
        {
            if (logic != null && logic.enabled)
            {
                logic.OnInputDown();
                logic.OnInputUp();
            }
        }
    }

    private bool IsMatch(bool isClick, SwipeDir swipe)
    {
        if (swipeConds == null || swipeConds.Length == 0)
        {
            return isClick;
        }
        return System.Array.Exists(swipeConds, cond => cond == swipe);
    }

    private SwipeDir CalculateSwipeDirection(Vector2 delta)
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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (defaultLogicObject != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, defaultLogicObject.transform.position);
            Gizmos.DrawWireSphere(defaultLogicObject.transform.position, 0.15f);
        }

        if (conditions == null) return;
        foreach (var c in conditions)
        {
            if (c?.TargetLogicObject == null) continue;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, c.TargetLogicObject.transform.position);
            Gizmos.DrawWireSphere(c.TargetLogicObject.transform.position, 0.15f);
        }
    }
#endif
}
