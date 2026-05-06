using UnityEngine;
using System.Collections;
using PixelCrushers.DialogueSystem;

public class StoryTrigger : MonoBehaviour
{
    [Header("對話設定")]
    public string conversationTitle;
    public DialogueMode mode = DialogueMode.Skip;

    public enum TriggerTiming { OnEnable, OnStart, OnUse }

    [Header("觸發時機")]
    public TriggerTiming triggerTiming = TriggerTiming.OnStart;

    private bool _hasStarted = false;

    private void OnEnable()
    {
        // 確保非初次場景載入時的 Enable 能觸發
        if (_hasStarted && triggerTiming == TriggerTiming.OnEnable)
        {
            StartCoroutine(WaitAndExecute());
        }
    }

    private void Start()
    {
        _hasStarted = true;
        if (triggerTiming == TriggerTiming.OnStart || triggerTiming == TriggerTiming.OnEnable)
        {
            StartCoroutine(WaitAndExecute());
        }
    }

    /// <summary>
    /// 給外部（如 UnityEvent）呼叫的接口
    /// </summary>
    public void OnUse()
    {
        if (triggerTiming == TriggerTiming.OnUse)
        {
            StartCoroutine(WaitAndExecute());
        }
    }

    /// <summary>
    /// 使用協程確保 StoryManager 單例已就緒
    /// </summary>
    private IEnumerator WaitAndExecute()
    {
        // 等待直到 StoryManager 實例出現
        yield return new WaitUntil(() => StoryManager.Instance != null);

        // 額外等待一幀確保所有系統初始化完畢
        yield return null;

        ExecuteTrigger();
    }

    /// <summary>
    /// 核心觸發邏輯：不再傳遞 delay 數值，交由 StoryManager 統一管理
    /// </summary>
    public void ExecuteTrigger()
    {
        if (string.IsNullOrEmpty(conversationTitle)) return;

        // 僅傳遞標題與模式。延遲秒數 ({{wait}}) 將由 StoryManager 自動注入為 customDelay
        StoryManager.Instance.PlayConversation(conversationTitle, mode);
    }
}