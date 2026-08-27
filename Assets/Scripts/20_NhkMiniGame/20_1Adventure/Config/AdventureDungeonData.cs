using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一個冒險地點（Dungeon）。
/// 抽牌模型：一趟最多散步 MaxMoves 次；每次散步用單次擲骰依「第幾次」的機率決定類別，再從對應池隨機抽：
///   [0, Quest%)                 → Quest 池
///   [Quest%, Quest%+Special%)   → Special 池
///   剩下                         → Normal 池（墊底 fallback）
/// 優先權 Quest > Special > Normal；某池被 gating 關掉（once/no-repeat/空）時機率算 0，那份歸給 Normal。
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
    [Tooltip("普通事件牌池（墊底 fallback；沒骰中 Quest / Special 就抽這個）")]
    public List<AdventureCardData> NormalCards = new List<AdventureCardData>();

    [Tooltip("特色 / 判定事件池（有機率、可 gating）")]
    public AdventureCardPool SpecialPool = new AdventureCardPool();

    [Tooltip("任務事件池（有機率、可 gating；優先權高於 Special）")]
    public AdventureCardPool QuestPool = new AdventureCardPool();

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

    /// <summary>從普通事件牌池隨機抽一張（無有效牌回傳 null）。</summary>
    public AdventureCardData PickRandomNormal()
    {
        if (NormalCards == null || NormalCards.Count == 0) return null;
        var valid = NormalCards.FindAll(c => c != null);
        if (valid.Count == 0) return null;
        return valid[Random.Range(0, valid.Count)];
    }
}
