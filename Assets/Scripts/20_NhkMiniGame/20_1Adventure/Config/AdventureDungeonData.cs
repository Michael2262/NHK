using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一個冒險地點（Dungeon）。定義里程目標、休息規則、通關旗標，以及依里程分段的牌池。
/// 命名用 Dungeon 以避免跟既有的 LocationData 語意混淆。
/// </summary>
[CreateAssetMenu(menuName = "Game/Adventure/Dungeon")]
public class AdventureDungeonData : ScriptableObject
{
    public string DungeonID => name;

    [Header("顯示（走多語系 Text Table）")]
    public string DisplayNameKey = "ADV_DUNGEON_NAME_DEFAULT";
    public Sprite Banner;

    [Header("規則")]
    [Tooltip("里程目標的「初始值」，開始時複製給 AdventureRunModel.TotalMileage。\n" +
             "本輪可用 AddRequiredMileage() 加長（繞遠路），不會改到這裡。\n" +
             "主要給進度條顯示；實際「結束」由牌上的 End Adventure 效果觸發")]
    public int TotalMileage = 8;

    // 休息次數上限 / 每次減壓量 → 改由 AdventureRunModel 統一定義（見其常數）

    [Header("通關標記")]
    [Tooltip("通關時要設的 persistent 進度旗標；也用來判定 IsCleared（供已通關顯示 / 解鎖）")]
    public ProgressFlagDefinition ClearedFlag;

    [Header("牌池")]
    [Tooltip("指定里程強制發牌（優先於分段牌池）")]
    public List<AdventureForcedDraw> ForcedDraws = new List<AdventureForcedDraw>();

    [Tooltip("依里程分段的加權牌池")]
    public List<AdventureMileageBand> Bands = new List<AdventureMileageBand>();

    /// <summary>是否已通關（依 ClearedFlag 是否存在於進度旗標）。</summary>
    public bool IsCleared
    {
        get
        {
            if (ClearedFlag == null) return false;
            var gss = GameStatusService.Instance;
            return gss != null && gss.ProgressFlags != null && gss.ProgressFlags.Contains(ClearedFlag.FlagID);
        }
    }

    /// <summary>
    /// 依目前里程抽一張牌：
    /// 1. 先看 ForcedDraw（符合就強制回傳）
    /// 2. 否則找里程所在的 Band，加權隨機抽一張
    /// 找不到對應 Band 時 fallback 到最後一段，避免里程 overshoot 抽不到牌。
    /// </summary>
    public AdventureCardData PickCard(int mileage)
    {
        // 1. ForcedDraw 優先
        if (ForcedDraws != null)
        {
            foreach (var forced in ForcedDraws)
            {
                if (forced != null && forced.Card != null && forced.Matches(mileage))
                    return forced.Card;
            }
        }

        if (Bands == null || Bands.Count == 0) return null;

        // 2. 找里程所在的 Band
        AdventureMileageBand band = null;
        foreach (var b in Bands)
        {
            if (b != null && b.Contains(mileage)) { band = b; break; }
        }
        if (band == null) band = Bands[Bands.Count - 1]; // fallback：最後一段

        return WeightedPick(band);
    }

    private static AdventureCardData WeightedPick(AdventureMileageBand band)
    {
        if (band == null || band.Cards == null || band.Cards.Count == 0) return null;

        int total = 0;
        foreach (var wc in band.Cards)
            if (wc != null && wc.Card != null) total += Mathf.Max(0, wc.Weight);

        // 全部權重為 0 時，回傳第一張有效牌
        if (total <= 0)
        {
            foreach (var wc in band.Cards)
                if (wc != null && wc.Card != null) return wc.Card;
            return null;
        }

        int roll = Random.Range(0, total);
        foreach (var wc in band.Cards)
        {
            if (wc == null || wc.Card == null) continue;
            roll -= Mathf.Max(0, wc.Weight);
            if (roll < 0) return wc.Card;
        }
        return null;
    }
}
