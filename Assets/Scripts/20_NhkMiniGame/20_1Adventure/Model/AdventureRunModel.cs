using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一趟大冒險的執行狀態與邏輯（純 C# Model，非 MonoBehaviour）。
/// 邏輯都在這裡；MonoBehaviour（AdventureController）只做轉接與發事件。
///
/// 生命週期：StartRun → (DrawCard → Flip)* → GoHome 或牌上的 End Adventure 效果結束。
/// 抽牌：每次散步（抽牌）算一次行動，依「第幾次散步」的機率決定普通/特色事件，再從對應池隨機抽。
/// 結束/通關純由卡片效果驅動。
///
/// 翻牌一律拆成兩階段，讓呼叫端有機會在中間停下來（等演出、等玩家決定要不要挑戰）：
///   ① ApplyAlwaysEffects()  必有效果
///   ② ResolveOutcome()      擲骰判定 + 成功/失敗效果
/// 停在①之後可以不做②就直接 DrawCard() 發下一張，未結算的結果會被丟棄。
/// 不需要中斷時直接呼叫 Flip()，它會一次跑完兩階段。
///
/// 註：牌的 OutcomeMode（Judge / AlwaysOnly / ForceSuccess）在階段②由本 Model 落實規則；
///     Controller 只讀它來決定演出節奏（第二拍要不要隔一段時間再演）。
/// </summary>
public class AdventureRunModel
{
    // ───── 依賴（由 GameStatusService 注入） ─────
    private readonly ProtagonistStatusModel _protagonist;
    private readonly ProtagonistInventoryModel _inventory;
    private readonly ProgressFlagModel _flags;

    // ───── 狀態 ─────
    public AdventureDungeonData Dungeon { get; private set; }

    /// <summary>本輪剩餘的行動次數（還能散步幾次）。開始＝Dungeon.MaxMoves；每次隨機抽牌 -1；也可由 AdvMovesEffect 額外增減。歸 0 就抽不到牌，但不會自動結束。</summary>
    public int MovesRemaining { get; private set; }

    /// <summary>本輪已散步幾次（隨機抽牌才算）。用來決定「第幾次」的機率。</summary>
    public int ActionsTaken { get; private set; }

    /// <summary>最近一次隨機抽牌抽到的類別（Normal / Special / Quest）。</summary>
    public AdventureCardCategory LastDrawCategory { get; private set; }

    /// <summary>本輪是否已出過特色事件（供除錯 / UI）。</summary>
    public bool SpecialHappened => _special.Happened;
    /// <summary>本輪是否已出過任務事件（供除錯 / UI）。</summary>
    public bool QuestHappened => _quest.Happened;

    public AdventureCardData CurrentCard { get; private set; }
    public bool IsEnded { get; private set; }

    /// <summary>已跑過必有效果、但還沒結算成敗（等待玩家決定要不要挑戰）。</summary>
    public bool HasPendingOutcome => _pendingAlwaysChanges != null;

    private AdventureEndReason _endReason;
    private bool _suppressEndEvent; // 套效果期間先壓住結束事件，等結果送出後再發

    // 翻牌期間的暫存（跨兩階段）
    private List<AdventureChangeRecord> _pendingAlwaysChanges;

    // 各機率池的本輪執行狀態（是否出過、已抽過哪些）
    private sealed class PoolState
    {
        public bool Happened;
        public readonly HashSet<AdventureCardData> Drawn = new HashSet<AdventureCardData>();
        public void Reset() { Happened = false; Drawn.Clear(); }
    }
    private readonly PoolState _special = new PoolState();
    private readonly PoolState _quest = new PoolState();

    // ───── 事件（給 Controller / UI / FSM 掛） ─────
    public event Action<AdventureCardData> OnCardDrawn;
    public event Action<List<AdventureChangeRecord>> OnAlwaysEffectsApplied; // 階段①完成
    public event Action<AdventureFlipResult> OnFlipResolved;                 // 階段②完成
    public event Action<int> OnMovesChanged;   // 剩餘行動次數改變（只由 AddMoves 造成，時機由外部控制）
    public event Action OnMovesExhausted;      // 剩餘行動次數剛歸 0（由正數變 0 時觸發一次；因為只有 AddMoves 會動它，所以時機＝外部扣到 0 的那刻）
    public event Action<AdventureEndReason> OnRunEnded;

    public AdventureRunModel(ProtagonistStatusModel protagonist,
                             ProtagonistInventoryModel inventory,
                             ProgressFlagModel flags)
    {
        _protagonist = protagonist;
        _inventory = inventory;
        _flags = flags;
    }

    // ============================================================
    // 開始 / 行動次數
    // ============================================================

    /// <summary>
    /// 開始一趟大冒險。行動次數從 Dungeon.MaxMoves 複製。
    /// 唯一會跨存檔留下的是「已攻克」的 persistent 旗標（見 MarkCurrentDungeonCleared）。
    /// </summary>
    public void StartRun(AdventureDungeonData dungeon)
    {
        Dungeon = dungeon;
        MovesRemaining = dungeon != null ? dungeon.MaxMoves : 0;
        ActionsTaken = 0;
        LastDrawCategory = AdventureCardCategory.Normal;
        _special.Reset();
        _quest.Reset();
        CurrentCard = null;
        IsEnded = false;
        _pendingAlwaysChanges = null;

        OnMovesChanged?.Invoke(MovesRemaining);
    }

    /// <summary>把行動次數重設為 Dungeon 的上限。時機由外部（FSM/對話）決定。</summary>
    public void ResetMovesToMax()
    {
        MovesRemaining = Dungeon != null ? Dungeon.MaxMoves : 0;
        OnMovesChanged?.Invoke(MovesRemaining);
    }

    // ============================================================
    // 發牌
    // ============================================================

    /// <summary>
    /// 散步一次：算「第幾次」→ 單次擲骰依機率瓜分決定類別（Quest > Special > Normal）→ 從對應池隨機抽一張。
    /// 抽牌只推進 ActionsTaken（給機率表用），不動 Moves（Moves 由外部在對話控制）。
    /// 行動次數已用完（MovesRemaining ≤ 0）則抽不到牌。
    /// 若上一張牌已跑過必有效果卻還沒結算（玩家選了「繞遠路」），這裡會把那個未完成的結果丟棄。
    /// </summary>
    public AdventureCardData DrawCard()
    {
        if (IsEnded || Dungeon == null) return null;
        if (MovesRemaining <= 0)
        {
            Debug.Log($"[Adventure] Dungeon '{Dungeon.DungeonID}' 行動次數已用完，無法再散步。");
            return null;
        }

        int actionIndex = ActionsTaken; // 0-based：這是第 (actionIndex+1) 次散步

        // 各池「實際生效」的機率（被 gating 關掉或抽不出牌的池 → 0，那份自動歸給 Normal）
        float questChance = EffectiveChance(Dungeon.QuestPool, _quest, actionIndex);
        float specialChance = EffectiveChance(Dungeon.SpecialPool, _special, actionIndex);
        // Quest 優先：兩者相加超過 100 時壓縮 Special
        specialChance = Mathf.Min(specialChance, 100f - questChance);

        float roll = UnityEngine.Random.Range(0f, 100f);

        AdventureCardData card;
        AdventureCardCategory category;

        if (roll < questChance)
        {
            category = AdventureCardCategory.Quest;
            card = Dungeon.QuestPool.PickRandom(Dungeon.QuestPool.NoRepeatInRun ? _quest.Drawn : null);
        }
        else if (roll < questChance + specialChance)
        {
            category = AdventureCardCategory.Special;
            card = Dungeon.SpecialPool.PickRandom(Dungeon.SpecialPool.NoRepeatInRun ? _special.Drawn : null);
        }
        else
        {
            category = AdventureCardCategory.Normal;
            card = Dungeon.PickRandomNormal();
        }

        // 保險：機率池臨時抽不出牌 → 退回普通（EffectiveChance 通常已擋掉，但防呆）
        if (card == null && category != AdventureCardCategory.Normal)
        {
            category = AdventureCardCategory.Normal;
            card = Dungeon.PickRandomNormal();
        }

        if (card == null)
        {
            Debug.LogWarning($"[Adventure] Dungeon '{Dungeon.DungeonID}' 第 {actionIndex + 1} 次散步抽不到牌（牌池皆空）。");
            return null; // 抽不到牌就不推進行動
        }

        LastDrawCategory = category;
        MarkPoolDraw(category, card);

        ActionsTaken++; // 只推進「第幾次散步」（給機率表用）；Moves 由外部在對話裡自己扣
        return SetDrawnCard(card);
    }

    /// <summary>某機率池這次「實際生效」的機率(%)：被 gating 關掉或抽不出牌就回 0。</summary>
    private static float EffectiveChance(AdventureCardPool pool, PoolState state, int actionIndex)
    {
        if (pool == null || !pool.HasCards) return 0f;
        if (pool.OnlyOncePerRun && state.Happened) return 0f;

        var exclude = pool.NoRepeatInRun ? state.Drawn : null;
        if (!pool.HasDrawable(exclude)) return 0f; // NoRepeat 下全抽過了

        return Mathf.Clamp(pool.GetChance(actionIndex), 0f, 100f);
    }

    private void MarkPoolDraw(AdventureCardCategory category, AdventureCardData card)
    {
        if (category == AdventureCardCategory.Quest)
        {
            _quest.Happened = true;
            _quest.Drawn.Add(card);
        }
        else if (category == AdventureCardCategory.Special)
        {
            _special.Happened = true;
            _special.Drawn.Add(card);
        }
    }

    /// <summary>
    /// 直接發一張「指定的」牌（略過牌池抽選），用於劇情腳本強制指定某張牌。
    /// 一樣推進「第幾次散步」（ActionsTaken +1），但不動 Moves（Moves 由外部在對話裡扣）。
    /// 不受行動次數用完限制（劇情可強制發），但一樣會丟棄上一張未結算的結果、發 OnCardDrawn。
    /// 註：不改動任何機率池狀態 / LastDrawCategory（那是隨機抽牌的狀態）。
    /// </summary>
    public AdventureCardData DrawSpecificCard(AdventureCardData card)
    {
        if (IsEnded) return null;
        if (card == null)
        {
            Debug.LogWarning("[Adventure] DrawSpecificCard 收到 null card。");
            return null;
        }

        ActionsTaken++;
        return SetDrawnCard(card);
    }

    private AdventureCardData SetDrawnCard(AdventureCardData card)
    {
        _pendingAlwaysChanges = null; // 丟棄上一張未結算的結果
        CurrentCard = card;
        OnCardDrawn?.Invoke(card);
        return card;
    }

    // ============================================================
    // 翻牌（兩階段）
    // ============================================================

    /// <summary>
    /// 階段①：套用「必有效果」。回傳這階段造成的數值變動。
    /// SorF 的牌由 Controller 呼叫這支，隔一段時間後再呼叫 ResolveOutcome()。
    /// </summary>
    public List<AdventureChangeRecord> ApplyAlwaysEffects()
    {
        if (IsEnded || CurrentCard == null) return null;

        _pendingAlwaysChanges = new List<AdventureChangeRecord>();

        _suppressEndEvent = true;
        ApplyEffectList(CurrentCard.AlwaysEffects, _pendingAlwaysChanges);
        _suppressEndEvent = false;

        OnAlwaysEffectsApplied?.Invoke(_pendingAlwaysChanges);
        return _pendingAlwaysChanges;
    }

    /// <summary>
    /// 階段②：依牌的 OutcomeMode 收尾 —— 判定成敗 / 必定成功 / 不判定。
    /// 若階段①已被呼叫過，會沿用其變動記錄；否則自動補跑階段①。
    /// AlwaysOnly 的牌仍要呼叫這支來收尾（會產生 OutcomeResolved=false 的結果並發事件）。
    /// </summary>
    public AdventureFlipResult ResolveOutcome()
    {
        if (CurrentCard == null) return null;

        // 沒先跑階段①就直接呼叫 → 自動補跑
        if (_pendingAlwaysChanges == null)
        {
            if (IsEnded) return null;
            ApplyAlwaysEffects();
        }

        var card = CurrentCard;
        bool endedByAlways = IsEnded;           // 必有效果就把大冒險結束掉了
        float rate = card.CalcSuccessRate(_protagonist);
        bool success = false;
        var branchChanges = new List<AdventureChangeRecord>();

        // AlwaysOnly 的牌不判定成敗，成功/失敗效果都不跑
        bool willResolve = !endedByAlways && card.OutcomeMode != AdventureOutcomeMode.AlwaysOnly;

        if (willResolve)
        {
            success = card.OutcomeMode == AdventureOutcomeMode.ForceSuccess
                   || UnityEngine.Random.Range(0f, 100f) < rate;

            _suppressEndEvent = true;
            ApplyEffectList(success ? card.SuccessEffects : card.FailureEffects, branchChanges);
            _suppressEndEvent = false;
        }

        var result = new AdventureFlipResult
        {
            Card = card,
            Success = success,
            SuccessRate = rate,
            OutcomeResolved = willResolve,
            AlwaysChanges = _pendingAlwaysChanges ?? new List<AdventureChangeRecord>(),
            Changes = branchChanges,
            Ended = IsEnded
        };

        _pendingAlwaysChanges = null;

        OnFlipResolved?.Invoke(result);

        // 若過程中觸發了結束，補發結束事件（此時翻牌結果已送出）
        if (IsEnded) OnRunEnded?.Invoke(_endReason);

        return result;
    }

    /// <summary>一次跑完整張牌（不演出的即時路徑）。需要在中間插演出的呼叫端請改用分階段的 API。</summary>
    public AdventureFlipResult Flip()
    {
        if (IsEnded || CurrentCard == null) return null;

        ApplyAlwaysEffects();
        return ResolveOutcome();
    }

    private void ApplyEffectList(List<AdventureEffect> effects, List<AdventureChangeRecord> records)
    {
        if (effects == null) return;

        var ctx = new AdventureContext
        {
            Protagonist = _protagonist,
            Inventory = _inventory,
            ProgressFlags = _flags,
            Run = this
        };

        foreach (var effect in effects)
        {
            if (effect == null) continue;
            effect.Apply(ctx);
            foreach (var record in effect.ReportChanges(ctx))
                records.Add(record);
        }
    }

    // ============================================================
    // 行動次數 / 結束（供效果或外部呼叫）
    // ============================================================

    /// <summary>
    /// 變更行動次數（AdvMovesEffect 或對話 Adventure(AddMoves) / Adventure(SpendMove) 呼叫）。負數＝消耗、正數＝補充。
    /// 抽牌本身「不」會動 Moves —— 什麼時候扣完全由外部（對話）控制。
    /// 次數不會低於 0；由正數扣到 0 時發 OnMovesExhausted（時機＝外部扣到 0 的那刻），但不會自動結束大冒險。
    /// </summary>
    public void AddMoves(int delta)
    {
        if (delta == 0) return;

        int before = MovesRemaining;
        MovesRemaining = Mathf.Max(0, MovesRemaining + delta);

        if (MovesRemaining != before) OnMovesChanged?.Invoke(MovesRemaining);
        if (before > 0 && MovesRemaining == 0) OnMovesExhausted?.Invoke();
    }

    /// <summary>玩家主動回家，結束這趟大冒險。</summary>
    public void GoHome()
    {
        if (IsEnded) return;
        EndAdventure(AdventureEndReason.GoHome);
    }

    /// <summary>結束這趟大冒險（可被牌上的 AdvEndAdventureEffect 呼叫）。</summary>
    public void EndAdventure(AdventureEndReason reason)
    {
        if (IsEnded) return;
        IsEnded = true;
        _endReason = reason;
        if (!_suppressEndEvent) OnRunEnded?.Invoke(reason);
    }

    /// <summary>把目前 Dungeon 標記為已通關（設其 ClearedFlag 的 persistent 旗標）。</summary>
    public void MarkCurrentDungeonCleared()
    {
        if (Dungeon == null || Dungeon.ClearedFlag == null || _flags == null) return;
        _flags.AddPersistentFlag(Dungeon.ClearedFlag.FlagID);
    }
}
