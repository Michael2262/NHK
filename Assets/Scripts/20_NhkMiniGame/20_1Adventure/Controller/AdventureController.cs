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
    [Header("資料來源")]
    [Tooltip("用 ID 開始大冒險時的查表來源")]
    [SerializeField] private AdventureDungeonDatabase _database;

    [Tooltip("StartDefaultAdventure() 用的預設地點，可留空")]
    [SerializeField] private AdventureDungeonData _defaultDungeon;

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

    [Header("事件（給 UI / FSM 掛）")]
    public AdventureCardEvent onCardDrawn;
    public AdventureChangeListEvent onAlwaysEffectsApplied;
    public AdventureFlipEvent onFlipResolved;
    public UnityEvent onSuccess;
    public UnityEvent onFail;
    public AdventureIntEvent onMileageChanged;
    public AdventureIntEvent onRestChanged;
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

    /// <summary>開始一趟大冒險（里程一律從 0 開始）。</summary>
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
        Run.OnMileageChanged += HandleMileageChanged;
        Run.OnRestChanged += HandleRestChanged;
        Run.OnRunEnded += HandleRunEnded;

        Run.StartRun(dungeon);
    }

    // ============================================================
    // 玩家動作（給 Button.onClick / FSM 呼叫）
    // ============================================================

    public void ResetRest() => Run?.ResetRestToMax();

    // ── 分階段（給 AdventureCardPresenter 這類演出層在對的時間點呼叫）──

    /// <summary>只發牌，不翻。回傳發到的牌。</summary>
    public AdventureCardData DrawNext() => Run?.DrawCard();

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

    public void Rest() => Run?.Rest();
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

    private void HandleMileageChanged(int mileage) => onMileageChanged?.Invoke(mileage);
    private void HandleRestChanged(int remaining) => onRestChanged?.Invoke(remaining);

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
        Run.OnMileageChanged -= HandleMileageChanged;
        Run.OnRestChanged -= HandleRestChanged;
        Run.OnRunEnded -= HandleRunEnded;
    }

    private void OnDestroy() => Unsubscribe();
}
