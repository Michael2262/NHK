using UnityEngine;

public class StoryBridge : MonoBehaviour
{
    //--------------------------------------------------------------
    // 公告橋接方法

    /// <summary>
    /// 顯示一般系統公告:傳入本地化 Key (查 Text Table)。
    /// 查不到時 fallback 顯示原字串,故直接傳純文字也可運作。
    /// </summary>
    public void ShowAlert(string key)
    {
        if (StoryManager.Instance != null)
            StoryManager.Instance.ShowLocalizedMessage(key);
    }

    /// <summary>
    /// 顯示一般系統公告:傳入「已本地化好的純文字」,不查表直接顯示。
    /// </summary>
    public void ShowAlertRaw(string message)
    {
        if (StoryManager.Instance != null)
            StoryManager.Instance.ShowSystemMessage(message);
    }

    /// <summary>
    /// 顯示「綠色-體力」公告：傳入本地化 Key (自動套用 [em3])
    /// </summary>
    public void ShowEnergyMessage(string key)
    {
        if (StoryManager.Instance != null)
            StoryManager.Instance.ShowLocalizedEnergyMessage(key);
    }

    /// <summary>
    /// 顯示「紅色-負面」公告：傳入本地化 Key (自動套用 [em2])
    /// </summary>
    public void ShowBadMessage(string key)
    {
        if (StoryManager.Instance != null)
            StoryManager.Instance.ShowLocalizedBadMessage(key);
    }

    //--------------------------------------------------------------
    // {{wait}} 延遲設定橋接方法

    /// <summary>
    /// 將 {{wait}} 延遲秒數重設為 Inspector 的原始預設值（給 Button.onClick 用）
    /// </summary>
    public void ResetWaitDelay()
    {
        if (StoryManager.Instance != null)
            StoryManager.Instance.ResetCustomDelay();
    }

    //--------------------------------------------------------------
    // 播放對話橋接方法

    /// <summary>
    /// 預設播放：使用 Skip 模式 (若已有對話則不執行新對話)
    /// </summary>
    public void PlayConversation(string conversationTitle)
    {
        if (StoryManager.Instance != null)
            StoryManager.Instance.PlayConversation(conversationTitle, DialogueMode.Skip);
    }

    /// <summary>
    /// 中斷播放：立即停止當前對話並播放新的
    /// </summary>
    public void PlayConversationInterrupt(string conversationTitle)
    {
        if (StoryManager.Instance != null)
            StoryManager.Instance.PlayConversation(conversationTitle, DialogueMode.Interrupt);
    }

    /// <summary>
    /// 排隊播放：等當前對話播完後自動播放
    /// </summary>
    public void PlayConversationQueue(string conversationTitle)
    {
        if (StoryManager.Instance != null)
            StoryManager.Instance.PlayConversation(conversationTitle, DialogueMode.Queue);
    }
}