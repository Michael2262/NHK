using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// ───── 給 Inspector 顯示的具型別 UnityEvent ─────
[System.Serializable] public class AdventureCardEvent : UnityEvent<AdventureCardData> { }
[System.Serializable] public class AdventureFlipEvent : UnityEvent<AdventureFlipResult> { }
[System.Serializable] public class AdventureIntEvent : UnityEvent<int> { }
[System.Serializable] public class AdventureChangeListEvent : UnityEvent<List<AdventureChangeRecord>> { }

/// <summary>
/// 大冒險的場景端暫存器 / 轉接層。
/// 持有目前這趟的 AdventureRunModel，把 Button.onClick / FSM 的呼叫轉發給 Model，
/// 並把 Model 事件轉成 UnityEvent 供 UI / FSM 掛。
///
/// 不做時間推進、不做表演 —— 那些交給 FSM 監聽事件後處理。
/// </summary>
public class AdventureController : MonoBehaviour
{
    /// <summary>
    /// 場上唯一的 Controller，供 Sequencer Command / 外部快速取用。
    /// 這個元件放在會卸載的場景上，所以離場時會把參照清掉。
    /// </summary>
    public static AdventureController Instance { get; private set; }

    [Header("資料來源")]
    [Tooltip("用 ID 開始大冒險時的查表來源")]
    [SerializeField] private AdventureDungeonDatabase _database;

    [Tooltip("StartDefaultAdventure() 用的預設地點，可留空")]
    [SerializeField] private AdventureDungeonData _defaultDungeon;

    [Tooltip("用卡片 ID 發牌時的 Resources 資料夾路徑（卡片資產名 = ID）")]
    [SerializeField] private string _cardResourcePath = "Adventure/AdvCard";

    /// <summary>目前進行中的 Run（尚未開始為 null）。</summary>
    public AdventureRunModel Run { get; private set; }

    /// <summary>目前正在跑哪個 Dungeon。</summary>
    public string CurrentDungeonID => Run?.Dungeon != null ? Run.Dungeon.DungeonID : null;

    /// <summary>是否有一趟進行中的大冒險。</summary>
    public bool IsRunning => Run != null && !Run.IsEnded;

    /// <summary>目前這張牌已跑必有效果、還沒結算成敗（等玩家決定挑戰 / 繞遠路）。</summary>
    public bool HasPendingOutcome => Run != null && Run.HasPendingOutcome;

    /// <summary>本趟已翻過的所有結果，供結算畫面使用。</summary>
    private readonly List<AdventureFlipResult> _history = new List<AdventureFlipResult>();
    public IReadOnlyList<AdventureFlipResult> History => _history;

    [Header("事件（給 UI / FSM 掛；不需要的留空即可，留空不掛沒有任何負擔）")]

    [Tooltip("【選用】抽到一張牌時（翻牌之前）觸發，帶那張牌的資料。\n" +
             "想在牌一出現就反應（放音效、依牌名分支）才需要。牌面演出本身由 Presenter 處理，不靠這個。")]
    public AdventureCardEvent onCardDrawn;

    [Tooltip("【選用】必有效果套用完、判定之前觸發，帶必有效果造成的數值變動。\n" +
             "只有想在『必有 → 停頓等挑戰』中間先跳必有效果的數值提示時才需要。\n" +
             "同一份資料在 onFlipResolved 的 result.AlwaysChanges 也拿得到（只是時機較晚）。")]
    public AdventureChangeListEvent onAlwaysEffectsApplied;

    [Tooltip("【核心】一次翻牌結算完觸發，帶完整結果：成功與否、成功率、必有/成功失敗的數值變動、是否結束。")]
    public AdventureFlipEvent onFlipResolved;

    [Tooltip("【方便】翻牌成功時觸發（無參數，好接 FSM 轉場）。等同 onFlipResolved 裡 Success==true。\n" +
             "AlwaysOnly、或必有效果就直接結束的牌不會發（因為沒跑成敗判定）。")]
    public UnityEvent onSuccess;

    [Tooltip("【方便】翻牌失敗時觸發（無參數，好接 FSM 轉場）。等同 onFlipResolved 裡 Success==false。")]
    public UnityEvent onFail;

    [Tooltip("【核心】剩餘行動次數改變時觸發，帶新的剩餘次數。AdventureMovesView 就是靠這個自動更新。")]
    public AdventureIntEvent onMovesChanged;

    [Tooltip("【核心】行動次數被扣到 0 時觸發（例如強制回家 / 結算）。\n" +
             "抽牌不會動 Moves —— Moves 由你在對話用 Adventure(AddMoves,-1) / Adventure(SpendMove) 扣，\n" +
             "所以這個事件發出的時機＝你在對話裡把它扣到 0 的那一刻。")]
    public UnityEvent onMovesExhausted;

    [Tooltip("【核心】這趟大冒險結束時觸發（玩家回家，或牌上的 End Adventure 效果）。")]
    public UnityEvent onRunEnded;

    // ============================================================
    // 開始
    // ============================================================

    /// <summary>用 Dungeon ID 開始一趟大冒險（需先在 Inspector 指定 Database）。</summary>
    public void StartDungeonByID(string dungeonID)
    {
        if (_database == null)
        {
            Debug.LogError("[AdventureController] 未指定 AdventureDungeonDatabase，無法用 ID 開始。");
            return;
        }

        var dungeon = _database.Find(dungeonID);
        if (dungeon == null)
        {
            Debug.LogError($"[AdventureController] Database 裡找不到 Dungeon ID：'{dungeonID}'");
            return;
        }

        StartAdventure(dungeon);
    }

    /// <summary>用預設地點開始（測試 / 單一地點場景用）。</summary>
    public void StartDefaultAdventure() => StartAdventure(_defaultDungeon);

    /// <summary>開始一趟大冒險。</summary>
    public void StartAdventure(AdventureDungeonData dungeon)
    {
        if (dungeon == null)
        {
            Debug.LogError("[AdventureController] StartAdventure 收到 null dungeon。");
            return;
        }

        var gss = GameStatusService.Instance;
        if (gss == null)
        {
            Debug.LogError("[AdventureController] 找不到 GameStatusService。");
            return;
        }

        Unsubscribe();
        _history.Clear();

        Run = new AdventureRunModel(gss.Protagonist, gss.Inventory, gss.ProgressFlags);
        Run.OnCardDrawn += HandleCardDrawn;
        Run.OnAlwaysEffectsApplied += HandleAlwaysEffectsApplied;
        Run.OnFlipResolved += HandleFlipResolved;
        Run.OnMovesChanged += HandleMovesChanged;
        Run.OnMovesExhausted += HandleMovesExhausted;
        Run.OnRunEnded += HandleRunEnded;

        Run.StartRun(dungeon);
    }

    // ============================================================
    // 玩家動作（給 Button.onClick / FSM 呼叫）
    // ============================================================

    public void ResetMoves() => Run?.ResetMovesToMax();

    // ── 分階段（給 AdventureCardPresenter 這類演出層在對的時間點呼叫）──

    /// <summary>只發牌（依牌池抽），不翻。回傳發到的牌。</summary>
    public AdventureCardData DrawNext() => Run?.DrawCard();

    /// <summary>只發「指定 ID」的牌（略過牌池），不翻。回傳發到的牌。</summary>
    public AdventureCardData DrawNextByID(string cardID)
    {
        var card = ResolveCard(cardID);
        return card == null ? null : Run?.DrawSpecificCard(card);
    }

    /// <summary>一次做完：發指定 ID 的牌並立刻觸發結果（不演出）。</summary>
    public AdventureFlipResult DrawAndResolveByID(string cardID)
    {
        if (Run == null || Run.IsEnded) return null;
        return DrawNextByID(cardID) == null ? null : Run.Flip();
    }

    /// <summary>依 ID 從 Resources 載入卡片資產（資產名 = ID）。</summary>
    public AdventureCardData ResolveCard(string cardID)
    {
        if (string.IsNullOrEmpty(cardID)) return null;

        string path = string.IsNullOrEmpty(_cardResourcePath) ? cardID : $"{_cardResourcePath}/{cardID}";
        var card = Resources.Load<AdventureCardData>(path);
        if (card == null)
            Debug.LogWarning($"[AdventureController] 找不到卡片資源：Resources/{path}");
        return card;
    }

    /// <summary>階段①：立刻套用必有效果。</summary>
    public List<AdventureChangeRecord> ApplyAlways() => Run?.ApplyAlwaysEffects();

    /// <summary>階段②：立刻依 OutcomeMode 收尾（判成敗 / 必定成功 / 不判定）。</summary>
    public AdventureFlipResult ResolveOutcome() => Run?.ResolveOutcome();

    // ── 即時（不演出。Debug 面板或不需要演出的流程用）──

    /// <summary>翻開目前的牌，兩階段一次跑完。</summary>
    public AdventureFlipResult Flip() => Run?.Flip();

    /// <summary>一次做完：依目前 Dungeon 的牌池發一張牌，並立刻觸發牌上的結果。</summary>
    public AdventureFlipResult DrawAndResolve()
    {
        if (Run == null || Run.IsEnded) return null;
        return Run.DrawCard() == null ? null : Run.Flip();
    }

    /// <summary>變更行動次數（負=消耗、正=補充）。可掛在 Button.onClick 或 FSM/對話呼叫。抽牌不會動它，時機由外部控制。</summary>
    public void AddMoves(int delta) => Run?.AddMoves(delta);
    public void GoHome() => Run?.GoHome();

    // ============================================================
    // Model 事件 → UnityEvent 轉發
    // ============================================================

    private void HandleCardDrawn(AdventureCardData card) => onCardDrawn?.Invoke(card);

    private void HandleAlwaysEffectsApplied(List<AdventureChangeRecord> changes)
        => onAlwaysEffectsApplied?.Invoke(changes);

    private void HandleFlipResolved(AdventureFlipResult result)
    {
        _history.Add(result);
        onFlipResolved?.Invoke(result);

        if (!result.OutcomeResolved) return; // 必有效果就結束了，沒有成功/失敗可分
        if (result.Success) onSuccess?.Invoke();
        else onFail?.Invoke();
    }

    private void HandleMovesChanged(int remaining) => onMovesChanged?.Invoke(remaining);
    private void HandleMovesExhausted() => onMovesExhausted?.Invoke();

    private void HandleRunEnded(AdventureEndReason reason)
    {
        onRunEnded?.Invoke();
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (Run == null) return;
        Run.OnCardDrawn -= HandleCardDrawn;
        Run.OnAlwaysEffectsApplied -= HandleAlwaysEffectsApplied;
        Run.OnFlipResolved -= HandleFlipResolved;
        Run.OnMovesChanged -= HandleMovesChanged;
        Run.OnMovesExhausted -= HandleMovesExhausted;
        Run.OnRunEnded -= HandleRunEnded;
    }

    private void OnEnable()
    {
        if (Instance == null) Instance = this;
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void OnDestroy() => Unsubscribe();
}
