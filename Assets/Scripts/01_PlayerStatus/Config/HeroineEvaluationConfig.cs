using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 單一角色的評價設定。
/// 每個等級對應一個 TextTable Key，由 UI 查本地化表取得實際文字。
/// </summary>
[Serializable]
public class HeroineEvaluationEntry
{
    [Tooltip("此評價對應的等級門檻（≥ 此等級時顯示此評價）")]
    public int level;

    [Tooltip("對應 TextTable 的 Key（例如：sister_affinity_eval_0）")]
    public string textKey;
}

/// <summary>
/// 單一角色的完整評價設定（包含「對你的評價」和「H的評價」）。
/// </summary>
[Serializable]
public class HeroineEvaluationData
{
    [Tooltip("必須與 HeroineStat.ID 一致")]
    public string heroineID;

    [Header("對你的評價（依親密度等級）")]
    [Tooltip("由低到高排列。系統會取「等級 ≥ level」的最後一筆。")]
    public List<HeroineEvaluationEntry> affinityEvaluations = new List<HeroineEvaluationEntry>();

    [Header("H的評價（依開發度等級）")]
    [Tooltip("由低到高排列。系統會取「等級 ≥ level」的最後一筆。")]
    public List<HeroineEvaluationEntry> lewdnessEvaluations = new List<HeroineEvaluationEntry>();

    [Header("當前關係（依親密度等級）")]
    [Tooltip("由低到高排列。用於 Status 頁面顯示「當前關係」欄位。")]
    public List<HeroineEvaluationEntry> relationshipLabels = new List<HeroineEvaluationEntry>();
}

/// <summary>
/// 全域評價設定檔。存放所有角色的評價資料。
/// 在 Unity Inspector 中設定每個角色、每個等級對應的 TextTable Key。
/// 
/// 使用方式：
///   var key = evaluationConfig.GetAffinityEvaluation("sister", currentAffinityLevel);
///   string displayText = TextTable.Get(key); // 用你的本地化系統查表
/// </summary>
[CreateAssetMenu(menuName = "Game/Config/HeroineEvaluation")]
public class HeroineEvaluationConfig : ScriptableObject
{
    [Header("所有角色的評價設定")]
    public List<HeroineEvaluationData> allEvaluations = new List<HeroineEvaluationData>();

    // ═══════════════════════════════════════════
    //              查詢方法
    // ═══════════════════════════════════════════

    /// <summary>
    /// 取得指定角色的評價資料
    /// </summary>
    public HeroineEvaluationData GetData(string heroineID)
    {
        if (allEvaluations == null || string.IsNullOrEmpty(heroineID)) return null;
        return allEvaluations.Find(e => e.heroineID == heroineID);
    }

    /// <summary>
    /// 取得「對你的評價」的 TextTable Key（依親密度等級）
    /// </summary>
    public string GetAffinityEvaluation(string heroineID, int affinityLevel)
    {
        var data = GetData(heroineID);
        if (data == null) return null;
        return FindMatchingKey(data.affinityEvaluations, affinityLevel);
    }

    /// <summary>
    /// 取得「H的評價」的 TextTable Key（依開發度等級）
    /// </summary>
    public string GetLewdnessEvaluation(string heroineID, int lewdnessLevel)
    {
        var data = GetData(heroineID);
        if (data == null) return null;
        return FindMatchingKey(data.lewdnessEvaluations, lewdnessLevel);
    }

    /// <summary>
    /// 取得「當前關係」的 TextTable Key（依親密度等級）
    /// </summary>
    public string GetRelationshipLabel(string heroineID, int affinityLevel)
    {
        var data = GetData(heroineID);
        if (data == null) return null;
        return FindMatchingKey(data.relationshipLabels, affinityLevel);
    }

    // ═══════════════════════════════════════════
    //              內部工具
    // ═══════════════════════════════════════════

    /// <summary>
    /// 從排序好的列表中，找出「等級 ≥ entry.level」的最後一筆。
    /// 例如列表為 [0, 2, 4]，傳入 level=3，會回傳 level=2 那筆的 key。
    /// </summary>
    private string FindMatchingKey(List<HeroineEvaluationEntry> entries, int currentLevel)
    {
        if (entries == null || entries.Count == 0) return null;

        string result = null;
        for (int i = 0; i < entries.Count; i++)
        {
            if (currentLevel >= entries[i].level)
                result = entries[i].textKey;
            else
                break; // 列表由低到高，超過就不用繼續了
        }
        return result;
    }
}
