using System.Collections.Generic;
using UnityEngine;

// 職責：定義「單一女主角」的完整靜態日程表。
// 這會是一個 ScriptableObject 或 JSON 檔案。
[CreateAssetMenu(menuName = "Game/Config/Heroine Schedule")]
public class HeroineScheduleConfig : ScriptableObject
{
    [Tooltip("必須與 HeroineStatusConfig 中的 ID 一致")]
    public string heroineID; // 例如："sister"

    // ==========================================================
    // 【NEW】每日行為鍊 (主要排程來源)
    // ==========================================================
    [Header("=== 每日行為鍊 (Chain-based Schedule) ===")]
    [Tooltip("每天開始時，系統會從符合條件的鍊中，依權重隨機抽一條執行一整天。\n" +
             "鍊是「一整天的故事線」，適合用於有因果關係的行為 (例如：洗澡→睡覺，不會在隔天又重複洗澡)。")]
    public List<DailyChain> dailyChains;

    // ==========================================================
    // 【保留】舊的 per-slot 排程 (作為 Fallback)
    // ==========================================================
    [Header("=== Fallback: 傳統時段排程 ===")]
    [Tooltip("當今天沒有任何可用的鍊被抽中時，使用這裡的傳統排程。\n" +
             "也可以完全不使用鍊，讓系統跑舊邏輯。")]
    public List<ScheduleBlock> schedule;
}

// ==========================================================
// 【NEW】DailyChain：一整天的行為鍊
// ==========================================================

/// <summary>
/// 一整天的行為鍊。從早到晚一口氣規劃好，用於表達有因果/序列性的動作。
/// 例如：「P1S1 進家門 → P2S1 洗澡 → P2S2 看電視 → P3S1 睡覺」
/// </summary>
[System.Serializable]
public class DailyChain
{
    [Header("Editor (易讀性)")]
    [Tooltip("這條鍊的名稱，純粹為了 Inspector 易讀性。例如：「平日慵懶線」、「假日購物線」")]
    public string chainName;

    [Header("WHEN: 鍊的觸發條件")]
    [Tooltip("此鍊在哪種日期生效")]
    public ScheduleDayType dayType = ScheduleDayType.EveryDay;

    [Tooltip("第一項 Flag 條件；保留舊設定。空值略過，反轉時須未持有此 Flag。")]
    public ProgressFlagDefinition requiredFlag;
    public bool invertRequiredFlag;

    [Tooltip("第二項 Flag 條件；保留舊禁止 Flag，預設為反向。")]
    public ProgressFlagDefinition forbiddenFlag;
    [Tooltip("勾選表示將舊禁止 Flag 改為正向；預設 false 以相容舊資產。日程編輯器會以反轉勾選顯示。")]
    public bool forbiddenFlagIsPositive;

    public ScheduleFlagMatchMode flagMatchMode = ScheduleFlagMatchMode.And;
    public List<ScheduleFlagCondition> additionalFlagConditions = new List<ScheduleFlagCondition>();

    public bool MatchesFlags(ProgressFlagModel flags)
        => ScheduleFlagCondition.Matches(flags, flagMatchMode, requiredFlag, invertRequiredFlag,
            additionalFlagConditions, forbiddenFlag, !forbiddenFlagIsPositive);

    [Header("CHANCE: 鍊之間的加權抽籤")]
    [Tooltip("鍊被抽中的權重。系統會在所有「符合條件」的鍊中，依此權重加權抽一條。\n" +
             "設為 0 表示不會被隨機抽中 (可配合 requiredFlag 做劇情觸發)")]
    public int probabilityWeight = 100;

    [Header("WHAT: 鍊中各時段的安排")]
    [Tooltip("這條鍊在一天各時段的安排。沒填的時段會 fallback 到舊的 schedule。")]
    public List<ChainBlock> blocks;
}

/// <summary>
/// 鍊中的一個時段格子。結構類似舊的 ScheduleBlock,但屬於某條鍊。
/// </summary>
[System.Serializable]
public class ChainBlock
{
    [Header("Editor (易讀性)")]
    [Tooltip("此時段的描述 (例如: \"白天1\")")]
    public string timeName;

    [Header("WHEN: 時間位置")]
    [Tooltip("對應 TimeSystemModel 的 Phase 索引 (0=Day, 1=Dusk...)")]
    public int phaseIndex;

    [Tooltip("對應 TimeSystemModel 的 Slot 索引 (1, 2...)")]
    public int slotIndex;

    [Header("WHERE & WHAT: 情境配置")]
    [Tooltip("此時段可能的情境。若多於一個，系統會在此鍊被選中後，再依權重抽一個。\n" +
             "大多數情況只放一個，鍊的骨幹應該是明確的;多個情境用於鍊內的局部變化。")]
    public List<ScheduleScenario> possibleScenarios;
}

// ==========================================================
// 【保留】舊的 ScheduleBlock / ScheduleScenario
// ==========================================================

/// <summary>
/// 一個「時間區塊」的定義 (舊版 per-slot 抽籤用)
/// </summary>
[System.Serializable]
public class ScheduleBlock
{
    [Header("Editor (易讀性)")]
    [Tooltip("【此時段的描述 (例如: \"白天1\")")]
    public string timeName;

    [Header("WHEN: 時間條件")]
    [Tooltip("對應 TimeSystemModel 的 Phase 索引 (0=Day, 1=Dusk...)")]
    public int phaseIndex;

    [Tooltip("對應 TimeSystemModel 的 Slot 索引 (1, 2...)")]
    public int slotIndex;

    [Tooltip("此行程在哪種日期生效")]
    public ScheduleDayType dayType = ScheduleDayType.EveryDay;

    [Header("WHERE & WHAT: 情境配置")]
    [Tooltip("在這個時間點，女主角「可能」出現的所有情境。" +
             "系統會根據權重隨機抽取一個。")]
    public List<ScheduleScenario> possibleScenarios;
}

/// <summary>
/// 一個具體的「情境」定義
/// </summary>
[System.Serializable]
public class ScheduleScenario
{
    [Tooltip("此情境發生的地點 ID (必須與 LocationConfiguration 的 ID 對應)")]
    public string locationID;

    [Tooltip("此情境下，女主角正在進行的「活動」ID")]
    public string activityState;

    [Tooltip("此情境被抽中的「權重」，可實現隨機")]
    public int probabilityWeight = 100;

    [Tooltip("第一項 Flag 條件；保留舊設定。空值略過，反轉時須未持有此 Flag。")]
    public ProgressFlagDefinition requiredFlag;
    public bool invertRequiredFlag;
    public ScheduleFlagMatchMode flagMatchMode = ScheduleFlagMatchMode.And;
    public List<ScheduleFlagCondition> additionalFlagConditions = new List<ScheduleFlagCondition>();

    public bool MatchesFlags(ProgressFlagModel flags)
        => ScheduleFlagCondition.Matches(flags, flagMatchMode, requiredFlag, invertRequiredFlag,
            additionalFlagConditions);
}

public enum ScheduleFlagMatchMode
{
    [InspectorName("AND（全部成立）")] And = 0,
    [InspectorName("OR（任一成立）")] Or = 1
}

/// <summary>日程共用 Flag 條件；反轉逐項套用，再依 AND／OR 合併。</summary>
[System.Serializable]
public class ScheduleFlagCondition
{
    public ProgressFlagDefinition flag;
    public bool invert;

    public static bool Matches(ProgressFlagModel flags, ScheduleFlagMatchMode mode,
        ProgressFlagDefinition firstFlag, bool firstInvert, List<ScheduleFlagCondition> additional,
        ProgressFlagDefinition secondFlag = null, bool secondInvert = true)
    {
        bool matchAny = mode == ScheduleFlagMatchMode.Or;
        bool hasCondition = false;
        if (firstFlag != null)
        {
            hasCondition = true;
            if (Evaluate(flags, firstFlag, firstInvert) == matchAny) return matchAny;
        }
        if (secondFlag != null)
        {
            hasCondition = true;
            if (Evaluate(flags, secondFlag, secondInvert) == matchAny) return matchAny;
        }
        if (additional != null)
        {
            foreach (var condition in additional)
            {
                if (condition == null || condition.flag == null) continue;
                hasCondition = true;
                if (Evaluate(flags, condition.flag, condition.invert) == matchAny) return matchAny;
            }
        }
        // 與 Tool Button Group 相同：空列略過；全部留空表示不限制。
        return !hasCondition || !matchAny;
    }

    private static bool Evaluate(ProgressFlagModel flags, ProgressFlagDefinition flag, bool invert)
        => flags != null && (flags.Contains(flag.FlagID) != invert);
}
