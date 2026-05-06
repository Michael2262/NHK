using System;
using System.Collections.Generic;

// 職責：ProtagonistSkillModel 的存檔數據容器
[Serializable]
public class SkillSaveData
{
    // 儲存所有已解鎖技能 ID 的列表 (HashSet 在序列化時會轉為 List)
    public List<string> UnlockedSkillIDs = new List<string>();
}