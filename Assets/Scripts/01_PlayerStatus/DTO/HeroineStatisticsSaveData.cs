using System;
using System.Collections.Generic;

/// <summary>
/// 女主角統計資料的存檔結構。
/// 使用兩個平行 List 來序列化 Dictionary（因為 C# 的 Dictionary 不能直接被 JsonUtility 序列化）。
/// </summary>
[Serializable]
public class HeroineStatisticsSaveData
{
    public List<HeroineStatisticType> Keys = new List<HeroineStatisticType>();
    public List<float> Values = new List<float>();

    /// <summary>
    /// 從 Dictionary 轉換為可序列化的平行 List
    /// </summary>
    public static HeroineStatisticsSaveData FromDictionary(Dictionary<HeroineStatisticType, float> dict)
    {
        var data = new HeroineStatisticsSaveData();
        foreach (var kvp in dict)
        {
            data.Keys.Add(kvp.Key);
            data.Values.Add(kvp.Value);
        }
        return data;
    }

    /// <summary>
    /// 從平行 List 還原為 Dictionary
    /// </summary>
    public Dictionary<HeroineStatisticType, float> ToDictionary()
    {
        var dict = new Dictionary<HeroineStatisticType, float>();
        for (int i = 0; i < Keys.Count && i < Values.Count; i++)
        {
            dict[Keys[i]] = Values[i];
        }
        return dict;
    }
}