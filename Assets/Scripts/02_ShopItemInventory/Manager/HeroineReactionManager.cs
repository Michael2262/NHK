using PixelCrushers.DialogueSystem;
using UnityEngine;

/// <summary>
/// 【重寫版本 4】
/// 負責執行「收下禮物後的反應」(對話 + UnityEvent)。
/// 
/// 資料來源改為 ItemConfigData:
///   - 每位女主角的專屬反應 → ItemConfigData.HeroineOverrides[n].Reaction
///   - 物品的預設反應       → ItemConfigData.DefaultGiftReaction
///   - 全域 fallback        → 此 Manager 上的 _globalFallbackReaction
///
/// 本 Manager 不再儲存 per-item 反應資料,只負責執行流程與全域保底。
/// </summary>
public class HeroineReactionManager : MonoBehaviour
{
    public static HeroineReactionManager Instance { get; private set; }

    [Header("全域 Fallback 反應")]
    [Tooltip("當物品沒有設定任何反應時(HeroineOverride.Reaction 與 DefaultGiftReaction 都空),\n" +
             "最後會 fallback 到這裡。")]
    [SerializeField] private GiftReaction _globalFallbackReaction = new GiftReaction();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("發現重複的 HeroineReactionManager,已銷毀。");
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; }
    }

    /// <summary>
    /// 【核心方法】觸發對指定禮物的反應。
    /// 會依序查找:HeroineOverride.Reaction → DefaultGiftReaction → GlobalFallback。
    /// </summary>
    public void TriggerGiftReaction(string heroineID, string itemID)
    {
        if (string.IsNullOrEmpty(itemID))
        {
            Debug.LogError("[HeroineReactionManager] 傳入的 itemID 為空!");
            return;
        }

        // 從 ItemDatabase 取得物品設定
        var database = GameStatusService.Instance?.ItemDatabase;
        var config = database?.GetItemConfig(itemID);

        if (config == null)
        {
            Debug.LogWarning($"[HeroineReactionManager] 找不到 ItemConfig: '{itemID}',改用全域 fallback。");
            ExecuteReaction(_globalFallbackReaction, $"GlobalFallback (itemID='{itemID}' not found)");
            return;
        }

        // 查詢反應(Override → Default → null)
        GiftReaction reaction = config.GetGiftReactionFor(heroineID);

        if (reaction != null)
        {
            ExecuteReaction(reaction, $"ItemConfig '{itemID}' for heroine '{heroineID}'");
        }
        else
        {
            // 物品沒設任何反應,走全域 fallback
            Debug.Log($"[HeroineReactionManager] Item '{itemID}' 沒有任何反應設定,使用全域 fallback。");
            ExecuteReaction(_globalFallbackReaction, "GlobalFallback");
        }
    }

    // ==========================================================
    // 內部輔助
    // ==========================================================

    /// <summary>
    /// 執行一個 GiftReaction:播對話 + 觸發 UnityEvent。
    /// </summary>
    private void ExecuteReaction(GiftReaction reaction, string sourceLabel)
    {
        if (reaction == null || !reaction.HasAnyReaction)
        {
            Debug.LogWarning($"[HeroineReactionManager] 反應為空或未設定內容 (來源: {sourceLabel})。");
            return;
        }

        // 1. 觸發對話
        if (!string.IsNullOrEmpty(reaction.DialogueConversationTitle))
        {
            Debug.Log($"[HeroineReactionManager] 觸發對話: {reaction.DialogueConversationTitle} (來源: {sourceLabel})");
            DialogueManager.StartConversation(reaction.DialogueConversationTitle, null, null);
        }

        // 2. 觸發 UnityEvent
        if (reaction.ReactionEvent != null && reaction.ReactionEvent.GetPersistentEventCount() > 0)
        {
            Debug.Log($"[HeroineReactionManager] 觸發 Unity Event (來源: {sourceLabel})");
            reaction.ReactionEvent.Invoke();
        }
    }
}