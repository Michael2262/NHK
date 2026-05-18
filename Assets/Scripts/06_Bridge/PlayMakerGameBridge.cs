using UnityEngine;

/// <summary>
/// NHK 版：PlayMaker FSMs 與 GameStatusService 核心 Model 溝通的橋樑。
/// 保留原 class 名，移除舊 Action / Suspicion / ExcuseCharges 依賴。
/// </summary>
[DefaultExecutionOrder(-800)]
public class PlayMakerGameBridge : MonoBehaviour
{
    private GameStatusService _service;

    void Awake()
    {
        _service = GameStatusService.Instance;
        if (_service == null)
        {
            Debug.LogError("PlayMakerGameBridge 找不到 GameStatusService.Instance！請確保 GameStatusService 已啟動。");
        }
    }

    // ==========================================================
    // Protagonist Getters
    // ==========================================================

    public ProtagonistStatusModel Protagonist => _service?.Protagonist;
    public TimeSystemModel Time => _service?.Time;

    public int PlayerStress => Protagonist?.Stress ?? 0;
    public int PlayerLifePower => Protagonist?.LifePower ?? 0;
    public int PlayerSociality => Protagonist?.Sociality ?? 0;
    public int PlayerDependency => Protagonist?.Dependency ?? 0;
    public int PlayerMoney => Protagonist?.Money ?? 0;
    public int PlayerSkillPoints => Protagonist?.SkillPoints ?? 0;
    public int CurrentDay => Protagonist?.Day ?? 1;
    public int CurrentPhase => Time?.CurrentPhaseIndex ?? 0;

    // Legacy property aliases. Prefer NHK properties above.
    public int PlayerAction => 0;
    public int PlayerSuspicion => 0;
    public int PlayerExcuseCharges => 0;

    // ==========================================================
    // Protagonist Actions
    // ==========================================================

    public void AdvanceGamePhase()
    {
        _service?.TimeManager?.AdvanceTime(1);
    }

    public void AddPlayerStress(int amount) => Protagonist?.AddStress(amount);
    public void ReducePlayerStress(int amount) => Protagonist?.ReduceStress(amount);
    public void AddPlayerLifePower(int amount) => Protagonist?.AddLifePower(amount);
    public void ReducePlayerLifePower(int amount) => Protagonist?.ReduceLifePower(amount);
    public void AddPlayerSociality(int amount) => Protagonist?.AddSociality(amount);
    public void ReducePlayerSociality(int amount) => Protagonist?.ReduceSociality(amount);
    public void AddPlayerDependency(int amount) => Protagonist?.AddDependency(amount);
    public void ReducePlayerDependency(int amount) => Protagonist?.ReduceDependency(amount);

    public void AddPlayerMoney(int amount) => Protagonist?.AddMoney(amount);
    public bool TryReducePlayerMoney(int cost) => Protagonist?.TryReduceMoney(cost) ?? false;
    public void AddPlayerSkillPoints(int amount) => Protagonist?.AddSkillPoints(amount);
    public bool TryReducePlayerSkillPoints(int cost) => Protagonist?.TryReduceSkillPoints(cost) ?? false;

    // Legacy method aliases. These are kept to avoid old FSM references breaking immediately.
    public void AddPlayerAction(int amount) => Debug.LogWarning("[PlayMakerGameBridge] AddPlayerAction ignored in NHK.");
    public bool TryReducePlayerAction(int cost) => true;
    public void AddPlayerSuspicion(int amount) => AddPlayerStress(amount);
    public void AddPlayerExcuseCharge(int amount) => Debug.LogWarning("[PlayMakerGameBridge] AddPlayerExcuseCharge ignored in NHK.");
    public void ReducePlayerExcuseCharge(int amount) => Debug.LogWarning("[PlayMakerGameBridge] ReducePlayerExcuseCharge ignored in NHK.");

    // ==========================================================
    // Heroine helpers - kept from previous version
    // ==========================================================

    private HeroineStatusModel GetHeroine(string heroineId)
    {
        if (_service != null && _service.Heroines.TryGetValue(heroineId, out var heroine))
        {
            return heroine;
        }
        Debug.LogWarning($"PlayMakerGameBridge: 找不到 ID 為 {heroineId} 的女主角。");
        return null;
    }

    public int GetHeroineDiscomfortMax(string heroineId)
    {
        HeroineStatusModel heroine = GetHeroine(heroineId);
        return heroine != null ? heroine.DiscomfortMax : 100;
    }

    public int GetHeroineExcitement(string heroineId)
    {
        HeroineStatusModel heroine = GetHeroine(heroineId);
        return heroine != null ? heroine.BaseExcitementExp : 0;
    }

    public int GetHeroineExcitementLv(string heroineId)
    {
        HeroineStatusModel heroine = GetHeroine(heroineId);
        return heroine != null ? heroine.BaseExcitementLevel : 0;
    }

    public void AddHeroineExcitement(string heroineId, int amount)
    {
        GetHeroine(heroineId)?.AddExcitementExp(amount);
    }

    public void SetHeroineExcitement(string heroineId, int level, int exp)
    {
        GetHeroine(heroineId)?.SetExcitement(level, exp);
    }

    public void AddHeroineDiscomfort(string heroineId, int amount)
    {
        GetHeroine(heroineId)?.AddDiscomfort(amount);
    }

    public void ResetHeroineDiscomfort(string heroineId)
    {
        GetHeroine(heroineId)?.ResetDiscomfort();
    }

    public void AddHeroineOrgasm(string heroineId, int amount)
    {
        GetHeroine(heroineId)?.AddOrgasm(amount);
    }
}