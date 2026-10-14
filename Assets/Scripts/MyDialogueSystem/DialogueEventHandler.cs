using System.Collections;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 將接續播放的對話視為同一段互動，於開始與全部結束時觸發 UnityEvent。
/// 掛在對話期間保持啟用的物件上；自動訂閱 Dialogue Manager。
/// </summary>
[AddComponentMenu("Dialogue System/NHK/Dialogue Event Handler")]
public class DialogueEventHandler : MonoBehaviour
{
    [Header("對話事件")]
    [Tooltip("第一段對話開始時觸發；接續對話不重複觸發。啟用時若已有對話進行中，也會補發一次。")]
    public UnityEvent onDialogueStarted = new UnityEvent();

    [Tooltip("最後一段結束後，等待兩幀確認沒有接續對話才觸發。適用目前 StoryManager 的下一幀接力流程。")]
    public UnityEvent onDialogueEnded = new UnityEvent();

    private PixelCrushers.DialogueSystem.DialogueSystemController subscribedManager;
    private Coroutine pendingEnd;
    private bool dialogueSessionActive;

    private void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        while (DialogueManager.instance == null) yield return null;
        subscribedManager = DialogueManager.instance;
        subscribedManager.conversationStarted += HandleConversationStarted;
        subscribedManager.conversationEnded += HandleConversationEnded;

        // 場景中途啟用時同步當前狀態，讓結束事件仍有對應的開始事件。
        if (subscribedManager.isConversationActive)
            HandleConversationStarted(null);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        pendingEnd = null;
        if (subscribedManager != null)
        {
            subscribedManager.conversationStarted -= HandleConversationStarted;
            subscribedManager.conversationEnded -= HandleConversationEnded;
        }
        subscribedManager = null;
        // 停用不代表對話結束，因此不在這裡觸發結束事件。
        dialogueSessionActive = false;
    }

    private void HandleConversationStarted(Transform actor)
    {
        if (!isActiveAndEnabled) return;
        CancelPendingEnd();
        if (dialogueSessionActive) return;

        dialogueSessionActive = true;
        onDialogueStarted?.Invoke();
    }

    private void HandleConversationEnded(Transform actor)
    {
        if (!isActiveAndEnabled || !dialogueSessionActive) return;
        CancelPendingEnd();
        pendingEnd = StartCoroutine(ConfirmDialogueEnded());
    }

    private void CancelPendingEnd()
    {
        if (pendingEnd == null) return;
        StopCoroutine(pendingEnd);
        pendingEnd = null;
    }

    private IEnumerator ConfirmDialogueEnded()
    {
        // StoryManager 在下一幀接力；多等一幀，避免兩個協程的執行順序影響判斷。
        yield return null;
        yield return null;
        pendingEnd = null;

        if (subscribedManager != null && subscribedManager.isConversationActive) yield break;
        if (!dialogueSessionActive) yield break;

        // 先更新狀態，允許 UnityEvent 內啟動另一段對話。
        dialogueSessionActive = false;
        onDialogueEnded?.Invoke();
    }
}
