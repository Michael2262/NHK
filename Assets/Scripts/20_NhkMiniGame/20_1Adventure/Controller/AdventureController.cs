using System.Collections;
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

    [Header("演出")]
    [Tooltip("必有效果與第二拍（成敗判定 / 必定成功）之間的延遲秒數。\n" +
             "AlwaysOnly 的牌沒有第二拍，不會用到這個值")]
    [SerializeField] private float _outcomeDelaySeconds = 0.6f;

    /// <summary>目前進行中的 Run（尚未開始為 null）。</summary>
    public AdventureRunModel Run { get; private set; }

    /// <summary>目前正在跑哪個 Dungeon。</summary>
    public string CurrentDungeonID => Run?.Dungeon != null ? Run.Dungeon.DungeonID : null;

    /// <summary>是否有一趟進行中的大冒險。</summary>
    public bool IsRunning => Run != null && !Run.IsEnded;

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

    /// <summary>開始一趟大冒險。</summary>
    public void StartAdventure(AdventureDungeonData dungeon, int startMileage = 0)
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

        StopAllCoroutines();
        Unsubscribe();
        _history.Clear();

        Run = new AdventureRunModel(gss.Protagonist, gss.Inventory, gss.ProgressFlags);
        Run.OnCardDrawn += HandleCardDrawn;
        Run.OnAlwaysEffectsApplied += HandleAlwaysEffectsApplied;
        Run.OnFlipResolved += HandleFlipResolved;
        Run.OnMileageChanged += HandleMileageChanged;
        Run.OnRestChanged += HandleRestChanged;
        Run.OnRunEnded += HandleRunEnded;

        Run.StartRun(dungeon, startMileage);
    }

    // ============================================================
    // 玩家動作（給 Button.onClick / FSM 呼叫）
    // ============================================================

    public void ResetRest() => Run?.ResetRestToMax();

    /// <summary>只發牌，不翻（要做「先看到牌背」的演出時用）。</summary>
    public void DrawNext() => Run?.DrawCard();

    /// <summary>翻開目前的牌，依牌的 OutcomeMode 決定收尾方式。</summary>
    public void Flip()
    {
        var card = Run?.CurrentCard;
        if (card == null) return;
        RunFlipByMode(card);
    }

    /// <summary>
    /// 一次做完：依目前 Dungeon 的牌池發一張牌，並觸發牌上的結果。
    /// </summary>
    public void DrawAndResolve()
    {
        if (Run == null || Run.IsEnded) return;

        var card = Run.DrawCard();
        if (card == null) return;
        RunFlipByMode(card);
    }

    private void RunFlipByMode(AdventureCardData card)
    {
        if (card.OutcomeMode == AdventureOutcomeMode.AlwaysOnly)
        {
            // 沒有第二拍：只跑必有效果，然後直接收尾（不判成敗）
            Run.ApplyAlwaysEffects();
            Run.ResolveOutcome();
            return;
        }

        // Judge / ForceSuccess：必有效果 → 延遲 → 第二拍
        StartCoroutine(FlipWithDelay());
    }

    private IEnumerator FlipWithDelay()
    {
        Run.ApplyAlwaysEffects();
        yield return new WaitForSeconds(_outcomeDelaySeconds);
        if (Run != null) Run.ResolveOutcome();
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
