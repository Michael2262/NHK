using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 職責：保存「整個地圖」在當前時間點的動態狀態。
/// 這是一個「全域快照」，時間改變時會被刷新。
/// </summary>
public class CurrentScenarioModel
{
    // --- 動態狀態 ---
    public bool IsInScenario { get; private set; }
    public string LocationID { get; private set; } // 玩家當前在哪

    // 儲存所有地點的狀態 (Key: LocationID, Value: 該地點的狀態物件)
    public Dictionary<string, LocationState> AllLocationStates { get; private set; }

    // ==========================================================
    // 【NEW】今日行為鍊指派
    // Key: heroineID, Value: 今天抽中的鍊名稱 (chainName)
    // 每天 OnDayPassed 時由 ScenarioManager 重抽並寫入此字典
    // ==========================================================
    public Dictionary<string, string> TodaysChainByHeroine { get; private set; }

    // 事件定義
    // 參數: (HeroineID, OldLocationID, NewLocationID, NewActivity)
    public event Action<string, string, string, string> OnHeroineMoved;

    public event Action<string, string, string, string> OnRiskMoved; // 參數: agentID, oldLoc, newLoc, actionID

    public event Action OnScenarioRecalculated; // 當整個場景狀態被重新計算完成後觸發

    public CurrentScenarioModel()
    {
        AllLocationStates = new Dictionary<string, LocationState>();
        TodaysChainByHeroine = new Dictionary<string, string>();
        NewGame();
    }

    // ==========================================================
    // 操作方法 (由 ScenarioManager 呼叫)
    // ==========================================================

    /// <summary>
    /// 清空舊的狀態，準備重新計算 (換時間時呼叫)
    /// </summary>
    public void ClearAllStates()
    {
        AllLocationStates.Clear();
    }

    /// <summary>
    /// 【NEW】清空今日鍊指派 (新的一天開始時由 ScenarioManager 呼叫)
    /// </summary>
    public void ClearTodaysChains()
    {
        TodaysChainByHeroine.Clear();
    }

    /// <summary>
    /// 【NEW】為某位女主角指派今日的鍊
    /// </summary>
    public void AssignTodaysChain(string heroineID, string chainName)
    {
        if (string.IsNullOrEmpty(heroineID)) return;
        TodaysChainByHeroine[heroineID] = chainName ?? string.Empty;
    }

    /// <summary>
    /// 【NEW】取得某位女主角今日被抽中的鍊名。回傳 null 表示今天沒有鍊 (走 fallback)。
    /// </summary>
    public string GetTodaysChain(string heroineID)
    {
        if (string.IsNullOrEmpty(heroineID)) return null;
        if (TodaysChainByHeroine.TryGetValue(heroineID, out var name) && !string.IsNullOrEmpty(name))
            return name;
        return null;
    }

    /// <summary>
    /// 將一位女主角「放置」到指定地點
    /// </summary>
    public void AddHeroineToLocation(string locationID, string heroineID, string activityState)
    {
        var state = GetOrCreateLocationState(locationID);
        state.Heroines.Add(new HeroineStateData { HeroineID = heroineID, Activity = activityState });
    }

    /// <summary>
    /// 將一個風險(家人)「放置」到指定地點
    /// </summary>
    public void AddRiskToLocation(string locationID, RiskAction riskAction)
    {
        var state = GetOrCreateLocationState(locationID);
        state.Risks.Add(riskAction);
    }

    /// <summary>
    /// 玩家進入某個地點 (只改變指針，不進行運算)
    /// </summary>
    public void SetPlayerLocation(string locationID)
    {
        IsInScenario = true;
        LocationID = locationID;
    }

    public void ExitScenario()
    {
        IsInScenario = false;
        LocationID = string.Empty;
    }

    public void NotifyScenarioRecalculated()
    {
        OnScenarioRecalculated?.Invoke();
    }

    // --- 輔助：取得或建立狀態 ---
    public LocationState GetState(string locID)
    {
        if (AllLocationStates.TryGetValue(locID, out var state))
            return state;
        return null; // 如果該地點沒人，可能回傳 null
    }

    private LocationState GetOrCreateLocationState(string locID)
    {
        if (!AllLocationStates.ContainsKey(locID))
        {
            AllLocationStates[locID] = new LocationState();
        }
        return AllLocationStates[locID];
    }

    /// <summary>
    /// 強制移動女主角(已修正：忽略 ID 大小寫 + 詳細 Log)
    /// </summary>
    public void ForceSetHeroineLocation(string heroineID, string targetLocID, string newActivity)
    {
        string oldLocID = string.Empty;
        string oldActivity = string.Empty;

        // 防呆：確保輸入的 ID 不為空
        if (string.IsNullOrEmpty(heroineID)) return;

        // 1. 先從「全地圖」中找到她現在在哪
        foreach (var kvp in AllLocationStates)
        {
            var roomState = kvp.Value;

            // 使用 StringComparison.OrdinalIgnoreCase 忽略大小寫尋找
            var existingData = roomState.Heroines.Find(h => h.HeroineID.Equals(heroineID, StringComparison.OrdinalIgnoreCase));

            if (existingData != null)
            {
                oldLocID = kvp.Key;
                oldActivity = existingData.Activity;

                // 為了保持資料一致性，我們移除時用「找到的那個物件」
                roomState.Heroines.Remove(existingData);

                // 如果這次移動只是「原地換動作」，我們也要先把舊的移除，稍後再加新的進去
                break;
            }
        }

        // Log 診斷：這行會告訴你系統到底有沒有找到她
        if (string.IsNullOrEmpty(oldLocID))
        {
            UnityEngine.Debug.LogWarning($"[Model] 注意：在地圖上找不到 ID 為 '{heroineID}' 的角色。系統將視為「新角色」直接生成到目標地點。");
        }
        else
        {
            UnityEngine.Debug.Log($"[Model] 找到 '{heroineID}' 原本在 '{oldLocID}' ({oldActivity})，已移除。");
        }

        // 2. 加到新的地點
        if (!string.IsNullOrEmpty(targetLocID))
        {
            var targetState = GetOrCreateLocationState(targetLocID);

            // 這裡我們存入的 ID 建議使用 Config 裡原本的標準寫法
            // 但因為這裡是動態指令，我們就存入你輸入的 heroineID
            targetState.Heroines.Add(new HeroineStateData
            {
                HeroineID = heroineID,
                Activity = newActivity
            });
        }

        // 3. 發送事件通知 HubController
        // HubController 會根據 oldLocID 判斷是否要執行「消失/刷新」邏輯
        OnHeroineMoved?.Invoke(heroineID, oldLocID, targetLocID, newActivity);
    }

    /// <summary>
    /// 強制移動風險角色 (家人)
    /// </summary>
    /// <param name="agentID">家人 ID</param>
    /// <param name="targetLocID">目標地點 ID (若為空則移除)</param>
    /// <param name="riskAction">對應的 RiskAction 資料</param>
    public void ForceSetRiskLocation(string agentID, string targetLocID, RiskAction riskAction)
    {
        string oldLocID = string.Empty;

        // 1. 尋找並移除
        foreach (var kvp in AllLocationStates)
        {
            // 建議加上 IndexOf 並忽略大小寫，確保搜尋更準確
            var existingRisk = kvp.Value.Risks.Find(r =>
                r.inspectionTypeID.IndexOf(agentID, StringComparison.OrdinalIgnoreCase) >= 0);

            if (existingRisk != null)
            {
                oldLocID = kvp.Key;
                kvp.Value.Risks.Remove(existingRisk);
                break;
            }
        }

        // 2. 添加到新地點
        if (!string.IsNullOrEmpty(targetLocID) && riskAction != null)
        {
            var targetState = GetOrCreateLocationState(targetLocID);
            targetState.Risks.Add(riskAction);
        }

        // 3. 通知 Hub (即使 oldLocID 是空的也發送，以便偵測新角色進入)
        OnRiskMoved?.Invoke(agentID, oldLocID, targetLocID, riskAction?.inspectionTypeID);
    }
    // ==========================================================
    // 存檔/讀檔 (簡化版，概念)
    // ==========================================================

    public void NewGame()
    {
        IsInScenario = false;
        LocationID = string.Empty;
        ClearAllStates();
        ClearTodaysChains();
    }

    public CurrentScenarioSaveData ToSaveData()
    {
        // 需注意：Dictionary 不容易直接序列化，通常需要轉成 List<Entry>
        // 這裡假設你的 SaveSystem 能處理，或者你在這裡做轉換
        return new CurrentScenarioSaveData
        {
            IsInScenario = this.IsInScenario,
            LocationID = this.LocationID,
            AllLocationStates = this.AllLocationStates,
            // 【NEW】保存今日鍊指派
            TodaysChainByHeroine = new Dictionary<string, string>(this.TodaysChainByHeroine)
        };
    }

    public void LoadFromSaveData(CurrentScenarioSaveData data)
    {
        if (data == null) { NewGame(); return; }
        IsInScenario = data.IsInScenario;
        LocationID = data.LocationID;
        AllLocationStates = data.AllLocationStates ?? new Dictionary<string, LocationState>();
        // 【NEW】還原今日鍊指派
        TodaysChainByHeroine = data.TodaysChainByHeroine ?? new Dictionary<string, string>();
    }
}