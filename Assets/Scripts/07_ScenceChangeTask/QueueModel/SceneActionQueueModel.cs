using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 場景動作佇列（純 C# Model，由 GameStatusService 持有）。
///
/// 【用途】
/// 跨轉場暫存「時間推進 / 女主角強制移動 / 風險強制移動 / Flag 變更」命令，
/// 由下一個場景的 SceneReadyCoordinator 透過 Task_ExecuteSceneActionQueue 統一執行
/// （時機：場景載入後、淡入前，畫面仍被壓幕蓋住）。
///
/// 【設計】
/// - 方案 A（簡單版）：佇列一律在「下一個場景 Ready」時全部執行，登記後請馬上切場景。
/// - 過期保險：命令若撐過一次完整轉場仍未被消費（該場景 Coordinator 沒掛 Task），
///   會在下下次轉場時被丟棄並印警告，避免殘留命令在之後某個場景誤發。
/// - Suspend/Resume：小遊戲串關期間由 MinigameManager 暫停消費與過期計時，
///   確保佇列命令等到小遊戲結束、時間推進後，才由返回場景執行（保留舊語意）。
/// - 執行順序固定為：時間推進 → 女主角移動 → 風險移動 → Flag 變更。
///   時間推進會觸發世界狀態重算，必須先做，否則強制移動會被覆蓋。
/// - 佇列為一次性轉場資料，不存檔；新遊戲 / 讀檔時由 GameStatusService 清空。
/// </summary>
public class SceneActionQueueModel
{
    public enum TimeAdvanceType { Slot, Phase, Day }

    private enum ActionType { TimeChange, HeroineMove, RiskMove, FlagChange }

    private class QueuedAction
    {
        public ActionType Type;
        public int EnqueueGeneration;

        // TimeChange
        public TimeAdvanceType TimeType;
        public int Slots;

        // HeroineMove / RiskMove（TargetID = heroineID / agentID，Detail = activity / inspectionTypeID）
        public string TargetID;
        public string LocationID;
        public string Detail;

        // FlagChange
        public string FlagID;
        public bool ShouldAdd;
        public FlagLifetime Lifetime;
    }

    private readonly List<QueuedAction> _queue = new List<QueuedAction>();
    private readonly TimeSystemModel _time;
    private readonly TimeConfig _timeConfig;
    private readonly ProgressFlagModel _progressFlags;

    // 轉場世代計數：每次場景切換 +1，用於過期保險
    private int _generation;
    private int _suspendCount;

    public bool IsSuspended => _suspendCount > 0;
    public bool HasAnyAction => _queue.Count > 0;

    public SceneActionQueueModel(TimeSystemModel time, TimeConfig timeConfig, ProgressFlagModel progressFlags)
    {
        _time = time;
        _timeConfig = timeConfig;
        _progressFlags = progressFlags;
    }

    // ==========================================================
    // 登記 API
    // ==========================================================

    public void EnqueueTimeChange(TimeAdvanceType type, int slots = 1)
    {
        _queue.Add(new QueuedAction
        {
            Type = ActionType.TimeChange,
            EnqueueGeneration = _generation,
            TimeType = type,
            Slots = slots
        });
        Debug.Log($"[SceneActionQueue] 佇列時間推進: {type}" + (type == TimeAdvanceType.Slot ? $" x{slots}" : ""));
    }

    public void EnqueueHeroineMove(string heroineID, string locationID, string activity)
    {
        if (string.IsNullOrEmpty(heroineID)) return;

        _queue.Add(new QueuedAction
        {
            Type = ActionType.HeroineMove,
            EnqueueGeneration = _generation,
            TargetID = heroineID,
            LocationID = locationID,
            Detail = activity
        });
        Debug.Log($"[SceneActionQueue] 佇列女主角移動: {heroineID} → {locationID} ({activity})");
    }

    public void EnqueueRiskMove(string agentID, string locationID, string inspectionTypeID)
    {
        if (string.IsNullOrEmpty(agentID)) return;

        _queue.Add(new QueuedAction
        {
            Type = ActionType.RiskMove,
            EnqueueGeneration = _generation,
            TargetID = agentID,
            LocationID = locationID,
            Detail = inspectionTypeID
        });
        Debug.Log($"[SceneActionQueue] 佇列風險移動: {agentID} → {locationID} ({inspectionTypeID})");
    }

    public void EnqueueFlagChange(string flagID, bool shouldAdd, FlagLifetime lifetime = FlagLifetime.Persistent)
    {
        if (string.IsNullOrEmpty(flagID)) return;

        _queue.Add(new QueuedAction
        {
            Type = ActionType.FlagChange,
            EnqueueGeneration = _generation,
            FlagID = flagID,
            ShouldAdd = shouldAdd,
            Lifetime = lifetime
        });
        Debug.Log($"[SceneActionQueue] 佇列 Flag 變更: {flagID} ({(shouldAdd ? "新增" : "移除")}, 生命週期: {lifetime})");
    }

    /// <summary>
    /// 清除所有待執行命令（不影響暫停狀態——小遊戲中途取消佇列時，串關暫停必須維持）。
    /// </summary>
    public void Clear()
    {
        if (_queue.Count > 0)
            Debug.Log($"[SceneActionQueue] 清除 {_queue.Count} 筆待執行命令。");
        _queue.Clear();
    }

    /// <summary>
    /// 整組重置：清除所有命令 + 重置暫停狀態。
    /// 僅供新遊戲 / 讀檔時由 GameStatusService 呼叫。
    /// </summary>
    public void ResetForNewSession()
    {
        Clear();
        _suspendCount = 0;
    }

    // ==========================================================
    // 暫停 / 恢復（小遊戲串關用）
    // ==========================================================

    /// <summary>暫停消費：Task 會跳過執行，過期保險也不計時。</summary>
    public void Suspend()
    {
        _suspendCount++;
        Debug.Log($"[SceneActionQueue] 佇列消費已暫停 (層數: {_suspendCount})");
    }

    /// <summary>恢復消費：下一次場景 Ready 時佇列會被執行。</summary>
    public void Resume()
    {
        _suspendCount = Math.Max(0, _suspendCount - 1);
        Debug.Log($"[SceneActionQueue] 佇列消費已恢復 (層數: {_suspendCount})");
    }

    // ==========================================================
    // 生命週期（由 GameStatusService / Task 呼叫）
    // ==========================================================

    /// <summary>
    /// 過期保險：場景切換完成時由 GameStatusService.HandleSceneChanged 呼叫。
    /// 命令撐過一次完整轉場仍未被消費 = 該場景的 Coordinator 沒掛 Task，丟棄並警告。
    /// </summary>
    public void HandleSceneChanged()
    {
        if (IsSuspended) return;

        _generation++;
        int removed = _queue.RemoveAll(a => _generation - a.EnqueueGeneration >= 2);
        if (removed > 0)
        {
            Debug.LogWarning($"[SceneActionQueue] 過期保險：丟棄 {removed} 筆未被消費的命令！" +
                             "請確認上一個場景的 SceneReadyCoordinator 有掛 Task_ExecuteSceneActionQueue。");
        }
    }

    /// <summary>
    /// 執行所有待執行命令。由 Task_ExecuteSceneActionQueue 在場景 Ready 階段呼叫。
    /// </summary>
    public void ExecuteAll()
    {
        if (IsSuspended)
        {
            Debug.Log("[SceneActionQueue] 佇列處於暫停狀態（小遊戲進行中），本次跳過執行。");
            return;
        }

        if (_queue.Count == 0) return;

        Debug.Log($"[SceneActionQueue] 開始執行 {_queue.Count} 筆命令...");

        // 固定類別順序執行（各類別內維持登記順序）
        ExecuteCategory(ActionType.TimeChange);
        ExecuteCategory(ActionType.HeroineMove);
        ExecuteCategory(ActionType.RiskMove);
        ExecuteCategory(ActionType.FlagChange);

        _queue.Clear();
        Debug.Log("[SceneActionQueue] 全部命令執行完畢。");
    }

    // ==========================================================
    // 內部執行邏輯
    // ==========================================================

    private void ExecuteCategory(ActionType type)
    {
        foreach (var action in _queue)
        {
            if (action.Type != type) continue;

            switch (type)
            {
                case ActionType.TimeChange: ExecuteTimeChange(action); break;
                case ActionType.HeroineMove: ExecuteHeroineMove(action); break;
                case ActionType.RiskMove: ExecuteRiskMove(action); break;
                case ActionType.FlagChange: ExecuteFlagChange(action); break;
            }
        }
    }

    private void ExecuteTimeChange(QueuedAction action)
    {
        switch (action.TimeType)
        {
            case TimeAdvanceType.Slot:
                if (!_time.CanAdvanceWithinDay(action.Slots))
                {
                    Debug.LogWarning($"[SceneActionQueue] 剩餘時段不足，無法推進 {action.Slots} 格，已跳過。");
                    return;
                }
                _time.AdvanceTime(action.Slots);
                Debug.Log($"[SceneActionQueue] 已推進 {action.Slots} 格時段。");
                break;

            case TimeAdvanceType.Phase:
                int currentPhase = _time.CurrentPhaseIndex;
                if (currentPhase >= _timeConfig.PhaseNames.Count - 1)
                {
                    Debug.LogWarning("[SceneActionQueue] 已在最後階段，無法推進 Phase（不允許自動跨日），已跳過。");
                    return;
                }
                int slotsNeeded = (_timeConfig.GetTotalSlots(currentPhase) - _time.CurrentSlotInPhase) + 1;
                if (!_time.CanAdvanceWithinDay(slotsNeeded))
                {
                    Debug.LogWarning("[SceneActionQueue] 時段不足以推進到下一階段，已跳過。");
                    return;
                }
                _time.AdvanceTime(slotsNeeded);
                Debug.Log($"[SceneActionQueue] 已推進 {slotsNeeded} 格，進入 Phase {currentPhase + 1}。");
                break;

            case TimeAdvanceType.Day:
                // 直接跳日，不走 SkipToNextDay()：
                // 執行時機在轉場管線內部，再觸發隔天演出會跟 SceneController 的轉場疊在一起
                int remaining = _time.GetRemainingSlotsInDay();
                _time.AdvanceTime(remaining + 1);
                Debug.Log("[SceneActionQueue] 已直接跳到隔天。");
                break;
        }
    }

    private void ExecuteHeroineMove(QueuedAction action)
    {
        if (ScenarioManager.Instance == null)
        {
            Debug.LogWarning($"[SceneActionQueue] 找不到 ScenarioManager，女主角移動 {action.TargetID} 已跳過！");
            return;
        }
        Debug.Log($"[SceneActionQueue] 執行女主角強制移動: {action.TargetID} → {action.LocationID} ({action.Detail})");
        ScenarioManager.Instance.ForceMoveHeroine(action.TargetID, action.LocationID, action.Detail);
    }

    private void ExecuteRiskMove(QueuedAction action)
    {
        if (ScenarioManager.Instance == null)
        {
            Debug.LogWarning($"[SceneActionQueue] 找不到 ScenarioManager，風險移動 {action.TargetID} 已跳過！");
            return;
        }
        Debug.Log($"[SceneActionQueue] 執行風險強制移動: {action.TargetID} → {action.LocationID} ({action.Detail})");
        ScenarioManager.Instance.ForceMoveRisk(action.TargetID, action.LocationID, action.Detail);
    }

    private void ExecuteFlagChange(QueuedAction action)
    {
        if (_progressFlags == null) return;

        if (action.ShouldAdd)
        {
            Debug.Log($"[SceneActionQueue] 新增 Flag: {action.FlagID} (生命週期: {action.Lifetime})");
            _progressFlags.AddFlag(action.FlagID, action.Lifetime);
        }
        else
        {
            Debug.Log($"[SceneActionQueue] 移除 Flag: {action.FlagID}");
            _progressFlags.RemoveFlag(action.FlagID);
        }
    }
}
