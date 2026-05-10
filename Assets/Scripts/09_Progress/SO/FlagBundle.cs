using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可重複使用的旗標組合包。在 Inspector 中拖入多個 ProgressFlagDefinition，
/// 即可在各種 FSM Action 中一次引用整組旗標。
/// </summary>
[CreateAssetMenu(menuName = "Game/Progress/Flag Bundle", fileName = "FlagBundle_New")]
public class FlagBundle : ScriptableObject
{
    [Tooltip("此組合包內的所有旗標定義")]
    [SerializeField] private List<ProgressFlagDefinition> flags = new List<ProgressFlagDefinition>();

    /// <summary>
    /// 取得所有旗標的 FlagID 清單（自動過濾 null 項目）。
    /// </summary>
    public List<string> GetFlagIDs()
    {
        var ids = new List<string>(flags.Count);
        foreach (var f in flags)
        {
            if (f != null)
                ids.Add(f.FlagID);
        }
        return ids;
    }

    /// <summary>
    /// 取得原始定義清單（唯讀用途）。
    /// </summary>
    public IReadOnlyList<ProgressFlagDefinition> Flags => flags;
}
