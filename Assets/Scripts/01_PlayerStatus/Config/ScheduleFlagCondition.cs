using System.Collections.Generic;
using UnityEngine;

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
