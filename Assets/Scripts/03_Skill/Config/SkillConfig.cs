using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "SkillConfig", menuName = "Skills/Skill Config (Database)")]
public class SkillConfig : ScriptableObject
{
    // 1. 在 Inspector 中，將你所有的 SkillData.asset 檔案拖到這個列表
    [SerializeField]
    private List<SkillData> allSkills = new List<SkillData>();

    // 2. 為了在遊戲中快速查找，我們在啟動時建立一個字典
    private Dictionary<string, SkillData> _skillDictionary;

    /// <summary>
    /// 在遊戲啟動時（例如 GameStatusService 中）呼叫一次
    /// </summary>
    public void Initialize()
    {
        if (_skillDictionary != null) return; // 避免重複初始化

        // 將 List 轉換為 Dictionary，Key 是 SkillID，Value 是 SkillData 物件
        _skillDictionary = allSkills.ToDictionary(skill => skill.SkillID, skill => skill);
        Debug.Log($"[SkillConfig] 技能資料庫初始化完畢，載入了 {_skillDictionary.Count} 個技能。");
    }

    /// <summary>
    /// 根據 SkillID 獲取完整的 SkillData 物件
    /// </summary>
    public SkillData GetSkillData(string skillID)
    {
        if (_skillDictionary == null)
        {
            Debug.LogError("[SkillConfig] 尚未初始化！請先呼叫 Initialize()");
            return null;
        }

        _skillDictionary.TryGetValue(skillID, out SkillData skill);

        if (skill == null)
        {
            Debug.LogWarning($"[SkillConfig] 找不到 SkillID: {skillID}");
        }
        return skill; // 找不到會回傳 null
    }

    /// <summary>
    /// (可選) 獲取所有技能，供 UI 顯示列表
    /// </summary>
    public List<SkillData> GetAllSkills()
    {
        return allSkills;
    }
}