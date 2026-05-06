using System;
using System.Collections.Generic;

/// <summary>
/// 女主角統計資料的執行時模型。
/// 負責累計、讀取各項統計數值，並在數值變動時發出事件通知 UI。
///
/// 設計重點：
/// - 使用 Dictionary 而非硬編碼欄位，新增統計項目只需擴充 Enum
/// - 所有數值僅能增加（或 SetMax），不提供減少方法
/// - float 統一型別，int 類的統計直接存為整數值的 float
/// </summary>
public class HeroineStatisticsModel
{
    // ── 核心資料 ──
    private readonly Dictionary<HeroineStatisticType, float> _stats
        = new Dictionary<HeroineStatisticType, float>();

    /// <summary>
    /// 當任意統計項目變動時觸發。
    /// 參數: (變動的項目類型, 變動後的新值)
    /// </summary>
    public event Action<HeroineStatisticType, float> OnStatChanged;

    // ═══════════════════════════════════════════
    //                  讀取
    // ═══════════════════════════════════════════

    /// <summary>取得指定統計的當前值（預設 0）</summary>
    public float Get(HeroineStatisticType type)
    {
        return _stats.TryGetValue(type, out float val) ? val : 0f;
    }

    /// <summary>取得指定統計的整數值（方便次數類統計使用）</summary>
    public int GetInt(HeroineStatisticType type)
    {
        return (int)Get(type);
    }

    /// <summary>取得所有統計的唯讀快照（供 UI 一次性讀取）</summary>
    public IReadOnlyDictionary<HeroineStatisticType, float> GetAll()
    {
        return _stats;
    }

    // ═══════════════════════════════════════════
    //                  累加
    // ═══════════════════════════════════════════

    /// <summary>
    /// 增加指定統計的數值（不允許傳入負數）。
    /// 這是最核心的寫入方法。
    /// </summary>
    public void Add(HeroineStatisticType type, float amount)
    {
        if (amount < 0f)
        {
            UnityEngine.Debug.LogWarning(
                $"[HeroineStatisticsModel] 嘗試對 {type} 傳入負數 ({amount})，已忽略。統計值只能增加。");
            return;
        }
        if (amount == 0f) return;

        if (!_stats.ContainsKey(type))
            _stats[type] = 0f;

        _stats[type] += amount;
        OnStatChanged?.Invoke(type, _stats[type]);
    }

    /// <summary>增加整數值的便捷方法</summary>
    public void Add(HeroineStatisticType type, int amount)
    {
        Add(type, (float)amount);
    }

    /// <summary>遞增 1（適用於次數類統計）</summary>
    public void Increment(HeroineStatisticType type)
    {
        Add(type, 1f);
    }

    // ═══════════════════════════════════════════
    //          取最大值 (用於紀錄類統計)
    // ═══════════════════════════════════════════

    /// <summary>
    /// 若傳入的值大於目前紀錄，則覆蓋。
    /// 適用於「單次最多高潮次數」「連續射精次數」等紀錄型統計。
    /// 
    /// 使用方式：小遊戲結束時呼叫
    ///   stats.SetMax(HeroineStatisticType.MaxOrgasmInOneSession, thisSessionOrgasmCount);
    /// </summary>
    public void SetMax(HeroineStatisticType type, float value)
    {
        float current = Get(type);
        if (value > current)
        {
            _stats[type] = value;
            OnStatChanged?.Invoke(type, value);
        }
    }

    /// <summary>SetMax 的 int 版本</summary>
    public void SetMax(HeroineStatisticType type, int value)
    {
        SetMax(type, (float)value);
    }

    // ═══════════════════════════════════════════
    //          推算值 (Computed)
    // ═══════════════════════════════════════════

    /// <summary>
    /// 取得最常H的地點名稱。
    /// 比較所有地點H次數，回傳最高的那個。
    /// 回傳 null 表示尚無任何地點紀錄。
    /// </summary>
    public HeroineStatisticType? GetMostFrequentSexLocation()
    {
        // 定義所有要比較的地點統計項目
        var locationTypes = new HeroineStatisticType[]
        {
            HeroineStatisticType.SexInBedroom,
            HeroineStatisticType.SexInLivingRoom,
            HeroineStatisticType.SexInBathroom,
            HeroineStatisticType.SexInToilet,
        };

        HeroineStatisticType? best = null;
        float bestCount = 0f;

        foreach (var loc in locationTypes)
        {
            float count = Get(loc);
            if (count > bestCount)
            {
                bestCount = count;
                best = loc;
            }
        }

        return best;
    }

    // ═══════════════════════════════════════════
    //              存檔 / 讀檔
    // ═══════════════════════════════════════════

    /// <summary>將當前統計資料轉為可序列化的存檔結構</summary>
    public HeroineStatisticsSaveData ToSaveData()
    {
        return HeroineStatisticsSaveData.FromDictionary(_stats);
    }

    /// <summary>從存檔資料還原統計</summary>
    public void LoadFromSaveData(HeroineStatisticsSaveData data)
    {
        _stats.Clear();
        if (data == null) return;

        var loaded = data.ToDictionary();
        foreach (var kvp in loaded)
        {
            _stats[kvp.Key] = kvp.Value;
        }

        // 通知所有訂閱者資料已載入（觸發 UI 刷新）
        foreach (var kvp in _stats)
        {
            OnStatChanged?.Invoke(kvp.Key, kvp.Value);
        }
    }
}