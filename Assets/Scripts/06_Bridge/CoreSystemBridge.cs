using UnityEngine;

/// <summary>
/// 提供給 Unity Events / Button / 動畫事件呼叫的公共接口。
/// 主角現役數值由正式 Model 管理。
/// </summary>
public class CoreSystemBridge : MonoBehaviour
{
    private ProtagonistStatusModel Model
    {
        get
        {
            if (GameStatusService.Instance == null)
            {
                Debug.LogError("[CoreSystemBridge] GameStatusService 尚未初始化。");
                return null;
            }
            return GameStatusService.Instance.Protagonist;
        }
    }

    private TimeSystemManager TimeManager => GameStatusService.Instance?.TimeManager;
    private SaveGameManager SaveManager => GameStatusService.Instance?.SaveManager;

    // ==========================================================
    // NHK 主角數值
    // ==========================================================

    // ==========================================================
    // 舊負面狀態已退役
    // ==========================================================

    // ==========================================================
    // Money / SkillPoints
    // ==========================================================
    public void AddMoney(int amount)
    {
        Model?.AddMoney(amount);
        Debug.Log($"[CoreSystemBridge] Money {amount:+#;-#;0}");
    }

    public void ReduceMoneyTest(int cost)
    {
        if (Model?.TryReduceMoney(cost) == true)
            Debug.Log($"[CoreSystemBridge] 成功扣除金錢: -{cost}");
        else
            Debug.LogWarning($"[CoreSystemBridge] 金錢不足，無法扣除: {cost}");
    }

    public void AddSkillPoints(int amount)
    {
        Model?.AddSkillPoints(amount);
        Debug.Log($"[CoreSystemBridge] SkillPoints {amount:+#;-#;0}");
    }

    public void TryReduceSkillPointsTest(int cost)
    {
        if (Model?.TryReduceSkillPoints(cost) == true)
            Debug.Log($"[CoreSystemBridge] 成功扣除技能點: -{cost}");
        else
            Debug.LogWarning($"[CoreSystemBridge] 技能點不足，無法扣除: {cost}");
    }

    // ==========================================================
    // 舊版相容 wrapper
    // ==========================================================
    public void AddStamina(int amount)
    {
        Model?.AddStamina(amount);
    }

    public void ReduceStamina(int amount)
    {
        if (amount > 0) Model?.ForceAddStamina(-amount);
    }

    public void AddActiont(int amount)
    {
        Debug.LogWarning("[CoreSystemBridge] AddActiont 已淘汰。NHK 使用固定白天/夜晚流程，不再使用 Action。");
    }

    public void TryReduceActionTest(int cost)
    {
        Debug.LogWarning("[CoreSystemBridge] TryReduceActionTest 已淘汰。NHK 不再使用 Action。");
    }

    public void AddStaminaMax(int amount) {
        Model?.AddStaminaMax(amount);
    }

    public void AddActionMax(int amount) => Debug.LogWarning("[CoreSystemBridge] AddActionMax 已淘汰。NHK 不使用 ActionMax。");

    // ==========================================================
    // 時間推進
    // ==========================================================
    public void AdvanceTimeSingleSlot()
    {
        TimeManager?.AdvanceTime(1);
        Debug.Log("[CoreSystemBridge] 時間前進 1 格。");
    }

    public void AdvanceTimeCustom(int slots)
    {
        TimeManager?.AdvanceTime(slots);
        Debug.Log($"[CoreSystemBridge] 時間前進 {slots} 格。");
    }

    public void NextDayTestButton()
    {
        if (TimeManager != null)
            TimeManager.AdvanceTime(99);
        else
            Debug.LogError("[CoreSystemBridge] TimeSystemManager 未初始化，無法 NextDay 測試。");
    }

    // ==========================================================
    // 新遊戲 / 存讀檔
    // ==========================================================
    [Header("新遊戲設定")]
    [SerializeField] private string newGameStartScene = "MyRoom";

    public void StartNewGameButton()
    {
        if (GameStatusService.Instance == null)
        {
            Debug.LogError("[CoreSystemBridge] GameStatusService 未初始化，無法啟動新遊戲。");
            return;
        }

        GameStatusService.Instance.StartNewGame();

        if (SceneController.Instance == null)
        {
            Debug.LogError("[CoreSystemBridge] SceneController.Instance 為 null。");
            return;
        }

        if (string.IsNullOrEmpty(newGameStartScene))
        {
            Debug.LogError("[CoreSystemBridge] 未設定 newGameStartScene。");
            return;
        }

        SceneController.ChangeScene(newGameStartScene);
    }

    public void SaveGameToSlot(int slotIndex)
    {
        SaveManager?.SaveGame(slotIndex);
        Debug.Log($"[CoreSystemBridge] Requested save slot {slotIndex}.");
    }

    public void LoadGameFromSlot(int slotIndex)
    {
        if (SaveManager == null)
        {
            Debug.LogError("[CoreSystemBridge] SaveManager 不存在，無法讀檔。");
            return;
        }

        if (!SaveManager.LoadGame(slotIndex))
            Debug.LogWarning($"[CoreSystemBridge] Failed to start load from slot {slotIndex}.");
    }
}
