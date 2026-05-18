using UnityEngine;

/// <summary>
/// 提供給 Unity Events / Button / 動畫事件呼叫的公共接口。
/// NHK 版：核心主角數值為 Stress / LifePower / Sociality / Dependency，並保留 Money / SkillPoints。
/// 舊版 Stamina / Spirit / Action / Suspicion 等方法保留為相容 wrapper，避免 UnityEvent 參照瞬間斷掉。
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
    public void AddStress(int amount) => Model?.AddStress(amount);
    public void ReduceStress(int amount) => Model?.ReduceStress(amount);
    public void SetStress(int value) => Model?.SetStress(value);

    public void AddLifePower(int amount) => Model?.AddLifePower(amount);
    public void ReduceLifePower(int amount) => Model?.ReduceLifePower(amount);
    public void SetLifePower(int value) => Model?.SetLifePower(value);

    public void AddSociality(int amount) => Model?.AddSociality(amount);
    public void ReduceSociality(int amount) => Model?.ReduceSociality(amount);
    public void SetSociality(int value) => Model?.SetSociality(value);

    public void AddDependency(int amount) => Model?.AddDependency(amount);
    public void ReduceDependency(int amount) => Model?.ReduceDependency(amount);
    public void SetDependency(int value) => Model?.SetDependency(value);

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
        // 舊「體力增加」在 NHK 中視為壓力下降。
        Model?.AddStress(-amount);
        Debug.LogWarning("[CoreSystemBridge] AddStamina 已淘汰，已轉換為 Stress 反向變化。");
    }

    public void ReduceStamina(int amount)
    {
        // 舊「體力消耗」在 NHK 中視為壓力上升。
        Model?.AddStress(amount);
        Debug.LogWarning("[CoreSystemBridge] ReduceStamina 已淘汰，已轉換為 Stress 上升。");
    }

    public void AddSpirit(int amount)
    {
        Model?.AddLifePower(amount);
        Debug.LogWarning("[CoreSystemBridge] AddSpirit 已淘汰，已轉換為 LifePower 變化。");
    }

    public void ReduceSpirit(int amount)
    {
        Model?.ReduceLifePower(amount);
        Debug.LogWarning("[CoreSystemBridge] ReduceSpirit 已淘汰，已轉換為 LifePower 下降。");
    }

    public void AddActiont(int amount)
    {
        Debug.LogWarning("[CoreSystemBridge] AddActiont 已淘汰。NHK 使用固定白天/夜晚流程，不再使用 Action。");
    }

    public void TryReduceActionTest(int cost)
    {
        Debug.LogWarning("[CoreSystemBridge] TryReduceActionTest 已淘汰。NHK 不再使用 Action。");
    }

    public void AddAttack(int amount)
    {
        Model?.AddLifePower(amount);
        Debug.LogWarning("[CoreSystemBridge] AddAttack 已淘汰，暫轉為 LifePower 增加。");
    }

    public void ReduceAttack(int amount)
    {
        Model?.ReduceLifePower(amount);
        Debug.LogWarning("[CoreSystemBridge] ReduceAttack 已淘汰，暫轉為 LifePower 下降。");
    }

    public void AddDefense(int amount)
    {
        Model?.ReduceStress(amount);
        Debug.LogWarning("[CoreSystemBridge] AddDefense 已淘汰，暫轉為 Stress 下降。");
    }

    public void ReduceDefense(int amount)
    {
        Model?.AddStress(amount);
        Debug.LogWarning("[CoreSystemBridge] ReduceDefense 已淘汰，暫轉為 Stress 上升。");
    }

    public void AddSuspicion(int amount)
    {
        Model?.AddStress(amount);
        Debug.LogWarning("[CoreSystemBridge] AddSuspicion 已淘汰，已轉換為 Stress 上升。");
    }

    public void ReducSuspicion(int amount)
    {
        Model?.ReduceStress(amount);
        Debug.LogWarning("[CoreSystemBridge] ReducSuspicion 已淘汰，已轉換為 Stress 下降。");
    }

    public void AddStaminaMax(int amount) => Debug.LogWarning("[CoreSystemBridge] AddStaminaMax 已淘汰。NHK Stress 固定 0～100。");
    public void AddSpiritMax(int amount) => Debug.LogWarning("[CoreSystemBridge] AddSpiritMax 已淘汰。NHK 不使用 SpiritMax。");
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