using UnityEngine;

/// <summary>
/// RequestRoll 擲骰結果。
/// </summary>
public struct RequestRollResult
{
    /// <summary>是否通過。</summary>
    public bool Pass;

    /// <summary>計算出的成功率（0~100），供 debug / UI 用。</summary>
    public float SuccessRate;

    /// <summary>擲骰當下取到的驅動值（絕對值）。</summary>
    public int DriverValue;

    /// <summary>是否因 GuaranteedAbove 直接必過（未經擲骰）。</summary>
    public bool Guaranteed;
}

/// <summary>
/// 請求擲骰邏輯（純 C#，無狀態）。
/// 流程：依 Driver 取驅動值 → 算成功率 → 擲骰 → 回傳結果。
/// 所有遊戲狀態一律從 GameStatusService.Instance 取。
/// </summary>
public static class RequestRoller
{
    public static RequestRollResult Roll(RequestArchetype archetype, string heroineID)
    {
        var result = new RequestRollResult();

        if (archetype == null)
        {
            Debug.LogWarning("[RequestRoller] archetype 為 null，視為失敗。");
            return result; // Pass = false
        }

        int v = ResolveDriverValue(archetype.Driver, heroineID);
        result.DriverValue = v;

        float rate = archetype.ComputeSuccessRate(v);
        result.SuccessRate = rate;

        // 保證過：驅動值達穩過線直接必過，不擲骰。
        if (archetype.GuaranteedAbove && v >= archetype.THigh)
        {
            result.Guaranteed = true;
            result.Pass = true;
            return result;
        }

        result.Pass = Random.value * 100f < rate;
        return result;
    }

    /// <summary>依 DriverStat 從 GameStatusService 取當前絕對值。取不到一律回 0。</summary>
    public static int ResolveDriverValue(DriverStat driver, string heroineID)
    {
        var svc = GameStatusService.Instance;
        if (svc == null)
        {
            Debug.LogWarning("[RequestRoller] GameStatusService.Instance 為 null。");
            return 0;
        }

        switch (driver)
        {
            case DriverStat.Heroine_Trust:
                return GetHeroine(svc, heroineID)?.Trust ?? 0;
            case DriverStat.Heroine_Libido:
                return GetHeroine(svc, heroineID)?.Libido ?? 0;
            case DriverStat.Protagonist_LifePower:
                return svc.Protagonist?.LifePower ?? 0;
            case DriverStat.Protagonist_Sociality:
                return svc.Protagonist?.Sociality ?? 0;
            case DriverStat.Protagonist_Dependency:
                return svc.Protagonist?.Dependency ?? 0;
            default:
                Debug.LogWarning($"[RequestRoller] 未處理的 DriverStat：{driver}，回 0。");
                return 0;
        }
    }

    private static HeroineStatusModel GetHeroine(GameStatusService svc, string heroineID)
    {
        if (svc.Heroines == null || string.IsNullOrEmpty(heroineID))
            return null;

        svc.Heroines.TryGetValue(heroineID, out var heroine);
        if (heroine == null)
            Debug.LogWarning($"[RequestRoller] 找不到女主角：{heroineID}");
        return heroine;
    }
}
