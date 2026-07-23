using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一趟大冒險的執行狀態與邏輯（純 C# Model，非 MonoBehaviour）。
/// 邏輯都在這裡；MonoBehaviour（AdventureController）只做轉接與發事件。
///
/// 生命週期：StartRun → (DrawCard → Flip)* / Rest* → GoHome 或牌上的結束效果。
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
    // ───── 休息規則（大冒險共通，不隨地點變動） ─────
    /// <summary>每趟大冒險的休息次數上限</summary>
    public const int MAX_REST_COUNT = 3;
    /// <summary>每次休息減少的壓力</summary>
    public const int REST_STRESS_RELIEF = 10;

    // ───── 依賴（由 GameStatusService 注入） ─────
    private readonly ProtagonistStatusModel _protagonist;
    private readonly ProtagonistInventoryModel _inventory;
    private readonly ProgressFlagModel _flags;

    // ───── 狀態 ─────
    public AdventureDungeonData Dungeon { get; private set; }
    public int CurrentMileage { get; private set; }
    public int RestRemaining { get; private set; }
    public AdventureCardData CurrentCard { get; private set; }
    public bool IsEnded { get; private set; }

    /// <summary>已跑過必有效果、但還沒結算成敗（等待玩家決定要不要挑戰）。</summary>
    public bool HasPendingOutcome => _pendingAlwaysChanges != null;

    private AdventureEndReason _endReason;
    private bool _suppressEndEvent; // 套效果期間先壓住結束事件，等結果送出後再發

    // 翻牌期間的暫存（跨兩階段）
    private int _flipMileageAnchor;
    private List<AdventureChangeRecord> _pendingAlwaysChanges;

    // ───── 事件（給 Controller / UI / FSM 掛） ─────
    public event Action<AdventureCardData> OnCardDrawn;
    public event Action<List<AdventureChangeRecord>> OnAlwaysEffectsApplied; // 階段①完成
    public event Action<AdventureFlipResult> OnFlipResolved;                 // 階段②完成
    public event Action<int> OnMileageChanged; // 新里程
    public event Action<int> OnRestChanged;    // 剩餘休息次數
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
    // 開始 / 休息重置
    // ============================================================

    /// <summary>
    /// 開始一趟大冒險。里程一律從 0 開始 —— 沒攻略成功就沒有進度保留，
    /// 唯一會跨存檔留下的是「已攻克」的 persistent 旗標（見 MarkCurrentDungeonCleared）。
    /// </summary>
    public void StartRun(AdventureDungeonData dungeon)
    {
        Dungeon = dungeon;
        CurrentMileage = 0;
        RestRemaining = MAX_REST_COUNT;
        CurrentCard = null;
        IsEnded = false;
        _pendingAlwaysChanges = null;

        OnMileageChanged?.Invoke(CurrentMileage);
        OnRestChanged?.Invoke(RestRemaining);
    }

    /// <summary>把休息次數重設為上限。時機由外部（你的 FSM，進場景時）決定。</summary>
    public void ResetRestToMax()
    {
        RestRemaining = MAX_REST_COUNT;
        OnRestChanged?.Invoke(RestRemaining);
    }

    // ============================================================
    // 發牌
    // ============================================================

    /// <summary>
    /// 依目前里程與 Dungeon 的牌池設定發一張牌。
    /// 若上一張牌已跑過必有效果卻還沒結算（玩家選了「繞遠路」），
    /// 這裡會直接把那個未完成的結果丟棄 —— 已生效的必有效果不會被回復，
    /// 只是那張牌的成功/失敗分支永遠不會跑。
    /// </summary>
    public AdventureCardData DrawCard()
    {
        if (IsEnded || Dungeon == null) return null;

        _pendingAlwaysChanges = null; // 丟棄上一張未結算的結果

        CurrentCard = Dungeon.PickCard(CurrentMileage);
        if (CurrentCard == null)
            Debug.LogWarning($"[Adventure] Dungeon '{Dungeon.DungeonID}' 在里程 {CurrentMileage} 抽不到牌，請檢查牌池設定。");

        OnCardDrawn?.Invoke(CurrentCard);
        return CurrentCard;
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

        _flipMileageAnchor = CurrentMileage;
        _pendingAlwaysChanges = new List<AdventureChangeRecord>();

        _suppressEndEvent = true;
        ApplyEffectList(CurrentCard.AlwaysEffects, _pendingAlwaysChanges);
        _suppressEndEvent = false;

        OnAlwaysEffectsApplied?.Invoke(_pendingAlwaysChanges);
        return _pendingAlwaysChanges;
    }

    /// <summary>
    /// 階段②：依牌的 OutcomeMode 收尾 —— 判定成敗 / 必定成功 / 不判定。
    /// 若階段①已被呼叫過，會沿用其里程錨點與變動記錄；否則自動補跑階段①。
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
            MileageDelta = CurrentMileage - _flipMileageAnchor,
            NewMileage = CurrentMileage,
            Ended = IsEnded
        };

        _pendingAlwaysChanges = null;

        OnFlipResolved?.Invoke(result);

        // 若過程中觸發了結束，補發結束事件（此時翻牌結果已送出）
        if (IsEnded) OnRunEnded?.Invoke(_endReason);

        return result;
    }

    /// <summary>一次跑完兩階段（沒有勾 SorF 的牌用這支）。</summary>
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
            var record = effect.ReportChange(ctx);
            if (record.HasValue) records.Add(record.Value);
        }
    }

    // ============================================================
    // 里程 / 休息 / 結束（供效果或外部呼叫）
    // ============================================================

    /// <summary>變更里程（AdvMileageEffect 會呼叫）。里程不會低於 0。</summary>
    public void AddMileage(int delta)
    {
        if (delta == 0) return;
        CurrentMileage = Mathf.Max(0, CurrentMileage + delta);
        OnMileageChanged?.Invoke(CurrentMileage);
    }

    /// <summary>休息一次：減壓並扣一次休息次數。次數用完回傳 false。</summary>
    public bool Rest()
    {
        if (IsEnded || RestRemaining <= 0) return false;

        if (REST_STRESS_RELIEF != 0)
            _protagonist?.ReduceStress(REST_STRESS_RELIEF);

        RestRemaining--;
        OnRestChanged?.Invoke(RestRemaining);
        return true;
    }

    /// <summary>
    /// 玩家主動回家，結束這趟大冒險。
    /// 里程不會被保留，下次進同一個 Dungeon 會從 0 重新開始。
    /// </summary>
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
