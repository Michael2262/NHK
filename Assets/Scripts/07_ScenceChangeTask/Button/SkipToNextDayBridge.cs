using UnityEngine;

/// <summary>
/// 橋接腳本：用於從 UI Button 或 Unity Event 觸發隔天跳轉。
/// 
/// 【使用方式】
/// 1. 將此腳本掛載到任意物件上（例如睡覺按鈕）
/// 2. 在 Button 的 OnClick() 中拖入此物件，選擇 SkipToNextDay()
/// 
/// 【或者】
/// 直接在其他腳本中呼叫：
/// SkipToNextDayBridge.Execute();
/// </summary>
public class SkipToNextDayBridge : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("是否在觸發前檢查 GameStatusService 是否存在")]
    [SerializeField] private bool logWarnings = true;

    /// <summary>
    /// 靜態方法：從任何地方呼叫
    /// </summary>
    public static void Execute()
    {
        if (GameStatusService.Instance != null && GameStatusService.Instance.Time != null)
        {
            GameStatusService.Instance.Time.SkipToNextDay();
        }
        else
        {
            Debug.LogError("[SkipToNextDayBridge] GameStatusService 或 Time 不存在！");
        }
    }

    /// <summary>
    /// 實例方法：供 Button OnClick() 或 Unity Event 使用
    /// </summary>
    public void SkipToNextDay()
    {
        if (GameStatusService.Instance != null && GameStatusService.Instance.Time != null)
        {
            GameStatusService.Instance.Time.SkipToNextDay();
            
            if (logWarnings)
                Debug.Log("[SkipToNextDayBridge] 已觸發 SkipToNextDay()");
        }
        else
        {
            if (logWarnings)
                Debug.LogError("[SkipToNextDayBridge] GameStatusService 或 Time 不存在！");
        }
    }

    /// <summary>
    /// 帶自訂 Tips 的版本
    /// </summary>
    public void SkipToNextDayWithTips(string tipsKey)
    {
        // 先設定 Tips
        DayTransitionUI.SetTipsKey(tipsKey);
        
        // 再觸發跳轉
        SkipToNextDay();
    }

    // ============================================================
    // 編輯器測試
    // ============================================================

    [ContextMenu("Test - Skip To Next Day")]
    private void TestSkip()
    {
        if (Application.isPlaying)
        {
            SkipToNextDay();
        }
        else
        {
            Debug.LogWarning("[SkipToNextDayBridge] 請在 Play Mode 中測試");
        }
    }
}
