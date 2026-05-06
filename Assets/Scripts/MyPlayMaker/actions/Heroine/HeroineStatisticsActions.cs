using HutongGames.PlayMaker;
using UnityEngine;
using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

// ==========================================================
// 1. AddHeroineStatistic — 增加指定統計數值 (float)
// ==========================================================
[ActionCategory("Heroine Status")]
[Tooltip("增加指定女主角的統計數值（例如：內射ml量、飲精ml量）。適用於 float 類數值。")]
public class AddHeroineStatistic : FsmStateAction
{
    [RequiredField]
    [Tooltip("女主角的 HeroineID")]
    public FsmString heroineID;

    [RequiredField]
    [ObjectType(typeof(HeroineStatisticType))]
    [Tooltip("要累加的統計項目")]
    public FsmEnum statisticType;

    [RequiredField]
    [Tooltip("要增加的數值 (必須 ≥ 0)")]
    public FsmFloat amount;

    [Tooltip("執行後存入：該統計項目的當前累計值")]
    [UIHint(UIHint.Variable)]
    public FsmFloat storeCurrentValue;

    public override void Reset()
    {
        heroineID = null;
        statisticType = null;
        amount = 0f;
        storeCurrentValue = null;
    }

    public override void OnEnter()
    {
        if (!GameStatusService.Instance.Heroines.TryGetValue(heroineID.Value, out var heroine))
        {
            Debug.LogWarning($"[AddHeroineStatistic] 找不到女主角: {heroineID.Value}");
            Finish();
            return;
        }

        var type = (HeroineStatisticType)statisticType.Value;
        heroine.Statistics.Add(type, amount.Value);

        if (!storeCurrentValue.IsNone)
            storeCurrentValue.Value = heroine.Statistics.Get(type);

        Finish();
    }
}

// ==========================================================
// 2. IncrementHeroineStatistic — 指定統計項目 +1
// ==========================================================
[ActionCategory("Heroine Status")]
[Tooltip("將指定女主角的統計項目 +1（例如：總做愛次數、口交次數）。適用於次數類統計。")]
public class IncrementHeroineStatistic : FsmStateAction
{
    [RequiredField]
    [Tooltip("女主角的 HeroineID")]
    public FsmString heroineID;

    [RequiredField]
    [ObjectType(typeof(HeroineStatisticType))]
    [Tooltip("要 +1 的統計項目")]
    public FsmEnum statisticType;

    [Tooltip("執行後存入：該統計項目的當前累計值 (int)")]
    [UIHint(UIHint.Variable)]
    public FsmInt storeCurrentCount;

    public override void Reset()
    {
        heroineID = null;
        statisticType = null;
        storeCurrentCount = null;
    }

    public override void OnEnter()
    {
        if (!GameStatusService.Instance.Heroines.TryGetValue(heroineID.Value, out var heroine))
        {
            Debug.LogWarning($"[IncrementHeroineStatistic] 找不到女主角: {heroineID.Value}");
            Finish();
            return;
        }

        var type = (HeroineStatisticType)statisticType.Value;
        heroine.Statistics.Increment(type);

        if (!storeCurrentCount.IsNone)
            storeCurrentCount.Value = heroine.Statistics.GetInt(type);

        Finish();
    }
}

// ==========================================================
// 3. GetHeroineStatistic — 讀取指定統計項目的值
// ==========================================================
[ActionCategory("Heroine Status")]
[Tooltip("讀取指定女主角的統計數值。可同時取得 float 和 int 版本。")]
public class GetHeroineStatistic : FsmStateAction
{
    [RequiredField]
    [Tooltip("女主角的 HeroineID")]
    public FsmString heroineID;

    [RequiredField]
    [ObjectType(typeof(HeroineStatisticType))]
    [Tooltip("要查詢的統計項目")]
    public FsmEnum statisticType;

    [Tooltip("存入 float 值（例如：飲精 3.5ml）")]
    [UIHint(UIHint.Variable)]
    public FsmFloat storeFloatValue;

    [Tooltip("存入 int 值（例如：做愛 12 次）")]
    [UIHint(UIHint.Variable)]
    public FsmInt storeIntValue;

    public override void Reset()
    {
        heroineID = null;
        statisticType = null;
        storeFloatValue = null;
        storeIntValue = null;
    }

    public override void OnEnter()
    {
        if (!GameStatusService.Instance.Heroines.TryGetValue(heroineID.Value, out var heroine))
        {
            Debug.LogWarning($"[GetHeroineStatistic] 找不到女主角: {heroineID.Value}");
            Finish();
            return;
        }

        var type = (HeroineStatisticType)statisticType.Value;

        if (!storeFloatValue.IsNone)
            storeFloatValue.Value = heroine.Statistics.Get(type);

        if (!storeIntValue.IsNone)
            storeIntValue.Value = heroine.Statistics.GetInt(type);

        Finish();
    }
}

// ==========================================================
// 4. SetMaxHeroineStatistic — 紀錄型統計（取最大值）
// ==========================================================
[ActionCategory("Heroine Status")]
[Tooltip("若傳入的值大於目前紀錄則覆蓋。適用於「單次最多高潮次數」「連續射精次數」等紀錄型統計。通常在小遊戲結束時使用。")]
public class SetMaxHeroineStatistic : FsmStateAction
{
    [RequiredField]
    [Tooltip("女主角的 HeroineID")]
    public FsmString heroineID;

    [RequiredField]
    [ObjectType(typeof(HeroineStatisticType))]
    [Tooltip("要更新的紀錄型統計項目")]
    public FsmEnum statisticType;

    [RequiredField]
    [Tooltip("本次的數值（會與歷史最高比較，較大才覆蓋）")]
    public FsmInt value;

    [Tooltip("執行後存入：該統計項目的當前紀錄值")]
    [UIHint(UIHint.Variable)]
    public FsmInt storeCurrentRecord;

    public override void Reset()
    {
        heroineID = null;
        statisticType = null;
        value = 0;
        storeCurrentRecord = null;
    }

    public override void OnEnter()
    {
        if (!GameStatusService.Instance.Heroines.TryGetValue(heroineID.Value, out var heroine))
        {
            Debug.LogWarning($"[SetMaxHeroineStatistic] 找不到女主角: {heroineID.Value}");
            Finish();
            return;
        }

        var type = (HeroineStatisticType)statisticType.Value;
        heroine.Statistics.SetMax(type, value.Value);

        if (!storeCurrentRecord.IsNone)
            storeCurrentRecord.Value = heroine.Statistics.GetInt(type);

        Finish();
    }
}

// ==========================================================
// 5. RecordSexResult — 一次結算完整的性行為統計
// ==========================================================
[ActionCategory("Heroine Status")]
[Tooltip("一次性結算完整的性行為統計。自動累加總次數，並根據勾選項目累加對應統計。適合在場景結算時使用。")]
public class RecordSexResult : FsmStateAction
{
    [RequiredField]
    [Tooltip("女主角的 HeroineID")]
    public FsmString heroineID;

    [Header("── 發生了什麼 ──")]
    [Tooltip("是否有口交")]
    public FsmBool hadOral;

    [Tooltip("是否有內射")]
    public FsmBool hadCreampie;

    [Tooltip("內射量 (ml)，僅在 hadCreampie 為 true 時生效")]
    public FsmFloat creampieMl;

    [Tooltip("是否有飲精")]
    public FsmBool hadSwallow;

    [Tooltip("飲精量 (ml)，僅在 hadSwallow 為 true 時生效")]
    public FsmFloat swallowMl;

    [Tooltip("是否有顏射")]
    public FsmBool hadFacial;

    [Tooltip("是否有接吻")]
    public FsmBool hadKiss;

    [Tooltip("是否有口內射精")]
    public FsmBool hadOralCreampie;

    [Tooltip("是否有體外射精")]
    public FsmBool hadExternalEjaculation;

    [Tooltip("體外射精精液量 (ml)")]
    public FsmFloat externalEjaculationMl;

    [Tooltip("是否為夜襲")]
    public FsmBool wasNightCrawl;

    [Tooltip("是否為女主角主動要求")]
    public FsmBool wasInitiatedByHer;

    [Tooltip("是否有家人在附近")]
    public FsmBool wasFamilyNearby;

    [Header("── 高潮 / 射精紀錄 ──")]
    [Tooltip("本次的高潮次數（會累加到總高潮次數，並與歷史最高比較）")]
    public FsmInt orgasmCountThisSession;

    [Tooltip("本次的連續射精次數（會與歷史最高比較）")]
    public FsmInt consecutiveEjaculationThisSession;

    [Header("── 發生在哪裡 ──")]
    [Tooltip("地點 ID（例如：living_room、bathroom）。留空則不記錄地點統計。")]
    public FsmString locationID;

    public override void Reset()
    {
        heroineID = null;
        hadOral = false;
        hadCreampie = false;
        creampieMl = 0f;
        hadSwallow = false;
        swallowMl = 0f;
        hadFacial = false;
        hadKiss = false;
        hadOralCreampie = false;
        hadExternalEjaculation = false;
        externalEjaculationMl = 0f;
        wasNightCrawl = false;
        wasInitiatedByHer = false;
        wasFamilyNearby = false;
        orgasmCountThisSession = 0;
        consecutiveEjaculationThisSession = 0;
        locationID = new FsmString { UseVariable = true };
    }

    public override void OnEnter()
    {
        if (!GameStatusService.Instance.Heroines.TryGetValue(heroineID.Value, out var heroine))
        {
            Debug.LogWarning($"[RecordSexResult] 找不到女主角: {heroineID.Value}");
            Finish();
            return;
        }

        var stats = heroine.Statistics;

        // ── 總次數一定 +1 ──
        stats.Increment(HeroineStatisticType.TotalSexCount);

        // ── 行為類 ──
        if (hadOral.Value)
            stats.Increment(HeroineStatisticType.OralSexCount);

        if (hadCreampie.Value)
        {
            stats.Increment(HeroineStatisticType.CreampieCount);
            if (creampieMl.Value > 0f)
                stats.Add(HeroineStatisticType.CreampieMl, creampieMl.Value);
        }

        if (hadSwallow.Value && swallowMl.Value > 0f)
            stats.Add(HeroineStatisticType.SwallowedMl, swallowMl.Value);

        if (hadFacial.Value)
            stats.Increment(HeroineStatisticType.FacialCount);

        if (hadKiss.Value)
            stats.Increment(HeroineStatisticType.KissCount);

        if (hadOralCreampie.Value)
            stats.Increment(HeroineStatisticType.OralCreampieCount);

        if (hadExternalEjaculation.Value)
        {
            stats.Increment(HeroineStatisticType.ExternalEjaculationCount);
            if (externalEjaculationMl.Value > 0f)
                stats.Add(HeroineStatisticType.ExternalEjaculationMl, externalEjaculationMl.Value);
        }

        // ── 情境類 ──
        if (wasNightCrawl.Value)
            stats.Increment(HeroineStatisticType.NightCrawlCount);

        if (wasInitiatedByHer.Value)
            stats.Increment(HeroineStatisticType.InitiatedByHerCount);

        if (wasFamilyNearby.Value)
            stats.Increment(HeroineStatisticType.SexWithFamilyNearby);

        // ── 高潮 / 射精紀錄 ──
        if (orgasmCountThisSession.Value > 0)
        {
            stats.Add(HeroineStatisticType.TotalOrgasmCount, orgasmCountThisSession.Value);
            stats.SetMax(HeroineStatisticType.MaxOrgasmInOneSession, orgasmCountThisSession.Value);
        }

        if (consecutiveEjaculationThisSession.Value > 0)
        {
            stats.SetMax(HeroineStatisticType.MaxConsecutiveEjaculation, consecutiveEjaculationThisSession.Value);
        }

        // ── 依地點記錄 ──
        if (!locationID.IsNone && !string.IsNullOrEmpty(locationID.Value))
        {
            switch (locationID.Value)
            {
                case "bedroom":
                    stats.Increment(HeroineStatisticType.SexInBedroom);
                    break;
                case "living_room":
                    stats.Increment(HeroineStatisticType.SexInLivingRoom);
                    break;
                case "bathroom":
                    stats.Increment(HeroineStatisticType.SexInBathroom);
                    break;
                case "toilet":
                    stats.Increment(HeroineStatisticType.SexInToilet);
                    break;
                default:
                    Debug.Log($"[RecordSexResult] 未定義的地點統計: {locationID.Value}，已跳過。");
                    break;
            }
        }

        Finish();
    }
}