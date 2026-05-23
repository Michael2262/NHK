using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 表情預設組合：將多個部位的名稱打包成一個預設，方便一次性套用。
/// 欄位留空代表「不改變該部位，維持現狀」。
/// </summary>
[System.Serializable]
public class ExpressionPreset
{
    [Tooltip("預設名稱，例如 Angry、Shy、Laugh")]
    public string presetName;
    public string eye;
    public string mouth;
    public string eyebrow;
    public string blush;
    public string other;
    public string above;
}

[CreateAssetMenu(menuName = "Tachie/Expression Config")]
public class TachieExpressionConfig : ScriptableObject
{
    public List<ExpressionPreset> presets = new List<ExpressionPreset>();

    /// <summary>
    /// 依名稱查找預設（不分大小寫）。找不到回傳 null。
    /// </summary>
    public ExpressionPreset GetPreset(string presetName)
    {
        foreach (var preset in presets)
        {
            if (string.Equals(preset.presetName, presetName, System.StringComparison.OrdinalIgnoreCase))
                return preset;
        }
        return null;
    }
}