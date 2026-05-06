using UnityEngine;
using System.Collections.Generic;

// (你的 SkillEffectType Enum - 保持不變)
public enum SkillEffectType
{
    None,
    PermanentAttackIncrease,
    PermanentDefenseIncrease,
    UnlockNewAction,
    // ... 其他效果
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "Skills/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("基本資訊")]
    [Tooltip("用於儲存/載入和系統內部查找的唯一ID")]
    public string SkillID;
    public string DisplayName = "新技能";
    [TextArea(3, 5)]
    public string Description;
    public Sprite Icon;

    [Header("解鎖條件")]
    public int Cost = 1;
    public int RequiredLevel = 1;
    [Tooltip("學習此技能前，必須先解鎖的前置技能 (請拖曳 SkillData.asset 檔案到這裡)")]
    public List<SkillData> Prerequisites = new List<SkillData>();

    [Header("技能效果")]
    public SkillEffectType EffectType;

    [Tooltip("當效果類型是 'Permanent...' 時，這是增加的數值")]
    public int EffectValue;

    
    [Tooltip("【新】當效果類型是 'UnlockNewAction' 時，請拖曳對應的 Flag Definition (SO) 到此處")]
    public ProgressFlagDefinition EffectFlag;
    
}