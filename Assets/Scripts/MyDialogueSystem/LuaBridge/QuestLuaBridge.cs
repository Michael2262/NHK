using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// 將 Quest 查詢提供給 Dialogue Conditions，啟停、成功計次與領獎提供給 Script。
/// 掛在 GameStatusService 同一個常駐 GameObject，整個遊戲保留一份。
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("NHK/Quest/Quest Lua Bridge")]
public sealed class QuestLuaBridge : MonoBehaviour
{
    private static QuestLuaBridge _registeredBridge;

    private void OnEnable()
    {
        if (_registeredBridge != null)
        {
            if (_registeredBridge != this)
                Debug.LogWarning("[QuestLuaBridge] 已有啟用中的 Quest Lua Bridge，略過重複註冊；請只保留一份常駐元件。", this);
            return;
        }
        Lua.RegisterFunction(nameof(QuestIsActive), this,
            SymbolExtensions.GetMethodInfo(() => QuestIsActive(string.Empty)));
        Lua.RegisterFunction(nameof(QuestHasIncomplete), this,
            SymbolExtensions.GetMethodInfo(() => QuestHasIncomplete()));
        Lua.RegisterFunction(nameof(QuestHasUnclaimedRewards), this,
            SymbolExtensions.GetMethodInfo(() => QuestHasUnclaimedRewards()));
        Lua.RegisterFunction(nameof(QuestSetActive), this,
            SymbolExtensions.GetMethodInfo(() => QuestSetActive(string.Empty, false)));
        Lua.RegisterFunction(nameof(QuestIncreaseSuccessCount), this,
            SymbolExtensions.GetMethodInfo(() => QuestIncreaseSuccessCount(string.Empty)));
        Lua.RegisterFunction(nameof(QuestClaimNextReward), this,
            SymbolExtensions.GetMethodInfo(() => QuestClaimNextReward()));
        _registeredBridge = this;
    }

    private void OnDisable()
    {
        // 未取得註冊權的重複場景物件，不得解除常駐 Bridge 的函式。
        if (_registeredBridge != this) return;
        Lua.UnregisterFunction(nameof(QuestIsActive));
        Lua.UnregisterFunction(nameof(QuestHasIncomplete));
        Lua.UnregisterFunction(nameof(QuestHasUnclaimedRewards));
        Lua.UnregisterFunction(nameof(QuestSetActive));
        Lua.UnregisterFunction(nameof(QuestIncreaseSuccessCount));
        Lua.UnregisterFunction(nameof(QuestClaimNextReward));
        _registeredBridge = null;
    }

    /// <summary>Condition：QuestIsActive("Quest_ID")。未知 ID 或尚未初始化時為 false。</summary>
    public bool QuestIsActive(string questID)
    {
        var model = GameStatusService.Instance?.Quests;
        return !string.IsNullOrWhiteSpace(questID) && model?.GetProgress(questID)?.IsActive == true;
    }

    /// <summary>Condition：QuestHasIncomplete()。只查已啟動且未完成的任務。</summary>
    public bool QuestHasIncomplete()
        => GameStatusService.Instance?.Quests?.HasActiveIncompleteQuests == true;

    /// <summary>Condition：QuestHasUnclaimedRewards()。查任一已完成但未領取的任務，不限制啟動狀態。</summary>
    public bool QuestHasUnclaimedRewards()
        => GameStatusService.Instance?.Quests?.HasUnclaimedRewards == true;

    /// <summary>Script：QuestSetActive("Quest_ID", true/false)。保留既有次數、日期與領獎紀錄。</summary>
    public bool QuestSetActive(string questID, bool active)
    {
        var model = GameStatusService.Instance?.Quests;
        if (model == null)
        {
            Debug.LogWarning("[QuestLuaBridge] Quest 尚未初始化，無法設定任務啟停。", this);
            return false;
        }
        var result = model.SetActive(questID, active);
        if (result == QuestResult.Success) return true;
        Debug.LogWarning($"[QuestLuaBridge] 任務「{questID}」啟停設定未執行：{result}。", this);
        return false;
    }

    /// <summary>Script：QuestIncreaseSuccessCount("Quest_ID")。成功次數加一，沿用啟動、完成與每日限制。</summary>
    public bool QuestIncreaseSuccessCount(string questID)
    {
        var model = GameStatusService.Instance?.Quests;
        if (model == null)
        {
            Debug.LogWarning("[QuestLuaBridge] Quest 尚未初始化，無法增加任務成功次數。", this);
            return false;
        }
        var result = model.TryRecordAction(questID);
        if (result == QuestResult.Success) return true;
        Debug.LogWarning($"[QuestLuaBridge] 任務「{questID}」成功次數加一未執行：{result}。", this);
        return false;
    }

    /// <summary>Script：QuestClaimNextReward()。依資料庫順序領取一筆，成功後標記已領取並停用任務。</summary>
    public bool QuestClaimNextReward()
    {
        var rewards = GameStatusService.Instance?.QuestRewards;
        if (rewards == null)
        {
            Debug.LogWarning("[QuestLuaBridge] Quest 獎勵服務尚未初始化，無法領獎。", this);
            return false;
        }
        var result = rewards.ClaimNextReward();
        if (result == QuestResult.Success) return true;
        // 無待領獎勵是正常結果；獎勵設定錯誤已由服務輸出明確原因。
        if (result != QuestResult.NoUnclaimedReward && result != QuestResult.InvalidReward)
            Debug.LogWarning($"[QuestLuaBridge] 本次未領取獎勵：{result}。", this);
        return false;
    }
}
