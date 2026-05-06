using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 職責：定義一個「條件對話」的觸發規則。
/// </summary>
[System.Serializable]
public class StoryCondition
{
    [Tooltip("要播放的對話標題 (Dialogue System 中的 Conversation Title)")]
    public string conversationTitle;

    [Tooltip("對話播放模式")]
    public DialogueMode mode = DialogueMode.Skip;

    [Tooltip("是否預設滿足？\nTrue = 只要檢查到這條就直接播。\nFalse = 需檢查下方 Flag。")]
    public bool isDefault = false;

    [Tooltip("需要檢查的 ProgressFlag SO。")]
    public ProgressFlagDefinition requiredFlag;
}

public class ConditionalStoryTrigger : MonoBehaviour
{
    [Header("條件清單 (由上而下優先權最高)")]
    public List<StoryCondition> storyConditions;

    /// <summary>
    /// 外部呼叫接口：嘗試觸發對話
    /// 可以綁定在 Button 的 OnClick，或由其他腳本呼叫。
    /// </summary>
    public void TriggerStory()
    {
        if (StoryManager.Instance == null)
        {
            Debug.LogError("找不到 StoryManager Instance！");
            return;
        }

        var flags = GameStatusService.Instance?.ProgressFlags;
        if (flags == null)
        {
            Debug.LogError("找不到 ProgressFlagModel，無法判斷條件！");
            return;
        }

        // 遍歷列表，找到第一個符合條件的就執行並中斷
        foreach (var condition in storyConditions)
        {
            if (string.IsNullOrEmpty(condition.conversationTitle)) continue;

            bool isMet = false;

            if (condition.isDefault)
            {
                isMet = true;
            }
            else if (condition.requiredFlag != null)
            {
                // 檢查是否擁有對應的 Flag
                isMet = flags.Contains(condition.requiredFlag.FlagID);
            }

            if (isMet)
            {
                Debug.Log($"[ConditionalStoryTrigger] 條件滿足，播放對話: {condition.conversationTitle}");
                StoryManager.Instance.PlayConversation(condition.conversationTitle, condition.mode);

                // 【核心要求 3】依 List 從上而下第一個為主，找到後立刻 return
                return;
            }
        }

        Debug.Log("[ConditionalStoryTrigger] 沒有任何條件被滿足，不播放對話。");
    }
}