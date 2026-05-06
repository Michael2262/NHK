using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// (V3 - Unity Event 支援版)
/// 代理器：將點擊事件轉發給目標物件上「所有」的邏輯核心。
/// 支援 Unity Event 呼叫 TriggerOnce()。
/// </summary>
public class PressLogicProxy : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("關聯的邏輯主體")]
    [Tooltip("請將掛有多個 ConditionalPressReactionBase (如 Spine 和 Pic 切換) 的物件拖到這裡")]
    [SerializeField] private GameObject targetLogicObject;

    // 緩存組件以提高效能
    private ConditionalPressReactionBase[] _cachedLogics;

    private void Awake()
    {
        if (targetLogicObject != null)
        {
            _cachedLogics = targetLogicObject.GetComponents<ConditionalPressReactionBase>();
        }
    }

    // === Event System 介面 (自身點擊) ===

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_cachedLogics == null) return;

        foreach (var logic in _cachedLogics)
        {
            if (logic != null) logic.OnInputDown();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_cachedLogics == null) return;

        foreach (var logic in _cachedLogics)
        {
            if (logic != null) logic.OnInputUp();
        }
    }

    // === 供 Unity Event / 外部腳本呼叫 ===

    /// <summary>
    /// 一次性觸發：執行按下+放開流程。
    /// 可直接拖到 Button.onClick 或其他 Unity Event 使用。
    /// </summary>
    public void TriggerOnce()
    {
        if (_cachedLogics == null) return;

        foreach (var logic in _cachedLogics)
        {
            if (logic != null && logic.enabled)
            {
                logic.OnInputDown();
                logic.OnInputUp();
            }
        }
    }
}