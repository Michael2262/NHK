using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一個冒險地點（Dungeon）。
/// 抽牌模型：一趟最多散步 MaxMoves 次；每次散步先依「第幾次」的機率決定這次是
/// 普通事件還是特色事件，再從對應牌池隨機抽一張。
/// 「結束大冒險 / 標記通關」純由卡片效果驅動（End Adventure / Mark Dungeon Cleared）。
/// </summary>
[CreateAssetMenu(menuName = "Game/Adventure/Dungeon")]
public class AdventureDungeonData : ScriptableObject
{
    public string DungeonID => name;

    [Header("顯示（走多語系 Text Table）")]
    public string DisplayNameKey = "ADV_DUNGEON_NAME_DEFAULT";
    public Sprite Banner;

    [Header("規則")]
    [Tooltip("一趟最多散步幾次（= 幾次行動）。抽一張牌就是散步一次")]
    public int MaxMoves = 3;

    [Header("通關標記")]
    [Tooltip("由卡片效果 Mark Dungeon Cleared 設的 persistent 進度旗標；也用來判定 IsCleared")]
    public ProgressFlagDefinition ClearedFlag;

    [Header("牌池")]
    [Tooltip("普通事件牌池")]
    public List<AdventureCardData> NormalCards = new List<AdventureCardData>();

    [Tooltip("特色 / 判定事件牌池")]
    public List<AdventureCardData> SpecialCards = new List<AdventureCardData>();

    [Header("抽牌機率")]
    [Tooltip("每次散步抽到「特色事件」的機率(%)，依第幾次散步查表。\n" +
             "index 0 = 第 1 次、index 1 = 第 2 次…（普通機率 = 100 - 特色）。\n" +
             "散步次數超過表格長度時，沿用最後一筆。\n" +
             "例：0 / 35 / 50 就是第1次0%、第2次35%、第3次50%")]
    public List<float> SpecialChancePerAction = new List<float> { 0f, 35f, 50f };

    [Tooltip("勾選：一趟只要出過一次特色事件，之後的散步就 100% 普通")]
    public bool SpecialOnlyOncePerRun = true;

    [Tooltip("勾選：本輪不會抽到重複的特色牌（同一趟每張特色牌最多出一次）。\n" +
             "若特色牌全出過了還骰到特色，會退回抽普通牌")]
    public bool NoRepeatSpecialInRun = false;

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

    /// <summary>特色牌池是否有可用牌。</summary>
    public bool HasSpecialCards => SpecialCards != null && SpecialCards.Exists(c => c != null);

    /// <summary>取第 (actionIndex+1) 次散步抽到「特色」的機率(%)。超過表格長度沿用最後一筆。</summary>
    public float GetSpecialChance(int actionIndex)
    {
        if (SpecialChancePerAction == null || SpecialChancePerAction.Count == 0) return 0f;
        if (actionIndex < 0) actionIndex = 0;
        if (actionIndex >= SpecialChancePerAction.Count) actionIndex = SpecialChancePerAction.Count - 1;
        return SpecialChancePerAction[actionIndex];
    }

    /// <summary>從普通事件牌池隨機抽一張（無有效牌回傳 null）。</summary>
    public AdventureCardData PickRandomNormal() => PickRandom(NormalCards, null);

    /// <summary>
    /// 從特色事件牌池隨機抽一張（無有效牌回傳 null）。
    /// exclude 內的牌會被排除（給「本輪不重複」用）。
    /// </summary>
    public AdventureCardData PickRandomSpecial(ICollection<AdventureCardData> exclude = null)
        => PickRandom(SpecialCards, exclude);

    private static AdventureCardData PickRandom(List<AdventureCardData> pool, ICollection<AdventureCardData> exclude)
    {
        if (pool == null || pool.Count == 0) return null;

        // 過濾 null 與被排除的牌後等機率隨機
        var valid = pool.FindAll(c => c != null && (exclude == null || !exclude.Contains(c)));
        if (valid.Count == 0) return null;
        return valid[Random.Range(0, valid.Count)];
    }
}
