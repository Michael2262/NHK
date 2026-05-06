using System.Collections.Generic;

/// <summary>
/// 這就是「資料背包」。
/// 我們把小遊戲可能需要的所有資料都裝在這裡，一次傳遞過去。
/// </summary>
public class MinigameContext
{
    // 原本的資料
    public ProtagonistStatusModel Protagonist { get; private set; }
    public List<HeroineStatusModel> ActiveHeroines { get; private set; }

    // ★ 你新增要求的資料
    public ProtagonistSkillModel Skills { get; private set; }
    public TimeSystemModel Time { get; private set; }
    public Dictionary<string, RiskAgentModel> RiskAgents { get; private set; }
    public ProtagonistStatusEffectModel StatusEffect { get; private set; }
    public ProgressFlagModel ProgressFlags { get; private set; }
    public CurrentScenarioModel Scenario { get; private set; }
    public TimeSystemManager TimeManager { get; private set; }

    // 建構函式：負責把資料裝進包包
    public MinigameContext(
        ProtagonistStatusModel protagonist,
        List<HeroineStatusModel> activeHeroines,
        ProtagonistSkillModel skills,
        TimeSystemModel time,
        Dictionary<string, RiskAgentModel> riskAgents,
        ProtagonistStatusEffectModel statusEffect,
        ProgressFlagModel progressFlags,
        CurrentScenarioModel scenario,
        TimeSystemManager timeManager)
    {
        Protagonist = protagonist;
        ActiveHeroines = activeHeroines;
        Skills = skills;
        Time = time;
        RiskAgents = riskAgents;
        StatusEffect = statusEffect;
        ProgressFlags = progressFlags;
        Scenario = scenario;
        TimeManager = timeManager;
    }
}